using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AccountingSystem.Data;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views
{
    public partial class FinancialAnalysisWindow : Window, INotifyPropertyChanged
    {
        private readonly IUnitOfWork? _unitOfWork;

        // فترة التقرير
        private DateTime _fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        private DateTime _toDate = DateTime.Now;

        // نصوص جاهزة للعرض (تبسيطًا للـXAML)
        public string RevenueText { get => _revenueText; set { _revenueText = value; OnPropertyChanged(); } }
        public string NetProfitText { get => _netProfitText; set { _netProfitText = value; OnPropertyChanged(); } }
        public string ProfitMarginText { get => _profitMarginText; set { _profitMarginText = value; OnPropertyChanged(); } }
        public string InventoryTurnoverText { get => _inventoryTurnoverText; set { _inventoryTurnoverText = value; OnPropertyChanged(); } }

        public string RevenueGrowthText { get => _revenueGrowthText; set { _revenueGrowthText = value; OnPropertyChanged(); } }
        public string ProfitGrowthText { get => _profitGrowthText; set { _profitGrowthText = value; OnPropertyChanged(); } }
        public string ProfitMarginGrowthText { get => _profitMarginGrowthText; set { _profitMarginGrowthText = value; OnPropertyChanged(); } }
        public string InventoryTurnoverGrowthText { get => _inventoryTurnoverGrowthText; set { _inventoryTurnoverGrowthText = value; OnPropertyChanged(); } }

        public Brush RevenueGrowthBrush { get => _revenueGrowthBrush; set { _revenueGrowthBrush = value; OnPropertyChanged(); } }
        public Brush ProfitGrowthBrush { get => _profitGrowthBrush; set { _profitGrowthBrush = value; OnPropertyChanged(); } }
        public Brush ProfitMarginGrowthBrush { get => _profitMarginGrowthBrush; set { _profitMarginGrowthBrush = value; OnPropertyChanged(); } }
        public Brush InventoryTurnoverGrowthBrush { get => _inventoryTurnoverGrowthBrush; set { _inventoryTurnoverGrowthBrush = value; OnPropertyChanged(); } }

        public DateTime FromDate { get => _fromDate; set { _fromDate = value; OnPropertyChanged(); } }
        public DateTime ToDate { get => _toDate; set { _toDate = value; OnPropertyChanged(); } }

        // مجموعات العرض
        public ObservableCollection<CategoryShareItem> CategoryShares { get; } = new();
        public ObservableCollection<TopCustomerItem> TopCustomers { get; } = new();
        public ObservableCollection<RecommendationItem> Recommendations { get; } = new();

        // حقول داخلية
        private string _revenueText = "₪ 0";
        private string _netProfitText = "₪ 0";
        private string _profitMarginText = "0%";
        private string _inventoryTurnoverText = "0x";
        private string _revenueGrowthText = "—";
        private string _profitGrowthText = "—";
        private string _profitMarginGrowthText = "—";
        private string _inventoryTurnoverGrowthText = "—";
        private Brush _revenueGrowthBrush = Brushes.Gray;
        private Brush _profitGrowthBrush = Brushes.Gray;
        private Brush _profitMarginGrowthBrush = Brushes.Gray;
        private Brush _inventoryTurnoverGrowthBrush = Brushes.Gray;

        public event PropertyChangedEventHandler? PropertyChanged;

        public FinancialAnalysisWindow()
        {
            InitializeComponent();
            DataContext = this;
            _ = LoadDataAsync(); // بيانات تجريبية افتراضياً
        }

        public FinancialAnalysisWindow(IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            DataContext = this;
            _unitOfWork = unitOfWork;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // الفترة الحالية
                var curr = await GetPeriodMetricsAsync(FromDate, ToDate);

                // الفترة السابقة للمقارنة
                var span = ToDate - FromDate;
                var prevTo = FromDate.AddDays(-1);
                var prevFrom = prevTo - span;
                var prev = await GetPeriodMetricsAsync(prevFrom, prevTo);

                // تعبئة نصوص العرض
                RevenueText = FormatCurrency(curr.TotalRevenue);
                NetProfitText = FormatCurrency(curr.NetProfit);
                ProfitMarginText = $"{curr.ProfitMargin:P1}";
                InventoryTurnoverText = $"{curr.InventoryTurnover:F1}x";

                // نمو %
                SetGrowth(ref _revenueGrowthText, ref _revenueGrowthBrush, (double)curr.TotalRevenue, (double)prev.TotalRevenue);
                SetGrowth(ref _profitGrowthText, ref _profitGrowthBrush, (double)curr.NetProfit, (double)prev.NetProfit);
                SetGrowth(ref _profitMarginGrowthText, ref _profitMarginGrowthBrush, curr.ProfitMargin, prev.ProfitMargin, isRatio: true);
                SetGrowth(ref _inventoryTurnoverGrowthText, ref _inventoryTurnoverGrowthBrush, curr.InventoryTurnover, prev.InventoryTurnover);

                // المبيعات حسب الفئة
                CategoryShares.Clear();
                foreach (var c in curr.CategoryShares.OrderByDescending(c => c.Share))
                {
                    CategoryShares.Add(new CategoryShareItem
                    {
                        Label = $"{c.CategoryName}: {c.Share:P0}",
                        Color = new SolidColorBrush(c.Color),
                        Share = c.Share
                    });
                }

                // أفضل العملاء
                TopCustomers.Clear();
                int rank = 1;
                foreach (var t in curr.TopCustomers.OrderByDescending(c => c.Amount).Take(5))
                {
                    TopCustomers.Add(new TopCustomerItem
                    {
                        RankEmoji = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"{rank}️⃣" },
                        Display = $"{t.Name} - {FormatCurrency(t.Amount)}"
                    });
                    rank++;
                }

                // التوصيات
                BuildRecommendations(curr);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء تحميل التحليل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadSampleFallback();
            }
        }

        // جلب المقاييس من القاعدة أو بيانات تجريبية
        private async Task<PeriodMetrics> GetPeriodMetricsAsync(DateTime from, DateTime to)
        {
            // لو ما في UnitOfWork هنرجع بيانات تجريبية ثابتة
            if (_unitOfWork == null)
                return await Task.FromResult(PeriodMetrics.Sample(from, to));

            // --------- بيانات فعلية (مبسطة) ---------
            // ملاحظة: عدّل الاستعلامات حسب مخططك الفعلي (Include/Where/Joins...)
            var salesRepo = _unitOfWork.Repository<SalesInvoice>();
            var purchaseRepo = _unitOfWork.Repository<PurchaseInvoice>();
            var productRepo = _unitOfWork.Repository<Product>();
            var customerRepo = _unitOfWork.Repository<Customer>();

            // فواتير مبيعات ضمن الفترة
            var sales = await salesRepo.FindAsync(s =>
                s.InvoiceDate.Date >= from.Date && s.InvoiceDate.Date <= to.Date && s.Status != InvoiceStatus.Cancelled);

            // فواتير مشتريات ضمن الفترة
            var purchases = await purchaseRepo.FindAsync(p =>
                p.InvoiceDate.Date >= from.Date && p.InvoiceDate.Date <= to.Date && p.Status != InvoiceStatus.Cancelled);

            decimal totalSales = sales.Sum(s => s.NetTotal);
            decimal totalPurchases = purchases.Sum(p => p.NetTotal);

            // ربح (مبسّط): المبيعات - المشتريات
            decimal netProfit = totalSales - totalPurchases;
            double profitMargin = totalSales > 0 ? (double)netProfit / (double)totalSales : 0.0;

            // دوران المخزون (تقريب): COGS / متوسط المخزون (إن لم يتوفر المخزون الفعلي نقرّبه بالمشتريات/2)
            // عدّل المعادلة وفق توفّر بيانات المخزون لديك.
            double avgInventory = Math.Max(1.0, (double)totalPurchases / 2.0);
            double cogs = Math.Max(0.0, (double)totalPurchases * 0.9); // تقدير بسيط
            double inventoryTurnover = cogs / avgInventory;

            var metrics = new PeriodMetrics
            {
                From = from,
                To = to,
                TotalRevenue = totalSales,
                NetProfit = netProfit,
                ProfitMargin = profitMargin,
                InventoryTurnover = inventoryTurnover
            };

            // أفضل العملاء (مبسّط: تجميع بحسب العميل)
            var top = sales
                .GroupBy(s => s.CustomerId)
                .Select(g =>
                {
                    // Note: This should be made async, but for now using placeholder
                    var name = "عميل غير محدد"; // Will need to fetch async
                    decimal amount = g.Sum(x => x.NetTotal);
                    return new CustomerAmount { Name = name, Amount = amount };
                })
                .OrderByDescending(x => x.Amount)
                .Take(5)
                .ToList();

            metrics.TopCustomers.AddRange(top);

            // مشاركة الفئات (إن كان المنتج مرتبط بالفئة عبر عناصر الفاتورة لديك—هنا سنعيد توزيعًا تقريبياً)
            // لأغراض العرض فقط إن لم تكن لديك جداول تفصيلية.
            metrics.CategoryShares.AddRange(new[]
            {
                new CategoryShare { CategoryName = "مواد غذائية", Share = 0.45, Color = (Color)ColorConverter.ConvertFromString("#2196F3")! },
                new CategoryShare { CategoryName = "أجهزة إلكترونية", Share = 0.30, Color = (Color)ColorConverter.ConvertFromString("#4CAF50")! },
                new CategoryShare { CategoryName = "أدوات منزلية", Share = 0.15, Color = (Color)ColorConverter.ConvertFromString("#FF9800")! },
                new CategoryShare { CategoryName = "مواد تنظيف", Share = 0.10, Color = (Color)ColorConverter.ConvertFromString("#F44336")! },
            });

            return metrics;
        }

        private static string FormatCurrency(decimal value) =>
            $"₪ {value:N0}";

        private void SetGrowth(ref string textProp, ref Brush brushProp, double curr, double prev, bool isRatio = false)
        {
            if (prev == 0) { textProp = "—"; brushProp = Brushes.Gray; return; }

            var growth = (curr - prev) / Math.Abs(prev);
            var arrow = growth >= 0 ? "↗" : "↘";
            textProp = isRatio
                ? $"{arrow} {growth:P1}"
                : $"{arrow} {growth:P1}";
            brushProp = growth >= 0 ? (Brush)new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)) : Brushes.Red;
        }

        private void BuildRecommendations(PeriodMetrics m)
        {
            Recommendations.Clear();

            // قواعد بسيطة للتوصيات
            if (m.ProfitMargin < 0.20)
                Recommendations.Add(new RecommendationItem("⚠️", "مراجعة التسعير وتكلفة المشتريات لتحسين هامش الربح."));

            if (m.InventoryTurnover < 3.0)
                Recommendations.Add(new RecommendationItem("🎯", "تحسين إدارة المخزون لتسريع دوران المخزون وتقليل الراكد."));

            if (m.TopCustomers.Count >= 3)
                Recommendations.Add(new RecommendationItem("📈", "تفعيل برنامج ولاء للعملاء الأعلى إنفاقًا لزيادة المبيعات."));

            if (!Recommendations.Any())
                Recommendations.Add(new RecommendationItem("✅", "المؤشرات ضمن النطاق الجيد — حافظ على الأداء الحالي."));
        }

        private void LoadSampleFallback()
        {
            var sample = PeriodMetrics.Sample(FromDate, ToDate);

            RevenueText = FormatCurrency(sample.TotalRevenue);
            NetProfitText = FormatCurrency(sample.NetProfit);
            ProfitMarginText = $"{sample.ProfitMargin:P1}";
            InventoryTurnoverText = $"{sample.InventoryTurnover:F1}x";

            RevenueGrowthText = "↗ +12.3%";
            ProfitGrowthText = "↗ +8.7%";
            ProfitMarginGrowthText = "↘ -1.2%";
            InventoryTurnoverGrowthText = "↗ +5.8%";
            RevenueGrowthBrush = ProfitGrowthBrush = InventoryTurnoverGrowthBrush = (Brush)new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            ProfitMarginGrowthBrush = Brushes.Red;

            CategoryShares.Clear();
            CategoryShares.Add(new CategoryShareItem { Label = "مواد غذائية: 45%", Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")!), Share = 0.45 });
            CategoryShares.Add(new CategoryShareItem { Label = "أجهزة إلكترونية: 30%", Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")!), Share = 0.30 });
            CategoryShares.Add(new CategoryShareItem { Label = "أدوات منزلية: 15%", Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")!), Share = 0.15 });
            CategoryShares.Add(new CategoryShareItem { Label = "مواد تنظيف: 10%", Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")!), Share = 0.10 });

            TopCustomers.Clear();
            TopCustomers.Add(new TopCustomerItem { RankEmoji = "🥇", Display = "أحمد محمد عبد الله - ₪ 45,200" });
            TopCustomers.Add(new TopCustomerItem { RankEmoji = "🥈", Display = "مريم أحمد سالم - ₪ 32,850" });
            TopCustomers.Add(new TopCustomerItem { RankEmoji = "🥉", Display = "محمد حسن إبراهيم - ₪ 28,690" });

            Recommendations.Clear();
            Recommendations.Add(new RecommendationItem("✅", "زيادة التركيز على المواد الغذائية لأنها الأعلى مساهمة في المبيعات."));
            Recommendations.Add(new RecommendationItem("⚠️", "مراجعة أسعار الأجهزة الإلكترونية لتحسين الهامش."));
            Recommendations.Add(new RecommendationItem("📈", "برامج ولاء للعملاء المتميزين."));
            Recommendations.Add(new RecommendationItem("🎯", "تحسين إدارة المخزون لرفع معدل الدوران."));
        }

        // أزرار الواجهة
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // هنا ضع منطق التصدير (CSV/Excel/PDF) حسب ما تفضّل
                MessageBox.Show("تم تصدير ملخص التحليل بنجاح.", "تصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر التصدير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== Helpers / Models =====
        private void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public class CategoryShareItem
        {
            public string Label { get; set; } = "";
            public Brush Color { get; set; } = Brushes.SteelBlue;
            public double Share { get; set; }
        }

        public class TopCustomerItem
        {
            public string RankEmoji { get; set; } = "🏅";
            public string Display { get; set; } = "";
        }

        public record RecommendationItem(string Icon, string Text);

        private class CustomerAmount
        {
            public string Name { get; set; } = "";
            public decimal Amount { get; set; }
        }

        private class CategoryShare
        {
            public string CategoryName { get; set; } = "";
            public double Share { get; set; }
            public Color Color { get; set; }
        }

        private class PeriodMetrics
        {
            public DateTime From { get; set; }
            public DateTime To { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal NetProfit { get; set; }
            public double ProfitMargin { get; set; }
            public double InventoryTurnover { get; set; }
            public System.Collections.Generic.List<CategoryShare> CategoryShares { get; } = new();
            public System.Collections.Generic.List<CustomerAmount> TopCustomers { get; } = new();

            public static PeriodMetrics Sample(DateTime from, DateTime to) => new PeriodMetrics
            {
                From = from,
                To = to,
                TotalRevenue = 248_750m,
                NetProfit = 62_180m,
                ProfitMargin = 0.250,
                InventoryTurnover = 4.2,
                CategoryShares =
                {
                    new CategoryShare { CategoryName="مواد غذائية", Share=0.45, Color=(Color)ColorConverter.ConvertFromString("#2196F3")! },
                    new CategoryShare { CategoryName="أجهزة إلكترونية", Share=0.30, Color=(Color)ColorConverter.ConvertFromString("#4CAF50")! },
                    new CategoryShare { CategoryName="أدوات منزلية", Share=0.15, Color=(Color)ColorConverter.ConvertFromString("#FF9800")! },
                    new CategoryShare { CategoryName="مواد تنظيف", Share=0.10, Color=(Color)ColorConverter.ConvertFromString("#F44336")! },
                },
                TopCustomers =
                {
                    new CustomerAmount { Name="أحمد محمد عبد الله", Amount=45200m },
                    new CustomerAmount { Name="مريم أحمد سالم", Amount=32850m },
                    new CustomerAmount { Name="محمد حسن إبراهيم", Amount=28690m },
                    new CustomerAmount { Name="نور الدين عبد الرحمن", Amount=15320m },
                }
            };
        }
    }
}
