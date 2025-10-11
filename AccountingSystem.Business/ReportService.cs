using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using AccountingSystem.Data;
using AccountingSystem.Models;

namespace AccountingSystem.Business
{
    // =========================
    // نماذج تقارير المبيعات والمخزون والأرباح والعملاء
    // =========================
    public class SalesReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal NetSales { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageInvoiceValue { get; set; }
        public List<SalesInvoice> Invoices { get; set; } = new();
    }

    public class InventoryReport
    {
        public List<InventoryReportItem> Items { get; set; } = new();
        public decimal TotalInventoryValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
    }

    public class InventoryReportItem
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal InventoryValue { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsOutOfStock { get; set; }
    }

    public class ProfitReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCostOfGoodsSold { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal GrossProfitMargin { get; set; }
        public List<ProfitReportItem> Items { get; set; } = new();
    }

    public class ProfitReportItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    public class CustomerBalanceReport
    {
        public List<CustomerBalanceItem> Customers { get; set; } = new();
        public decimal TotalReceivables { get; set; }
        public decimal TotalPayables { get; set; }
        public decimal NetBalance { get; set; }
    }

    public class CustomerBalanceItem
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPayments { get; set; }
        public DateTime LastTransactionDate { get; set; }
    }

    // =========================
    // واجهة خدمة التقارير
    // =========================
    public interface IReportService
    {
        Task<SalesReport> GenerateSalesReportAsync(DateTime fromDate, DateTime toDate, int? customerId = null);
        Task<InventoryReport> GenerateInventoryReportAsync(int? categoryId = null, bool? lowStockOnly = null);
        Task<ProfitReport> GenerateProfitReportAsync(DateTime fromDate, DateTime toDate);
        Task<CustomerBalanceReport> GenerateCustomerBalanceReportAsync();
        Task<List<StockMovement>> GetStockMovementsReportAsync(DateTime fromDate, DateTime toDate, int? productId = null);
    }

    // =========================
    // تنفيذ خدمة التقارير
    // =========================
    public class ReportService : IReportService
    {
        private readonly AccountingDbContext _context;

        public ReportService(AccountingDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // توحيد مدى التاريخ ليكون [بداية اليوم.. بداية اليوم التالي)
        private static (DateTime From, DateTime ToExclusive) NormalizeRange(DateTime fromDate, DateTime toDate)
        {
            var from = fromDate.Date;
            var toExclusive = toDate.Date.AddDays(1);
            return (from, toExclusive);
        }

        public async Task<SalesReport> GenerateSalesReportAsync(DateTime fromDate, DateTime toDate, int? customerId = null)
        {
            var (from, toEx) = NormalizeRange(fromDate, toDate);

            var query = _context.SalesInvoices
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.Items).ThenInclude(d => d.Product)
                .Where(s => s.IsPosted && s.InvoiceDate >= from && s.InvoiceDate < toEx);

            if (customerId.HasValue)
                query = query.Where(s => s.CustomerId == customerId.Value);

            var invoices = await query
                .OrderByDescending(s => s.InvoiceDate)
                .ToListAsync();

            var totalSales = invoices.Sum(s => s.SubTotal);
            var totalTax = invoices.Sum(s => s.TaxAmount);
            var totalDiscount = invoices.Sum(s => s.DiscountAmount);
            var netSales = invoices.Sum(s => s.NetTotal);
            var count = invoices.Count;

            return new SalesReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                Invoices = invoices,
                InvoiceCount = count,
                TotalSales = totalSales,
                TotalTax = totalTax,
                TotalDiscount = totalDiscount,
                NetSales = netSales,
                AverageInvoiceValue = count > 0 ? Math.Round(netSales / count, 2) : 0
            };
        }

        public async Task<InventoryReport> GenerateInventoryReportAsync(int? categoryId = null, bool? lowStockOnly = null)
        {
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.MainUnit)
                .Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var products = await query
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            var items = products.Select(p =>
            {
                var purchasePrice = p.PurchasePrice < 0 ? 0 : p.PurchasePrice;
                var currentStock = p.CurrentStock < 0 ? 0 : p.CurrentStock;

                var item = new InventoryReportItem
                {
                    ProductId = p.ProductId,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    CategoryName = p.Category?.CategoryName ?? "غير محدد",
                    UnitName = p.MainUnit?.UnitName ?? "غير محدد",
                    CurrentStock = currentStock,
                    MinimumStock = Math.Max(0, p.MinimumStock),
                    PurchasePrice = purchasePrice,
                    SalePrice = Math.Max(0, p.SalePrice),
                    InventoryValue = currentStock * purchasePrice,
                };
                item.IsOutOfStock = item.CurrentStock <= 0;
                item.IsLowStock = !item.IsOutOfStock && item.CurrentStock <= item.MinimumStock;
                return item;
            }).ToList();

            if (lowStockOnly == true)
                items = items.Where(i => i.IsLowStock || i.IsOutOfStock).ToList();

            return new InventoryReport
            {
                Items = items,
                TotalInventoryValue = items.Sum(i => i.InventoryValue),
                LowStockCount = items.Count(i => i.IsLowStock),
                OutOfStockCount = items.Count(i => i.IsOutOfStock)
            };
        }

        public async Task<ProfitReport> GenerateProfitReportAsync(DateTime fromDate, DateTime toDate)
        {
            var (from, toEx) = NormalizeRange(fromDate, toDate);

            // نسحب تفاصيل الفواتير بشكل مسطح لتسهيل التجميع
            var flat = await _context.SalesInvoiceItems
                .AsNoTracking()
                .Include(d => d.SalesInvoice)
                .Include(d => d.Product)
                .Where(d => d.SalesInvoice.IsPosted &&
                            d.SalesInvoice.InvoiceDate >= from &&
                            d.SalesInvoice.InvoiceDate < toEx)
                .Select(d => new
                {
                    d.ProductId,
                    ProductName = d.Product != null ? d.Product.ProductName : null,
                    Quantity = d.Quantity,
                    // NOTE: نعتمد NetAmount كقيمة إيراد البند (متسقة مع كودك السابق)
                    Revenue = d.NetAmount,
                    Cost = d.Quantity * (d.Product != null ? (d.Product.PurchasePrice < 0 ? 0m : d.Product.PurchasePrice) : 0m)
                })
                .ToListAsync();

            var items = flat
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g =>
                {
                    var qty = g.Sum(x => x.Quantity);
                    var rev = g.Sum(x => x.Revenue);
                    var cogs = g.Sum(x => x.Cost);
                    var prof = rev - cogs;
                    return new ProfitReportItem
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName ?? "غير محدد",
                        QuantitySold = qty,
                        Revenue = rev,
                        CostOfGoodsSold = cogs,
                        Profit = prof,
                        ProfitMargin = rev > 0 ? Math.Round((prof / rev) * 100, 2) : 0
                    };
                })
                .OrderByDescending(i => i.Profit)
                .ToList();

            var totalRevenue = items.Sum(i => i.Revenue);
            var totalCogs = items.Sum(i => i.CostOfGoodsSold);
            var grossProfit = items.Sum(i => i.Profit);

            return new ProfitReport
            {
                FromDate = fromDate,
                ToDate = toDate,
                Items = items,
                TotalRevenue = totalRevenue,
                TotalCostOfGoodsSold = totalCogs,
                GrossProfit = grossProfit,
                GrossProfitMargin = totalRevenue > 0 ? Math.Round((grossProfit / totalRevenue) * 100, 2) : 0
            };
        }

        public async Task<CustomerBalanceReport> GenerateCustomerBalanceReportAsync()
        {
            // ملاحظة: AsNoTracking يحسّن أداء القراءات
            var customers = await _context.Customers
                .AsNoTracking()
                .Include(c => c.SalesInvoices)
                .Include(c => c.CustomerTransactions)
                .Where(c => c.IsActive)
                .ToListAsync();

            var items = customers.Select(c =>
            {
                var totalSales = (c.SalesInvoices?.Where(s => s.IsPosted).Sum(s => s.NetTotal)) ?? 0;
                // نفترض أن Expense = مدفوع من العميل (تحصيل)، و Income = مديونية/فاتورة
                var totalPayments = (c.CustomerTransactions?
                                        .Where(t => t.TransactionType == TransactionType.Expense)
                                        .Sum(t => t.Amount)) ?? 0;

                return new CustomerBalanceItem
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    Phone = c.Phone ?? "غير محدد",
                    Balance = c.Balance,
                    TotalSales = totalSales,
                    TotalPayments = totalPayments,
                    LastTransactionDate = c.CustomerTransactions?.Count > 0
                        ? c.CustomerTransactions.Max(t => t.TransactionDate)
                        : c.CreatedDate
                };
            }).OrderByDescending(i => i.Balance).ToList();

            var totalReceivables = items.Where(i => i.Balance > 0).Sum(i => i.Balance);
            var totalPayables = items.Where(i => i.Balance < 0).Sum(i => Math.Abs(i.Balance));
            var netBalance = items.Sum(i => i.Balance);

            return new CustomerBalanceReport
            {
                Customers = items,
                TotalReceivables = totalReceivables,
                TotalPayables = totalPayables,
                NetBalance = netBalance
            };
        }

        public async Task<List<StockMovement>> GetStockMovementsReportAsync(DateTime fromDate, DateTime toDate, int? productId = null)
        {
            var (from, toEx) = NormalizeRange(fromDate, toDate);

            var query = _context.StockMovements
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Unit)
                .Where(s => s.MovementDate >= from && s.MovementDate < toEx);

            if (productId.HasValue)
                query = query.Where(s => s.ProductId == productId.Value);

            return await query
                .OrderByDescending(s => s.MovementDate)
                .ThenByDescending(s => s.StockMovementId)
                .ToListAsync();
        }
    }
}
