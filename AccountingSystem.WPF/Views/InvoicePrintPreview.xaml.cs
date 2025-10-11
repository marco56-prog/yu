using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Input;
using AccountingSystem.Models;
using AccountingSystem.Data;
using AccountingSystem.Business;
using AccountingSystem.WPF.ViewModels;
using AccountingSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Windows.Xps.Packaging;
using System.Collections.Generic;

namespace AccountingSystem.WPF.Views
{
    public partial class InvoicePrintPreview : Window
    {
        private readonly SalesInvoice _invoiceArg;
        private SalesInvoice? _fullInvoice;
        private readonly AccountingDbContext _context;
        private decimal _previousBalance;
        private const string UnspecifiedText = "غير محدد";

        // متغيرات للتنقل
        private List<int> _allInvoiceIds = new();
        private int _currentIndex = -1;

        // الثقافة والفورماتر
        private readonly CultureInfo _arEg = new("ar-EG") { NumberFormat = { CurrencySymbol = "ج.م" } };
        private static string Money(decimal v, CultureInfo c) => v.ToString("C", c);

        public InvoicePrintPreview(SalesInvoice invoice, AccountingDbContext context)
        {
            InitializeComponent();

            _invoiceArg = invoice ?? throw new ArgumentNullException(nameof(invoice));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // تحميل البيانات عند اكتمال التحميل
            Loaded += async (_, __) => await LoadInvoiceDataAsync();
        }

        // ====== تحميل بيانات الفاتورة بالكامل ======
        private async Task LoadInvoiceDataAsync()
        {
            try
            {
                if (_invoiceArg == null)
                {
                    ShowError("الفاتورة غير صالحة!");
                    Close();
                    return;
                }

                // Load full invoice with related data
                _fullInvoice = await _context.SalesInvoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items).ThenInclude(l => l.Product)
                    .Include(i => i.Items).ThenInclude(l => l.Unit)
                    .FirstOrDefaultAsync(i => i.SalesInvoiceId == _invoiceArg.SalesInvoiceId);

                if (_fullInvoice == null)
                {
                    ShowError("لم يتم العثور على الفاتورة!");
                    Close();
                    return;
                }

                // حساب الرصيد السابق
                _previousBalance = await CalculatePreviousBalanceAsync(_fullInvoice.Customer?.CustomerId ?? 0, _fullInvoice.InvoiceDate);

                Title = $"معاينة طباعة الفاتورة - {_fullInvoice.InvoiceNumber}";
                RefreshVisualPreview();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل بيانات الفاتورة: {ex.Message}");
                Close();
            }
        }

        // ====== تعبئة عناصر المعاينة من الفاتورة الفعلية ======
        private void RefreshVisualPreview()
        {
            if (_fullInvoice == null) return;

            var vm = ConvertToPrintVM(_fullInvoice, _previousBalance);

            // رأس الفاتورة
            lblInvoiceNumber.SetTextSafe(vm.InvoiceNumber);
            lblInvoiceDate.SetTextSafe(vm.InvoiceDate.ToString("yyyy/MM/dd", _arEg));
            lblInvoiceTime.SetTextSafe(vm.InvoiceDate.ToString("hh:mm tt", _arEg));

            // العميل
            lblCustomerName.SetTextSafe(vm.CustomerName);
            lblCustomerPhone.SetTextSafe(vm.CustomerPhone);
            lblCustomerAddress.SetTextSafe(vm.CustomerAddress);

            // الرصيد السابق + هذه الفاتورة + الإجمالي الكلي
            lblPreviousBalance.SetTextSafe(Money(vm.PreviousBalance, _arEg));
            lblCurrentInvoiceTotal.SetTextSafe(Money(vm.NetTotal, _arEg));
            lblGrandTotal.SetTextSafe(Money(vm.GrandTotal, _arEg));

            // الإجماليات
            lblSubTotal.SetTextSafe(Money(vm.SubTotal, _arEg));
            lblTotalDiscount.SetTextSafe(Money(vm.DiscountAmount, _arEg));
            lblTax.SetTextSafe(Money(vm.TaxAmount, _arEg));
            lblNetTotal.SetTextSafe(Money(vm.NetTotal, _arEg));
            lblPaidAmount.SetTextSafe(Money(vm.PaidAmount, _arEg));
            lblRemainingAmount.SetTextSafe(Money(vm.RemainingAmount, _arEg));

            // ملاحظات
            lblNotes.SetTextSafe(_fullInvoice.Notes ?? "");

            // تفاصيل الأصناف (بنظهر عمود "الإجمالي" كـ Quantity * UnitPrice)
            var lines = vm.Lines.Select((l, i) => new InvoiceItemViewModel
            {
                ItemNumber = (i + 1).ToString(),
                ProductName = l.ProductName,
                UnitName = l.UnitName,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                TotalPrice = l.Quantity * l.UnitPrice,
                DiscountAmount = l.DiscountAmount,
                NetAmount = l.NetAmount
            }).ToList();

            InvoiceItemsControl.ItemsSource = lines;
        }

        // الرصيد السابق = مجموع المبالغ المتبقية للفواتير المرحّلة قبل تاريخ هذه الفاتورة
        private async Task<decimal> CalculatePreviousBalanceAsync(int customerId, DateTime invoiceDate)
        {
            try
            {
                return await _context.SalesInvoices
                    .Where(i => i.CustomerId == customerId &&
                                i.InvoiceDate < invoiceDate &&
                                i.IsPosted)
                    .SumAsync(i => i.RemainingAmount);
            }
            catch
            {
                return 0m;
            }
        }

        // ====== طباعة باستخدام الخدمة الجديدة المحسنة ======
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fullInvoice == null)
                {
                    ShowWarning("الفاتورة غير جاهزة للطباعة.");
                    return;
                }

                var printDlg = new PrintDialog();
                if (printDlg.ShowDialog() == true)
                {
                    var printVM = ConvertToPrintVM(_fullInvoice, _previousBalance);
                    var doc = DocumentBuilder.BuildInvoiceDocument(printVM);

                    doc.PageHeight = printDlg.PrintableAreaHeight;
                    doc.PageWidth = printDlg.PrintableAreaWidth;

                    printDlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"فاتورة بيع - {_fullInvoice.InvoiceNumber}");
                    ShowInfo("تم إرسال الفاتورة للطابعة بنجاح!");
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في الطباعة: {ex.Message}");
            }
        }

        // ====== معاينة طباعة باستخدام الخدمة الجديدة المحسنة ======
        private void btnPrintPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fullInvoice == null)
                {
                    MessageBox.Show("الفاتورة غير جاهزة للمعاينة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // تحويل إلى PrintVM وبناء المستند باستخدام DocumentBuilder
                var printVM = ConvertToPrintVM(_fullInvoice, _previousBalance);
                FlowDocument doc = DocumentBuilder.BuildInvoiceDocument(printVM);

                // إعدادات A4 مضبوطة (96 DPI)
                const double A4W = 96.0 * 8.27;  // 793.92
                const double A4H = 96.0 * 11.69; // 1121.24
                doc.PageWidth = A4W;
                doc.PageHeight = A4H;
                doc.PagePadding = new Thickness(40);
                doc.ColumnWidth = double.PositiveInfinity;

                var viewer = new DocumentViewer { Document = doc };

                // شريط أدوات بسيط للتنقل والطباعة
                var toolbar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
                var btnPrevDoc = new Button { Content = "⬅ السابق", Margin = new Thickness(4), Padding = new Thickness(10, 6, 10, 6) };
                var btnNextDoc = new Button { Content = "التالي ➡", Margin = new Thickness(4), Padding = new Thickness(10, 6, 10, 6) };
                var btnZoomIn = new Button { Content = "تكبير +", Margin = new Thickness(4), Padding = new Thickness(10, 6, 10, 6) };
                var btnZoomOut = new Button { Content = "تصغير -", Margin = new Thickness(4), Padding = new Thickness(10, 6, 10, 6) };
                var btnPrintCmd = new Button { Content = "🖨️ طباعة", Margin = new Thickness(12, 4, 4, 4), Padding = new Thickness(12, 6, 12, 6) };

                btnPrevDoc.Click += (_, __) => NavigationCommands.PreviousPage.Execute(null, viewer);
                btnNextDoc.Click += (_, __) => NavigationCommands.NextPage.Execute(null, viewer);
                btnZoomIn.Click += (_, __) => viewer.IncreaseZoom();
                btnZoomOut.Click += (_, __) => viewer.DecreaseZoom();
                btnPrintCmd.Click += (_, __) => ApplicationCommands.Print.Execute(null, viewer);

                toolbar.Children.Add(btnPrevDoc);
                toolbar.Children.Add(btnNextDoc);
                toolbar.Children.Add(btnZoomOut);
                toolbar.Children.Add(btnZoomIn);
                toolbar.Children.Add(btnPrintCmd);

                var layout = new DockPanel();
                DockPanel.SetDock(toolbar, Dock.Top);
                layout.Children.Add(toolbar);
                layout.Children.Add(viewer);

                var previewWindow = new Window
                {
                    Title = $"معاينة الطباعة - فاتورة {_fullInvoice.InvoiceNumber}",
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    WindowState = WindowState.Maximized,
                    Content = layout,
                    FlowDirection = FlowDirection.RightToLeft
                };

                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في معاينة الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        // ====== حفظ كـ PDF باستخدام Print to PDF ======
        private void btnSaveAsPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fullInvoice == null)
                {
                    ShowWarning("الفاتورة غير جاهزة للحفظ.");
                    return;
                }

                var printVM = ConvertToPrintVM(_fullInvoice, _previousBalance);
                var doc = DocumentBuilder.BuildInvoiceDocument(printVM);

                var pd = new PrintDialog();
                // جرّب تعيين طابعة PDF لو متوفرة (اختياري):
                // pd.PrintQueue = new System.Printing.PrintQueue(new System.Printing.PrintServer(), "Microsoft Print to PDF");

                if (pd.ShowDialog() == true)
                {
                    pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Invoice_{_fullInvoice.InvoiceNumber}");
                    ShowInfo("تم إنشاء ملف PDF عبر الطابعة الافتراضية.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في إنشاء PDF: {ex.Message}");
            }
        }

        // ====== حفظ كـ XPS (احتياطي) ======
        private void btnSaveAsXps_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fullInvoice == null)
                {
                    ShowWarning("الفاتورة غير جاهزة للحفظ.");
                    return;
                }

                // تحويل إلى PrintVM وبناء المستند باستخدام DocumentBuilder
                var printVM = ConvertToPrintVM(_fullInvoice, _previousBalance);
                FlowDocument doc = DocumentBuilder.BuildInvoiceDocument(printVM);
                var sfd = new SaveFileDialog
                {
                    Title = "حفظ الفاتورة",
                    Filter = "XPS Document (*.xps)|*.xps",
                    FileName = $"Invoice_{_fullInvoice.InvoiceNumber}.xps"
                };

                if (sfd.ShowDialog() == true)
                {
                    using var xps = new XpsDocument(sfd.FileName, FileAccess.Write);
                    var writer = XpsDocument.CreateXpsDocumentWriter(xps);
                    writer.Write(((IDocumentPaginatorSource)doc).DocumentPaginator);
                    xps.Close();
                    ShowInfo("تم حفظ الملف بصيغة XPS بنجاح.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في حفظ الملف: {ex.Message}");
            }
        }

        // ====== وظائف التنقل بين الفواتير ======
        private async void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToInvoice(-1);
        }

        private async void btnNext_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToInvoice(1);
        }

        private async Task NavigateToInvoice(int direction)
        {
            try
            {
                // تحميل قائمة الفواتير إذا لم تكن محملة
                if (_allInvoiceIds.Count == 0)
                {
                    _allInvoiceIds = await _context.SalesInvoices
                        .OrderBy(x => x.SalesInvoiceId)
                        .Select(x => x.SalesInvoiceId)
                        .ToListAsync();

                    _currentIndex = _allInvoiceIds.IndexOf(_fullInvoice?.SalesInvoiceId ?? _invoiceArg.SalesInvoiceId);
                }

                // حساب الفهرس الجديد
                int newIndex = _currentIndex + direction;

                if (newIndex < 0 || newIndex >= _allInvoiceIds.Count)
                {
                    MessageBox.Show(direction < 0 ? "لا توجد فواتير سابقة" : "لا توجد فواتير تالية",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // تحميل الفاتورة الجديدة
                var invoiceId = _allInvoiceIds[newIndex];
                var newInvoice = await _context.SalesInvoices
                    .Include(x => x.Customer)
                    .Include(x => x.Items).ThenInclude(x => x.Product)
                    .Include(x => x.Items).ThenInclude(x => x.Unit)
                    .FirstOrDefaultAsync(x => x.SalesInvoiceId == invoiceId);

                if (newInvoice != null)
                {
                    _fullInvoice = newInvoice;
                    _currentIndex = newIndex;

                    // إعادة حساب الرصيد السابق والتحديث
                    _previousBalance = await CalculatePreviousBalanceAsync(_fullInvoice.Customer?.CustomerId ?? 0, _fullInvoice.InvoiceDate);
                    Title = $"معاينة طباعة الفاتورة - {_fullInvoice.InvoiceNumber}";
                    RefreshVisualPreview();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التنقل: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ====== دعم لوحة المفاتيح للتنقل والطباعة ======
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Left) btnPrevious_Click(this, new RoutedEventArgs());
            if (e.Key == Key.Right) btnNext_Click(this, new RoutedEventArgs());
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.P)
                btnPrint_Click(this, new RoutedEventArgs());
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
                btnSaveAsPdf_Click(this, new RoutedEventArgs());
        }

        // ====== تحويل SalesInvoice إلى PrintVM ======
        private static SalesInvoicePrintViewModel ConvertToPrintVM(SalesInvoice invoice, decimal previousBalance)
        {
            return new SalesInvoicePrintViewModel
            {
                InvoiceNumber = invoice.InvoiceNumber ?? UnspecifiedText,
                InvoiceDate = invoice.InvoiceDate,
                CustomerName = invoice.Customer?.CustomerName ?? "عميل نقدي",
                CustomerPhone = invoice.Customer?.Phone ?? UnspecifiedText,
                CustomerAddress = invoice.Customer?.Address ?? UnspecifiedText,

                SubTotal = invoice.SubTotal,
                DiscountAmount = invoice.DiscountAmount,
                TaxAmount = invoice.TaxAmount,
                NetTotal = invoice.NetTotal,
                PaidAmount = invoice.PaidAmount,
                RemainingAmount = invoice.RemainingAmount,
                PreviousBalance = previousBalance,
                GrandTotal = invoice.NetTotal + previousBalance,

                Lines = invoice.Items?.Select(line => new SalesInvoiceLineViewModel
                {
                    ProductName = line.Product?.ProductName ?? UnspecifiedText,
                    UnitName = line.Unit?.UnitName ?? UnspecifiedText,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    DiscountAmount = line.DiscountAmount,
                    NetAmount = line.NetAmount
                }).ToArray() ?? Array.Empty<SalesInvoiceLineViewModel>()
            };
        }

        // Helper methods لتبسيط رسائل التنبيه
        private static void ShowError(string message) =>
            MessageBox.Show(message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);

        private static void ShowWarning(string message) =>
            MessageBox.Show(message, "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowInfo(string message) =>
            MessageBox.Show(message, "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // امتدادات صغيرة لتبسيط ضبط النص
    internal static class UiExtensions
    {
        public static void SetTextSafe(this TextBlock? tb, string text)
        {
            if (tb != null) tb.Text = text ?? string.Empty;
        }
    }

    // ViewModel لعرض عناصر الفاتورة في List/ItemsControl
    public class InvoiceItemViewModel
    {
        public string ItemNumber { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string UnitName { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
    }
}
