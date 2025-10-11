using System;
using System.Collections.Generic;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Services
{
    #region Enums

    public enum TrendPeriod
    {
        Daily,
        Weekly,
        Monthly
    }

    public enum ProfitPeriod
    {
        Daily,
        Weekly,
        Monthly
    }

    public enum AlertLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum StockStatus
    {
        OutOfStock,
        Low,
        Medium,
        Good
    }

    #endregion

    #region Report Parameters

    public class SalesReportParameters
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? CustomerId { get; set; }
        public InvoiceStatus? Status { get; set; }
        public bool IncludeDetails { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
    }

    public class InventoryReportParameters
    {
        public int? CategoryId { get; set; }
        public bool ShowLowStockOnly { get; set; }
        public bool IncludeValuation { get; set; } = true;
        public DateTime? AsOfDate { get; set; }
    }

    public class CustomReportDefinition
    {
        public string ReportName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> SelectedFields { get; set; } = new();
        public List<ReportFilter> Filters { get; set; } = new();
        public List<ReportGrouping> Groupings { get; set; } = new();
        public List<ReportSorting> Sortings { get; set; } = new();
    }

    public class ReportFilter
    {
        public string FieldName { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty; // =, !=, >, <, >=, <=, LIKE, IN
        public object Value { get; set; } = new();
    }

    public class ReportGrouping
    {
        public string FieldName { get; set; } = string.Empty;
        public string AggregateFunction { get; set; } = string.Empty; // SUM, AVG, COUNT, MAX, MIN
    }

    public class ReportSorting
    {
        public string FieldName { get; set; } = string.Empty;
        public bool IsDescending { get; set; }
    }

    #endregion

    #region Sales Report Data

    public class SalesReportResult
    {
        public SalesReportParameters Parameters { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public int TotalInvoices { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalRemainingAmount { get; set; }
        public decimal AverageSaleAmount { get; set; }
        public List<SalesInvoice> Invoices { get; set; } = new();
        public List<DailySalesData> SalesByDay { get; set; } = new();
    }

    public class DailySalesData
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageAmount { get; set; }
    }

    public class SalesTrendData
    {
        public string Period { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal GrowthRate { get; set; } // نسبة النمو مقارنة بالفترة السابقة
    }

    public class TopSellingProductData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public int TransactionCount { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    public class CustomerSalesData
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageInvoiceAmount { get; set; }
        public DateTime LastInvoiceDate { get; set; }
        public string CustomerSegment { get; set; } = string.Empty; // VIP, Regular, New
    }

    #endregion

    #region Inventory Report Data

    public class InventoryReportResult
    {
        public InventoryReportParameters Parameters { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<ProductInventoryData> Products { get; set; } = new();
    }

    public class ProductInventoryData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal StockValue { get; set; }
        public StockStatus StockStatus { get; set; }
    }

    public class StockMovementData
    {
        public int MovementId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class LowStockAlertData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public AlertLevel AlertLevel { get; set; }
        public decimal SuggestedOrderQuantity { get; set; }
    }

    public class ProductValuationData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal TotalPurchaseValue { get; set; }
        public decimal TotalSaleValue { get; set; }
        public decimal PotentialProfit { get; set; }
    }

    #endregion

    #region Financial Report Data

    public class FinancialSummaryResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }

        // المبيعات
        public decimal TotalSales { get; set; }
        public int TotalSalesCount { get; set; }
        public decimal TotalSalesTax { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalAccountsReceivable { get; set; }

        // المشتريات
        public decimal TotalPurchases { get; set; }
        public int TotalPurchasesCount { get; set; }
        public decimal TotalPurchasesTax { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalAccountsPayable { get; set; }

        // الربحية
        public decimal GrossProfit { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMargin { get; set; }

        // النقدية
        public decimal CashInflow { get; set; }
        public decimal CashOutflow { get; set; }
        public decimal NetCashFlow { get; set; }
    }

    public class ProfitLossData
    {
        public string Period { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal Costs { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    public class CashFlowData
    {
        public DateTime Date { get; set; }
        public decimal Inflow { get; set; }
        public decimal Outflow { get; set; }
        public decimal NetFlow { get; set; }
        public decimal RunningBalance { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
    }

    public class TaxReportData
    {
        public string Period { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal SalesTax { get; set; }
        public decimal PurchaseAmount { get; set; }
        public decimal PurchaseTax { get; set; }
        public decimal NetTax { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmissionDate { get; set; }
    }

    #endregion

    #region Advanced Analytics Data

    public class PerformanceMetricsResult
    {
        public DateTime GeneratedAt { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal CustomerLifetimeValue { get; set; }
        public decimal InventoryTurnoverRatio { get; set; }
        public decimal GrossMarginPercent { get; set; }
        public decimal NetMarginPercent { get; set; }
        public decimal ReturnOnInvestment { get; set; }
        public int DaysSalesOutstanding { get; set; }
        public int DaysPayableOutstanding { get; set; }
        public decimal CashConversionCycle { get; set; }
        public List<KPIData> KeyPerformanceIndicators { get; set; } = new();
    }

    public class KPIData
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal TargetValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal PercentChange { get; set; }
        public string Status { get; set; } = string.Empty; // Good, Warning, Critical
        public string Unit { get; set; } = string.Empty;
    }

    public class SeasonalAnalysisData
    {
        public int Quarter { get; set; }
        public int Month { get; set; }
        public string Season { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal AverageMonthlySales { get; set; }
        public decimal SeasonalIndex { get; set; }
        public decimal GrowthRate { get; set; }
        public List<ProductSeasonalData> TopProducts { get; set; } = new();
    }

    public class ProductSeasonalData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal SeasonalIndex { get; set; }
    }

    public class ComparisonAnalysisResult
    {
        public DateTime Period1Start { get; set; }
        public DateTime Period1End { get; set; }
        public DateTime Period2Start { get; set; }
        public DateTime Period2End { get; set; }
        public DateTime GeneratedAt { get; set; }

        public ComparisonMetrics SalesComparison { get; set; } = new();
        public ComparisonMetrics ProfitComparison { get; set; } = new();
        public ComparisonMetrics CustomerComparison { get; set; } = new();
        public List<ProductComparisonData> ProductComparisons { get; set; } = new();
    }

    public class ComparisonMetrics
    {
        public decimal Period1Value { get; set; }
        public decimal Period2Value { get; set; }
        public decimal AbsoluteChange { get; set; }
        public decimal PercentChange { get; set; }
        public string TrendDirection { get; set; } = string.Empty; // Up, Down, Stable
    }

    public class ProductComparisonData
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Period1Sales { get; set; }
        public decimal Period2Sales { get; set; }
        public decimal SalesChange { get; set; }
        public decimal SalesChangePercent { get; set; }
    }

    #endregion

    #region Report Templates

    public class ReportTemplate
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TemplateContent { get; set; } = string.Empty; // JSON أو XML
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }
        public List<TemplateParameter> Parameters { get; set; } = new();
    }

    public class TemplateParameter
    {
        public string ParameterName { get; set; } = string.Empty;
        public string ParameterType { get; set; } = string.Empty; // String, Integer, Decimal, Date, Boolean
        public bool IsRequired { get; set; }
        public object? DefaultValue { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CustomReportResult
    {
        public string ReportName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public List<Dictionary<string, object>> Data { get; set; } = new();
        public List<string> ColumnHeaders { get; set; } = new();
        public Dictionary<string, object> Summary { get; set; } = new();
        public int TotalRecords { get; set; }
    }

    #endregion

    #region Chart Data

    public class ChartDataSet
    {
        public string Label { get; set; } = string.Empty;
        public List<decimal> Data { get; set; } = new();
        public List<string> Labels { get; set; } = new();
        public string ChartType { get; set; } = string.Empty; // Line, Bar, Pie, Doughnut
        public string BackgroundColor { get; set; } = string.Empty;
        public string BorderColor { get; set; } = string.Empty;
    }

    public class ChartConfiguration
    {
        public string Title { get; set; } = string.Empty;
        public string XAxisLabel { get; set; } = string.Empty;
        public string YAxisLabel { get; set; } = string.Empty;
        public bool ShowLegend { get; set; } = true;
        public bool ShowGridLines { get; set; } = true;
        public string Theme { get; set; } = "default";
        public List<ChartDataSet> DataSets { get; set; } = new();
    }

    #endregion
}