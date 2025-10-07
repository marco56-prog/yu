using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Business
{
    /// <summary>
    /// خدمة التقارير المتقدمة مع تحسينات الأداء وتحليلات شاملة
    /// </summary>
    public interface IAdvancedReportsService
    {
        Task<KPIDashboardReport> GenerateKPIDashboardAsync(DateTime fromDate, DateTime toDate);
        Task<ProfitabilityAnalysisReport> GenerateProfitabilityAnalysisAsync(DateTime fromDate, DateTime toDate);
        Task<DetailedInventoryMovementReport> GenerateDetailedInventoryMovementAsync(DateTime fromDate, DateTime toDate, int? productId = null);
        Task<InventoryValuationReportAdvanced> GenerateInventoryValuationAsync(DateTime asOfDate);
        Task<CustomerPerformanceReport> GenerateCustomerPerformanceAsync(DateTime fromDate, DateTime toDate);
    }

    /// <summary>
    /// تنفيذ خدمة التقارير المتقدمة
    /// </summary>
    public class AdvancedReportsService : IAdvancedReportsService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdvancedReportsService> _logger;

        private static readonly Action<ILogger, string, Exception?> LogReportGeneration =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(1001, "ReportGeneration"),
                "تم إنشاء التقرير: {ReportType}");

        private static readonly Action<ILogger, string, string, Exception?> LogReportError =
            LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(1002, "ReportError"),
                "خطأ في إنشاء التقرير {ReportType}: {ErrorMessage}");

        public AdvancedReportsService(IUnitOfWork unitOfWork, ILogger<AdvancedReportsService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region تقارير الأداء والتحليلات

        /// <summary>
        /// تقرير مؤشرات الأداء الرئيسية
        /// </summary>
        public async Task<KPIDashboardReport> GenerateKPIDashboardAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var salesInvoices = await _unitOfWork.SalesInvoices
                    .FindAsync(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);

                var purchaseInvoices = await _unitOfWork.PurchaseInvoices
                    .FindAsync(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate);

                var products = await _unitOfWork.Products.GetAllAsync();

                var salesList = salesInvoices.ToList();
                var purchasesList = purchaseInvoices.ToList();

                var report = new KPIDashboardReport
                {
                    PeriodStart = fromDate,
                    PeriodEnd = toDate,
                    GeneratedDate = DateTime.Now,
                    TotalSalesRevenue = salesList.Sum(s => s.NetTotal),
                    TotalPurchaseCost = purchasesList.Sum(p => p.NetTotal),
                    TotalProfit = salesList.Sum(s => s.NetTotal) - purchasesList.Sum(p => p.NetTotal),
                    TotalTransactions = salesList.Count + purchasesList.Count,
                    AverageOrderValue = salesList.Count > 0 ? salesList.Average(s => s.NetTotal) : 0,
                    TopSellingProducts = await GetTopSellingProductsAsync(fromDate, toDate),
                    LowStockProducts = products.Where(p => p.CurrentStock <= p.MinimumStock).ToList(),
                    SalesGrowthRate = await CalculateSalesGrowthRateAsync(fromDate, toDate)
                };

                LogReportGeneration(_logger, "KPI Dashboard", null);
                return report;
            }
            catch (Exception ex)
            {
                LogReportError(_logger, "KPI Dashboard", ex.Message, ex);
                throw new InvalidOperationException($"فشل في إنشاء تقرير مؤشرات الأداء: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تقرير تحليل الربحية
        /// </summary>
        public async Task<ProfitabilityAnalysisReport> GenerateProfitabilityAnalysisAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var salesData = await _unitOfWork.SalesInvoices
                    .FindAsync(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);

                var purchaseData = await _unitOfWork.PurchaseInvoices
                    .FindAsync(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate);

                var salesList = salesData.ToList();
                var purchasesList = purchaseData.ToList();

                var report = new ProfitabilityAnalysisReport
                {
                    PeriodStart = fromDate,
                    PeriodEnd = toDate,
                    GeneratedDate = DateTime.Now,
                    TotalRevenue = salesList.Sum(s => s.NetTotal),
                    TotalCost = purchasesList.Sum(p => p.NetTotal),
                    GrossProfit = salesList.Sum(s => s.NetTotal) - purchasesList.Sum(p => p.NetTotal),
                    ProfitMargin = await CalculateProfitMarginAsync(fromDate, toDate),
                    ProductProfitability = await GetProductProfitabilityAsync(fromDate, toDate),
                    CustomerProfitability = await GetCustomerProfitabilityAsync(fromDate, toDate)
                };

                LogReportGeneration(_logger, "Profitability Analysis", null);
                return report;
            }
            catch (Exception ex)
            {
                LogReportError(_logger, "Profitability Analysis", ex.Message, ex);
                throw new InvalidOperationException($"فشل في إنشاء تقرير تحليل الربحية: {ex.Message}", ex);
            }
        }

        #endregion

        #region تقارير المخزون المتقدمة

        /// <summary>
        /// تقرير حركة المخزون التفصيلية
        /// </summary>
        public async Task<DetailedInventoryMovementReport> GenerateDetailedInventoryMovementAsync(
            DateTime fromDate, DateTime toDate, int? productId = null)
        {
            try
            {
                var movements = await _unitOfWork.StockMovements
                    .FindAsync(m => m.MovementDate >= fromDate && m.MovementDate <= toDate &&
                                    (productId == null || m.ProductId == productId.Value));

                var movementsList = movements.ToList();

                var report = new DetailedInventoryMovementReport
                {
                    PeriodStart = fromDate,
                    PeriodEnd = toDate,
                    GeneratedDate = DateTime.Now,
                    ProductId = productId,
                    TotalInbound = movementsList.Where(m => m.MovementType == StockMovementType.In)
                        .Sum(m => m.QuantityInMainUnit),
                    TotalOutbound = movementsList.Where(m => m.MovementType == StockMovementType.Out)
                        .Sum(m => m.QuantityInMainUnit),
                    NetMovement = movementsList.Sum(m => m.MovementType == StockMovementType.In 
                        ? m.QuantityInMainUnit : -m.QuantityInMainUnit),
                    MovementDetails = movementsList.Select(m => new MovementDetail
                    {
                        Date = m.MovementDate,
                        ProductName = m.Product?.ProductName ?? "غير محدد",
                        MovementType = m.MovementType.ToString(),
                        Quantity = m.Quantity,
                        UnitName = m.Unit?.UnitName ?? "غير محدد",
                        Reference = $"{m.ReferenceType}-{m.ReferenceId}",
                        Notes = m.Notes
                    }).ToList()
                };

                LogReportGeneration(_logger, "Detailed Inventory Movement", null);
                return report;
            }
            catch (Exception ex)
            {
                LogReportError(_logger, "Detailed Inventory Movement", ex.Message, ex);
                throw new InvalidOperationException($"فشل في إنشاء تقرير حركة المخزون: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// تقرير تقييم المخزون
        /// </summary>
        public async Task<InventoryValuationReportAdvanced> GenerateInventoryValuationAsync(DateTime asOfDate)
        {
            try
            {
                var products = await _unitOfWork.Products.GetAllAsync();
                var productsList = products.ToList();

                var valuationItems = new List<InventoryValuationItemAdvanced>();

                foreach (var product in productsList)
                {
                    var avgCost = await CalculateAverageCostAsync(product.ProductId, asOfDate);
                    
                    valuationItems.Add(new InventoryValuationItemAdvanced
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        CurrentStock = product.CurrentStock,
                        UnitCost = avgCost,
                        TotalValue = product.CurrentStock * avgCost,
                        LastPurchasePrice = await GetLastPurchasePriceAsync(product.ProductId),
                        Category = product.Category?.CategoryName ?? "غير مصنف"
                    });
                }

                var report = new InventoryValuationReportAdvanced
                {
                    AsOfDate = asOfDate,
                    GeneratedDate = DateTime.Now,
                    TotalInventoryValue = valuationItems.Sum(i => i.TotalValue),
                    TotalItems = valuationItems.Count,
                    ValuationItems = valuationItems
                };

                LogReportGeneration(_logger, "Inventory Valuation", null);
                return report;
            }
            catch (Exception ex)
            {
                LogReportError(_logger, "Inventory Valuation", ex.Message, ex);
                throw new InvalidOperationException($"فشل في إنشاء تقرير تقييم المخزون: {ex.Message}", ex);
            }
        }

        #endregion

        #region تقارير العملاء والموردين

        /// <summary>
        /// تقرير أداء العملاء
        /// </summary>
        public async Task<CustomerPerformanceReport> GenerateCustomerPerformanceAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var customers = await _unitOfWork.Customers.GetAllAsync();
                var salesInvoices = await _unitOfWork.SalesInvoices
                    .FindAsync(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);

                var customersList = customers.ToList();
                var salesList = salesInvoices.ToList();
                var customerMetrics = new List<CustomerMetrics>();

                foreach (var customer in customersList)
                {
                    var customerInvoices = salesList.Where(s => s.CustomerId == customer.CustomerId).ToList();
                    
                    if (customerInvoices.Count > 0)
                    {
                        customerMetrics.Add(new CustomerMetrics
                        {
                            CustomerId = customer.CustomerId,
                            CustomerName = customer.CustomerName,
                            TotalOrders = customerInvoices.Count,
                            TotalRevenue = customerInvoices.Sum(i => i.NetTotal),
                            AverageOrderValue = customerInvoices.Average(i => i.NetTotal),
                            LastOrderDate = customerInvoices.Max(i => i.InvoiceDate),
                            OutstandingBalance = customer.Balance
                        });
                    }
                }

                var report = new CustomerPerformanceReport
                {
                    PeriodStart = fromDate,
                    PeriodEnd = toDate,
                    GeneratedDate = DateTime.Now,
                    TotalCustomers = customersList.Count,
                    ActiveCustomers = customerMetrics.Count,
                    TopCustomers = customerMetrics.OrderByDescending(c => c.TotalRevenue).Take(10).ToList(),
                    CustomerMetrics = customerMetrics
                };

                LogReportGeneration(_logger, "Customer Performance", null);
                return report;
            }
            catch (Exception ex)
            {
                LogReportError(_logger, "Customer Performance", ex.Message, ex);
                throw new InvalidOperationException($"فشل في إنشاء تقرير أداء العملاء: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        private async Task<List<Product>> GetTopSellingProductsAsync(DateTime fromDate, DateTime toDate)
        {
            var salesItems = await _unitOfWork.SalesInvoiceItems
                .FindAsync(si => si.SalesInvoice.InvoiceDate >= fromDate && 
                                 si.SalesInvoice.InvoiceDate <= toDate);

            var salesItemsList = salesItems.ToList();

            var topProductIds = salesItemsList
                .GroupBy(si => si.ProductId)
                .OrderByDescending(g => g.Sum(x => x.Quantity))
                .Take(10)
                .Select(g => g.Key)
                .ToList();

            var topProducts = new List<Product>();
            foreach (var productId in topProductIds)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(productId);
                if (product != null)
                {
                    topProducts.Add(product);
                }
            }

            return topProducts;
        }

        private async Task<decimal> CalculateSalesGrowthRateAsync(DateTime fromDate, DateTime toDate)
        {
            var currentPeriodSales = await _unitOfWork.SalesInvoices
                .FindAsync(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);

            var previousPeriodStart = fromDate.AddDays(-(toDate - fromDate).Days);
            var previousPeriodEnd = fromDate.AddDays(-1);

            var previousPeriodSales = await _unitOfWork.SalesInvoices
                .FindAsync(s => s.InvoiceDate >= previousPeriodStart && s.InvoiceDate <= previousPeriodEnd);

            var currentTotal = currentPeriodSales.Sum(s => s.NetTotal);
            var previousTotal = previousPeriodSales.Sum(s => s.NetTotal);

            if (previousTotal == 0) return 0;

            return ((currentTotal - previousTotal) / previousTotal) * 100;
        }

        private async Task<decimal> CalculateProfitMarginAsync(DateTime fromDate, DateTime toDate)
        {
            var sales = await _unitOfWork.SalesInvoices
                .FindAsync(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);

            var purchases = await _unitOfWork.PurchaseInvoices
                .FindAsync(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate);

            var totalRevenue = sales.Sum(s => s.NetTotal);
            var totalCost = purchases.Sum(p => p.NetTotal);

            if (totalRevenue == 0) return 0;

            return ((totalRevenue - totalCost) / totalRevenue) * 100;
        }

        private async Task<List<ProductProfitability>> GetProductProfitabilityAsync(DateTime fromDate, DateTime toDate)
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            var productsList = products.ToList();
            var profitabilityList = new List<ProductProfitability>();

            foreach (var product in productsList)
            {
                var salesItems = await _unitOfWork.SalesInvoiceItems
                    .FindAsync(si => si.ProductId == product.ProductId && 
                                     si.SalesInvoice.InvoiceDate >= fromDate && 
                                     si.SalesInvoice.InvoiceDate <= toDate);

                var totalRevenue = salesItems.Sum(si => si.LineTotal);
                var totalCost = await CalculateProductCostAsync(product.ProductId, fromDate, toDate);

                profitabilityList.Add(new ProductProfitability
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    Profit = totalRevenue - totalCost,
                    ProfitMargin = totalRevenue > 0 ? ((totalRevenue - totalCost) / totalRevenue) * 100 : 0
                });
            }

            return profitabilityList.OrderByDescending(p => p.Profit).ToList();
        }

        private async Task<List<CustomerProfitability>> GetCustomerProfitabilityAsync(DateTime fromDate, DateTime toDate)
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();
            var customersList = customers.ToList();
            var profitabilityList = new List<CustomerProfitability>();

            foreach (var customer in customersList)
            {
                var invoices = await _unitOfWork.SalesInvoices
                    .FindAsync(s => s.CustomerId == customer.CustomerId && 
                                    s.InvoiceDate >= fromDate && 
                                    s.InvoiceDate <= toDate);

                var invoicesList = invoices.ToList();
                var totalRevenue = invoicesList.Sum(i => i.NetTotal);
                var totalCost = await CalculateCustomerCostAsync(customer.CustomerId, fromDate, toDate);

                profitabilityList.Add(new CustomerProfitability
                {
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    Profit = totalRevenue - totalCost,
                    ProfitMargin = totalRevenue > 0 ? ((totalRevenue - totalCost) / totalRevenue) * 100 : 0
                });
            }

            return profitabilityList.OrderByDescending(c => c.Profit).ToList();
        }

        private async Task<decimal> CalculateAverageCostAsync(int productId, DateTime asOfDate)
        {
            var purchaseItems = await _unitOfWork.PurchaseInvoiceItems
                .FindAsync(pi => pi.ProductId == productId && 
                                 pi.PurchaseInvoice.InvoiceDate <= asOfDate);

            var itemsList = purchaseItems.ToList();
            if (itemsList.Count == 0) return 0;

            var totalCost = itemsList.Sum(pi => pi.LineTotal);
            var totalQuantity = itemsList.Sum(pi => pi.Quantity);

            return totalQuantity > 0 ? totalCost / totalQuantity : 0;
        }

        private async Task<decimal> GetLastPurchasePriceAsync(int productId)
        {
            var lastPurchase = await _unitOfWork.PurchaseInvoiceItems
                .FindAsync(pi => pi.ProductId == productId);

            var lastItem = lastPurchase
                .OrderByDescending(pi => pi.PurchaseInvoice.InvoiceDate)
                .FirstOrDefault();

            return lastItem?.UnitCost ?? 0;
        }

        private async Task<decimal> CalculateProductCostAsync(int productId, DateTime fromDate, DateTime toDate)
        {
            var purchaseItems = await _unitOfWork.PurchaseInvoiceItems
                .FindAsync(pi => pi.ProductId == productId && 
                                 pi.PurchaseInvoice.InvoiceDate >= fromDate && 
                                 pi.PurchaseInvoice.InvoiceDate <= toDate);

            return purchaseItems.Sum(pi => pi.LineTotal);
        }

        private async Task<decimal> CalculateCustomerCostAsync(int customerId, DateTime fromDate, DateTime toDate)
        {
            var customerInvoices = await _unitOfWork.SalesInvoices
                .FindAsync(s => s.CustomerId == customerId && 
                                s.InvoiceDate >= fromDate && 
                                s.InvoiceDate <= toDate);

            decimal totalCost = 0;

            foreach (var invoice in customerInvoices)
            {
                var invoiceItems = await _unitOfWork.SalesInvoiceItems
                    .FindAsync(si => si.SalesInvoiceId == invoice.SalesInvoiceId);

                foreach (var item in invoiceItems)
                {
                    var productCost = await CalculateAverageCostAsync(item.ProductId, invoice.InvoiceDate);
                    totalCost += productCost * item.Quantity;
                }
            }

            return totalCost;
        }

        #endregion
    }
}