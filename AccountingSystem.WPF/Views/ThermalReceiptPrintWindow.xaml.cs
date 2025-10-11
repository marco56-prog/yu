using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.Win32;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Xps.Packaging;
using System.IO.Packaging;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingPrinting = System.Drawing.Printing;
using WpfFontFamily = System.Windows.Media.FontFamily;

namespace AccountingSystem.WPF.Views
{
    public partial class ThermalReceiptPrintWindow : Window
    {
        private const string DefaultFontName = "Arial";
        private readonly SalesInvoice _invoice;
        private readonly IUnitOfWork _unitOfWork;
        private readonly List<PosItem> _items;
        private readonly decimal _paidAmount;
        private readonly string _paymentMethod;

        // إعدادات الطباعة
        private readonly DrawingPrinting.PrinterSettings _printerSettings;
        private double _receiptWidth = 300; // العرض بالبكسل للعرض 80mm

        public ThermalReceiptPrintWindow(SalesInvoice invoice, IUnitOfWork unitOfWork,
                                       List<PosItem> items, decimal paidAmount, string paymentMethod)
        {
            InitializeComponent();

            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _paidAmount = paidAmount;
            _paymentMethod = paymentMethod;

            _printerSettings = new DrawingPrinting.PrinterSettings();

            Loaded += (s, e) => LoadReceiptDataAsync();
            _ = _unitOfWork; // reference to avoid unused field analysis warning
        }

        private void LoadReceiptDataAsync()
        {
            try
            {
                // تحميل الطابعات المتاحة
                LoadAvailablePrinters();

                // تحديث بيانات الإيصال
                UpdateReceiptContent();

                // ضبط حجم المعاينة
                UpdateReceiptSize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الإيصال: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailablePrinters()
        {
            try
            {
                cmbAvailablePrinters.Items.Clear();

                // إضافة طابعة افتراضية
                cmbAvailablePrinters.Items.Add("الطابعة الافتراضية");

                // تحميل جميع الطابعات المثبتة
                foreach (string printerName in DrawingPrinting.PrinterSettings.InstalledPrinters)
                {
                    cmbAvailablePrinters.Items.Add(printerName);
                }

                // اختيار الطابعة الافتراضية
                if (cmbAvailablePrinters.Items.Count > 0)
                {
                    cmbAvailablePrinters.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الطابعات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);

                // إضافة خيار افتراضي
                cmbAvailablePrinters.Items.Add("لا توجد طابعات متاحة");
                cmbAvailablePrinters.SelectedIndex = 0;
            }
        }

        private void UpdateReceiptContent()
        {
            try
            {
                // تحديث معلومات الفاتورة
                lblReceiptNumber.Text = $"إيصال رقم: {_invoice.InvoiceNumber}";
                lblReceiptDate.Text = _invoice.InvoiceDate.ToString("yyyy/MM/dd");
                lblReceiptTime.Text = _invoice.InvoiceDate.ToString("HH:mm:ss");

                // تحديث معلومات الكاشير
                lblCashier.Text = "الكاشير: admin"; // يمكن تحسينها لاحقاً

                // مسح العناصر الموجودة
                DynamicItems.Children.Clear();

                // إضافة عناصر الفاتورة
                foreach (var item in _items)
                {
                    var itemGrid = CreateReceiptItemGrid(item);
                    DynamicItems.Children.Add(itemGrid);
                }

                // تحديث الإجماليات
                lblSubTotal.Text = $"{_invoice.SubTotal:N2} ج.م";
                lblTax.Text = $"{_invoice.TaxAmount:N2} ج.م";
                lblDiscount.Text = $"{_invoice.DiscountAmount:N2} ج.م";
                lblTotalAmount.Text = $"{_invoice.NetTotal:N2} ج.م";

                // تحديث معلومات الدفع
                lblPaymentMethod.Text = _paymentMethod;
                lblPaidAmount.Text = $"{_paidAmount:N2} ج.م";
                var changeAmount = _paidAmount - _invoice.NetTotal;
                lblChangeAmount.Text = $"{changeAmount:N2} ج.م";
                lblChangeAmount.Foreground = changeAmount >= 0 ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

                // تحديث الباركود
                lblBarcodeNumber.Text = $"{_invoice.InvoiceNumber}-{_invoice.InvoiceDate:yyyyMMdd}";

                // تحديث وقت الطباعة
                lblPrintTime.Text = $"طبع في: {DateTime.Now:yyyy/MM/dd HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث محتوى الإيصال: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Grid CreateReceiptItemGrid(PosItem item)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.Margin = new Thickness(0, 0, 0, 2);

            // اسم المنتج
            var nameBlock = new TextBlock
            {
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            Grid.SetColumn(nameBlock, 0);
            grid.Children.Add(nameBlock);

            // الكمية × السعر
            var quantityPriceBlock = new TextBlock
            {
                Text = $"{item.Quantity:N0}×{item.UnitPrice:N2}",
                FontSize = 9,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            Grid.SetColumn(quantityPriceBlock, 1);
            grid.Children.Add(quantityPriceBlock);

            // الإجمالي
            var totalBlock = new TextBlock
            {
                Text = $"{item.TotalPrice:N2}",
                FontSize = 9,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            Grid.SetColumn(totalBlock, 2);
            grid.Children.Add(totalBlock);

            return grid;
        }

        private void UpdateReceiptSize()
        {
            switch (cmbPrinterSize.SelectedIndex)
            {
                case 0: // 80mm
                    _receiptWidth = 300;
                    ReceiptPreview.Width = 300;
                    break;
                case 1: // 58mm
                    _receiptWidth = 220;
                    ReceiptPreview.Width = 220;
                    break;
                case 2: // A4
                    _receiptWidth = 400;
                    ReceiptPreview.Width = 400;
                    break;
            }
        }

        #region Event Handlers

        private void cmbPrinterSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                UpdateReceiptSize();
            }
        }

        private void cmbAvailablePrinters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbAvailablePrinters.SelectedItem != null)
            {
                var selectedPrinter = cmbAvailablePrinters.SelectedItem?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(selectedPrinter) && selectedPrinter != "الطابعة الافتراضية" && selectedPrinter != "لا توجد طابعات متاحة")
                {
                    _printerSettings.PrinterName = selectedPrinter;
                }
            }
        }

        private async void btnPrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await PrintThermalReceiptAsync();

                if (chkOpenCashDrawer.IsChecked == true)
                {
                    OpenCashDrawer();
                }

                MessageBox.Show("تم طباعة الإيصال بنجاح!", "نجحت العملية",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في طباعة الإيصال: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPreviewPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // إنشاء معاينة طباعة تقليدية
                var printDialog = new PrintDialog();
                var flowDoc = CreateFlowDocument();

                printDialog.ShowDialog();

                if (printDialog.PrintQueue != null)
                {
                    var paginator = ((IDocumentPaginatorSource)flowDoc).DocumentPaginator;
                    printDialog.PrintDocument(paginator, "إيصال الدفع");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في معاينة الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf|XPS Files|*.xps",
                    FileName = $"Receipt_{_invoice.InvoiceNumber}_{DateTime.Now:yyyyMMdd}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    if (saveDialog.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        await SaveAsPdfAsync(saveDialog.FileName);
                    }
                    else
                    {
                        SaveAsXps(saveDialog.FileName);
                    }

                    MessageBox.Show("تم حفظ الإيصال بنجاح!", "نجحت العملية",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الإيصال: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnTestPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // طباعة إيصال تجريبي
                await PrintTestReceiptAsync();

                MessageBox.Show("تم طباعة إيصال تجريبي!", "اكتملت العملية",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة التجريبية: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Printing Methods

        private async Task PrintThermalReceiptAsync()
        {
            try
            {
                // إنشاء مستند للطباعة
                var printDocument = new DrawingPrinting.PrintDocument();
                printDocument.PrinterSettings = _printerSettings;

                // ضبط حجم الورق للطباعة الحرارية
                var paperSize = new DrawingPrinting.PaperSize("Receipt", (int)_receiptWidth, 800);
                printDocument.DefaultPageSettings.PaperSize = paperSize;
                printDocument.DefaultPageSettings.Margins = new DrawingPrinting.Margins(10, 10, 10, 10);

                printDocument.PrintPage += (sender, e) =>
                {
                    if (e.Graphics != null)
                        DrawReceiptContent(e.Graphics, e.MarginBounds);
                };

                // طباعة
                printDocument.Print();

                // حفظ نسخة إضافية إذا طُلب ذلك
                if (chkPrintCopy.IsChecked == true)
                {
                    await Task.Delay(1000); // انتظار قصير
                    printDocument.Print();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"فشل في طباعة الإيصال: {ex.Message}", ex);
            }
        }

        private void DrawReceiptContent(DrawingGraphics graphics, DrawingRectangle bounds)
        {
            try
            {
                var yPosition = bounds.Top;
                var leftMargin = bounds.Left;
                var rightMargin = bounds.Right;
                var centerX = bounds.Left + bounds.Width / 2;

                // خطوط مختلفة
                var headerFont = new DrawingFont(DefaultFontName, 12, DrawingFontStyle.Bold);
                var normalFont = new DrawingFont(DefaultFontName, 9);
                var smallFont = new DrawingFont(DefaultFontName, 8);

                // رأس المتجر
                var storeNameSize = graphics.MeasureString(lblStoreName.Text, headerFont);
                graphics.DrawString(lblStoreName.Text, headerFont, System.Drawing.Brushes.Black,
                    centerX - storeNameSize.Width / 2, yPosition);
                yPosition += (int)storeNameSize.Height + 5;

                graphics.DrawString(lblStoreAddress.Text, smallFont, System.Drawing.Brushes.Black,
                    leftMargin, yPosition);
                yPosition += 15;

                graphics.DrawString(lblStorePhone.Text, smallFont, System.Drawing.Brushes.Black,
                    leftMargin, yPosition);
                yPosition += 20;

                // خط فاصل
                graphics.DrawLine(System.Drawing.Pens.Black, leftMargin, yPosition, rightMargin, yPosition);
                yPosition += 10;

                // معلومات الفاتورة
                graphics.DrawString($"إيصال: {_invoice.InvoiceNumber}", normalFont, System.Drawing.Brushes.Black,
                    leftMargin, yPosition);
                graphics.DrawString(_invoice.InvoiceDate.ToString("yyyy/MM/dd HH:mm"), normalFont, System.Drawing.Brushes.Black,
                    rightMargin - 100, yPosition);
                yPosition += 20;

                // خط فاصل
                graphics.DrawLine(System.Drawing.Pens.Black, leftMargin, yPosition, rightMargin, yPosition);
                yPosition += 10;

                // رأس الجدول
                graphics.DrawString("الصنف", normalFont, System.Drawing.Brushes.Black, leftMargin, yPosition);
                graphics.DrawString("كمية×سعر", normalFont, System.Drawing.Brushes.Black, centerX - 30, yPosition);
                graphics.DrawString("الإجمالي", normalFont, System.Drawing.Brushes.Black, rightMargin - 60, yPosition);
                yPosition += 20;

                // خط تحت الرأس
                graphics.DrawLine(System.Drawing.Pens.Gray, leftMargin, yPosition, rightMargin, yPosition);
                yPosition += 5;

                // العناصر
                foreach (var item in _items)
                {
                    // اسم المنتج (مع التفاف النص إذا كان طويلاً)
                    var productName = item.ProductName;
                    if (productName.Length > 20)
                        productName = productName.Substring(0, 17) + "...";

                    graphics.DrawString(productName, smallFont, System.Drawing.Brushes.Black, leftMargin, yPosition);
                    graphics.DrawString($"{item.Quantity:N0}×{item.UnitPrice:N2}", smallFont, System.Drawing.Brushes.Black,
                        centerX - 30, yPosition);
                    graphics.DrawString($"{item.TotalPrice:N2}", smallFont, System.Drawing.Brushes.Black,
                        rightMargin - 60, yPosition);
                    yPosition += 15;
                }

                yPosition += 10;

                // خط فاصل
                graphics.DrawLine(System.Drawing.Pens.Black, leftMargin, yPosition, rightMargin, yPosition);
                yPosition += 10;

                // الإجماليات
                graphics.DrawString("المجموع:", normalFont, System.Drawing.Brushes.Black, leftMargin, yPosition);
                graphics.DrawString($"{_invoice.SubTotal:N2} ج.م", normalFont, System.Drawing.Brushes.Black,
                    rightMargin - 80, yPosition);
                yPosition += 15;

                if (_invoice.TaxAmount > 0)
                {
                    graphics.DrawString("الضريبة:", normalFont, System.Drawing.Brushes.Black, leftMargin, yPosition);
                    graphics.DrawString($"{_invoice.TaxAmount:N2} ج.م", normalFont, System.Drawing.Brushes.Black,
                        rightMargin - 80, yPosition);
                    yPosition += 15;
                }

                if (_invoice.DiscountAmount > 0)
                {
                    graphics.DrawString("الخصم:", normalFont, System.Drawing.Brushes.Black, leftMargin, yPosition);
                    graphics.DrawString($"{_invoice.DiscountAmount:N2} ج.م", normalFont, System.Drawing.Brushes.Black,
                        rightMargin - 80, yPosition);
                    yPosition += 15;
                }

                // الإجمالي النهائي
                graphics.DrawLine(System.Drawing.Pens.Black, leftMargin, yPosition, rightMargin, yPosition);
                yPosition += 5;

                graphics.DrawString("الإجمالي:", headerFont, System.Drawing.Brushes.Black, leftMargin, yPosition);
                graphics.DrawString($"{_invoice.NetTotal:N2} ج.م", headerFont, System.Drawing.Brushes.Black,
                    rightMargin - 100, yPosition);
                yPosition += 25;

                // معلومات الدفع
                graphics.DrawString($"الدفع: {_paymentMethod}", normalFont, System.Drawing.Brushes.Black, leftMargin, yPosition);
                yPosition += 15;
                graphics.DrawString($"المدفوع: {_paidAmount:N2} ج.م", normalFont, System.Drawing.Brushes.Black, leftMargin, yPosition);
                yPosition += 15;

                var changeAmount = _paidAmount - _invoice.NetTotal;
                if (changeAmount != 0)
                {
                    graphics.DrawString($"الباقي: {changeAmount:N2} ج.م", normalFont,
                        changeAmount >= 0 ? System.Drawing.Brushes.Green : System.Drawing.Brushes.Red, leftMargin, yPosition);
                    yPosition += 20;
                }

                // رسالة الشكر
                yPosition += 10;
                graphics.DrawLine(System.Drawing.Pens.Black, leftMargin, yPosition, rightMargin, yPosition);
                yPosition += 10;

                var thankYouSize = graphics.MeasureString("شكراً لزيارتكم", headerFont);
                graphics.DrawString("شكراً لزيارتكم", headerFont, System.Drawing.Brushes.Black,
                    centerX - thankYouSize.Width / 2, yPosition);
            }
            catch (Exception ex)
            {
                // في حالة الخطأ، اطبع رسالة خطأ بسيطة
                graphics.DrawString($"خطأ في الطباعة: {ex.Message}",
                    new DrawingFont(DefaultFontName, 10), System.Drawing.Brushes.Red, bounds.Left, bounds.Top);
            }
        }

        private async Task PrintTestReceiptAsync()
        {
            try
            {
                var testDocument = new PrintDocument();
                testDocument.PrinterSettings = _printerSettings;

                testDocument.PrintPage += (sender, e) =>
                {
                    var graphics = e.Graphics;
                    if (graphics == null) return;
                    var yPos = e.MarginBounds.Top;
                    var font = new System.Drawing.Font("Arial", 10);

                    graphics.DrawString("*** إيصال تجريبي ***", font, System.Drawing.Brushes.Black,
                        e.MarginBounds.Left, yPos);
                    yPos += 20;

                    graphics.DrawString($"التاريخ: {DateTime.Now:yyyy/MM/dd HH:mm:ss}", font, System.Drawing.Brushes.Black,
                        e.MarginBounds.Left, yPos);
                    yPos += 20;

                    graphics.DrawString("هذا اختبار لطباعة الإيصالات", font, System.Drawing.Brushes.Black,
                        e.MarginBounds.Left, yPos);
                    yPos += 20;

                    graphics.DrawString("إذا ظهر هذا النص، فالطابعة تعمل بشكل صحيح", font, System.Drawing.Brushes.Black,
                        e.MarginBounds.Left, yPos);
                };

                testDocument.Print();
                await Task.Delay(500); // انتظار قصير
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"فشل في الطباعة التجريبية: {ex.Message}", ex);
            }
        }

        private static void OpenCashDrawer()
        {
            try
            {
                // محاولة إرسال أمر فتح درج النقود
                // هذا يعتمد على نوع الطابعة
                // ESC/POS example command could be sent to a compatible printer here if supported

                // يمكن تطوير هذا لاحقاً لدعم أنواع مختلفة من الطابعات
                Console.Beep(1000, 200); // صوت تنبيه مؤقت
            }
            catch (Exception ex)
            {
                // تجاهل أخطاء درج النقود
                System.Diagnostics.Debug.WriteLine($"خطأ في فتح درج النقود: {ex.Message}");
            }
        }

        #endregion

        #region Document Creation

        private FlowDocument CreateFlowDocument()
        {
            var doc = new FlowDocument();
            doc.PageWidth = _receiptWidth;
            doc.PagePadding = new Thickness(10);
            doc.FontFamily = new System.Windows.Media.FontFamily("Arial");

            // يمكن إضافة المحتوى هنا حسب الحاجة
            var paragraph = new System.Windows.Documents.Paragraph();
            paragraph.Inlines.Add(new Run("محتوى الإيصال..."));
            doc.Blocks.Add(paragraph);

            return doc;
        }

        private async Task SaveAsPdfAsync(string fileName)
        {
            try
            {
                await Task.Run(() =>
                {
                    using var writer = new PdfWriter(fileName);
                    using var pdf = new PdfDocument(writer);
                    using var document = new Document(pdf);

                    // إعداد الخط
                    var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // إضافة اسم المتجر
                    var storeName = new iText.Layout.Element.Paragraph(lblStoreName?.Text ?? "المتجر")
                        .SetFont(font)
                        .SetFontSize(14)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                    document.Add(storeName);

                    // إضافة العنوان والهاتف
                    if (!string.IsNullOrWhiteSpace(lblStoreAddress?.Text))
                    {
                        var address = new iText.Layout.Element.Paragraph(lblStoreAddress.Text)
                            .SetFont(font)
                            .SetFontSize(10)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                        document.Add(address);
                    }

                    if (!string.IsNullOrWhiteSpace(lblStorePhone?.Text))
                    {
                        var phone = new iText.Layout.Element.Paragraph(lblStorePhone.Text)
                            .SetFont(font)
                            .SetFontSize(10)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                        document.Add(phone);
                    }

                    // إضافة فاصل
                    document.Add(new iText.Layout.Element.Paragraph(" "));

                    // إضافة معلومات الفاتورة
                    var invoiceInfo = new iText.Layout.Element.Paragraph($"إيصال: {_invoice.InvoiceNumber}")
                        .SetFont(font)
                        .SetFontSize(10);
                    document.Add(invoiceInfo);

                    var dateInfo = new iText.Layout.Element.Paragraph($"التاريخ: {_invoice.InvoiceDate:yyyy/MM/dd HH:mm}")
                        .SetFont(font)
                        .SetFontSize(10);
                    document.Add(dateInfo);

                    document.Add(new iText.Layout.Element.Paragraph(" "));

                    // إضافة العناصر
                    var table = new iText.Layout.Element.Table(3).UseAllAvailableWidth();

                    // رؤوس الجدول
                    table.AddHeaderCell(new Cell().Add(new iText.Layout.Element.Paragraph("الصنف").SetFont(font)));
                    table.AddHeaderCell(new Cell().Add(new iText.Layout.Element.Paragraph("الكمية×السعر").SetFont(font)));
                    table.AddHeaderCell(new Cell().Add(new iText.Layout.Element.Paragraph("المجموع").SetFont(font)));

                    // العناصر
                    foreach (var item in _items)
                    {
                        table.AddCell(new Cell().Add(new iText.Layout.Element.Paragraph(item.ProductName ?? "").SetFont(font)));
                        table.AddCell(new Cell().Add(new iText.Layout.Element.Paragraph($"{item.Quantity:N0}×{item.UnitPrice:N2}").SetFont(font)));
                        table.AddCell(new Cell().Add(new iText.Layout.Element.Paragraph(item.TotalPrice.ToString("N2")).SetFont(font)));
                    }

                    document.Add(table);

                    // إضافة الإجمالي
                    document.Add(new iText.Layout.Element.Paragraph(" "));
                    var total = new iText.Layout.Element.Paragraph($"الإجمالي: {_invoice.NetTotal:N2} ج.م")
                        .SetFont(font)
                        .SetFontSize(12)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);
                    document.Add(total);

                    var paymentMethod = new iText.Layout.Element.Paragraph($"طريقة الدفع: {_paymentMethod}")
                        .SetFont(font)
                        .SetFontSize(10);
                    document.Add(paymentMethod);

                    // شكراً لكم
                    document.Add(new iText.Layout.Element.Paragraph(" "));
                    var thanks = new iText.Layout.Element.Paragraph("شكراً لتسوقكم معنا")
                        .SetFont(font)
                        .SetFontSize(10)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                    document.Add(thanks);
                });

                MessageBox.Show("تم حفظ PDF بنجاح!", "نجح الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ PDF: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsXps(string fileName)
        {
            try
            {
                var flowDoc = CreateFlowDocument();
                using (var package = System.IO.Packaging.Package.Open(fileName, FileMode.Create))
                {
                    using (var xpsDoc = new XpsDocument(package))
                    {
                        var writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                        writer.Write(((IDocumentPaginatorSource)flowDoc).DocumentPaginator);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"فشل في حفظ XPS: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
