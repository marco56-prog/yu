// File: AdvancedReportsViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AccountingSystem.Business;
using Microsoft.Win32;

namespace AccountingSystem.WPF.ViewModels
{
    public class AdvancedReportsViewModel : INotifyPropertyChanged
    {
        private readonly IReportsService _reportsService;
        private DateTime _fromDate;
        private DateTime _toDate;
        private decimal _totalSales;
        private decimal _totalPurchases;
        private decimal _netProfit;
        private int _totalInvoices;
        private string? _salesGrowthText;
        private string? _purchasesGrowthText;
        private string? _profitMarginText;
        private string? _invoicesCountText;
        private string? _lowStockItemsText;
        private string? _lastUpdateText;
        private int _selectedSalesReportType;

        // تقليل فرص التداخل أثناء تنفيذ أوامر متكررة سريعاً
        private int _isRefreshing = 0;

        public AdvancedReportsViewModel(IReportsService reportsService)
        {
            _reportsService = reportsService;
            InitializeCommands();
            InitializeData();
        }

        #region Properties

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value))
                {
                    _ = RefreshDataAsync(); // Fire and forget
                }
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value))
                {
                    _ = RefreshDataAsync(); // Fire and forget
                }
            }
        }

        public decimal TotalSales
        {
            get => _totalSales;
            set => SetProperty(ref _totalSales, value);
        }

        public decimal TotalPurchases
        {
            get => _totalPurchases;
            set => SetProperty(ref _totalPurchases, value);
        }

        public decimal NetProfit
        {
            get => _netProfit;
            set => SetProperty(ref _netProfit, value);
        }

        public int TotalInvoices
        {
            get => _totalInvoices;
            set => SetProperty(ref _totalInvoices, value);
        }

        public string SalesGrowthText
        {
            get => _salesGrowthText ?? "";
            set => SetProperty(ref _salesGrowthText, value);
        }

        public string PurchasesGrowthText
        {
            get => _purchasesGrowthText ?? "";
            set => SetProperty(ref _purchasesGrowthText, value);
        }

        public string ProfitMarginText
        {
            get => _profitMarginText ?? "";
            set => SetProperty(ref _profitMarginText, value);
        }

        public string InvoicesCountText
        {
            get => _invoicesCountText ?? "";
            set => SetProperty(ref _invoicesCountText, value);
        }

        public string LowStockItemsText
        {
            get => _lowStockItemsText ?? "";
            set => SetProperty(ref _lowStockItemsText, value);
        }

        public string LastUpdateText
        {
            get => _lastUpdateText ?? "";
            set => SetProperty(ref _lastUpdateText, value);
        }

        public int SelectedSalesReportType
        {
            get => _selectedSalesReportType;
            set
            {
                if (SetProperty(ref _selectedSalesReportType, value))
                {
                    _ = GenerateSalesReportAsync(); // Fire and forget
                }
            }
        }

        public List<string> SalesReportTypes { get; } = new List<string>
        {
            "تقرير يومي",
            "تقرير أسبوعي",
            "تقرير شهري"
        };

        public ObservableCollection<SalesReportItemDto> SalesReportItems { get; } = new ObservableCollection<SalesReportItemDto>();
        public ObservableCollection<InventoryReportItemDto> InventoryReportItems { get; } = new ObservableCollection<InventoryReportItemDto>();
        public ObservableCollection<ProfitReportItemDto> ProfitReportItems { get; } = new ObservableCollection<ProfitReportItemDto>();
        public ObservableCollection<TopProductDto> TopProducts { get; } = new ObservableCollection<TopProductDto>();

        #endregion

        #region Commands

        public ICommand RefreshDataCommand { get; private set; } = null!;
        public ICommand GenerateSalesReportCommand { get; private set; } = null!;
        public ICommand GenerateInventoryReportCommand { get; private set; } = null!;
        public ICommand GenerateProfitReportCommand { get; private set; } = null!;
        public ICommand ExportSalesCommand { get; private set; } = null!;
        public ICommand ExportInventoryCommand { get; private set; } = null!;
        public ICommand ExportProfitCommand { get; private set; } = null!;
        public ICommand ExportTopProductsCommand { get; private set; } = null!;
        public ICommand ExportAllReportsCommand { get; private set; } = null!;
        public ICommand RefreshAllDataCommand { get; private set; } = null!;
        public ICommand PrintReportsCommand { get; private set; } = null!;
        public ICommand EmailReportsCommand { get; private set; } = null!;

        #endregion

        #region Private Methods

        private void InitializeCommands()
        {
            RefreshDataCommand = new AsyncRelayCommand(RefreshDataAsync);
            GenerateSalesReportCommand = new AsyncRelayCommand(GenerateSalesReportAsync);
            GenerateInventoryReportCommand = new AsyncRelayCommand(GenerateInventoryReportAsync);
            GenerateProfitReportCommand = new AsyncRelayCommand(GenerateProfitReportAsync);
            ExportSalesCommand = new AsyncRelayCommand(ExportSalesAsync);
            ExportInventoryCommand = new AsyncRelayCommand(ExportInventoryAsync);
            ExportProfitCommand = new AsyncRelayCommand(ExportProfitAsync);
            ExportTopProductsCommand = new AsyncRelayCommand(ExportTopProductsAsync);
            ExportAllReportsCommand = new AsyncRelayCommand(ExportAllReportsAsync);
            RefreshAllDataCommand = new AsyncRelayCommand(RefreshAllDataAsync);
            PrintReportsCommand = new AsyncRelayCommand(async () => await PrintReportsAsync());
            EmailReportsCommand = new AsyncRelayCommand(async () => await EmailReportsAsync());
        }

        private void InitializeData()
        {
            FromDate = DateTime.Today.AddDays(-30);
            ToDate = DateTime.Today;
            SelectedSalesReportType = 0;
            _ = RefreshDataAsync(); // Fire and forget
        }

        private async Task RefreshDataAsync()
        {
            // منع أكثر من تنفيذ متوازي
            if (Interlocked.Exchange(ref _isRefreshing, 1) == 1) return;

            try
            {
                var summary = await _reportsService.GetFinancialSummaryAsync(FromDate, ToDate);

                TotalSales = summary.TotalSales;
                TotalPurchases = summary.TotalPurchases;
                NetProfit = summary.NetProfit;
                TotalInvoices = summary.TotalInvoices;

                UpdateGrowthIndicators();

                LastUpdateText = $"آخر تحديث: {DateTime.Now:yyyy/MM/dd HH:mm:ss}";

                await GenerateSalesReportAsync();
                await GenerateInventoryReportAsync();
                await GenerateProfitReportAsync();
                await LoadTopProductsAsync();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحديث البيانات: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _isRefreshing, 0);
            }
        }

        private void UpdateGrowthIndicators()
        {
            var periodDays = Math.Max((ToDate - FromDate).Days, 1);
            var profitMargin = TotalSales > 0 ? (NetProfit / TotalSales) * 100m : 0m;

            SalesGrowthText = $"خلال {periodDays} يوم";
            PurchasesGrowthText = $"خلال {periodDays} يوم";
            ProfitMarginText = $"هامش الربح: {profitMargin:F2}%";

            // تحويل القسمة إلى عشرية لتجنّب قسمة صحيحة
            var avgPerDay = (decimal)TotalInvoices / periodDays;
            InvoicesCountText = $"متوسط {avgPerDay:F1} فاتورة/يوم";
        }

        private async Task GenerateSalesReportAsync()
        {
            try
            {
                var reportType = (SalesReportType)SelectedSalesReportType;
                var items = await _reportsService.GetSalesReportAsync(FromDate, ToDate, reportType);

                SalesReportItems.Clear();
                foreach (var item in items)
                    SalesReportItems.Add(item);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في إنتاج تقرير المبيعات: {ex.Message}");
            }
        }

        private async Task GenerateInventoryReportAsync()
        {
            try
            {
                var items = await _reportsService.GetInventoryReportAsync();

                InventoryReportItems.Clear();
                foreach (var item in items)
                    InventoryReportItems.Add(item);

                var lowStockCount = items.Count(i => i.Status == "كمية منخفضة" || i.Status == "نفدت الكمية");
                LowStockItemsText = lowStockCount > 0 ? $"تحذير: {lowStockCount} صنف بحاجة لإعادة تموين" : "";
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في إنتاج تقرير المخزون: {ex.Message}");
            }
        }

        private async Task GenerateProfitReportAsync()
        {
            try
            {
                var items = await _reportsService.GetProfitReportAsync(FromDate, ToDate);

                ProfitReportItems.Clear();
                foreach (var item in items)
                    ProfitReportItems.Add(item);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في إنتاج تقرير الأرباح: {ex.Message}");
            }
        }

        private async Task LoadTopProductsAsync()
        {
            try
            {
                var items = await _reportsService.GetTopProductsAsync(FromDate, ToDate, 10);

                TopProducts.Clear();
                foreach (var item in items)
                    TopProducts.Add(item);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل أفضل المنتجات: {ex.Message}");
            }
        }

        private async Task ExportSalesAsync()
        {
            await ExportToCsvAsync(SalesReportItems, "SalesReport", "تقرير المبيعات");
        }

        private async Task ExportInventoryAsync()
        {
            await ExportToCsvAsync(InventoryReportItems, "InventoryReport", "تقرير المخزون");
        }

        private async Task ExportProfitAsync()
        {
            await ExportToCsvAsync(ProfitReportItems, "ProfitReport", "تقرير الأرباح");
        }

        private async Task ExportTopProductsAsync()
        {
            await ExportToCsvAsync(TopProducts, "TopProductsReport", "أفضل المنتجات");
        }

        private async Task ExportAllReportsAsync()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "ZIP Files (*.zip)|*.zip",
                    FileName = $"AllReports_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // إنشاء ZIP وكتابة كل تقرير كملف CSV داخل الأرشيف مباشرةً
                    using (var zipStream = new FileStream(saveDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
                    {
                        await AddCollectionCsvToArchiveAsync(archive, SalesReportItems, "SalesReport.csv");
                        await AddCollectionCsvToArchiveAsync(archive, InventoryReportItems, "InventoryReport.csv");
                        await AddCollectionCsvToArchiveAsync(archive, ProfitReportItems, "ProfitReport.csv");
                        await AddCollectionCsvToArchiveAsync(archive, TopProducts, "TopProductsReport.csv");
                    }

                    ShowInfo("تم تصدير جميع التقارير في ملف ZIP بنجاح!");
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تصدير التقارير: {ex.Message}");
            }
        }

        private async Task RefreshAllDataAsync()
        {
            await RefreshDataAsync();
        }

        private static async Task PrintReportsAsync()
        {
            // يمكن لاحقاً توليد PDF/Excel للطباعة
            ShowInfo("سيتم إضافة وظيفة الطباعة قريباً");
            await Task.CompletedTask;
        }

        private static async Task EmailReportsAsync()
        {
            // يمكن لاحقاً إرفاق ملفات وتكامل SMTP
            ShowInfo("سيتم إضافة وظيفة الإرسال بالبريد قريباً");
            await Task.CompletedTask;
        }

        private async Task ExportToCsvAsync<T>(IEnumerable<T> data, string defaultFileName, string displayName)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = $"{defaultFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await ExportToCsvAsync(data, saveDialog.FileName);
                    ShowInfo($"تم تصدير {displayName} بنجاح!");
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تصدير {displayName}: {ex.Message}");
            }
        }

        private static async Task ExportToCsvAsync<T>(IEnumerable<T> data, string filePath)
        {
            await Task.Run(() =>
            {
                var csv = BuildCsvForCollection(data);

                // كتابة UTF-8 مع BOM لتحسين التوافق مع Excel
                var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                File.WriteAllText(filePath, csv, utf8Bom);
            });
        }

        private static async Task AddCollectionCsvToArchiveAsync<T>(ZipArchive archive, IEnumerable<T> data, string entryName)
        {
            await Task.Run(() =>
            {
                var csv = BuildCsvForCollection(data);
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                writer.Write(csv);
            });
        }

        private static string BuildCsvForCollection<T>(IEnumerable<T> data)
        {
            var sb = new StringBuilder();

            if (data.Any())
            {
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead && (p.PropertyType.IsValueType || p.PropertyType == typeof(string)))
                    .ToArray();

                // Headers
                sb.AppendLine(string.Join(",", properties.Select(p => QuoteField(GetDisplayName(p.Name)))));

                // Rows
                foreach (var item in data)
                {
                    var values = properties.Select(p =>
                    {
                        var value = p.GetValue(item);
                        // استخدام ToString وفق الثقافة الحالية؛ الاقتباس يعالج الفواصل
                        return QuoteField(value?.ToString() ?? "");
                    });
                    sb.AppendLine(string.Join(",", values));
                }
            }

            return sb.ToString();
        }

        private static string QuoteField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\r") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        private static string GetDisplayName(string propertyName)
        {
            var displayNames = new Dictionary<string, string>
            {
                { "InvoiceNumber", "رقم الفاتورة" },
                { "InvoiceDate", "التاريخ" },
                { "CustomerName", "العميل" },
                { "SubTotal", "المجموع الفرعي" },
                { "TaxAmount", "الضريبة" },
                { "DiscountAmount", "الخصم" },
                { "NetTotal", "الإجمالي" },
                { "ItemsCount", "عدد الأصناف" },
                { "PaymentStatus", "حالة الدفع" },
                { "ProductCode", "كود المنتج" },
                { "ProductName", "اسم المنتج" },
                { "CategoryName", "الفئة" },
                { "UnitName", "الوحدة" },
                { "CurrentStock", "الكمية الحالية" },
                { "MinimumStock", "الحد الأدنى" },
                { "PurchasePrice", "سعر الشراء" },
                { "SalePrice", "سعر البيع" },
                { "StockValue", "قيمة المخزون" },
                { "Status", "الحالة" },
                { "Date", "التاريخ" },
                { "Period", "الفترة" },
                { "Revenue", "الإيرادات" },
                { "CostOfGoodsSold", "تكلفة البضاعة" },
                { "GrossProfit", "إجمالي الربح" },
                { "Expenses", "المصروفات" },
                { "NetProfit", "صافي الربح" },
                { "ProfitMargin", "هامش الربح %" },
                { "Rank", "الترتيب" },
                { "TotalQuantitySold", "إجمالي الكمية المباعة" },
                { "TotalSalesValue", "إجمالي المبيعات" },
                { "TotalProfit", "إجمالي الربح" },
                { "SalesTransactions", "عدد مرات البيع" },
                { "AveragePrice", "متوسط السعر" }
            };

            return displayNames.TryGetValue(propertyName, out string? displayName) ? displayName : propertyName;
        }

        private static void ShowInfo(string message)
        {
            MessageBox.Show(message, "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region AsyncRelayCommand

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    CommandManager.InvalidateRequerySuggested();
                    await _execute();
                }
                finally
                {
                    _isExecuting = false;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
    }

    #endregion
}
