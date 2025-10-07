using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AccountingSystem.Business;
using AccountingSystem.WPF;

namespace AccountingSystem.WPF.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly ISalesInvoiceService _salesService;
        private readonly IProductService _productService;

        private decimal _todaySales;
        private int _todayInvoicesCount;
        private int _lowStockCount;
        private decimal _monthlyProfit;
        private bool _isLoading;

        public DashboardViewModel(ISalesInvoiceService salesService, IProductService productService)
        {
            _salesService = salesService;
            _productService = productService;
            
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            
            // تحميل البيانات عند التهيئة
            _ = Task.Run(LoadDataAsync);
        }

        public decimal TodaySales
        {
            get => _todaySales;
            set => SetProperty(ref _todaySales, value);
        }

        public int TodayInvoicesCount
        {
            get => _todayInvoicesCount;
            set => SetProperty(ref _todayInvoicesCount, value);
        }

        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        public decimal MonthlyProfit
        {
            get => _monthlyProfit;
            set => SetProperty(ref _monthlyProfit, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RefreshCommand { get; }

        public async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // تحميل بيانات المبيعات اليومية (آخر 7 أيام)
                var today = DateTime.Today;
                var startDate = today.AddDays(-7);
                
                var salesSummary = await _salesService.GetSalesSummaryAsync(startDate, today);
                TodaySales = salesSummary.TotalAmount;
                TodayInvoicesCount = salesSummary.InvoiceCount;

                // تحميل إجمالي الربح الشهري
                var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Local);
                var cogsSummary = await _salesService.GetCOGSSummaryAsync(monthStart, today);
                MonthlyProfit = cogsSummary; // COGS returns decimal directly

                // تحميل عدد المنتجات منخفضة المخزون
                LowStockCount = await _productService.GetLowStockCountAsync();
            }
            catch (Exception)
            {
                // في حالة الخطأ، استخدم قيم افتراضية
                TodaySales = 0;
                TodayInvoicesCount = 0;
                MonthlyProfit = 0;
                LowStockCount = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
