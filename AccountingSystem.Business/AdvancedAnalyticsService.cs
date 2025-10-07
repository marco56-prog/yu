using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business;

// واجهة خدمة التحليلات المتقدمة
public interface IAdvancedAnalyticsService
{
    Task<DashboardData> GetDashboardDataAsync();
    Task<SalesAnalytics> GetSalesAnalyticsAsync(DateTime fromDate, DateTime toDate);
    Task<InventoryAnalytics> GetInventoryAnalyticsAsync();
    Task<CustomerAnalytics> GetCustomerAnalyticsAsync();
    Task<ProfitAnalysis> CalculateProfitAnalysisAsync(DateTime fromDate, DateTime toDate);
    Task<List<ChartSeries>> GetSalesChartDataAsync(int months = 12);
    Task<List<ChartSeries>> GetInventoryChartDataAsync();
    Task<List<ChartSeries>> GetCustomerChartDataAsync();
    Task RefreshCacheAsync();
}

// تنفيذ خدمة التحليلات المتقدمة
public class AdvancedAnalyticsService : IAdvancedAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AccountingDbContext _context;

    public AdvancedAnalyticsService(IUnitOfWork unitOfWork, AccountingDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        // تحديد حدود الأيام/الأشهر لتُترجَم SQL بكفاءة (بدون .Date)
        var now = DateTime.Now;
        var todayStart = DateTime.Today;
        var tomorrowStart = todayStart.AddDays(1);

        var yesterdayStart = todayStart.AddDays(-1);
        var yesterdayEnd = todayStart;

        var monthStart = new DateTime(todayStart.Year, todayStart.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var lastMonthStart = monthStart.AddMonths(-1);
        var lastMonthEnd = monthStart;

        var yearStart = new DateTime(todayStart.Year, 1, 1);

        var postedInvoices = _context.SalesInvoices.AsNoTracking().Where(i => i.Status == InvoiceStatus.Posted);

        var dashboard = new DashboardData();

        // ===== إحصائيات المبيعات =====
        var todaySales = await postedInvoices
            .Where(x => x.InvoiceDate >= todayStart && x.InvoiceDate < tomorrowStart)
            .SumAsync(x => (decimal?)x.NetTotal) ?? 0m;

        var yesterdaySales = await postedInvoices
            .Where(x => x.InvoiceDate >= yesterdayStart && x.InvoiceDate < yesterdayEnd)
            .SumAsync(x => (decimal?)x.NetTotal) ?? 0m;

        var monthSales = await postedInvoices
            .Where(x => x.InvoiceDate >= monthStart && x.InvoiceDate < nextMonthStart)
            .SumAsync(x => (decimal?)x.NetTotal) ?? 0m;

        var lastMonthSales = await postedInvoices
            .Where(x => x.InvoiceDate >= lastMonthStart && x.InvoiceDate < lastMonthEnd)
            .SumAsync(x => (decimal?)x.NetTotal) ?? 0m;

        var yearSales = await postedInvoices
            .Where(x => x.InvoiceDate >= yearStart)
            .SumAsync(x => (decimal?)x.NetTotal) ?? 0m;

        dashboard.TodaySales = todaySales;
        dashboard.YesterdaySales = yesterdaySales;
        dashboard.MonthSales = monthSales;
        dashboard.LastMonthSales = lastMonthSales;
        dashboard.YearSales = yearSales;

        // ===== إحصائيات الفواتير =====
        dashboard.TodayInvoicesCount = await _context.SalesInvoices.AsNoTracking()
            .CountAsync(x => x.InvoiceDate >= todayStart && x.InvoiceDate < tomorrowStart);

        dashboard.MonthInvoicesCount = await _context.SalesInvoices.AsNoTracking()
            .CountAsync(x => x.InvoiceDate >= monthStart && x.InvoiceDate < nextMonthStart);

        dashboard.PendingInvoicesCount = await _context.SalesInvoices.AsNoTracking()
            .CountAsync(x => x.Status == InvoiceStatus.Draft);

        dashboard.OverdueInvoicesCount = await _context.SalesInvoices.AsNoTracking()
            .CountAsync(x => x.RemainingAmount > 0 && x.InvoiceDate < todayStart.AddDays(-30));

        // ===== إحصائيات المخزون =====
        dashboard.TotalProductsCount = await _context.Products.AsNoTracking()
            .CountAsync(x => x.IsActive);

        dashboard.LowStockCount = await _context.Products.AsNoTracking()
            .CountAsync(x => x.IsActive && x.CurrentStock <= x.MinimumStock);

        dashboard.OutOfStockCount = await _context.Products.AsNoTracking()
            .CountAsync(x => x.IsActive && x.CurrentStock <= 0);

        dashboard.ExpiringProductsCount = await _context.Products.AsNoTracking()
            .CountAsync(x => x.IsActive && x.HasExpiryDate && x.ExpiryDate <= todayStart.AddDays(30));

        // ===== إحصائيات العملاء =====
        dashboard.TotalCustomersCount = await _context.Customers.AsNoTracking()
            .CountAsync(x => x.IsActive);

        var activeSince = todayStart.AddMonths(-3);
        dashboard.ActiveCustomersCount = await _context.Customers.AsNoTracking()
            .CountAsync(x => x.IsActive && x.LastPurchaseDate >= activeSince);

        dashboard.NewCustomersThisMonth = await _context.Customers.AsNoTracking()
            .CountAsync(x => x.CreatedDate >= monthStart && x.CreatedDate < nextMonthStart);

        // ===== إحصائيات مالية =====
        dashboard.TotalReceivables = await _context.Customers.AsNoTracking()
            .Where(x => x.Balance > 0)
            .SumAsync(x => (decimal?)x.Balance) ?? 0m;

        dashboard.TotalPayables = await _context.Suppliers.AsNoTracking()
            .Where(x => x.Balance > 0)
            .SumAsync(x => (decimal?)x.Balance) ?? 0m;

        dashboard.CashBalance = await _context.CashBoxes.AsNoTracking()
            .SumAsync(x => (decimal?)x.CurrentBalance) ?? 0m;

        // ===== صافي الربح (تقريبي للشهر الحالي) =====
        var totalCost = await _context.SalesInvoiceItems.AsNoTracking()
            .Where(item => postedInvoices
                .Where(inv => inv.InvoiceDate >= monthStart && inv.InvoiceDate < nextMonthStart)
                .Select(inv => inv.SalesInvoiceId)
                .Contains(item.SalesInvoiceId))
            .SumAsync(x => (decimal?)(x.Quantity * x.Product.PurchasePrice)) ?? 0m;

        dashboard.NetProfit = monthSales - totalCost;
        dashboard.LastUpdated = now;

        return dashboard;
    }

    public async Task<SalesAnalytics> GetSalesAnalyticsAsync(DateTime fromDate, DateTime toDate)
    {
        // توحيد الحدود لتكون شاملة البداية وحصرية النهاية
        var start = fromDate;
        var end = toDate;

        var salesInvoices = await _context.SalesInvoices.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Include(x => x.Customer)
            .Where(x => x.InvoiceDate >= start && x.InvoiceDate <= end && x.Status == InvoiceStatus.Posted)
            .ToListAsync();

        var analytics = new SalesAnalytics
        {
            FromDate = fromDate,
            ToDate = toDate,
            TotalSales = salesInvoices.Sum(x => x.NetTotal),
            InvoicesCount = salesInvoices.Count
        };

        analytics.AverageInvoiceValue = analytics.InvoicesCount > 0
            ? analytics.TotalSales / analytics.InvoicesCount
            : 0m;

        // التكلفة والربح الإجمالي
        analytics.TotalCost = salesInvoices.SelectMany(x => x.Items)
            .Sum(item => item.Quantity * item.Product.PurchasePrice);

        analytics.GrossProfit = analytics.TotalSales - analytics.TotalCost;
        analytics.ProfitMargin = analytics.TotalSales > 0
            ? (analytics.GrossProfit / analytics.TotalSales) * 100
            : 0m;

        // شهريًا
        analytics.MonthlySales = salesInvoices
            .GroupBy(x => new { x.InvoiceDate.Year, x.InvoiceDate.Month })
            .Select(g => new MonthlySalesData
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
                Sales = g.Sum(x => x.NetTotal),
                Cost = g.SelectMany(x => x.Items).Sum(item => item.Quantity * item.Product.PurchasePrice),
                InvoicesCount = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        foreach (var month in analytics.MonthlySales)
            month.Profit = month.Sales - month.Cost;

        // يوميًا (آخر 30 يوم)
        var last30 = end.AddDays(-30);
        analytics.DailySales = salesInvoices
            .Where(x => x.InvoiceDate >= last30)
            .GroupBy(x => x.InvoiceDate.Date)
            .Select(g => new DailySalesData
            {
                Date = g.Key,
                Sales = g.Sum(x => x.NetTotal),
                Cost = g.SelectMany(x => x.Items).Sum(item => item.Quantity * item.Product.PurchasePrice),
                InvoicesCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        foreach (var day in analytics.DailySales)
            day.Profit = day.Sales - day.Cost;

        // أفضل المنتجات
        analytics.TopSellingProducts = salesInvoices
            .SelectMany(x => x.Items)
            .GroupBy(x => x.Product)
            .Select(g => new ProductSalesData
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ProductCode = g.Key.ProductCode,
                Quantity = g.Sum(x => x.Quantity),
                Sales = g.Sum(x => x.LineTotal),
                Cost = g.Sum(x => x.Quantity * g.Key.PurchasePrice),
                TimesOrdered = g.Count()
            })
            .OrderByDescending(x => x.Sales)
            .Take(20)
            .ToList();

        foreach (var p in analytics.TopSellingProducts)
            p.Profit = p.Sales - p.Cost;

        // أفضل العملاء
        analytics.TopCustomers = salesInvoices
            .GroupBy(x => x.Customer)
            .Select(g => new CustomerSalesData
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.CustomerName,
                TotalSales = g.Sum(x => x.NetTotal),
                InvoicesCount = g.Count(),
                LastPurchaseDate = g.Max(x => x.InvoiceDate)
            })
            .OrderByDescending(x => x.TotalSales)
            .Take(20)
            .ToList();

        return analytics;
    }

    public async Task<InventoryAnalytics> GetInventoryAnalyticsAsync()
    {
        var analytics = new InventoryAnalytics();

        // إحصائيات عامة
        analytics.TotalProducts = await _context.Products.AsNoTracking().CountAsync();
        analytics.ActiveProducts = await _context.Products.AsNoTracking().CountAsync(x => x.IsActive);
        analytics.LowStockProducts = await _context.Products.AsNoTracking().CountAsync(x => x.IsActive && x.CurrentStock <= x.MinimumStock);
        analytics.OutOfStockProducts = await _context.Products.AsNoTracking().CountAsync(x => x.IsActive && x.CurrentStock <= 0);

        // جلب المنتجات الفعّالة لحساب القيم
        var products = await _context.Products.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(p => new { p.CurrentStock, p.SalePrice, p.PurchasePrice })
            .ToListAsync();

        analytics.TotalInventoryValue = products.Sum(x => x.CurrentStock * x.SalePrice);
        analytics.TotalInventoryCost = products.Sum(x => x.CurrentStock * x.PurchasePrice);
        analytics.AverageStockLevel = products.Select(x => x.CurrentStock).DefaultIfEmpty(0).Average();

        // تحليل حسب الفئات
        analytics.CategoryAnalysis = await _context.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new CategoryStockData
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ProductsCount = c.Products.Count(p => p.IsActive),
                TotalValue = c.Products.Where(p => p.IsActive).Sum(p => p.CurrentStock * p.SalePrice),
                TotalCost = c.Products.Where(p => p.IsActive).Sum(p => p.CurrentStock * p.PurchasePrice),
                AverageStock = c.Products.Where(p => p.IsActive).Select(p => p.CurrentStock).DefaultIfEmpty(0).Average()
            })
            .ToListAsync();

        // المنتجات سريعة الحركة (آخر 3 شهور)
        var last3Months = DateTime.Now.AddMonths(-3);
        analytics.FastMovingProducts = await _context.SalesInvoiceItems.AsNoTracking()
            .Where(x => x.SalesInvoice.InvoiceDate >= last3Months && x.SalesInvoice.Status == InvoiceStatus.Posted)
            .GroupBy(x => x.Product)
            .Select(g => new ProductMovementData
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                ProductCode = g.Key.ProductCode,
                CurrentStock = g.Key.CurrentStock,
                MovementFrequency = g.Count(),
                AverageMonthlyMovement = g.Sum(x => x.Quantity) / 3m,
                LastMovementDate = g.Max(x => x.SalesInvoice.InvoiceDate)
            })
            .OrderByDescending(x => x.MovementFrequency)
            .Take(20)
            .ToListAsync();

        // المنتجات بطيئة الحركة (لم تُبع خلال 3 شهور)
        analytics.SlowMovingProducts = await _context.Products.AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => !_context.SalesInvoiceItems
                .Any(item => item.ProductId == x.ProductId &&
                             item.SalesInvoice.InvoiceDate >= last3Months &&
                             item.SalesInvoice.Status == InvoiceStatus.Posted))
            .Select(x => new ProductMovementData
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                ProductCode = x.ProductCode,
                CurrentStock = x.CurrentStock,
                MovementFrequency = 0,
                AverageMonthlyMovement = 0,
                LastMovementDate = DateTime.MinValue
            })
            .Take(20)
            .ToListAsync();

        // المنتجات منخفضة المخزون
        analytics.LowStockItems = await _context.Products.AsNoTracking()
            .Where(x => x.IsActive && x.CurrentStock <= x.MinimumStock)
            .Select(x => new LowStockData
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                ProductCode = x.ProductCode,
                CurrentStock = x.CurrentStock,
                MinimumStock = x.MinimumStock,
                RecommendedOrder = x.MinimumStock * 2, // توصية بسيطة
                SupplierName = "" // يحتاج ربط مع الموردين
            })
            .ToListAsync();

        return analytics;
    }

    public async Task<CustomerAnalytics> GetCustomerAnalyticsAsync()
    {
        var analytics = new CustomerAnalytics();
        var last3Months = DateTime.Now.AddMonths(-3);
        var lastMonth = DateTime.Now.AddMonths(-1);

        // إحصائيات عامة
        analytics.TotalCustomers = await _context.Customers.AsNoTracking().CountAsync();

        analytics.ActiveCustomers = await _context.Customers.AsNoTracking()
            .CountAsync(x => x.IsActive && x.LastPurchaseDate >= last3Months);

        analytics.NewCustomers = await _context.Customers.AsNoTracking()
            .CountAsync(x => x.CreatedDate >= lastMonth);

        analytics.TotalReceivables = await _context.Customers.AsNoTracking()
            .Where(x => x.Balance > 0)
            .SumAsync(x => (decimal?)x.Balance) ?? 0m;

        analytics.AverageCustomerValue = await _context.Customers.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.TotalPurchases)
            .DefaultIfEmpty(0m)
            .AverageAsync();

        // تقسيم العملاء (VIP/عادي/جديد) — حسب إجمالي المشتريات
        var customers = await _context.Customers.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(c => new
            {
                c.CustomerId,
                c.CustomerName,
                c.TotalPurchases,
                c.LastPurchaseDate,
                InvoicesCount = c.SalesInvoices.Count
            })
            .ToListAsync();

        var vipCustomers = customers.Where(c => c.TotalPurchases > 50000m).ToList();
        var regularCustomers = customers.Where(c => c.TotalPurchases > 10000m && c.TotalPurchases <= 50000m).ToList();
        var newCustomers = customers.Where(c => c.TotalPurchases <= 10000m).ToList();

        analytics.CustomerSegments = new List<CustomerSegmentData>
        {
            new()
            {
                SegmentName = "عملاء VIP",
                CustomersCount = vipCustomers.Count,
                TotalSales = vipCustomers.Sum(x => x.TotalPurchases),
                AverageSales = vipCustomers.Count > 0 ? vipCustomers.Average(x => x.TotalPurchases) : 0m,
                Percentage = customers.Count > 0 ? (decimal)vipCustomers.Count / customers.Count * 100m : 0m
            },
            new()
            {
                SegmentName = "عملاء عاديون",
                CustomersCount = regularCustomers.Count,
                TotalSales = regularCustomers.Sum(x => x.TotalPurchases),
                AverageSales = regularCustomers.Count > 0 ? regularCustomers.Average(x => x.TotalPurchases) : 0m,
                Percentage = customers.Count > 0 ? (decimal)regularCustomers.Count / customers.Count * 100m : 0m
            },
            new()
            {
                SegmentName = "عملاء جدد",
                CustomersCount = newCustomers.Count,
                TotalSales = newCustomers.Sum(x => x.TotalPurchases),
                AverageSales = newCustomers.Count > 0 ? newCustomers.Average(x => x.TotalPurchases) : 0m,
                Percentage = customers.Count > 0 ? (decimal)newCustomers.Count / customers.Count * 100m : 0m
            }
        };

        // أفضل العملاء
        analytics.TopCustomers = customers
            .OrderByDescending(x => x.TotalPurchases)
            .Take(20)
            .Select(x => new CustomerSalesData
            {
                CustomerId = x.CustomerId,
                CustomerName = x.CustomerName,
                TotalSales = x.TotalPurchases,
                InvoicesCount = x.InvoicesCount,
                LastPurchaseDate = x.LastPurchaseDate ?? DateTime.MinValue
            })
            .ToList();

        // العملاء مرتفعي الديون (Placeholder لمعادلة أكثر دقة لاحقًا)
        analytics.HighDebtCustomers = await _context.Customers.AsNoTracking()
            .Where(x => x.Balance > 0)
            .OrderByDescending(x => x.Balance)
            .Select(x => new CustomerDebtData
            {
                CustomerId = x.CustomerId,
                CustomerName = x.CustomerName,
                TotalDebt = x.Balance,
                OverdueAmount = x.Balance, // لاحقًا: حساب الاستحقاق الفعلي بناءً على آجال الفواتير
                OverdueDays = 0,           // لاحقًا
                LastPaymentDate = DateTime.MinValue // يحتاج ربط المدفوعات
            })
            .Take(20)
            .ToListAsync();

        return analytics;
    }

    public async Task<ProfitAnalysis> CalculateProfitAnalysisAsync(DateTime fromDate, DateTime toDate)
    {
        var start = fromDate;
        var end = toDate;

        var salesData = await _context.SalesInvoices.AsNoTracking()
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .Where(x => x.InvoiceDate >= start && x.InvoiceDate <= end && x.Status == InvoiceStatus.Posted)
            .ToListAsync();

        var analysis = new ProfitAnalysis
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        analysis.TotalRevenue = salesData.Sum(x => x.NetTotal);
        analysis.TotalCost = salesData.SelectMany(x => x.Items).Sum(item => item.Quantity * item.Product.PurchasePrice);
        analysis.GrossProfit = analysis.TotalRevenue - analysis.TotalCost;

        // مبدئيًا: صافي الربح = مجمل الربح (يمكن طرح المصروفات لاحقًا)
        analysis.NetProfit = analysis.GrossProfit;

        analysis.ProfitMargin = analysis.TotalRevenue > 0
            ? (analysis.GrossProfit / analysis.TotalRevenue) * 100m
            : 0m;

        analysis.MonthlyProfit = salesData
            .GroupBy(x => new { x.InvoiceDate.Year, x.InvoiceDate.Month })
            .Select(g => new ProfitByPeriod
            {
                Period = new DateTime(g.Key.Year, g.Key.Month, 1),
                Revenue = g.Sum(x => x.NetTotal),
                Cost = g.SelectMany(x => x.Items).Sum(item => item.Quantity * item.Product.PurchasePrice)
            })
            .OrderBy(x => x.Period)
            .ToList();

        foreach (var period in analysis.MonthlyProfit)
        {
            period.Profit = period.Revenue - period.Cost;
            period.Margin = period.Revenue > 0 ? (period.Profit / period.Revenue) * 100m : 0m;
        }

        return analysis;
    }

    public async Task<List<ChartSeries>> GetSalesChartDataAsync(int months = 12)
    {
        var startDate = DateTime.Now.AddMonths(-months);

        var salesData = await _context.SalesInvoices.AsNoTracking()
            .Where(x => x.InvoiceDate >= startDate && x.Status == InvoiceStatus.Posted)
            .GroupBy(x => new { x.InvoiceDate.Year, x.InvoiceDate.Month })
            .Select(g => new ChartDataPoint
            {
                Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                Value = g.Sum(x => x.NetTotal),
                Date = new DateTime(g.Key.Year, g.Key.Month, 1)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return new List<ChartSeries>
        {
            new()
            {
                Name = "المبيعات الشهرية",
                DataPoints = salesData,
                Color = "#2196F3",
                ChartType = "Line"
            }
        };
    }

    public async Task<List<ChartSeries>> GetInventoryChartDataAsync()
    {
        var categoryData = await _context.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new ChartDataPoint
            {
                Label = c.CategoryName,
                Value = c.Products.Where(p => p.IsActive).Sum(p => p.CurrentStock * p.SalePrice)
            })
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        return new List<ChartSeries>
        {
            new()
            {
                Name = "قيمة المخزون حسب الفئة",
                DataPoints = categoryData,
                Color = "#4CAF50",
                ChartType = "Pie"
            }
        };
    }

    public async Task<List<ChartSeries>> GetCustomerChartDataAsync()
    {
        // توزيع العملاء حسب إجمالي المشتريات (إجمالي العد فقط)
        var vipCount = await _context.Customers.AsNoTracking().CountAsync(x => x.TotalPurchases > 50000m);
        var regularCount = await _context.Customers.AsNoTracking().CountAsync(x => x.TotalPurchases > 10000m && x.TotalPurchases <= 50000m);
        var newCount = await _context.Customers.AsNoTracking().CountAsync(x => x.TotalPurchases <= 10000m);

        var customerSegments = new List<ChartDataPoint>
        {
            new() { Label = "عملاء VIP", Value = vipCount },
            new() { Label = "عملاء عاديون", Value = regularCount },
            new() { Label = "عملاء جدد", Value = newCount }
        };

        return new List<ChartSeries>
        {
            new()
            {
                Name = "تقسيم العملاء",
                DataPoints = customerSegments,
                Color = "#FF9800",
                ChartType = "Doughnut"
            }
        };
    }

    public async Task RefreshCacheAsync()
    {
        // Placeholder لتحديث أي ذاكرة مؤقتة مستقبلًا
        await Task.CompletedTask;
    }
}
