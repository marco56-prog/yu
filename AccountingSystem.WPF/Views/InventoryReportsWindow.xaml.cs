using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using AccountingSystem.Business;
using AccountingSystem.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace AccountingSystem.WPF.Views
{
    public partial class InventoryReportsWindow : Window, INotifyPropertyChanged
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IReportService _reportService;
        private readonly ICategoryService _categoryService;
        private readonly CultureInfo _culture;

        // قائمة معروضة + View للفلترة/الفرز
        private readonly ObservableCollection<InventoryRow> _rows = new();
        public ICollectionView InventoryView { get; }

        // بحث نصّي
        private string _searchText = string.Empty;

        // حالة التحميل
        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); lblLoading.Visibility = value ? Visibility.Visible : Visibility.Collapsed; } }

        public event PropertyChangedEventHandler? PropertyChanged;

        public InventoryReportsWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _reportService = serviceProvider.GetRequiredService<IReportService>();
            _categoryService = serviceProvider.GetRequiredService<ICategoryService>();

            // ثقافة مصر مع رمز ج.م
            _culture = new CultureInfo("ar-EG");
            _culture.NumberFormat.CurrencySymbol = "ج.م";

            // Binding
            DataContext = this;
            InventoryView = CollectionViewSource.GetDefaultView(_rows);
            InventoryView.Filter = FilterRows;

            Loaded += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // تحميل الفئات
                var categories = (await _categoryService.GetAllCategoriesAsync()).ToList();
                categories.Insert(0, new Category { CategoryId = 0, CategoryName = "جميع الفئات" });
                cmbCategory.ItemsSource = categories;
                cmbCategory.SelectedIndex = 0;

                await GenerateReportAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateReportAsync()
        {
            try
            {
                IsLoading = true;

                var selectedCategory = cmbCategory.SelectedItem as Category;
                int? categoryId = selectedCategory?.CategoryId == 0 ? null : selectedCategory?.CategoryId;
                bool? onlyLow = chkLowStockOnly.IsChecked == true ? true : null;

                var report = await _reportService.GenerateInventoryReportAsync(categoryId, onlyLow);

                // تعبئة الإحصائيات
                lblTotalValue.Text = report.TotalInventoryValue.ToString("C", _culture);
                lblTotalProducts.Text = report.Items.Count.ToString("N0", _culture);
                lblLowStock.Text = report.LowStockCount.ToString("N0", _culture);
                lblOutOfStock.Text = report.OutOfStockCount.ToString("N0", _culture);

                // تعبئة الجدول (قيم typed)
                _rows.Clear();
                foreach (var item in report.Items)
                {
                    _rows.Add(new InventoryRow
                    {
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        CategoryName = item.CategoryName,
                        UnitName = item.UnitName,
                        CurrentStock = item.CurrentStock,
                        MinimumStock = item.MinimumStock,
                        PurchasePrice = item.PurchasePrice,
                        SalePrice = item.SalePrice,
                        InventoryValue = item.InventoryValue,
                        IsOutOfStock = item.IsOutOfStock,
                        IsLowStock = item.IsLowStock,
                        Status = item.IsOutOfStock ? "نفد المخزون" : item.IsLowStock ? "ناقص المخزون" : "متوفر"
                    });
                }

                AccountingSystem.WPF.Helpers.CollectionViewHelper.SafeRefresh(InventoryView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في توليد التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // فلترة البحث + خيار ناقص المخزون (الخيار الأخير يُرسل للخدمة – هنا فقط فلترة نصية)
        private bool FilterRows(object obj)
        {
            if (obj is not InventoryRow r) return false;
            if (string.IsNullOrWhiteSpace(_searchText)) return true;

            var q = _searchText.Trim().ToLowerInvariant();
            return (r.ProductName ?? string.Empty).ToLowerInvariant().Contains(q)
                || (r.ProductCode ?? string.Empty).ToLowerInvariant().Contains(q)
                || (r.CategoryName ?? string.Empty).ToLowerInvariant().Contains(q);
        }

        private async void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            await GenerateReportAsync();
        }

        private void txtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _searchText = txtSearch.Text ?? string.Empty;
            AccountingSystem.WPF.Helpers.CollectionViewHelper.SafeRefresh(InventoryView);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ====== تصدير CSV متوافق مع Excel (UTF-8 BOM) ======
        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Title = "تصدير تقرير المخزون",
                    Filter = "CSV (Excel)|*.csv",
                    FileName = $"InventoryReport_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() != true) return;

                var sb = new StringBuilder();

                // رأس
                sb.AppendLine("كود المنتج,اسم المنتج,الفئة,الوحدة,المخزون الحالي,الحد الأدنى,سعر الشراء,سعر البيع,قيمة المخزون,الحالة");

                // صفوف
                foreach (InventoryRow r in InventoryView)
                {
                    // استخدام Invariant للارقام + تعريب داخل Excel حسب التنسيق
                    string line = string.Join(",",
                        Csv(r.ProductCode),
                        Csv(r.ProductName),
                        Csv(r.CategoryName),
                        Csv(r.UnitName),
                        r.CurrentStock.ToString(CultureInfo.InvariantCulture),
                        r.MinimumStock.ToString(CultureInfo.InvariantCulture),
                        r.PurchasePrice.ToString(CultureInfo.InvariantCulture),
                        r.SalePrice.ToString(CultureInfo.InvariantCulture),
                        r.InventoryValue.ToString(CultureInfo.InvariantCulture),
                        Csv(r.Status)
                    );
                    sb.AppendLine(line);
                }

                // كتابة UTF-8 BOM لضمان العربية
                var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
                System.IO.File.WriteAllBytes(sfd.FileName, bytes);

                MessageBox.Show("تم التصدير بنجاح (CSV متوافق مع Excel).", "تصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Csv(string? s)
        {
            s ??= string.Empty;
            // إحاطة بالقوسين لو فيها فاصلة/علامات اقتباس
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return $"\"{s.Replace("\"", "\"\"")}\"";
            return s;
        }

        // ====== طباعة عبر FlowDocument ======
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = BuildFlowDocumentForPrint();
                var pd = new PrintDialog();
                if (pd.ShowDialog() != true) return;

                // تكبير ملائم للورقة
                doc.PagePadding = new Thickness(40);
                doc.PageWidth = pd.PrintableAreaWidth;
                doc.PageHeight = pd.PrintableAreaHeight;

                var idp = new IDocumentPaginatorSourceAdapter(doc);
                pd.PrintDocument(idp.DocumentPaginator, "تقرير المخزون");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument BuildFlowDocumentForPrint()
        {
            var doc = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12
            };

            // عنوان
            var title = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            title.Inlines.Add(new Run("تقرير المخزون"));
            doc.Blocks.Add(title);

            // ملخص
            var summary = new Paragraph { TextAlignment = TextAlignment.Right };
            summary.Inlines.Add(new Run($"قيمة المخزون: {lblTotalValue.Text}   |   "));
            summary.Inlines.Add(new Run($"عدد المنتجات: {lblTotalProducts.Text}   |   "));
            summary.Inlines.Add(new Run($"ناقص المخزون: {lblLowStock.Text}   |   "));
            summary.Inlines.Add(new Run($"نفد المخزون: {lblOutOfStock.Text}"));
            doc.Blocks.Add(summary);

            // جدول
            var table = new Table();
            doc.Blocks.Add(table);

            string[] headers = new[]
            {
                "كود المنتج","اسم المنتج","الفئة","الوحدة",
                "المخزون الحالي","الحد الأدنى","سعر الشراء","سعر البيع","قيمة المخزون","الحالة"
            };

            foreach (var _ in headers) table.Columns.Add(new TableColumn());

            var headerRowGroup = new TableRowGroup();
            var headerRow = new TableRow();
            foreach (var h in headers)
            {
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h)))
                {
                    FontWeight = FontWeights.Bold,
                    Background = Brushes.LightGray,
                    Padding = new Thickness(4),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5)
                });
            }
            headerRowGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerRowGroup);

            var body = new TableRowGroup();
            foreach (InventoryRow r in InventoryView)
            {
                var row = new TableRow();
                void add(string text) => row.Cells.Add(new TableCell(new Paragraph(new Run(text))) { Padding = new Thickness(3), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.25) });

                add(r.ProductCode ?? "");
                add(r.ProductName ?? "");
                add(r.CategoryName ?? "");
                add(r.UnitName ?? "");
                add(r.CurrentStock.ToString("N2", _culture));
                add(r.MinimumStock.ToString("N2", _culture));
                add(r.PurchasePrice.ToString("C", _culture));
                add(r.SalePrice.ToString("C", _culture));
                add(r.InventoryValue.ToString("C", _culture));
                add(r.Status ?? "");

                body.Rows.Add(row);
            }
            table.RowGroups.Add(body);

            return doc;
        }

        private sealed class IDocumentPaginatorSourceAdapter : IDocumentPaginatorSource
        {
            private readonly FlowDocument _doc;
            public IDocumentPaginatorSourceAdapter(FlowDocument doc) => _doc = doc;
            public DocumentPaginator DocumentPaginator => ((IDocumentPaginatorSource)_doc).DocumentPaginator;
        }

        private void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // صف العرض
        public class InventoryRow
        {
            public string? ProductCode { get; set; }
            public string? ProductName { get; set; }
            public string? CategoryName { get; set; }
            public string? UnitName { get; set; }

            public decimal CurrentStock { get; set; }
            public decimal MinimumStock { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal SalePrice { get; set; }
            public decimal InventoryValue { get; set; }

            public bool IsOutOfStock { get; set; }
            public bool IsLowStock { get; set; }
            public string? Status { get; set; }
        }
    }
}
