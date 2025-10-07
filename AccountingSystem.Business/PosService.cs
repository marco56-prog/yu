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
    /// <summary>
    /// خدمة نقطة البيع
    /// </summary>
    public interface IPosService
    {
        // إدارة المعاملات
        Task<string> GenerateTransactionNumberAsync();
        Task<POSTransaction?> CreateTransactionAsync(POSTransactionRequest request);
        Task<POSTransaction?> GetTransactionByIdAsync(int transactionId);
        Task<POSTransaction?> GetTransactionByNumberAsync(string transactionNumber);
        Task<bool> VoidTransactionAsync(int transactionId, string reason, int cashierId);
        Task<IEnumerable<POSTransaction>> GetTransactionsBySessionAsync(int sessionId);
        Task<IEnumerable<POSTransaction>> GetTransactionsByDateAsync(DateTime transactionDate);

        // إدارة الأصناف في المعاملة
        Task<bool> AddItemToTransactionAsync(int transactionId, POSTransactionItemRequest item);
        Task<bool> UpdateTransactionItemAsync(int itemId, decimal quantity, decimal? discountPercent = null);
        Task<bool> RemoveItemFromTransactionAsync(int itemId);

        // إدارة المدفوعات
        Task<bool> ProcessPaymentAsync(int transactionId, POSPaymentRequest payment);
        Task<bool> CompleteTransactionAsync(int transactionId);

        // الخصومات والعروض
        Task<bool> ApplyDiscountAsync(int transactionId, int? discountId = null, decimal? discountPercent = null, decimal? discountAmount = null);
        Task<bool> RemoveDiscountAsync(int transactionId);

        // التقارير
        Task<POSSalesReport> GetSalesReportAsync(DateTime from, DateTime toDate, int? cashierId = null);
        Task<IEnumerable<TopSellingProduct>> GetTopSellingProductsAsync(DateTime from, DateTime toDate, int limit = 10);
    }

    public class PosService : IPosService
    {
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDiscountService _discountService;
    private readonly ICashDrawerService _cashDrawerService;

    private const string PaymentMethodCash = "نقداً";
    private const string PaymentMethodCard = "بطاقة";
    private const string PaymentMethodMultiple = "متعدد";

        public PosService(IUnitOfWork unitOfWork, IDiscountService discountService, ICashDrawerService cashDrawerService)
        {
            _unitOfWork = unitOfWork;
            _discountService = discountService;
            _cashDrawerService = cashDrawerService;
        }

        public async Task<string> GenerateTransactionNumberAsync()
        {
            // تفادي التضارب: جرّب لحد 5 محاولات
            for (int attempt = 0; attempt < 5; attempt++)
            {
                var repo = _unitOfWork.Repository<POSTransaction>();
                var today = DateTime.Today;
                var todayCode = today.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                var todayTransactions = await repo.FindAsync(t => t.TransactionDate.Date == today);
                var count = todayTransactions.Count() + 1;
                var number = $"POS-{todayCode}-{count:D4}";

                var exists = await repo.AnyAsync(t => t.TransactionNumber == number);
                if (!exists) return number;
            }

            // fallback: رقم فريد بإضافة جزء عشوائي قصير
            return $"POS-{DateTime.Today:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper(CultureInfo.InvariantCulture)}";
        }

        public async Task<POSTransaction?> CreateTransactionAsync(POSTransactionRequest request)
        {
            try
            {
                var transaction = new POSTransaction
                {
                    TransactionNumber = await GenerateTransactionNumberAsync(),
                    CashierId = request.CashierId,
                    SessionId = request.SessionId,
                    CustomerId = request.CustomerId,
                    TransactionType = string.IsNullOrWhiteSpace(request.TransactionType) ? "بيع" : request.TransactionType,
                    Status = "جديدة",
                    TransactionDate = DateTime.Now,
                    TaxRate = 0, // يضبط لاحقًا من الإعدادات إن رغبت
                };

                var repo = _unitOfWork.Repository<POSTransaction>();
                await repo.AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                return transaction;
            }
            catch
            {
                return null;
            }
        }

        public async Task<POSTransaction?> GetTransactionByIdAsync(int transactionId)
        {
            var repo = _unitOfWork.Repository<POSTransaction>();
            var transaction = await repo.GetByIdAsync(transactionId);

            if (transaction != null)
            {
                // تحميل الأصناف والمدفوعات
                var itemsRepo = _unitOfWork.Repository<POSTransactionItem>();
                var paymentsRepo = _unitOfWork.Repository<POSPayment>();

                var items = await itemsRepo.FindAsync(i => i.TransactionId == transactionId);
                var payments = await paymentsRepo.FindAsync(p => p.TransactionId == transactionId);

                transaction.Items = items.ToList();
                transaction.Payments = payments.ToList();
            }

            return transaction;
        }

        public async Task<POSTransaction?> GetTransactionByNumberAsync(string transactionNumber)
        {
            var repo = _unitOfWork.Repository<POSTransaction>();
            var transactions = await repo.FindAsync(t => t.TransactionNumber == transactionNumber);
            return transactions.FirstOrDefault();
        }

        public async Task<bool> VoidTransactionAsync(int transactionId, string reason, int cashierId)
        {
            // إلغاء معاملة: رجوع مخزون + تحديث الحالة
            _unitOfWork.BeginTransaction();
            try
            {
                var transaction = await GetTransactionByIdAsync(transactionId);
                if (transaction == null || transaction.IsVoided) { _unitOfWork.RollbackTransaction(); return false; }

                transaction.IsVoided = true;
                transaction.VoidedAt = DateTime.Now;
                transaction.VoidReason = reason;
                transaction.Status = "ملغية";

                // إعادة المخزون لو كانت بيع وتمت (أو لو خرجت كميات)
                if (transaction.TransactionType == "بيع" && transaction.Items?.Count > 0)
                {
                    foreach (var it in transaction.Items)
                    {
                        var ok = await AdjustStockAsync(it.ProductId, it.Quantity, StockMovementType.In,
                            refType: "POSTransaction", refId: transaction.Id,
                            note: $"إلغاء نقطة بيع - {transaction.TransactionNumber}");
                        if (!ok) { _unitOfWork.RollbackTransaction(); return false; }
                    }
                }

                var repo = _unitOfWork.Repository<POSTransaction>();
                repo.Update(transaction);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                return false;
            }
        }

        public async Task<IEnumerable<POSTransaction>> GetTransactionsBySessionAsync(int sessionId)
        {
            var repo = _unitOfWork.Repository<POSTransaction>();
            return await repo.FindAsync(t => t.SessionId == sessionId);
        }

        public async Task<IEnumerable<POSTransaction>> GetTransactionsByDateAsync(DateTime transactionDate)
        {
            var repo = _unitOfWork.Repository<POSTransaction>();
            return await repo.FindAsync(t => t.TransactionDate.Date == transactionDate.Date);
        }

        public async Task<bool> AddItemToTransactionAsync(int transactionId, POSTransactionItemRequest item)
        {
            _unitOfWork.BeginTransaction();
            try
            {
                // التحقق من وجود المنتج
                var productRepo = _unitOfWork.Repository<Product>();
                var product = await productRepo.GetByIdAsync(item.ProductId);
                if (product == null) { _unitOfWork.RollbackTransaction(); return false; }

                // جلب المعاملة
                var transRepo = _unitOfWork.Repository<POSTransaction>();
                var transaction = await transRepo.GetByIdAsync(transactionId);
                if (transaction == null || transaction.IsVoided) { _unitOfWork.RollbackTransaction(); return false; }

                // إنشاء الصنف
                var unitPrice = item.UnitPrice ?? product.SalePrice;
                if (unitPrice < 0) unitPrice = 0;

                var line = new POSTransactionItem
                {
                    TransactionId = transactionId,
                    ProductId = item.ProductId,
                    Quantity = Math.Max(0, item.Quantity),
                    UnitPrice = unitPrice,
                    DiscountPercent = Math.Max(0, item.DiscountPercent),
                    DiscountAmount = Math.Max(0, item.DiscountAmount)
                };

                // حساب المجموع
                RecalculateItemTotals(line);

                var itemRepo = _unitOfWork.Repository<POSTransactionItem>();
                await itemRepo.AddAsync(line);

                // تحديث إجماليات المعاملة
                await UpdateTransactionTotalsAsync(transactionId);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                return false;
            }
        }

        public async Task<bool> UpdateTransactionItemAsync(int itemId, decimal quantity, decimal? discountPercent = null)
        {
            _unitOfWork.BeginTransaction();
            try
            {
                var itemRepo = _unitOfWork.Repository<POSTransactionItem>();
                var item = await itemRepo.GetByIdAsync(itemId);
                if (item == null) { _unitOfWork.RollbackTransaction(); return false; }

                item.Quantity = Math.Max(0, quantity);
                if (discountPercent.HasValue)
                    item.DiscountPercent = Math.Max(0, discountPercent.Value);

                // إعادة حساب المجموع
                RecalculateItemTotals(item);

                itemRepo.Update(item);

                // تحديث إجماليات المعاملة
                await UpdateTransactionTotalsAsync(item.TransactionId);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                return false;
            }
        }

        public async Task<bool> RemoveItemFromTransactionAsync(int itemId)
        {
            _unitOfWork.BeginTransaction();
            try
            {
                var itemRepo = _unitOfWork.Repository<POSTransactionItem>();
                var item = await itemRepo.GetByIdAsync(itemId);
                if (item == null) { _unitOfWork.RollbackTransaction(); return false; }

                var transactionId = item.TransactionId;
                itemRepo.Remove(item);

                // تحديث إجماليات المعاملة
                await UpdateTransactionTotalsAsync(transactionId);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                return false;
            }
        }

        public async Task<bool> ProcessPaymentAsync(int transactionId, POSPaymentRequest payment)
        {
            _unitOfWork.BeginTransaction();
            try
            {
                if (payment.Amount <= 0) { _unitOfWork.RollbackTransaction(); return false; }

                var pay = new POSPayment
                {
                    TransactionId = transactionId,
                    PaymentMethod = NormalizePaymentMethod(payment.PaymentMethod),
                    Amount = payment.Amount,
                    Reference = payment.Reference,
                    Notes = payment.Notes
                    // لا يوجد PaymentDate في الموديل الحالي
                };

                var paymentRepo = _unitOfWork.Repository<POSPayment>();
                await paymentRepo.AddAsync(pay);

                // تحديث المعاملة
                var transaction = await GetTransactionByIdAsync(transactionId);
                if (transaction != null)
                {
                    var totalPaid = (transaction.Payments?.Sum(p => p.Amount) ?? 0) + pay.Amount;
                    transaction.AmountPaid = totalPaid;
                    transaction.ChangeAmount = Math.Max(0, totalPaid - transaction.Total);

                    if (totalPaid >= transaction.Total)
                        transaction.Status = "مدفوعة";

                    // تحديث طريقة الدفع بناءً على جميع الدفعات الحالية + الجديدة
                    var paymentMethods = new List<string>();
                    if (transaction.Payments != null)
                    {
                        paymentMethods.AddRange(transaction.Payments
                            .Select(p => NormalizePaymentMethod(p.PaymentMethod))
                            .Where(m => !string.IsNullOrWhiteSpace(m)));
                    }
                    paymentMethods.Add(pay.PaymentMethod);

                    var distinctMethods = paymentMethods
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    transaction.PaymentMethod = distinctMethods.Count switch
                    {
                        0 => PaymentMethodCash,
                        1 => distinctMethods[0],
                        _ => PaymentMethodMultiple
                    };

                    var transactionRepo = _unitOfWork.Repository<POSTransaction>();
                    transactionRepo.Update(transaction);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                return false;
            }
        }

        public async Task<bool> CompleteTransactionAsync(int transactionId)
        {
            _unitOfWork.BeginTransaction();
            try
            {
                var transaction = await GetTransactionByIdAsync(transactionId);
                if (transaction == null) { _unitOfWork.RollbackTransaction(); return false; }

                // التأكد من التغطية المالية
                if (transaction.AmountPaid < transaction.Total) { _unitOfWork.RollbackTransaction(); return false; }

                if (!await ApplyStockAdjustmentsAsync(transaction))
                {
                    _unitOfWork.RollbackTransaction();
                    return false;
                }

                // تسجيل في الخزينة إذا كان الدفع نقداً
                if (!await HandleCashDrawerAsync(transaction))
                {
                    _unitOfWork.RollbackTransaction();
                    return false;
                }

                transaction.Status = "مكتملة";
                var repo = _unitOfWork.Repository<POSTransaction>();
                repo.Update(transaction);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                return false;
            }
        }

        public async Task<bool> ApplyDiscountAsync(int transactionId, int? discountId = null, decimal? discountPercent = null, decimal? discountAmount = null)
        {
            _unitOfWork.BeginTransaction();
            try
            {
                var transaction = await GetTransactionByIdAsync(transactionId);
                if (transaction == null) { _unitOfWork.RollbackTransaction(); return false; }

                if (!await ApplyDiscountCoreAsync(transaction, discountId, discountPercent, discountAmount))
                {
                    _unitOfWork.RollbackTransaction();
                    return false;
                }

                await UpdateTransactionTotalsAsync(transactionId);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                return false;
            }
        }

        public async Task<bool> RemoveDiscountAsync(int transactionId)
        {
            _unitOfWork.BeginTransaction();
            try
            {
                var transaction = await GetTransactionByIdAsync(transactionId);
                if (transaction == null) { _unitOfWork.RollbackTransaction(); return false; }

                transaction.DiscountPercent = 0;
                transaction.DiscountAmount = 0;

                await UpdateTransactionTotalsAsync(transactionId);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                return false;
            }
        }

        private async Task UpdateTransactionTotalsAsync(int transactionId)
        {
            var transaction = await GetTransactionByIdAsync(transactionId);
            if (transaction == null) return;

            var itemsSubtotal = transaction.Items?.Sum(i => i.LineTotal) ?? 0m;
            transaction.Subtotal = Math.Max(0, itemsSubtotal);

            // خصم على مستوى الفاتورة
            if (transaction.DiscountPercent > 0)
                transaction.DiscountAmount = transaction.Subtotal * (transaction.DiscountPercent / 100m);

            if (transaction.DiscountAmount < 0) transaction.DiscountAmount = 0;
            if (transaction.DiscountAmount > transaction.Subtotal) transaction.DiscountAmount = transaction.Subtotal;

            var afterDiscount = transaction.Subtotal - transaction.DiscountAmount;

            // ضريبة
            if (transaction.TaxRate < 0) transaction.TaxRate = 0;
            transaction.TaxAmount = afterDiscount * (transaction.TaxRate / 100m);
            if (transaction.TaxAmount < 0) transaction.TaxAmount = 0;

            transaction.Total = afterDiscount + transaction.TaxAmount;

            var repo = _unitOfWork.Repository<POSTransaction>();
            repo.Update(transaction);
        }

        public async Task<POSSalesReport> GetSalesReportAsync(DateTime from, DateTime toDate, int? cashierId = null)
        {
            var repo = _unitOfWork.Repository<POSTransaction>();
            var transactions = await repo.FindAsync(t =>
                t.TransactionDate >= from &&
                t.TransactionDate <= toDate &&
                !t.IsVoided &&
                t.Status == "مكتملة" &&
                (cashierId == null || t.CashierId == cashierId));

            var salesTransactions = transactions.Where(t => t.TransactionType == "بيع");
            var returnTransactions = transactions.Where(t => t.TransactionType == "مرتجع");

            return new POSSalesReport
            {
                FromDate = from,
                ToDate = toDate,
                TotalSales = salesTransactions.Sum(t => t.Total),
                TotalReturns = returnTransactions.Sum(t => t.Total),
                NetSales = salesTransactions.Sum(t => t.Total) - returnTransactions.Sum(t => t.Total),
                TotalTransactions = salesTransactions.Count(),
                TotalReturnsCount = returnTransactions.Count(),
                TotalDiscounts = salesTransactions.Sum(t => t.DiscountAmount),
                CashSales = salesTransactions.Where(t => t.PaymentMethod == "نقداً").Sum(t => t.Total),
                CardSales = salesTransactions.Where(t => t.PaymentMethod == "بطاقة").Sum(t => t.Total),
                AverageTransactionValue = salesTransactions.Any() ? salesTransactions.Average(t => t.Total) : 0
            };
        }

        public async Task<IEnumerable<TopSellingProduct>> GetTopSellingProductsAsync(DateTime from, DateTime toDate, int limit = 10)
        {
            var itemRepo = _unitOfWork.Repository<POSTransactionItem>();
            var transactionRepo = _unitOfWork.Repository<POSTransaction>();
            var productRepo = _unitOfWork.Repository<Product>();

            var transactions = await transactionRepo.FindAsync(t =>
                t.TransactionDate >= from &&
                t.TransactionDate <= toDate &&
                !t.IsVoided &&
                t.Status == "مكتملة" &&
                t.TransactionType == "بيع");

            var transactionIds = transactions.Select(t => t.Id).ToList();
            var items = await itemRepo.FindAsync(i => transactionIds.Contains(i.TransactionId));

            var topProducts = items
                .GroupBy(i => i.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.LineTotal),
                    TransactionCount = g.Count()
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(limit)
                .ToList();

            // جلب أسماء المنتجات
            var ids = topProducts.Select(x => x.ProductId).ToList();
            var products = await productRepo.FindAsync(p => ids.Contains(p.ProductId));
            var names = products.ToDictionary(p => p.ProductId, p => p.ProductName);

            return topProducts.Select(t => new TopSellingProduct
            {
                ProductId = t.ProductId,
                ProductName = names.TryGetValue(t.ProductId, out var n) ? n : string.Empty,
                TotalQuantity = t.TotalQuantity,
                TotalRevenue = t.TotalRevenue,
                TransactionCount = t.TransactionCount
            });
        }

        // =========================
        // Helpers داخليّة
        // =========================

        private static void RecalculateItemTotals(POSTransactionItem item)
        {
            var subtotal = item.Quantity * item.UnitPrice;
            var discountAmount = item.DiscountAmount;

            if (item.DiscountPercent > 0)
                discountAmount = subtotal * (item.DiscountPercent / 100m);

            if (discountAmount < 0) discountAmount = 0;
            if (discountAmount > subtotal) discountAmount = subtotal;

            item.LineTotal = subtotal - discountAmount;
        }

        /// <summary>
        /// تعديل مخزون منتج بوحدة الصنف الأساسية + تسجيل حركة مخزون.
        /// </summary>
        private async Task<bool> AdjustStockAsync(int productId, decimal quantity, StockMovementType movementType, string refType, int refId, string note)
        {
            var productRepo = _unitOfWork.Repository<Product>();
            var unitRepo = _unitOfWork.Repository<Unit>();
            var stockMoveRepo = _unitOfWork.Repository<StockMovement>();

            var product = await productRepo.GetByIdAsync(productId);
            if (product == null) throw new InvalidOperationException("المنتج غير موجود.");

            // نشتغل بالوحدة الأساسية دومًا
            var unit = await unitRepo.GetByIdAsync(product.MainUnitId);
            if (unit == null) throw new InvalidOperationException("الوحدة الأساسية غير موجودة.");

            var qty = Math.Max(0, quantity);

            switch (movementType)
            {
                case StockMovementType.In:
                    product.CurrentStock += qty;
                    break;
                case StockMovementType.Out:
                    if (product.CurrentStock < qty)
                        return false;
                    product.CurrentStock -= qty;
                    break;
                case StockMovementType.Adjustment:
                    product.CurrentStock = qty;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(movementType));
            }

            productRepo.Update(product);

            var movement = new StockMovement
            {
                ProductId = product.ProductId,
                Product = product,
                MovementType = movementType,
                Quantity = qty,
                UnitId = unit.UnitId,
                Unit = unit,
                QuantityInMainUnit = qty,
                MovementDate = DateTime.Now,
                CreatedBy = "POS",
                Notes = note,
                ReferenceType = refType,
                ReferenceId = refId
            };

            await stockMoveRepo.AddAsync(movement);
            return true;
        }

        private async Task<bool> HandleCashDrawerAsync(POSTransaction transaction)
        {
            if (!transaction.SessionId.HasValue)
                return true;

            var payments = transaction.Payments ?? new List<POSPayment>();
            var cashPaid = payments
                .Where(p => NormalizePaymentMethod(p.PaymentMethod) == PaymentMethodCash)
                .Sum(p => p.Amount);

            if (cashPaid <= 0)
                return true;

            var reason = $"{transaction.TransactionType} - فاتورة {transaction.TransactionNumber}";

            if (transaction.TransactionType == "بيع")
            {
                return await _cashDrawerService.AddCashAsync(
                    transaction.CashierId,
                    transaction.SessionId.Value,
                    cashPaid,
                    reason);
            }

            if (transaction.TransactionType == "مرتجع")
            {
                return await _cashDrawerService.RemoveCashAsync(
                    transaction.CashierId,
                    transaction.SessionId.Value,
                    cashPaid,
                    reason);
            }

            return true;
        }

        private async Task<bool> ApplyDiscountCoreAsync(POSTransaction transaction, int? discountId, decimal? discountPercent, decimal? discountAmount)
        {
            if (discountId.HasValue)
            {
                var discount = await _discountService.GetDiscountByIdAsync(discountId.Value);
                if (discount == null || !discount.IsActive) return false;

                if (discount.DiscountType == "نسبة")
                {
                    transaction.DiscountPercent = discount.DiscountValue;
                    transaction.DiscountAmount = transaction.Subtotal * (discount.DiscountValue / 100m);
                }
                else
                {
                    transaction.DiscountPercent = 0;
                    transaction.DiscountAmount = discount.DiscountValue;
                }

                if (discount.MaximumDiscount.HasValue && transaction.DiscountAmount > discount.MaximumDiscount.Value)
                    transaction.DiscountAmount = discount.MaximumDiscount.Value;

                return true;
            }

            if (discountPercent.HasValue)
            {
                transaction.DiscountPercent = Math.Max(0, discountPercent.Value);
                transaction.DiscountAmount = transaction.Subtotal * (transaction.DiscountPercent / 100m);
                return true;
            }

            if (discountAmount.HasValue)
            {
                transaction.DiscountPercent = 0;
                transaction.DiscountAmount = Math.Max(0, discountAmount.Value);
            }

            return true;
        }

        private async Task<bool> ApplyStockAdjustmentsAsync(POSTransaction transaction)
        {
            if (transaction.Items == null || transaction.Items.Count == 0)
                return true;

            var movementType = transaction.TransactionType switch
            {
                "بيع" => StockMovementType.Out,
                "مرتجع" => StockMovementType.In,
                _ => (StockMovementType?)null
            };

            if (movementType is null)
                return true;

            var notePrefix = transaction.TransactionType == "بيع" ? "بيع نقطة بيع" : "مرتجع نقطة بيع";

            foreach (var item in transaction.Items)
            {
                var ok = await AdjustStockAsync(
                    item.ProductId,
                    item.Quantity,
                    movementType.Value,
                    refType: "POSTransaction",
                    refId: transaction.Id,
                    note: $"{notePrefix} - {transaction.TransactionNumber}");

                if (!ok)
                    return false;
            }

            return true;
        }

        private static string NormalizePaymentMethod(string? paymentMethod)
        {
            var normalized = (paymentMethod ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalized))
                return PaymentMethodCash;

            // توحيد مسميات الدفع النقدي
            var cashKeywords = new[] { PaymentMethodCash, "نقدي", "كاش", "cash" };
            if (cashKeywords.Any(k => string.Equals(normalized, k, StringComparison.OrdinalIgnoreCase)))
                return PaymentMethodCash;

            var cardKeywords = new[] { PaymentMethodCard, "فيزا", "كارت", "card" };
            if (cardKeywords.Any(k => string.Equals(normalized, k, StringComparison.OrdinalIgnoreCase)))
                return PaymentMethodCard;

            return normalized;
        }
    }

    // نماذج الطلبات والتقارير
    public class POSTransactionRequest
    {
        public int CashierId { get; set; }
        public int? SessionId { get; set; }
        public int? CustomerId { get; set; }
        public string TransactionType { get; set; } = "بيع";
    }

    public class POSTransactionItemRequest
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal? UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
    }

    public class POSPaymentRequest
    {
        public string PaymentMethod { get; set; } = "نقداً";
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
    }

    public class POSSalesReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalReturnsCount { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal CashSales { get; set; }
        public decimal CardSales { get; set; }
        public decimal AverageTransactionValue { get; set; }
    }

    public class TopSellingProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TransactionCount { get; set; }
    }
}
