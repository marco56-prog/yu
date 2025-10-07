using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    public interface IReportsService
    {
        Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime fromDate, DateTime toDate);
        Task<List<SalesReportItemDto>> GetSalesReportAsync(DateTime fromDate, DateTime toDate, SalesReportType reportType);
        Task<List<InventoryReportItemDto>> GetInventoryReportAsync();
        Task<List<ProfitReportItemDto>> GetProfitReportAsync(DateTime fromDate, DateTime toDate);
        Task<List<TopProductDto>> GetTopProductsAsync(DateTime fromDate, DateTime toDate, int topCount);

        // === جديدة ===
        Task<List<CustomerReportItemDto>> GetCustomerReportAsync(DateTime fromDate, DateTime toDate);
        Task<List<SupplierReportItemDto>> GetSupplierReportAsync(DateTime fromDate, DateTime toDate);
        Task<List<CashFlowReportItemDto>> GetCashFlowReportAsync(DateTime fromDate, DateTime toDate, bool monthly = true);
        Task<List<ExpenseReportItemDto>> GetExpensesReportAsync(DateTime fromDate, DateTime toDate, bool groupByCategory = true); // Stub لحين تزويد كيان المصروفات
    }

    public class ReportsService : IReportsService
    {
        private readonly AccountingDbContext _context;

        public ReportsService(AccountingDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            var salesInvoices = await _context.SalesInvoices
                .AsNoTracking()
                .Where(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate)
                .Select(s => new { s.NetTotal })
                .ToListAsync();

            var purchaseInvoices = await _context.PurchaseInvoices
                .AsNoTracking()
                .Where(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
                .Select(p => new { p.NetTotal })
                .ToListAsync();

            var totalSales = salesInvoices.Sum(s => s.NetTotal);
            var totalPurchases = purchaseInvoices.Sum(p => p.NetTotal);

            return new FinancialSummaryDto
            {
                TotalSales = totalSales,
                TotalPurchases = totalPurchases,
                NetProfit = totalSales - totalPurchases,
                TotalInvoices = salesInvoices.Count + purchaseInvoices.Count
            };
        }

        public async Task<List<SalesReportItemDto>> GetSalesReportAsync(DateTime fromDate, DateTime toDate, SalesReportType reportType)
        {
            var salesInvoices = await _context.SalesInvoices
                .AsNoTracking()
                .Include(s => s.Items)
                .Include(s => s.Customer)
                .Where(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate)
                .ToListAsync();

            var reportItems = salesInvoices.Select(invoice => new SalesReportItemDto
            {
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                CustomerName = invoice.Customer?.CustomerName ?? "عميل غير محدد",
                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount,
                DiscountAmount = invoice.DiscountAmount,
                NetTotal = invoice.NetTotal,
                ItemsCount = invoice.Items?.Count ?? 0,
                PaymentStatus = GetPaymentStatus(invoice)
            }).ToList();

            switch (reportType)
            {
                case SalesReportType.Daily:
                    return reportItems.OrderByDescending(r => r.InvoiceDate).ToList();

                case SalesReportType.Weekly:
                    return reportItems
                        .GroupBy(r => GetWeekOfYear(r.InvoiceDate))
                        .SelectMany(g => g.OrderByDescending(x => x.InvoiceDate))
                        .ToList();

                case SalesReportType.Monthly:
                    return reportItems
                        .GroupBy(r => new { r.InvoiceDate.Year, r.InvoiceDate.Month })
                        .SelectMany(g => g.OrderByDescending(x => x.InvoiceDate))
                        .ToList();

                default:
                    return reportItems.OrderByDescending(r => r.InvoiceDate).ToList();
            }
        }

        public async Task<List<InventoryReportItemDto>> GetInventoryReportAsync()
        {
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.MainUnit)
                .ToListAsync();

            var inventoryItems = products.Select(product => new InventoryReportItemDto
            {
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                CategoryName = product.Category?.CategoryName ?? "غير محدد",
                UnitName = product.MainUnit?.UnitName ?? "قطعة",
                CurrentStock = product.CurrentStock,
                MinimumStock = product.MinimumStock,
                PurchasePrice = product.PurchasePrice,
                SalePrice = product.SalePrice,
                StockValue = product.CurrentStock * product.PurchasePrice,
                Status = GetStockStatus(product)
            }).OrderBy(i => i.ProductName).ToList();

            return inventoryItems;
        }

        public async Task<List<ProfitReportItemDto>> GetProfitReportAsync(DateTime fromDate, DateTime toDate)
        {
            var salesByDate = await _context.SalesInvoices
                .AsNoTracking()
                .Where(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate)
                .GroupBy(s => s.InvoiceDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(x => x.NetTotal),
                    ItemsSold = g.SelectMany(x => x.Items).Sum(item => item.Quantity)
                })
                .ToListAsync();

            var purchasesByDate = await _context.PurchaseInvoices
                .AsNoTracking()
                .Where(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
                .GroupBy(p => p.InvoiceDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Purchases = g.Sum(x => x.NetTotal)
                })
                .ToListAsync();

            var profitItems = new List<ProfitReportItemDto>();

            var allDates = salesByDate.Select(s => s.Date)
                .Union(purchasesByDate.Select(p => p.Date))
                .OrderBy(d => d);

            foreach (var date in allDates)
            {
                var salesData = salesByDate.FirstOrDefault(s => s.Date == date);
                var purchaseData = purchasesByDate.FirstOrDefault(p => p.Date == date);

                var revenue = salesData?.Revenue ?? 0;
                var cogs = purchaseData?.Purchases ?? 0; // تبسيط: تكلفة = مشتريات (لو عندك جدول تكلفة مباشر عدّله)
                var grossProfit = revenue - cogs;
                var expenses = 0m; // يمكن ربطها بجدول المصروفات لاحقًا
                var netProfit = grossProfit - expenses;

                profitItems.Add(new ProfitReportItemDto
                {
                    Date = date,
                    Period = GetPeriodName(date),
                    Revenue = revenue,
                    CostOfGoodsSold = cogs,
                    GrossProfit = grossProfit,
                    Expenses = expenses,
                    NetProfit = netProfit,
                    ProfitMargin = revenue > 0 ? (netProfit / revenue) * 100 : 0
                });
            }

            return profitItems.OrderByDescending(p => p.Date).ToList();
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(DateTime fromDate, DateTime toDate, int topCount)
        {
            var list = await _context.SalesInvoiceItems
                .AsNoTracking()
                .Include(i => i.Product)
                .Include(i => i.SalesInvoice)
                .Where(item => item.SalesInvoice != null &&
                               item.SalesInvoice.InvoiceDate >= fromDate &&
                               item.SalesInvoice.InvoiceDate <= toDate &&
                               item.Product != null)
                .GroupBy(item => new { item.ProductId, item.Product!.ProductCode, item.Product!.ProductName, item.Product!.PurchasePrice })
                .Select(g => new TopProductDto
                {
                    Rank = 0,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalSalesValue = g.Sum(x => x.NetAmount),
                    TotalProfit = g.Sum(x => x.NetAmount - (x.Quantity * g.Key.PurchasePrice)),
                    SalesTransactions = g.Count(),
                    AveragePrice = g.Average(x => x.UnitPrice)
                })
                .OrderByDescending(p => p.TotalSalesValue)
                .Take(topCount)
                .ToListAsync();

            for (int i = 0; i < list.Count; i++)
                list[i].Rank = i + 1;

            return list;
        }

        // =========================
        // جديدة: تقرير العملاء
        // =========================
        public async Task<List<CustomerReportItemDto>> GetCustomerReportAsync(DateTime fromDate, DateTime toDate)
        {
            var invoices = await _context.SalesInvoices
                .AsNoTracking()
                .Include(s => s.Customer)
                .Where(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate)
                .ToListAsync();

            var grouped = invoices
                .GroupBy(i => new { i.CustomerId, Name = i.Customer != null ? i.Customer.CustomerName : "غير محدد" })
                .Select(g => new
                {
                    g.Key.CustomerId,
                    g.Key.Name,
                    TotalSales = g.Sum(x => x.NetTotal),
                    Paid = g.Sum(x => x.PaidAmount),
                    Outstanding = g.Sum(x => x.NetTotal - x.PaidAmount),
                    InvoicesCount = g.Count(),
                    LastPurchase = g.Max(x => (DateTime?)x.InvoiceDate)
                })
                .ToList();

            // جلب الرصيد الحالي من جدول العملاء
            var customerBalances = await _context.Customers
                .AsNoTracking()
                .Where(c => grouped.Select(x => x.CustomerId).Contains(c.CustomerId))
                .Select(c => new { c.CustomerId, c.Balance })
                .ToListAsync();

            var mapBalance = customerBalances.ToDictionary(x => x.CustomerId, x => x.Balance);

            var result = grouped.Select(x => new CustomerReportItemDto
            {
                CustomerId = x.CustomerId,
                CustomerName = x.Name,
                TotalSales = x.TotalSales,
                PaidAmount = x.Paid,
                OutstandingAmount = x.Outstanding,
                InvoicesCount = x.InvoicesCount,
                LastPurchaseDate = x.LastPurchase,
                CurrentBalance = (x.CustomerId != 0 && mapBalance.TryGetValue(x.CustomerId, out var bal)) ? bal : 0m
            })
            .OrderByDescending(x => x.TotalSales)
            .ToList();

            return result;
        }

        // =========================
        // جديدة: تقرير المورّدين
        // =========================
        public async Task<List<SupplierReportItemDto>> GetSupplierReportAsync(DateTime fromDate, DateTime toDate)
        {
            var invoices = await _context.PurchaseInvoices
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Where(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
                .ToListAsync();

            var grouped = invoices
                .GroupBy(i => new { i.SupplierId, Name = i.Supplier != null ? i.Supplier.SupplierName : "غير محدد" })
                .Select(g => new
                {
                    g.Key.SupplierId,
                    g.Key.Name,
                    TotalPurchases = g.Sum(x => x.NetTotal),
                    Paid = g.Sum(x => x.PaidAmount),
                    Outstanding = g.Sum(x => x.NetTotal - x.PaidAmount),
                    InvoicesCount = g.Count(),
                    LastPurchase = g.Max(x => (DateTime?)x.InvoiceDate)
                })
                .ToList();

            var supplierBalances = await _context.Suppliers
                .AsNoTracking()
                .Where(s => grouped.Select(x => x.SupplierId).Contains(s.SupplierId))
                .Select(s => new { s.SupplierId, s.Balance })
                .ToListAsync();

            var mapBalance = supplierBalances.ToDictionary(x => x.SupplierId, x => x.Balance);

            var result = grouped.Select(x => new SupplierReportItemDto
            {
                SupplierId = x.SupplierId,
                SupplierName = x.Name,
                TotalPurchases = x.TotalPurchases,
                PaidAmount = x.Paid,
                OutstandingAmount = x.Outstanding,
                InvoicesCount = x.InvoicesCount,
                LastPurchaseDate = x.LastPurchase,
                CurrentBalance = (x.SupplierId != 0 && mapBalance.TryGetValue(x.SupplierId, out var bal)) ? bal : 0m
            })
            .OrderByDescending(x => x.TotalPurchases)
            .ToList();

            return result;
        }

        // =========================
        // جديدة: تقرير التدفق النقدي (مبسّط)
        // =========================
        public async Task<List<CashFlowReportItemDto>> GetCashFlowReportAsync(DateTime fromDate, DateTime toDate, bool monthly = true)
        {
            // تبسيط منطقي: التدفق الداخل = PaidAmount من فواتير المبيعات، الخارج = PaidAmount من فواتير المشتريات
            var sales = await _context.SalesInvoices
                .AsNoTracking()
                .Where(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate)
                .Select(s => new { s.InvoiceDate, s.PaidAmount })
                .ToListAsync();

            var purchases = await _context.PurchaseInvoices
                .AsNoTracking()
                .Where(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
                .Select(p => new { p.InvoiceDate, p.PaidAmount })
                .ToListAsync();

            var salesGroups = monthly
                ? sales.GroupBy(s => new { s.InvoiceDate.Year, s.InvoiceDate.Month })
                       .Select(g => new { Key = new DateTime(g.Key.Year, g.Key.Month, 1), CashIn = g.Sum(x => x.PaidAmount) })
                : sales.GroupBy(s => s.InvoiceDate.Date)
                       .Select(g => new { Key = g.Key, CashIn = g.Sum(x => x.PaidAmount) });

            var purchaseGroups = monthly
                ? purchases.GroupBy(p => new { p.InvoiceDate.Year, p.InvoiceDate.Month })
                           .Select(g => new { Key = new DateTime(g.Key.Year, g.Key.Month, 1), CashOut = g.Sum(x => x.PaidAmount) })
                : purchases.GroupBy(p => p.InvoiceDate.Date)
                           .Select(g => new { Key = g.Key, CashOut = g.Sum(x => x.PaidAmount) });

            var inflow = salesGroups.ToDictionary(x => x.Key, x => x.CashIn);
            var outflow = purchaseGroups.ToDictionary(x => x.Key, x => x.CashOut);

            var allPeriods = inflow.Keys.Union(outflow.Keys).OrderBy(d => d).ToList();

            var result = new List<CashFlowReportItemDto>();
            foreach (var dt in allPeriods)
            {
                var cashIn = inflow.TryGetValue(dt, out var ci) ? ci : 0m;
                var cashOut = outflow.TryGetValue(dt, out var co) ? co : 0m;

                result.Add(new CashFlowReportItemDto
                {
                    Date = dt,
                    Period = monthly ? dt.ToString("yyyy/MM", CultureInfo.InvariantCulture) : dt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
                    CashIn = cashIn,
                    CashOut = cashOut,
                    Net = cashIn - cashOut
                });
            }

            return result.OrderBy(r => r.Date).ToList();
        }

        // =========================
        // جديدة: تقرير المصروفات (Stub حتى تبعتلي كيان المصروفات)
        // =========================
        public Task<List<ExpenseReportItemDto>> GetExpensesReportAsync(DateTime fromDate, DateTime toDate, bool groupByCategory = true)
        {
            // لما تبعتلي كيان المصروفات (مثلاً Expense/ExpenseCategory) هكمّل الاستعلام بالتجميع.
            // مؤقتًا بنرجّع قائمة فاضية علشان ما نكسرش التطبيق.
            return Task.FromResult(new List<ExpenseReportItemDto>());
        }

        #region Helper Methods

        private static string GetPaymentStatus(SalesInvoice invoice)
        {
            if (invoice.PaidAmount >= invoice.NetTotal)
                return "مدفوعة بالكامل";
            else if (invoice.PaidAmount > 0)
                return "مدفوعة جزئياً";
            else
                return "غير مدفوعة";
        }

        private static string GetStockStatus(Product product)
        {
            if (product.CurrentStock <= 0)
                return "نفدت الكمية";
            else if (product.CurrentStock <= product.MinimumStock)
                return "كمية منخفضة";
            else
                return "كمية جيدة";
        }

        private static int GetWeekOfYear(DateTime date)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;
            return calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Sunday);
        }

        private static string GetPeriodName(DateTime date)
        {
            return date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }

        #endregion
    }

    // =========================
    // DTOs الجديدة للتقارير المضافة
    // =========================

    public class CustomerReportItemDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public decimal TotalSales { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public int InvoicesCount { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    public class SupplierReportItemDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = "";
        public decimal TotalPurchases { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public int InvoicesCount { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    public class CashFlowReportItemDto
    {
        public DateTime Date { get; set; }
        public string Period { get; set; } = "";
        public decimal CashIn { get; set; }
        public decimal CashOut { get; set; }
        public decimal Net { get; set; }
    }

    public class ExpenseReportItemDto
    {
        public DateTime Date { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
    }
}
