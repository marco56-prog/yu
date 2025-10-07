using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;
using AccountingSystem.WPF.Helpers;
using System.Globalization;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة التقارير المتقدمة مع تحليلات شاملة ورسوم بيانية
    /// </summary>
    public interface IAdvancedReportsService
    {
        // تقارير المبيعات
        Task<SalesReportResult> GenerateSalesReportAsync(SalesReportParameters parameters);
        Task<List<SalesTrendData>> GetSalesTrendsAsync(DateTime startDate, DateTime endDate, TrendPeriod period);
        Task<List<TopSellingProductData>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int topCount = 10);
        Task<List<CustomerSalesData>> GetCustomerSalesAnalysisAsync(DateTime startDate, DateTime endDate);

        // تقارير المخزون
        Task<InventoryReportResult> GenerateInventoryReportAsync(InventoryReportParameters parameters);
        Task<List<StockMovementData>> GetStockMovementsAsync(DateTime startDate, DateTime endDate, int? productId = null);
        Task<List<LowStockAlertData>> GetLowStockAlertsAsync();
        Task<List<ProductValuationData>> GetInventoryValuationAsync(DateTime? asOfDate = null);

        // تقارير مالية
        Task<FinancialSummaryResult> GenerateFinancialSummaryAsync(DateTime startDate, DateTime endDate);
        Task<List<ProfitLossData>> GetProfitLossAnalysisAsync(DateTime startDate, DateTime endDate, ProfitPeriod period);
        Task<List<CashFlowData>> GetCashFlowAnalysisAsync(DateTime startDate, DateTime endDate);
        Task<List<TaxReportData>> GenerateTaxReportAsync(DateTime startDate, DateTime endDate);

        // تحليلات متقدمة
        Task<PerformanceMetricsResult> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate);
        Task<List<SeasonalAnalysisData>> GetSeasonalAnalysisAsync(int year);
        Task<ComparisonAnalysisResult> GetPeriodComparisonAsync(DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End);

        // تقارير مخصصة
        Task<CustomReportResult> GenerateCustomReportAsync(CustomReportDefinition definition);
        Task<List<ReportTemplate>> GetAvailableTemplatesAsync();
        Task SaveReportTemplateAsync(ReportTemplate template);
    }

    public class AdvancedReportsService : IAdvancedReportsService, INotifyPropertyChanged
    {
        private const string ComponentName = "AdvancedReportsService";
        private readonly AccountingDbContext _context;
        private readonly CultureInfo _culture;

        public AdvancedReportsService(AccountingDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _culture = new CultureInfo("ar-EG");
            _culture.NumberFormat.CurrencySymbol = "ج.م";

            ComprehensiveLogger.LogInfo("تم تهيئة خدمة التقارير المتقدمة", ComponentName);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        #region Sales Reports

        public async Task<SalesReportResult> GenerateSalesReportAsync(SalesReportParameters parameters)
        {
            try
            {
                ComprehensiveLogger.LogInfo($"بدء إنشاء تقرير المبيعات من {parameters.StartDate:yyyy-MM-dd} إلى {parameters.EndDate:yyyy-MM-dd}", ComponentName);

                var query = _context.SalesInvoices
                    .AsNoTracking()
                    .Include(s => s.Customer)
                    .Include(s => s.Items)
                        .ThenInclude(i => i.Product)
                    .Where(s => s.InvoiceDate >= parameters.StartDate && s.InvoiceDate <= parameters.EndDate);

                if (parameters.CustomerId.HasValue)
                    query = query.Where(s => s.CustomerId == parameters.CustomerId.Value);

                if (parameters.Status.HasValue)
                    query = query.Where(s => s.Status == parameters.Status.Value);

                var invoices = await query.ToListAsync();

                var result = new SalesReportResult
                {
                    Parameters = parameters,
                    GeneratedAt = DateTime.Now,
                    TotalInvoices = invoices.Count,
                    TotalSales = invoices.Sum(i => i.NetTotal),
                    TotalPaidAmount = invoices.Sum(i => i.PaidAmount),
                    TotalRemainingAmount = invoices.Sum(i => i.RemainingAmount),
                    AverageSaleAmount = invoices.Any() ? invoices.Average(i => i.NetTotal) : 0,
                    Invoices = invoices,
                    SalesByDay = invoices
                        .GroupBy(i => i.InvoiceDate.Date)
                        .Select(g => new DailySalesData
                        {
                            Date = g.Key,
                            TotalSales = g.Sum(i => i.NetTotal),
                            InvoiceCount = g.Count(),
                            AverageAmount = g.Average(i => i.NetTotal)
                        })
                        .OrderBy(d => d.Date)
                        .ToList()
                };

                ComprehensiveLogger.LogBusinessOperation("تم إنشاء تقرير المبيعات بنجاح", 
                    $"إجمالي الفواتير: {result.TotalInvoices}, إجمالي المبيعات: {result.TotalSales:C}", 
                    isSuccess: true);

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء تقرير المبيعات", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<SalesTrendData>> GetSalesTrendsAsync(DateTime startDate, DateTime endDate, TrendPeriod period)
        {
            try
            {
                var invoices = await _context.SalesInvoices
                    .AsNoTracking()
                    .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                    .ToListAsync();

                var trends = period switch
                {
                    TrendPeriod.Daily => invoices
                        .GroupBy(i => i.InvoiceDate.Date)
                        .Select(g => new SalesTrendData
                        {
                            Period = g.Key.ToString("yyyy-MM-dd"),
                            Date = g.Key,
                            TotalSales = g.Sum(i => i.NetTotal),
                            InvoiceCount = g.Count(),
                            AverageAmount = g.Average(i => i.NetTotal)
                        })
                        .OrderBy(t => t.Date)
                        .ToList(),

                    TrendPeriod.Weekly => invoices
                        .GroupBy(i => GetWeekOfYear(i.InvoiceDate))
                        .Select(g => new SalesTrendData
                        {
                            Period = $"أسبوع {g.Key}",
                            Date = startDate.AddDays((g.Key - 1) * 7),
                            TotalSales = g.Sum(i => i.NetTotal),
                            InvoiceCount = g.Count(),
                            AverageAmount = g.Average(i => i.NetTotal)
                        })
                        .OrderBy(t => t.Date)
                        .ToList(),

                    TrendPeriod.Monthly => invoices
                        .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                        .Select(g => new SalesTrendData
                        {
                            Period = $"{g.Key.Year}-{g.Key.Month:00}",
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            TotalSales = g.Sum(i => i.NetTotal),
                            InvoiceCount = g.Count(),
                            AverageAmount = g.Average(i => i.NetTotal)
                        })
                        .OrderBy(t => t.Date)
                        .ToList(),

                    _ => throw new ArgumentException("نوع الفترة غير صحيح", nameof(period))
                };

                ComprehensiveLogger.LogInfo($"تم إنشاء تحليل اتجاهات المبيعات - {period} - {trends.Count} نقطة بيانات", ComponentName);
                return trends;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في تحليل اتجاهات المبيعات", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<TopSellingProductData>> GetTopSellingProductsAsync(DateTime startDate, DateTime endDate, int topCount = 10)
        {
            try
            {
                var topProducts = await _context.SalesInvoiceItems
                    .AsNoTracking()
                    .Include(item => item.Product)
                    .Include(item => item.SalesInvoice)
                    .Where(item => item.SalesInvoice!.InvoiceDate >= startDate && 
                                  item.SalesInvoice.InvoiceDate <= endDate)
                    .GroupBy(item => new { item.ProductId, item.Product!.ProductName })
                    .Select(g => new TopSellingProductData
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName ?? "غير محدد",
                        TotalQuantitySold = g.Sum(item => item.Quantity),
                        TotalRevenue = g.Sum(item => item.NetAmount),
                        AveragePrice = g.Average(item => item.UnitPrice),
                        TransactionCount = g.Count()
                    })
                    .OrderByDescending(p => p.TotalRevenue)
                    .Take(topCount)
                    .ToListAsync();

                ComprehensiveLogger.LogInfo($"تم تحليل أفضل {topProducts.Count} منتجات مبيعاً", ComponentName);
                return topProducts;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في تحليل أفضل المنتجات مبيعاً", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<CustomerSalesData>> GetCustomerSalesAnalysisAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var customerAnalysis = await _context.SalesInvoices
                    .AsNoTracking()
                    .Include(s => s.Customer)
                    .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                    .GroupBy(s => new { s.CustomerId, s.Customer!.CustomerName })
                    .Select(g => new CustomerSalesData
                    {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = g.Key.CustomerName ?? "غير محدد",
                        TotalSales = g.Sum(s => s.NetTotal),
                        TotalPaid = g.Sum(s => s.PaidAmount),
                        TotalRemaining = g.Sum(s => s.RemainingAmount),
                        InvoiceCount = g.Count(),
                        AverageInvoiceAmount = g.Average(s => s.NetTotal),
                        LastInvoiceDate = g.Max(s => s.InvoiceDate)
                    })
                    .OrderByDescending(c => c.TotalSales)
                    .ToListAsync();

                ComprehensiveLogger.LogInfo($"تم تحليل بيانات {customerAnalysis.Count} عميل", ComponentName);
                return customerAnalysis;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في تحليل بيانات العملاء", ex, ComponentName);
                throw;
            }
        }

        #endregion

        #region Inventory Reports

        public async Task<InventoryReportResult> GenerateInventoryReportAsync(InventoryReportParameters parameters)
        {
            try
            {
                var query = _context.Products
                    .AsNoTracking()
                    .Include(p => p.MainUnit)
                    .AsQueryable();

                if (parameters.CategoryId.HasValue)
                    query = query.Where(p => p.CategoryId == parameters.CategoryId.Value);

                if (parameters.ShowLowStockOnly)
                    query = query.Where(p => p.CurrentStock <= p.MinimumStock);

                var products = await query.ToListAsync();

                var result = new InventoryReportResult
                {
                    Parameters = parameters,
                    GeneratedAt = DateTime.Now,
                    TotalProducts = products.Count,
                    TotalStockValue = products.Sum(p => p.CurrentStock * p.PurchasePrice),
                    LowStockCount = products.Count(p => p.CurrentStock <= p.MinimumStock),
                    OutOfStockCount = products.Count(p => p.CurrentStock <= 0),
                    Products = products.Select(p => new ProductInventoryData
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        ProductCode = p.ProductCode,
                        CurrentStock = p.CurrentStock,
                        MinimumStock = p.MinimumStock,
                        UnitName = p.MainUnit?.UnitName ?? "",
                        PurchasePrice = p.PurchasePrice,
                        SalePrice = p.SalePrice,
                        StockValue = p.CurrentStock * p.PurchasePrice,
                        StockStatus = GetStockStatus(p.CurrentStock, p.MinimumStock)
                    }).ToList()
                };

                ComprehensiveLogger.LogBusinessOperation("تم إنشاء تقرير المخزون بنجاح", 
                    $"إجمالي المنتجات: {result.TotalProducts}, قيمة المخزون: {result.TotalStockValue:C}", 
                    isSuccess: true);

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء تقرير المخزون", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<StockMovementData>> GetStockMovementsAsync(DateTime startDate, DateTime endDate, int? productId = null)
        {
            try
            {
                var query = _context.StockMovements
                    .AsNoTracking()
                    .Include(sm => sm.Product)
                    .Where(sm => sm.MovementDate >= startDate && sm.MovementDate <= endDate);

                if (productId.HasValue)
                    query = query.Where(sm => sm.ProductId == productId.Value);

                var movements = await query
                    .OrderByDescending(sm => sm.MovementDate)
                    .Select(sm => new StockMovementData
                    {
                        MovementId = sm.StockMovementId,
                        ProductId = sm.ProductId,
                        ProductName = sm.Product!.ProductName ?? "غير محدد",
                        MovementDate = sm.MovementDate,
                        MovementType = sm.MovementType.ToString(),
                        Quantity = sm.Quantity,
                        ReferenceNumber = sm.ReferenceNumber ?? "",
                        Notes = sm.Notes ?? ""
                    })
                    .ToListAsync();

                ComprehensiveLogger.LogInfo($"تم استعلام {movements.Count} حركة مخزون", ComponentName);
                return movements;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في استعلام حركات المخزون", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<LowStockAlertData>> GetLowStockAlertsAsync()
        {
            try
            {
                var alerts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.MainUnit)
                    .Where(p => p.CurrentStock <= p.MinimumStock)
                    .Select(p => new LowStockAlertData
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName ?? "غير محدد",
                        ProductCode = p.ProductCode ?? "",
                        CurrentStock = p.CurrentStock,
                        MinimumStock = p.MinimumStock,
                        UnitName = p.MainUnit!.UnitName ?? "",
                        AlertLevel = p.CurrentStock <= 0 ? AlertLevel.Critical : 
                                   p.CurrentStock <= p.MinimumStock * 0.5m ? AlertLevel.High : AlertLevel.Medium,
                        SuggestedOrderQuantity = Math.Max(p.MinimumStock * 2 - p.CurrentStock, 0)
                    })
                    .OrderBy(a => a.CurrentStock)
                    .ToListAsync();

                ComprehensiveLogger.LogInfo($"تم العثور على {alerts.Count} تنبيه مخزون منخفض", ComponentName);
                return alerts;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في استعلام تنبيهات المخزون المنخفض", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<ProductValuationData>> GetInventoryValuationAsync(DateTime? asOfDate = null)
        {
            try
            {
                var effectiveDate = asOfDate ?? DateTime.Now;

                var valuation = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.MainUnit)
                    .Select(p => new ProductValuationData
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName ?? "غير محدد",
                        ProductCode = p.ProductCode ?? "",
                        CurrentStock = p.CurrentStock,
                        UnitName = p.MainUnit!.UnitName ?? "",
                        PurchasePrice = p.PurchasePrice,
                        SalePrice = p.SalePrice,
                        TotalPurchaseValue = p.CurrentStock * p.PurchasePrice,
                        TotalSaleValue = p.CurrentStock * p.SalePrice,
                        PotentialProfit = (p.CurrentStock * p.SalePrice) - (p.CurrentStock * p.PurchasePrice)
                    })
                    .OrderByDescending(v => v.TotalPurchaseValue)
                    .ToListAsync();

                ComprehensiveLogger.LogInfo($"تم حساب تقييم المخزون لـ {valuation.Count} منتج بتاريخ {effectiveDate:yyyy-MM-dd}", ComponentName);
                return valuation;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في حساب تقييم المخزون", ex, ComponentName);
                throw;
            }
        }

        #endregion

        #region Financial Reports

        public async Task<FinancialSummaryResult> GenerateFinancialSummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var salesInvoices = await _context.SalesInvoices
                    .AsNoTracking()
                    .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                    .ToListAsync();

                var purchaseInvoices = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .Where(p => p.InvoiceDate >= startDate && p.InvoiceDate <= endDate)
                    .ToListAsync();

                var result = new FinancialSummaryResult
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.Now,
                    
                    // المبيعات
                    TotalSales = salesInvoices.Sum(s => s.NetTotal),
                    TotalSalesCount = salesInvoices.Count,
                    TotalSalesTax = salesInvoices.Sum(s => s.TaxAmount),
                    TotalCollected = salesInvoices.Sum(s => s.PaidAmount),
                    TotalAccountsReceivable = salesInvoices.Sum(s => s.RemainingAmount),
                    
                    // المشتريات
                    TotalPurchases = purchaseInvoices.Sum(p => p.NetTotal),
                    TotalPurchasesCount = purchaseInvoices.Count,
                    TotalPurchasesTax = purchaseInvoices.Sum(p => p.TaxAmount),
                    TotalPaid = purchaseInvoices.Sum(p => p.PaidAmount),
                    TotalAccountsPayable = purchaseInvoices.Sum(p => p.RemainingAmount),
                    
                    // الربحية
                    GrossProfit = salesInvoices.Sum(s => s.NetTotal) - purchaseInvoices.Sum(p => p.NetTotal),
                    NetProfit = (salesInvoices.Sum(s => s.NetTotal) - purchaseInvoices.Sum(p => p.NetTotal)) - 
                               (salesInvoices.Sum(s => s.TaxAmount) + purchaseInvoices.Sum(p => p.TaxAmount))
                };

                result.ProfitMargin = result.TotalSales > 0 ? (result.GrossProfit / result.TotalSales) * 100 : 0;

                ComprehensiveLogger.LogBusinessOperation("تم إنشاء الملخص المالي بنجاح", 
                    $"المبيعات: {result.TotalSales:C}, المشتريات: {result.TotalPurchases:C}, الربح: {result.GrossProfit:C}", 
                    isSuccess: true);

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء الملخص المالي", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<ProfitLossData>> GetProfitLossAnalysisAsync(DateTime startDate, DateTime endDate, ProfitPeriod period)
        {
            try
            {
                var salesInvoices = await _context.SalesInvoices
                    .AsNoTracking()
                    .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                    .ToListAsync();

                var purchaseInvoices = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .Where(p => p.InvoiceDate >= startDate && p.InvoiceDate <= endDate)
                    .ToListAsync();

                var profitLossData = period switch
                {
                    ProfitPeriod.Daily => GenerateDailyProfitLoss(salesInvoices, purchaseInvoices),
                    ProfitPeriod.Weekly => GenerateWeeklyProfitLoss(salesInvoices, purchaseInvoices),
                    ProfitPeriod.Monthly => GenerateMonthlyProfitLoss(salesInvoices, purchaseInvoices),
                    _ => throw new ArgumentException("نوع فترة الربح غير صحيح", nameof(period))
                };

                ComprehensiveLogger.LogInfo($"تم تحليل الربح والخسارة - {period} - {profitLossData.Count} فترة", ComponentName);
                return profitLossData;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في تحليل الربح والخسارة", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<CashFlowData>> GetCashFlowAnalysisAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var salesInvoices = await _context.SalesInvoices
                    .AsNoTracking()
                    .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                    .ToListAsync();

                var purchaseInvoices = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .Where(p => p.InvoiceDate >= startDate && p.InvoiceDate <= endDate)
                    .ToListAsync();

                var cashFlow = new List<CashFlowData>();
                var runningBalance = 0m;

                // دمج وترتيب المعاملات حسب التاريخ
                var allTransactions = new List<(DateTime Date, decimal Amount, string Type, string Description)>();

                foreach (var sale in salesInvoices)
                {
                    if (sale.PaidAmount > 0)
                    {
                        allTransactions.Add((sale.InvoiceDate, sale.PaidAmount, "مقبوضات", $"فاتورة بيع {sale.InvoiceNumber}"));
                    }
                }

                foreach (var purchase in purchaseInvoices)
                {
                    if (purchase.PaidAmount > 0)
                    {
                        allTransactions.Add((purchase.InvoiceDate, -purchase.PaidAmount, "مدفوعات", $"فاتورة شراء {purchase.InvoiceNumber}"));
                    }
                }

                foreach (var transaction in allTransactions.OrderBy(t => t.Date))
                {
                    runningBalance += transaction.Amount;
                    cashFlow.Add(new CashFlowData
                    {
                        Date = transaction.Date,
                        Inflow = transaction.Amount > 0 ? transaction.Amount : 0,
                        Outflow = transaction.Amount < 0 ? Math.Abs(transaction.Amount) : 0,
                        NetFlow = transaction.Amount,
                        RunningBalance = runningBalance,
                        Description = transaction.Description,
                        TransactionType = transaction.Type
                    });
                }

                ComprehensiveLogger.LogInfo($"تم تحليل التدفق النقدي - {cashFlow.Count} معاملة", ComponentName);
                return cashFlow;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في تحليل التدفق النقدي", ex, ComponentName);
                throw;
            }
        }

        public async Task<List<TaxReportData>> GenerateTaxReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var salesTax = await _context.SalesInvoices
                    .AsNoTracking()
                    .Where(s => s.InvoiceDate >= startDate && s.InvoiceDate <= endDate)
                    .GroupBy(s => s.InvoiceDate.Month)
                    .Select(g => new TaxReportData
                    {
                        Period = $"{g.First().InvoiceDate:yyyy-MM}",
                        Month = g.Key,
                        Year = g.First().InvoiceDate.Year,
                        SalesAmount = g.Sum(s => s.SubTotal),
                        SalesTax = g.Sum(s => s.TaxAmount),
                        PurchaseAmount = 0,
                        PurchaseTax = 0,
                        NetTax = g.Sum(s => s.TaxAmount)
                    })
                    .ToListAsync();

                var purchaseTax = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .Where(p => p.InvoiceDate >= startDate && p.InvoiceDate <= endDate)
                    .GroupBy(p => p.InvoiceDate.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Year = g.First().InvoiceDate.Year,
                        PurchaseAmount = g.Sum(p => p.SubTotal),
                        PurchaseTax = g.Sum(p => p.TaxAmount)
                    })
                    .ToListAsync();

                // دمج بيانات ضرائب المبيعات والمشتريات
                foreach (var purchase in purchaseTax)
                {
                    var existing = salesTax.FirstOrDefault(s => s.Month == purchase.Month && s.Year == purchase.Year);
                    if (existing != null)
                    {
                        existing.PurchaseAmount = purchase.PurchaseAmount;
                        existing.PurchaseTax = purchase.PurchaseTax;
                        existing.NetTax = existing.SalesTax - existing.PurchaseTax;
                    }
                    else
                    {
                        salesTax.Add(new TaxReportData
                        {
                            Period = $"{purchase.Year:yyyy}-{purchase.Month:00}",
                            Month = purchase.Month,
                            Year = purchase.Year,
                            SalesAmount = 0,
                            SalesTax = 0,
                            PurchaseAmount = purchase.PurchaseAmount,
                            PurchaseTax = purchase.PurchaseTax,
                            NetTax = -purchase.PurchaseTax
                        });
                    }
                }

                var result = salesTax.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList();

                ComprehensiveLogger.LogInfo($"تم إنشاء تقرير الضرائب - {result.Count} شهر", ComponentName);
                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل في إنشاء تقرير الضرائب", ex, ComponentName);
                throw;
            }
        }

        #endregion

        #region Advanced Analytics - في الجزء التالي

        public async Task<PerformanceMetricsResult> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate)
        {
            // سيتم تنفيذها في الجزء التالي
            await Task.Delay(1);
            return new PerformanceMetricsResult();
        }

        public async Task<List<SeasonalAnalysisData>> GetSeasonalAnalysisAsync(int year)
        {
            // سيتم تنفيذها في الجزء التالي
            await Task.Delay(1);
            return new List<SeasonalAnalysisData>();
        }

        public async Task<ComparisonAnalysisResult> GetPeriodComparisonAsync(DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End)
        {
            // سيتم تنفيذها في الجزء التالي
            await Task.Delay(1);
            return new ComparisonAnalysisResult();
        }

        public async Task<CustomReportResult> GenerateCustomReportAsync(CustomReportDefinition definition)
        {
            // سيتم تنفيذها في الجزء التالي
            await Task.Delay(1);
            return new CustomReportResult();
        }

        public async Task<List<ReportTemplate>> GetAvailableTemplatesAsync()
        {
            // سيتم تنفيذها في الجزء التالي
            await Task.Delay(1);
            return new List<ReportTemplate>();
        }

        public async Task SaveReportTemplateAsync(ReportTemplate template)
        {
            // سيتم تنفيذها في الجزء التالي
            await Task.Delay(1);
        }

        #endregion

        #region Helper Methods

        private static int GetWeekOfYear(DateTime date)
        {
            var culture = new CultureInfo("ar-EG");
            var calendar = culture.Calendar;
            return calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
        }

        private static StockStatus GetStockStatus(decimal currentStock, decimal minimumStock)
        {
            if (currentStock <= 0)
                return StockStatus.OutOfStock;
            if (currentStock <= minimumStock)
                return StockStatus.Low;
            if (currentStock <= minimumStock * 2)
                return StockStatus.Medium;
            return StockStatus.Good;
        }

        private static List<ProfitLossData> GenerateDailyProfitLoss(List<SalesInvoice> sales, List<PurchaseInvoice> purchases)
        {
            var result = new List<ProfitLossData>();
            var allDates = sales.Select(s => s.InvoiceDate.Date)
                .Union(purchases.Select(p => p.InvoiceDate.Date))
                .Distinct()
                .OrderBy(d => d);

            foreach (var date in allDates)
            {
                var daySales = sales.Where(s => s.InvoiceDate.Date == date).Sum(s => s.NetTotal);
                var dayPurchases = purchases.Where(p => p.InvoiceDate.Date == date).Sum(p => p.NetTotal);
                var dayProfit = daySales - dayPurchases;

                result.Add(new ProfitLossData
                {
                    Period = date.ToString("yyyy-MM-dd"),
                    Date = date,
                    Revenue = daySales,
                    Costs = dayPurchases,
                    GrossProfit = dayProfit,
                    ProfitMargin = daySales > 0 ? (dayProfit / daySales) * 100 : 0
                });
            }

            return result;
        }

        private static List<ProfitLossData> GenerateWeeklyProfitLoss(List<SalesInvoice> sales, List<PurchaseInvoice> purchases)
        {
            var result = new List<ProfitLossData>();
            var culture = new CultureInfo("ar-EG");

            var weeklyData = sales.Union(purchases.Cast<object>())
                .GroupBy(invoice => 
                {
                    var date = invoice is SalesInvoice s ? s.InvoiceDate : ((PurchaseInvoice)invoice).InvoiceDate;
                    return culture.Calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
                });

            foreach (var week in weeklyData)
            {
                var weekSales = sales.Where(s => culture.Calendar.GetWeekOfYear(s.InvoiceDate, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek) == week.Key).Sum(s => s.NetTotal);
                var weekPurchases = purchases.Where(p => culture.Calendar.GetWeekOfYear(p.InvoiceDate, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek) == week.Key).Sum(p => p.NetTotal);
                var weekProfit = weekSales - weekPurchases;

                result.Add(new ProfitLossData
                {
                    Period = $"أسبوع {week.Key}",
                    Date = DateTime.Now, // سيتم تحسينها لاحقاً
                    Revenue = weekSales,
                    Costs = weekPurchases,
                    GrossProfit = weekProfit,
                    ProfitMargin = weekSales > 0 ? (weekProfit / weekSales) * 100 : 0
                });
            }

            return result.OrderBy(r => r.Period).ToList();
        }

        private static List<ProfitLossData> GenerateMonthlyProfitLoss(List<SalesInvoice> sales, List<PurchaseInvoice> purchases)
        {
            var result = new List<ProfitLossData>();

            var monthlyData = sales.GroupBy(s => new { s.InvoiceDate.Year, s.InvoiceDate.Month })
                .Union(purchases.GroupBy(p => new { p.InvoiceDate.Year, p.InvoiceDate.Month }).Cast<IGrouping<object, object>>())
                .Select(g => g.Key)
                .Distinct();

            foreach (dynamic month in monthlyData)
            {
                var monthSales = sales.Where(s => s.InvoiceDate.Year == month.Year && s.InvoiceDate.Month == month.Month).Sum(s => s.NetTotal);
                var monthPurchases = purchases.Where(p => p.InvoiceDate.Year == month.Year && p.InvoiceDate.Month == month.Month).Sum(p => p.NetTotal);
                var monthProfit = monthSales - monthPurchases;

                result.Add(new ProfitLossData
                {
                    Period = $"{month.Year}-{month.Month:00}",
                    Date = new DateTime(month.Year, month.Month, 1),
                    Revenue = monthSales,
                    Costs = monthPurchases,
                    GrossProfit = monthProfit,
                    ProfitMargin = monthSales > 0 ? (monthProfit / monthSales) * 100 : 0
                });
            }

            return result.OrderBy(r => r.Date).ToList();
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}