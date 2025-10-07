using System;
using System.Collections.Generic;

namespace AccountingSystem.Models
{
    #region Report Base Classes

    /// <summary>
    /// فئة أساسية للتقارير
    /// </summary>
    public abstract class BaseReport
    {
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public int GeneratedByUserId { get; set; }
    }

    /// <summary>
    /// فئة أساسية للتقارير الفترية
    /// </summary>
    public abstract class PeriodReport : BaseReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PeriodDays => (EndDate - StartDate).Days + 1;
    }

    /// <summary>
    /// فئة أساسية للتقارير في تاريخ محدد
    /// </summary>
    public abstract class PointInTimeReport : BaseReport
    {
        public DateTime AsOfDate { get; set; }
    }

    #endregion

    #region Financial Reports

    /// <summary>
    /// التقرير المالي الموجز
    /// </summary>
    public class FinancialSummaryReport : PeriodReport
    {
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalCash { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageInvoiceValue => InvoiceCount > 0 ? TotalSales / InvoiceCount : 0;
    }

    /// <summary>
    /// تقرير الأرباح والخسائر
    /// </summary>
    public class ProfitLossReport : PeriodReport
    {
        public RevenueSection Revenue { get; set; } = new();
        public CostOfGoodsSoldSection CostOfGoodsSold { get; set; } = new();
        public decimal GrossProfit { get; set; }
        public OperatingExpensesSection OperatingExpenses { get; set; } = new();
        public decimal NetProfit { get; set; }
        public decimal GrossProfitMargin => Revenue.TotalRevenue > 0 ? (GrossProfit / Revenue.TotalRevenue) * 100 : 0;
        public decimal NetProfitMargin => Revenue.TotalRevenue > 0 ? (NetProfit / Revenue.TotalRevenue) * 100 : 0;
    }

    /// <summary>
    /// قسم الإيرادات
    /// </summary>
    public class RevenueSection
    {
        public decimal SalesRevenue { get; set; }
        public decimal ServiceRevenue { get; set; }
        public decimal OtherRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// قسم تكلفة البضاعة المباعة
    /// </summary>
    public class CostOfGoodsSoldSection
    {
        public decimal BeginningInventory { get; set; }
        public decimal Purchases { get; set; }
        public decimal EndingInventory { get; set; }
        public decimal TotalCostOfGoodsSold { get; set; }
    }

    /// <summary>
    /// قسم المصروفات التشغيلية
    /// </summary>
    public class OperatingExpensesSection
    {
        public decimal Salaries { get; set; }
        public decimal Rent { get; set; }
        public decimal Utilities { get; set; }
        public decimal Marketing { get; set; }
        public decimal OtherExpenses { get; set; }
        public decimal TotalOperatingExpenses { get; set; }
    }

    /// <summary>
    /// تقرير الميزانية العمومية
    /// </summary>
    public class BalanceSheetReport : PointInTimeReport
    {
        public AssetsSection Assets { get; set; } = new();
        public LiabilitiesSection Liabilities { get; set; } = new();
        public EquitySection Equity { get; set; } = new();
        public bool IsBalanced => Math.Abs(Assets.TotalAssets - (Liabilities.TotalLiabilities + Equity.TotalEquity)) < 0.01m;
    }

    /// <summary>
    /// قسم الأصول
    /// </summary>
    public class AssetsSection
    {
        // الأصول المتداولة
        public decimal Cash { get; set; }
        public decimal AccountsReceivable { get; set; }
        public decimal Inventory { get; set; }
        public decimal OtherCurrentAssets { get; set; }
        public decimal TotalCurrentAssets => Cash + AccountsReceivable + Inventory + OtherCurrentAssets;

        // الأصول الثابتة
        public decimal FixedAssets { get; set; }
        public decimal AccumulatedDepreciation { get; set; }
        public decimal NetFixedAssets => FixedAssets - AccumulatedDepreciation;

        public decimal TotalAssets => TotalCurrentAssets + NetFixedAssets;
    }

    /// <summary>
    /// قسم الخصوم
    /// </summary>
    public class LiabilitiesSection
    {
        // الخصوم المتداولة
        public decimal AccountsPayable { get; set; }
        public decimal ShortTermDebt { get; set; }
        public decimal AccruedExpenses { get; set; }
        public decimal OtherCurrentLiabilities { get; set; }
        public decimal TotalCurrentLiabilities => AccountsPayable + ShortTermDebt + AccruedExpenses + OtherCurrentLiabilities;

        // الخصوم طويلة الأجل
        public decimal LongTermDebt { get; set; }
        public decimal OtherLongTermLiabilities { get; set; }
        public decimal TotalLongTermLiabilities => LongTermDebt + OtherLongTermLiabilities;

        public decimal TotalLiabilities => TotalCurrentLiabilities + TotalLongTermLiabilities;
    }

    /// <summary>
    /// قسم حقوق الملكية
    /// </summary>
    public class EquitySection
    {
        public decimal OwnerEquity { get; set; }
        public decimal RetainedEarnings { get; set; }
        public decimal CurrentYearEarnings { get; set; }
        public decimal TotalEquity => OwnerEquity + RetainedEarnings + CurrentYearEarnings;
    }

    /// <summary>
    /// تقرير التدفق النقدي
    /// </summary>
    public class CashFlowReport : PeriodReport
    {
        public decimal BeginningCash { get; set; }
        public decimal CashFromOperations { get; set; }
        public decimal CashFromInvesting { get; set; }
        public decimal CashFromFinancing { get; set; }
        public decimal NetCashFlow { get; set; }
        public decimal EndingCash { get; set; }

        public List<CashFlowItem> OperatingActivities { get; set; } = new();
        public List<CashFlowItem> InvestingActivities { get; set; } = new();
        public List<CashFlowItem> FinancingActivities { get; set; } = new();
    }

    /// <summary>
    /// عنصر التدفق النقدي
    /// </summary>
    public class CashFlowItem
    {
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsInflow { get; set; }
    }

    #endregion

    #region Sales and Purchase Reports

    /// <summary>
    /// تقرير تحليل المبيعات
    /// </summary>
    public class SalesAnalysisReport : PeriodReport
    {
        public decimal TotalSales { get; set; }
        public int TotalInvoices { get; set; }
        public decimal AverageInvoiceValue { get; set; }
        public List<ReportProductSalesData> ProductSales { get; set; } = new();
        public List<ReportDailySalesData> DailySales { get; set; } = new();
        public List<ReportMonthlySalesData> MonthlySales { get; set; } = new();
    }

    /// <summary>
    /// بيانات مبيعات المنتج للتقارير
    /// </summary>
    public class ReportProductSalesData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Revenue { get; set; }
        public decimal AveragePrice => Quantity > 0 ? Revenue / Quantity : 0;
        public decimal RevenuePercentage { get; set; }
    }

    /// <summary>
    /// بيانات المبيعات اليومية للتقارير
    /// </summary>
    public class ReportDailySalesData
    {
        public DateTime Date { get; set; }
        public decimal Sales { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageInvoiceValue => InvoiceCount > 0 ? Sales / InvoiceCount : 0;
    }

    /// <summary>
    /// بيانات المبيعات الشهرية للتقارير
    /// </summary>
    public class ReportMonthlySalesData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public int InvoiceCount { get; set; }
        public decimal GrowthPercentage { get; set; }
    }

    /// <summary>
    /// تقرير المنتجات الأكثر مبيعاً
    /// </summary>
    public class TopProductsReport : PeriodReport
    {
        public int TopCount { get; set; }
        public List<TopProductData> TopProducts { get; set; } = new();
    }

    /// <summary>
    /// بيانات المنتج الأعلى مبيعاً
    /// </summary>
    public class TopProductData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public int TransactionCount { get; set; }
        public decimal AveragePrice => QuantitySold > 0 ? Revenue / QuantitySold : 0;
        public decimal AverageQuantityPerTransaction => TransactionCount > 0 ? QuantitySold / TransactionCount : 0;
    }

    /// <summary>
    /// تقرير تحليل العملاء
    /// </summary>
    public class CustomerAnalysisReport : PeriodReport
    {
        public int TotalCustomers { get; set; }
        public decimal AveragePurchasePerCustomer { get; set; }
        public ReportCustomerSalesData? TopCustomer { get; set; }
        public List<ReportCustomerSalesData> CustomerSales { get; set; } = new();
    }

    /// <summary>
    /// بيانات مبيعات العميل للتقارير
    /// </summary>
    public class ReportCustomerSalesData
    {
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalPurchases { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime LastPurchaseDate { get; set; }
        public decimal AverageInvoiceValue => InvoiceCount > 0 ? TotalPurchases / InvoiceCount : 0;
        public int DaysSinceLastPurchase => (DateTime.Now - LastPurchaseDate).Days;
    }

    /// <summary>
    /// تقرير تحليل الموردين
    /// </summary>
    public class SupplierAnalysisReport : PeriodReport
    {
        public int TotalSuppliers { get; set; }
        public decimal AveragePurchasePerSupplier { get; set; }
        public SupplierPurchaseData? TopSupplier { get; set; }
        public List<SupplierPurchaseData> SupplierPurchases { get; set; } = new();
    }

    /// <summary>
    /// بيانات مشتريات المورد
    /// </summary>
    public class SupplierPurchaseData
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal TotalPurchases { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime LastPurchaseDate { get; set; }
        public decimal AverageInvoiceValue => InvoiceCount > 0 ? TotalPurchases / InvoiceCount : 0;
        public int DaysSinceLastPurchase => (DateTime.Now - LastPurchaseDate).Days;
    }

    #endregion

    #region Inventory Reports

    /// <summary>
    /// تقرير تقييم المخزون
    /// </summary>
    public class InventoryValuationReport : PointInTimeReport
    {
        public decimal TotalInventoryValue { get; set; }
        public int TotalProducts { get; set; }
        public decimal AverageProductValue { get; set; }
        public List<InventoryValuationItem> InventoryItems { get; set; } = new();
        public List<CategoryValuation> CategoryBreakdown { get; set; } = new();
    }

    /// <summary>
    /// عنصر تقييم المخزون
    /// </summary>
    public class InventoryValuationItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public decimal ValuePercentage { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// تقييم الفئة
    /// </summary>
    public class CategoryValuation
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal ValuePercentage { get; set; }
    }

    /// <summary>
    /// تقرير حركة المخزون
    /// </summary>
    public class StockMovementReport : PeriodReport
    {
        public int TotalMovements { get; set; }
        public decimal TotalInQuantity { get; set; }
        public decimal TotalOutQuantity { get; set; }
        public decimal NetMovement { get; set; }
        public List<StockMovementData> Movements { get; set; } = new();
        public List<ProductMovementSummary> ProductSummaries { get; set; } = new();
    }

    /// <summary>
    /// بيانات حركة المخزون
    /// </summary>
    public class StockMovementData
    {
        public DateTime Date { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string MovedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// ملخص حركة المنتج
    /// </summary>
    public class ProductMovementSummary
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal NetMovement { get; set; }
        public int MovementCount { get; set; }
    }

    /// <summary>
    /// تقرير المخزون المنخفض
    /// </summary>
    public class LowStockReport : BaseReport
    {
        public int CriticalStockCount { get; set; }
        public int LowStockCount { get; set; }
        public decimal EstimatedReorderCost { get; set; }
        public List<LowStockItem> LowStockItems { get; set; } = new();
    }

    /// <summary>
    /// عنصر المخزون المنخفض
    /// </summary>
    public class LowStockItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal SuggestedOrderQuantity { get; set; }
        public decimal EstimatedCost { get; set; }
        public string Priority { get; set; } = string.Empty; // Critical, Low, Normal
        public string Unit { get; set; } = string.Empty;
    }

    /// <summary>
    /// تقرير المنتجات منتهية الصلاحية
    /// </summary>
    public class ExpiryReport : BaseReport
    {
        public int DaysAhead { get; set; }
        public int ExpiredCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public decimal ExpiredValue { get; set; }
        public decimal ExpiringSoonValue { get; set; }
        public List<ExpiryItem> ExpiringItems { get; set; } = new();
    }

    /// <summary>
    /// عنصر انتهاء الصلاحية
    /// </summary>
    public class ExpiryItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal UnitValue { get; set; }
        public decimal TotalValue { get; set; }
        public string Status { get; set; } = string.Empty; // Expired, Critical, Warning, Normal
    }

    #endregion

    #region Performance Reports

    /// <summary>
    /// تقرير مقاييس الأداء
    /// </summary>
    public class PerformanceMetricsReport : PeriodReport
    {
        public decimal AverageDailyInvoices { get; set; }
        public decimal AverageInvoiceValue { get; set; }
        public decimal InventoryTurnoverRatio { get; set; }
        public decimal GrossProfitMargin { get; set; }
        public decimal NetProfitMargin { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public decimal SalesGrowthRate { get; set; }
        public List<KPIMetric> KPIs { get; set; } = new();
    }

    /// <summary>
    /// مقياس أداء رئيسي
    /// </summary>
    public class KPIMetric
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Target { get; set; }
        public decimal PreviousPeriodValue { get; set; }
        public decimal VarianceFromTarget => Target > 0 ? ((Value - Target) / Target) * 100 : 0;
        public decimal VarianceFromPrevious => PreviousPeriodValue > 0 ? ((Value - PreviousPeriodValue) / PreviousPeriodValue) * 100 : 0;
        public string Status { get; set; } = string.Empty; // Excellent, Good, Fair, Poor
    }

    /// <summary>
    /// تقرير نشاط المستخدمين
    /// </summary>
    public class UserActivityReport : PeriodReport
    {
        public int TotalUsers { get; set; }
        public int TotalActions { get; set; }
        public decimal AverageActionsPerUser { get; set; }
        public List<UserActivityData> UserActivities { get; set; } = new();
        public Dictionary<string, int> OperationSummary { get; set; } = new();
    }

    /// <summary>
    /// بيانات نشاط المستخدم
    /// </summary>
    public class UserActivityData
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int TotalActions { get; set; }
        public int LoginCount { get; set; }
        public DateTime LastActivity { get; set; }
        public Dictionary<string, int> OperationBreakdown { get; set; } = new();
        public TimeSpan TotalActiveTime { get; set; }
        public decimal ProductivityScore { get; set; }
    }

    #endregion

    #region Custom Reports

    /// <summary>
    /// طلب تقرير مخصص
    /// </summary>
    public class CustomReportRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SqlQuery { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<ReportColumn> Columns { get; set; } = new();
        public List<ReportFilter> Filters { get; set; } = new();
        public ReportGrouping? Grouping { get; set; }
        public List<ReportSorting> Sorting { get; set; } = new();
    }

    /// <summary>
    /// عمود التقرير
    /// </summary>
    public class ReportColumn
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
        public string Format { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    /// <summary>
    /// فلتر التقرير
    /// </summary>
    public class ReportFilter
    {
        public string ColumnName { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty; // =, !=, >, <, >=, <=, LIKE, IN
        public object Value { get; set; } = new();
        public string LogicalOperator { get; set; } = "AND"; // AND, OR
    }

    /// <summary>
    /// تجميع التقرير
    /// </summary>
    public class ReportGrouping
    {
        public List<string> GroupByColumns { get; set; } = new();
        public Dictionary<string, string> Aggregations { get; set; } = new(); // Column -> Function (SUM, COUNT, AVG, etc.)
    }

    /// <summary>
    /// ترتيب التقرير
    /// </summary>
    public class ReportSorting
    {
        public string ColumnName { get; set; } = string.Empty;
        public string Direction { get; set; } = "ASC"; // ASC, DESC
        public int Order { get; set; }
    }

    /// <summary>
    /// نتيجة التقرير المخصص
    /// </summary>
    public class CustomReportResult : BaseReport
    {
        public string ReportName { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public List<ReportColumn> Columns { get; set; } = new();
        public int TotalRows { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }

    #endregion

    #region Export and Format Options

    /// <summary>
    /// خيارات تصدير التقرير
    /// </summary>
    public class ReportExportOptions
    {
        public ExportFormat Format { get; set; } = ExportFormat.Excel;
        public string FileName { get; set; } = string.Empty;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeSummary { get; set; } = true;
        public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
        public PageSize PageSize { get; set; } = PageSize.A4;
    }

    /// <summary>
    /// تنسيق التصدير
    /// </summary>
    public enum ExportFormat
    {
        Excel,
        PDF,
        CSV,
        JSON,
        XML,
        HTML
    }

    /// <summary>
    /// اتجاه الصفحة
    /// </summary>
    public enum PageOrientation
    {
        Portrait,
        Landscape
    }

    /// <summary>
    /// حجم الصفحة
    /// </summary>
    public enum PageSize
    {
        A4,
        A3,
        Letter,
        Legal
    }

    #endregion

    #region Advanced Reports Models

    /// <summary>
    /// تقرير مؤشرات الأداء الرئيسية
    /// </summary>
    public class KPIDashboardReport : BaseReport
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalSalesRevenue { get; set; }
        public decimal TotalPurchaseCost { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<Product> TopSellingProducts { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
        public decimal SalesGrowthRate { get; set; }
    }

    /// <summary>
    /// تقرير تحليل الربحية
    /// </summary>
    public class ProfitabilityAnalysisReport : BaseReport
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public List<ProductProfitability> ProductProfitability { get; set; } = new();
        public List<CustomerProfitability> CustomerProfitability { get; set; } = new();
    }

    /// <summary>
    /// تقرير حركة المخزون التفصيلية
    /// </summary>
    public class DetailedInventoryMovementReport : BaseReport
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int? ProductId { get; set; }
        public decimal TotalInbound { get; set; }
        public decimal TotalOutbound { get; set; }
        public decimal NetMovement { get; set; }
        public List<MovementDetail> MovementDetails { get; set; } = new();
    }

    /// <summary>
    /// تفاصيل حركة المخزون
    /// </summary>
    public class MovementDetail
    {
        public DateTime Date { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// تقرير تقييم المخزون
    /// </summary>
    public class InventoryValuationReportAdvanced : BaseReport
    {
        public DateTime AsOfDate { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public int TotalItems { get; set; }
        public List<InventoryValuationItemAdvanced> ValuationItems { get; set; } = new();
    }

    /// <summary>
    /// عنصر تقييم المخزون
    /// </summary>
    public class InventoryValuationItemAdvanced
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public decimal LastPurchasePrice { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// تقرير أداء العملاء
    /// </summary>
    public class CustomerPerformanceReport : BaseReport
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public List<CustomerMetrics> TopCustomers { get; set; } = new();
        public List<CustomerMetrics> CustomerMetrics { get; set; } = new();
    }

    /// <summary>
    /// مقاييس أداء العميل
    /// </summary>
    public class CustomerMetrics
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime LastOrderDate { get; set; }
        public decimal OutstandingBalance { get; set; }
    }

    /// <summary>
    /// ربحية المنتج
    /// </summary>
    public class ProductProfitability
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    /// <summary>
    /// ربحية العميل
    /// </summary>
    public class CustomerProfitability
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    #endregion
}