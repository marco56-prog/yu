using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Models;

// نموذج بيانات لوحة التحكم
public class DashboardData
{
    // إحصائيات المبيعات
    public decimal TodaySales { get; set; }
    public decimal YesterdaySales { get; set; }
    public decimal MonthSales { get; set; }
    public decimal LastMonthSales { get; set; }
    public decimal YearSales { get; set; }

    // إحصائيات الفواتير
    public int TodayInvoicesCount { get; set; }
    public int MonthInvoicesCount { get; set; }
    public int PendingInvoicesCount { get; set; }
    public int OverdueInvoicesCount { get; set; }

    // إحصائيات المخزون
    public int TotalProductsCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int ExpiringProductsCount { get; set; }

    // إحصائيات العملاء
    public int TotalCustomersCount { get; set; }
    public int ActiveCustomersCount { get; set; }
    public int NewCustomersThisMonth { get; set; }

    // إحصائيات مالية
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public decimal CashBalance { get; set; }
    public decimal NetProfit { get; set; }

    // خصائص إضافية للداشبورد المحسن
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int TotalCustomers { get; set; }
    public int ActiveCustomersThisMonth { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal PendingPayments { get; set; }
    public int UnreadNotifications { get; set; }
    public int HighPriorityNotifications { get; set; }
    public decimal SalesGrowthRate { get; set; }
    public decimal CustomerGrowthRate { get; set; }
    public decimal ProfitMarginRate { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

// نموذج تحليل المبيعات
public class SalesAnalytics
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal TotalSales { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }

    public int InvoicesCount { get; set; }
    public decimal AverageInvoiceValue { get; set; }

    public List<MonthlySalesData> MonthlySales { get; set; } = new();
    public List<DailySalesData> DailySales { get; set; } = new();
    public List<ProductSalesData> TopSellingProducts { get; set; } = new();
    public List<CustomerSalesData> TopCustomers { get; set; } = new();
}

public class MonthlySalesData
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Sales { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public int InvoicesCount { get; set; }
}

public class DailySalesData
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public int InvoicesCount { get; set; }
}

public class ProductSalesData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Sales { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public int TimesOrdered { get; set; }
}

public class CustomerSalesData
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public decimal TotalProfit { get; set; }
    public int InvoicesCount { get; set; }
    public DateTime LastPurchaseDate { get; set; }
}

// نموذج تحليل المخزون
public class InventoryAnalytics
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    
    public decimal TotalInventoryValue { get; set; }
    public decimal TotalInventoryCost { get; set; }
    public decimal AverageStockLevel { get; set; }

    public List<CategoryStockData> CategoryAnalysis { get; set; } = new();
    public List<ProductMovementData> FastMovingProducts { get; set; } = new();
    public List<ProductMovementData> SlowMovingProducts { get; set; } = new();
    public List<LowStockData> LowStockItems { get; set; } = new();
}

public class CategoryStockData
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductsCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AverageStock { get; set; }
}

public class ProductMovementData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MovementFrequency { get; set; }
    public decimal AverageMonthlyMovement { get; set; }
    public DateTime LastMovementDate { get; set; }
}

public class LowStockData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal RecommendedOrder { get; set; }
    public string SupplierName { get; set; } = string.Empty;
}

// نموذج تحليل العملاء
public class CustomerAnalytics
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int NewCustomers { get; set; }
    
    public decimal TotalReceivables { get; set; }
    public decimal AverageCustomerValue { get; set; }
    
    public List<CustomerSegmentData> CustomerSegments { get; set; } = new();
    public List<CustomerSalesData> TopCustomers { get; set; } = new();
    public List<CustomerDebtData> HighDebtCustomers { get; set; } = new();
}

public class CustomerSegmentData
{
    public string SegmentName { get; set; } = string.Empty;
    public int CustomersCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageSales { get; set; }
    public decimal Percentage { get; set; }
}

public class CustomerDebtData
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalDebt { get; set; }
    public decimal OverdueAmount { get; set; }
    public int OverdueDays { get; set; }
    public DateTime LastPaymentDate { get; set; }
}

// نموذج تحليل الأرباح
public class ProfitAnalysis
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }

    public List<ProfitByPeriod> MonthlyProfit { get; set; } = new();
    public List<ProfitByCategory> CategoryProfit { get; set; } = new();
    public List<ProfitByProduct> ProductProfit { get; set; } = new();
}

public class ProfitByPeriod
{
    public DateTime Period { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal Margin { get; set; }
}

public class ProfitByCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal Margin { get; set; }
}

public class ProfitByProduct
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal Margin { get; set; }
}

// نموذج البيانات للرسوم البيانية
public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Color { get; set; }
    public DateTime? Date { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class ChartSeries
{
    public string Name { get; set; } = string.Empty;
    public List<ChartDataPoint> DataPoints { get; set; } = new();
    public string Color { get; set; } = "#2196F3";
    public string ChartType { get; set; } = "Line"; // Line, Bar, Pie, Area
}

// نموذج التقرير المخصص
public class CustomReport
{
    [Key]
    public int ReportId { get; set; }

    [Required, StringLength(100)]
    public required string ReportName { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    [Required, StringLength(50)]
    public required string ReportType { get; set; } // "Sales", "Inventory", "Financial", "Custom"

    [Required]
    public string QueryDefinition { get; set; } = string.Empty; // JSON للاستعلام

    [Required]
    public string ColumnDefinitions { get; set; } = string.Empty; // JSON للأعمدة

    public string? ChartDefinitions { get; set; } // JSON للرسوم البيانية

    public bool IsPublic { get; set; } = false;
    public bool IsActive { get; set; } = true;

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? LastRun { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // العلاقات - سنربط بـ Username بدلاً من UserId لتجنب مشاكل الأنواع
    // لا نضع ForeignKey هنا لتجنب المشاكل
    public virtual User? Creator { get; set; }
}