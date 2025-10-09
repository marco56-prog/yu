using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    // واجهة خدمة فواتير البيع
    public interface ISalesInvoiceService
    {
        Task<IEnumerable<SalesInvoice>> GetAllSalesInvoicesAsync();
        Task<SalesInvoice?> GetSalesInvoiceByIdAsync(int id);
        Task<SalesInvoice?> GetSalesInvoiceByNumberAsync(string number);
        Task<Result<SalesInvoice>> CreateSalesInvoiceAsync(SalesInvoice invoice);
        Task<SalesInvoice> UpdateSalesInvoiceAsync(SalesInvoice invoice);
        Task<bool> DeleteSalesInvoiceAsync(int id);
        Task<SalesInvoice> PostSalesInvoiceAsync(int invoiceId);
        Task<SalesInvoice> CancelSalesInvoiceAsync(int invoiceId);
        Task<decimal> GetCustomerTotalSalesAsync(int customerId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<Result<IEnumerable<SalesInvoice>>> GetSalesInvoicesByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<Result<SalesInvoice>> SaveAsDraftAsync(SalesInvoice invoice);
        Task<(decimal TotalAmount, int InvoiceCount, decimal TotalNet)> GetSalesSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
        Task<decimal> GetCOGSSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    }

    // تنفيذ خدمة فواتير البيع
    public class SalesInvoiceService : ISalesInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INumberSequenceService _numberSequenceService;
        private readonly IProductService _productService;
        private readonly ICustomerService _customerService;
        private readonly IPriceHistoryService _priceHistoryService;

        public SalesInvoiceService(
            IUnitOfWork unitOfWork,
            INumberSequenceService numberSequenceService,
            IProductService productService,
            ICustomerService customerService,
            IPriceHistoryService priceHistoryService)
        {
            _unitOfWork = unitOfWork;
            _numberSequenceService = numberSequenceService;
            _productService = productService;
            _customerService = customerService;
            _priceHistoryService = priceHistoryService;
        }

        public async Task<IEnumerable<SalesInvoice>> GetAllSalesInvoicesAsync()
        {
            // لو عندك includes مطلوبة اعمل نسخة بديلة للعرض فقط
            return await _unitOfWork.Repository<SalesInvoice>().GetAllAsync();
        }

        public async Task<SalesInvoice?> GetSalesInvoiceByIdAsync(int id)
        {
            return await _unitOfWork.Repository<SalesInvoice>().GetByIdAsync(id);
        }

        public async Task<SalesInvoice?> GetSalesInvoiceByNumberAsync(string number)
        {
            number = (number ?? string.Empty).Trim();
            if (number == "") return null;

            return await _unitOfWork.Repository<SalesInvoice>()
                .SingleOrDefaultAsync(s => s.InvoiceNumber == number);
        }

    public async Task<Result<SalesInvoice>> CreateSalesInvoiceAsync(SalesInvoice invoice)
        {
            // تنظيف متتبع EF لتجنب تعارض التتبع مع كيانات ملاحة قادمة من UI
            _unitOfWork.Context.ChangeTracker.Clear();

            // إزالة الملاحة من تفاصيل الفاتورة لتفادي تتبّع مزدوج
            if (invoice.Items != null)
            {
                foreach (var d in invoice.Items)
                {
                    d.Product = null!;
                    d.Unit = null!;
                    d.SalesInvoice = null!;
                }
            }

            // رقم فاتورة فريد
            invoice.InvoiceNumber = await GenerateUniqueSalesInvoiceNumberAsync();

            // تحقق شامل
            if (invoice.Items == null || invoice.Items.Count == 0)
                return Result<SalesInvoice>.Failure("يجب إدخال تفاصيل الفاتورة");

            // سريع: تحقق من توفر المخزون لكل بند قبل المتابعة
            foreach (var d in invoice.Items)
            {
                var product = await _unitOfWork.Repository<Product>().GetByIdAsync(d.ProductId);
                if (product == null || !product.IsActive)
                    return Result<SalesInvoice>.Failure($"المنتج رقم {d.ProductId} غير موجود أو غير نشط");

                var unitId = d.UnitId ?? product.MainUnitId;
                decimal qtyInMain = d.Quantity;
                if (unitId != product.MainUnitId)
                {
                    var productUnit = await _unitOfWork.Repository<ProductUnit>()
                        .SingleOrDefaultAsync(pu => pu.ProductId == product.ProductId && pu.UnitId == unitId && pu.IsActive);
                    if (productUnit == null || productUnit.ConversionFactor <= 0)
                        return Result<SalesInvoice>.Failure("معامل التحويل للوحدة غير محدد أو غير صالح لهذا المنتج.");
                    qtyInMain = d.Quantity * productUnit.ConversionFactor;
                }

                if (product.CurrentStock < qtyInMain)
                    return Result<SalesInvoice>.Failure("المخزون غير كافي");
            }

            try
            {
                await ValidateSalesInvoiceAsync(invoice);
            }
            catch (Exception ex)
            {
                return Result<SalesInvoice>.Failure(ex.Message);
            }

            // حساب الإجماليات
            CalculateInvoiceTotals(invoice);

            invoice.CreatedDate = DateTime.Now;
            // Preserve caller's choice of draft/confirmed (tests set IsDraft explicitly).
            // Ensure CreatedBy is set
            invoice.CreatedBy   = string.IsNullOrWhiteSpace(invoice.CreatedBy) ? "1" : invoice.CreatedBy;

            _unitOfWork.BeginTransaction();
            try
            {
                await _unitOfWork.Repository<SalesInvoice>().AddAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                // حفظ تاريخ أسعار العميل (best-effort)
                await SavePriceHistoryAsync(invoice);

                // إذا لم تكن مسودة، نطبّق الحركات (خصم المخزون وتعديل رصيد العميل)
                if (!invoice.IsDraft)
                {
                    var details = invoice.Items;
                    if (details != null)
                    {
                        foreach (var detail in details)
                        {
                            var unitId = detail.UnitId ?? (await _unitOfWork.Repository<Product>().GetByIdAsync(detail.ProductId))!.MainUnitId;

                            // Use product service to update stock (will manage transactions appropriately per provider)
                            await _productService.UpdateProductStockAsync(
                                detail.ProductId,
                                detail.Quantity,
                                unitId,
                                StockMovementType.Out,
                                $"فاتورة بيع رقم {invoice.InvoiceNumber}",
                                "SalesInvoice",
                                invoice.SalesInvoiceId
                            );
                        }
                    }

                    // زيادة مديونية العميل بالباقي
                    await _customerService.UpdateCustomerBalanceAsync(
                        invoice.CustomerId,
                        invoice.RemainingAmount,
                        $"فاتورة بيع رقم {invoice.InvoiceNumber}",
                        "SalesInvoice",
                        invoice.SalesInvoiceId
                    );

                    invoice.Status = InvoiceStatus.Confirmed;
                    invoice.IsPosted = true;
                    _unitOfWork.Repository<SalesInvoice>().Update(invoice);
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
                return Result<SalesInvoice>.Success(invoice);
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        public async Task<SalesInvoice> UpdateSalesInvoiceAsync(SalesInvoice invoice)
        {
            _unitOfWork.Context.ChangeTracker.Clear();

            if (invoice.Items != null)
            {
                foreach (var d in invoice.Items)
                {
                    d.Product = null!;
                    d.Unit = null!;
                    d.SalesInvoice = null!;
                }
            }

            var existing = await _unitOfWork.Repository<SalesInvoice>().GetByIdAsync(invoice.SalesInvoiceId);
            if (existing == null)
                throw new InvalidOperationException("الفاتورة غير موجودة");

            if (existing.IsPosted)
                throw new InvalidOperationException("لا يمكن تعديل فاتورة مرحلة");

            // التحقق والحساب
            await ValidateSalesInvoiceAsync(invoice);
            CalculateInvoiceTotals(invoice);

            // تحديث رأس الفاتورة (نحافظ على InvoiceNumber كما هو)
            existing.CustomerId       = invoice.CustomerId;
            existing.InvoiceDate      = invoice.InvoiceDate;
            existing.SubTotal         = invoice.SubTotal;
            existing.TaxAmount        = Math.Max(0, invoice.TaxAmount);
            existing.DiscountAmount   = Math.Max(0, invoice.DiscountAmount);
            existing.NetTotal         = invoice.NetTotal;
            existing.PaidAmount       = Math.Max(0, invoice.PaidAmount);
            existing.RemainingAmount  = Math.Max(0, invoice.RemainingAmount);
            existing.Notes            = invoice.Notes;

            _unitOfWork.BeginTransaction();
            try
            {
                // استبدال تفاصيل الفاتورة
                var oldDetails = await _unitOfWork.Repository<SalesInvoiceItem>()
                    .FindAsync(d => d.SalesInvoiceId == existing.SalesInvoiceId);
                _unitOfWork.Repository<SalesInvoiceItem>().RemoveRange(oldDetails);

                foreach (var detail in invoice.Items)
                {
                    detail.SalesInvoiceId = existing.SalesInvoiceId;
                    await _unitOfWork.Repository<SalesInvoiceItem>().AddAsync(detail);
                }

                _unitOfWork.Repository<SalesInvoice>().Update(existing);
                await _unitOfWork.SaveChangesAsync();

                // تحديث تاريخ الأسعار
                await SavePriceHistoryAsync(new SalesInvoice
                {
                    SalesInvoiceId = existing.SalesInvoiceId,
                    CustomerId = existing.CustomerId,
                    Items = invoice.Items
                });

                await _unitOfWork.CommitTransactionAsync();
                return existing;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        public async Task<bool> DeleteSalesInvoiceAsync(int id)
        {
            var invoice = await _unitOfWork.Repository<SalesInvoice>().GetByIdAsync(id);
            if (invoice == null) return false;

            if (invoice.IsPosted)
                throw new InvalidOperationException("لا يمكن حذف فاتورة مرحلة");

            _unitOfWork.BeginTransaction();
            try
            {
                var details = await _unitOfWork.Repository<SalesInvoiceItem>()
                    .FindAsync(d => d.SalesInvoiceId == id);

                _unitOfWork.Repository<SalesInvoiceItem>().RemoveRange(details);
                _unitOfWork.Repository<SalesInvoice>().Remove(invoice);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        public async Task<SalesInvoice> PostSalesInvoiceAsync(int invoiceId)
        {
            var invoice = await _unitOfWork.Repository<SalesInvoice>().GetByIdAsync(invoiceId)
                          ?? throw new InvalidOperationException("الفاتورة غير موجودة");

            if (invoice.IsPosted)
                throw new InvalidOperationException("الفاتورة مرحلة بالفعل");

            var details = await _unitOfWork.Repository<SalesInvoiceItem>()
                .FindAsync(d => d.SalesInvoiceId == invoiceId);
            if (!details.Any())
                throw new InvalidOperationException("لا توجد تفاصيل للفاتورة");

            _unitOfWork.BeginTransaction();
            try
            {
                // خصم المخزون
                foreach (var detail in details)
                {
                    if (detail.Quantity <= 0)
                        throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر");

                    var unitId = detail.UnitId
                                 ?? (await _unitOfWork.Repository<Product>().GetByIdAsync(detail.ProductId))!.MainUnitId;

                    await _productService.UpdateProductStockAsync(
                        detail.ProductId,
                        detail.Quantity,
                        unitId,
                        StockMovementType.Out,
                        $"فاتورة بيع رقم {invoice.InvoiceNumber}",
                        "SalesInvoice",
                        invoiceId
                    );
                }

                // رصيد العميل (نزيد بالباقي)
                await _customerService.UpdateCustomerBalanceAsync(
                    invoice.CustomerId,
                    invoice.RemainingAmount,
                    $"فاتورة بيع رقم {invoice.InvoiceNumber}",
                    "SalesInvoice",
                    invoiceId
                );

                invoice.Status  = InvoiceStatus.Confirmed;
                invoice.IsPosted = true;
                _unitOfWork.Repository<SalesInvoice>().Update(invoice);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return invoice;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        public async Task<SalesInvoice> CancelSalesInvoiceAsync(int invoiceId)
        {
            var invoice = await _unitOfWork.Repository<SalesInvoice>().GetByIdAsync(invoiceId)
                          ?? throw new InvalidOperationException("الفاتورة غير موجودة");

            if (!invoice.IsPosted)
                throw new InvalidOperationException("الفاتورة غير مرحلة");

            var details = await _unitOfWork.Repository<SalesInvoiceItem>()
                .FindAsync(d => d.SalesInvoiceId == invoiceId);
            if (!details.Any())
                throw new InvalidOperationException("لا توجد تفاصيل للفاتورة");

            _unitOfWork.BeginTransaction();
            try
            {
                // إعادة المخزون
                foreach (var detail in details)
                {
                    var unitId = detail.UnitId
                                 ?? (await _unitOfWork.Repository<Product>().GetByIdAsync(detail.ProductId))!.MainUnitId;

                    await _productService.UpdateProductStockAsync(
                        detail.ProductId,
                        detail.Quantity,
                        unitId,
                        StockMovementType.In,
                        $"إلغاء فاتورة بيع رقم {invoice.InvoiceNumber}",
                        "SalesInvoiceCancel",
                        invoiceId
                    );
                }

                // تقليل مديونية العميل بالباقي
                await _customerService.UpdateCustomerBalanceAsync(
                    invoice.CustomerId,
                    -invoice.RemainingAmount,
                    $"إلغاء فاتورة بيع رقم {invoice.InvoiceNumber}",
                    "SalesInvoiceCancel",
                    invoiceId
                );

                invoice.Status   = InvoiceStatus.Cancelled;
                invoice.IsPosted = false; // للتأكيد أنها لم تعد مرحلة
                _unitOfWork.Repository<SalesInvoice>().Update(invoice);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return invoice;
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        public async Task<decimal> GetCustomerTotalSalesAsync(int customerId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var invoices = await _unitOfWork.Repository<SalesInvoice>()
                .FindAsync(s => s.CustomerId == customerId && s.IsPosted);

            if (fromDate.HasValue) invoices = invoices.Where(s => s.InvoiceDate >= fromDate.Value);
            if (toDate.HasValue)   invoices = invoices.Where(s => s.InvoiceDate <= toDate.Value);

            return invoices.Sum(s => s.NetTotal);
        }

        // =========================
        // Helpers
        // =========================

        private async Task ValidateSalesInvoiceAsync(SalesInvoice invoice)
        {
            // العميل
            var customer = await _unitOfWork.Repository<Customer>().GetByIdAsync(invoice.CustomerId);
            if (customer == null || !customer.IsActive)
                throw new InvalidOperationException("العميل غير موجود أو غير نشط");

            // تفاصيل
            if (invoice.Items == null || invoice.Items.Count == 0)
                throw new InvalidOperationException("يجب إدخال تفاصيل الفاتورة");

            foreach (var d in invoice.Items)
            {
                var product = await _unitOfWork.Repository<Product>().GetByIdAsync(d.ProductId);
                if (product == null || !product.IsActive)
                    throw new InvalidOperationException($"المنتج رقم {d.ProductId} غير موجود أو غير نشط");

                var unitId = d.UnitId ?? product.MainUnitId;
                var unit = await _unitOfWork.Repository<Unit>().GetByIdAsync(unitId);
                if (unit == null || !unit.IsActive)
                    throw new InvalidOperationException($"الوحدة رقم {unitId} غير موجودة أو غير نشطة");

                if (d.Quantity <= 0)      throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر");
                if (d.UnitPrice < 0)      throw new InvalidOperationException("سعر الوحدة لا يمكن أن يكون سالباً");
                if (d.DiscountAmount < 0) d.DiscountAmount = 0;

                // تحقق من توافر المخزون (نفذ دائماً؛ لا يسمح بإنشاء فاتورة بكمية أكبر من المخزون)
                {
                    // حساب الكمية بوحدة الأساس للمنتج
                    decimal qtyInMain = d.Quantity;
                    if (unitId != product.MainUnitId)
                    {
                        var productUnit = await _unitOfWork.Repository<ProductUnit>()
                            .SingleOrDefaultAsync(pu => pu.ProductId == product.ProductId && pu.UnitId == unitId && pu.IsActive);
                        if (productUnit == null)
                            throw new InvalidOperationException("معامل التحويل للوحدة غير محدد لهذا المنتج.");
                        if (productUnit.ConversionFactor <= 0)
                            throw new InvalidOperationException("معامل التحويل غير صالح.");
                        qtyInMain = d.Quantity * productUnit.ConversionFactor;
                    }

                    if (product.CurrentStock < qtyInMain)
                        throw new InvalidOperationException("المخزون غير كافي");
                }
            }

            if (invoice.PaidAmount < 0)      invoice.PaidAmount = 0;
            if (invoice.TaxAmount < 0)       invoice.TaxAmount  = 0;
            if (invoice.DiscountAmount < 0)  invoice.DiscountAmount = 0;
        }

        private static void CalculateInvoiceTotals(SalesInvoice invoice)
        {
            foreach (var d in invoice.Items)
            {
                d.TotalPrice = d.Quantity * d.UnitPrice;
                d.NetAmount  = Math.Max(0, d.TotalPrice - d.DiscountAmount);
            }

            var subTotal        = invoice.Items.Sum(x => x.TotalPrice);
            var detailsDiscount = invoice.Items.Sum(x => x.DiscountAmount);

            invoice.SubTotal       = subTotal;
            invoice.DiscountAmount = Math.Max(0, invoice.DiscountAmount) + detailsDiscount;

            // Tax is calculated on the value after discounts.
            var taxableAmount = invoice.SubTotal - invoice.DiscountAmount;

            // If TaxRate provided, compute tax from the taxable amount
            if (invoice.TaxRate > 0)
            {
                invoice.TaxAmount = Math.Round(taxableAmount * (invoice.TaxRate / 100m), 2);
            }
            else
            {
                invoice.TaxAmount = Math.Max(0, invoice.TaxAmount);
            }

            invoice.PaidAmount     = Math.Max(0, invoice.PaidAmount);

            invoice.NetTotal = taxableAmount + invoice.TaxAmount;
            if (invoice.NetTotal < 0) invoice.NetTotal = 0;

            if (invoice.PaidAmount > invoice.NetTotal)
                invoice.PaidAmount = invoice.NetTotal;

            invoice.RemainingAmount = invoice.NetTotal - invoice.PaidAmount;
            if (invoice.RemainingAmount < 0) invoice.RemainingAmount = 0;

            // After review, TotalAmount seems to represent the final billable amount, which is NetTotal.
            invoice.TotalAmount = invoice.NetTotal;
        }

        private async Task SavePriceHistoryAsync(SalesInvoice invoice)
        {
            try
            {
                if (invoice.Items?.Count > 0)
                {
                    foreach (var detail in invoice.Items)
                    {
                        await _priceHistoryService.UpdatePriceAsync(detail.ProductId, detail.UnitPrice);
                    }
                }
            }
            catch (Exception ex)
            {
                // لوج صامت بدون إيقاف العملية
                System.Diagnostics.Debug.WriteLine($"SavePriceHistoryAsync Error: {ex.Message}");
            }
        }

        private async Task<string> GenerateUniqueSalesInvoiceNumberAsync()
        {
            const int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                var candidate = await _numberSequenceService.GenerateSalesInvoiceNumberAsync();
                var exists = await _unitOfWork.Repository<SalesInvoice>()
                    .AnyAsync(s => s.InvoiceNumber == candidate);
                if (!exists) return candidate;
            }
            throw new InvalidOperationException("تعذّر توليد رقم فاتورة فريد. حاول مرة أخرى.");
        }

        /// <summary>
        /// حفظ الفاتورة كمسودة
        /// </summary>
        public async Task<Result<SalesInvoice>> SaveAsDraftAsync(SalesInvoice invoice)
        {
            try
            {
                invoice.IsPosted = false;
                return await CreateSalesInvoiceAsync(invoice);
            }
            catch
            {
                return Result<SalesInvoice>.Failure("فشل في حفظ المسودة");
            }
        }

        /// <summary>
        /// البحث عن الفواتير حسب فترة زمنية
        /// </summary>
        public async Task<Result<IEnumerable<SalesInvoice>>> GetSalesInvoicesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var list = await _unitOfWork.Repository<SalesInvoice>()
                    .FindAsync(s => s.InvoiceDate >= fromDate && s.InvoiceDate <= toDate);
                return Result<IEnumerable<SalesInvoice>>.Success(list);
            }
            catch
            {
                return Result<IEnumerable<SalesInvoice>>.Failure("فشل في جلب الفواتير") ;
            }
        }

        /// <summary>
        /// ملخص المبيعات لفترة محددة
        /// </summary>
        public async Task<(decimal TotalAmount, int InvoiceCount, decimal TotalNet)> GetSalesSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var invoices = await _unitOfWork.Repository<SalesInvoice>()
                    .FindAsync(s => s.InvoiceDate >= fromDate && s.InvoiceDate < toDate && s.IsPosted);

                return (
                    TotalAmount: invoices.Sum(i => i.SubTotal),
                    InvoiceCount: invoices.Count(),
                    TotalNet: invoices.Sum(i => i.NetTotal)
                );
            }
            catch
            {
                return (0, 0, 0);
            }
        }

        /// <summary>
        /// تكلفة البضاعة المباعة لفترة محددة
        /// </summary>
        public async Task<decimal> GetCOGSSummaryAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            try
            {
                // نحصل على الفواتير المرسلة للفترة المحددة
                var invoices = await _unitOfWork.Repository<SalesInvoice>()
                    .FindAsync(s => s.InvoiceDate >= fromDate && s.InvoiceDate < toDate && s.IsPosted);

                // نحسب إجمالي تكلفة المنتجات
                decimal totalCost = 0;
                foreach (var invoice in invoices)
                {
                    if (invoice.Items != null)
                    {
                        foreach (var item in invoice.Items)
                        {
                            totalCost += item.Quantity * (item.UnitPrice * 0.7m); // افتراض هامش ربح 30%
                        }
                    }
                }

                return totalCost;
            }
            catch
            {
                return 0;
            }
        }
    }
}
