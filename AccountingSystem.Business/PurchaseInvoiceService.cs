using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    public interface IPurchaseInvoiceService
    {
        Task<List<PurchaseInvoice>> GetAllPurchaseInvoicesAsync();
        Task<PurchaseInvoice?> GetPurchaseInvoiceByIdAsync(int id);
        Task<PurchaseInvoice> CreatePurchaseInvoiceAsync(PurchaseInvoice invoice);
        Task<PurchaseInvoice> UpdatePurchaseInvoiceAsync(PurchaseInvoice invoice);
        Task<bool> DeletePurchaseInvoiceAsync(int id);
        Task<bool> PostPurchaseInvoiceAsync(int id);
        Task<bool> CancelPurchaseInvoiceAsync(int invoiceId);
        Task<List<PurchaseInvoice>> GetPurchaseInvoicesBySupplierAsync(int supplierId);
        Task<List<PurchaseInvoice>> GetPurchaseInvoicesByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<decimal> GetTotalPurchaseAmountAsync(DateTime fromDate, DateTime toDate);
    }

    public class PurchaseInvoiceService : IPurchaseInvoiceService
    {
        private readonly AccountingDbContext _context;

        public PurchaseInvoiceService(AccountingDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================
        // Reads
        // =========================
        public async Task<List<PurchaseInvoice>> GetAllPurchaseInvoicesAsync()
        {
            return await _context.PurchaseInvoices
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.Items).ThenInclude(d => d.Product)
                .Include(p => p.Items).ThenInclude(d => d.Unit)
                .OrderByDescending(p => p.InvoiceDate)
                .ToListAsync();
        }

        public async Task<PurchaseInvoice?> GetPurchaseInvoiceByIdAsync(int id)
        {
            return await _context.PurchaseInvoices
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.Items).ThenInclude(d => d.Product)
                .Include(p => p.Items).ThenInclude(d => d.Unit)
                .FirstOrDefaultAsync(p => p.PurchaseInvoiceId == id);
        }

        public async Task<List<PurchaseInvoice>> GetPurchaseInvoicesBySupplierAsync(int supplierId)
        {
            return await _context.PurchaseInvoices
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.Items).ThenInclude(d => d.Product)
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.InvoiceDate)
                .ToListAsync();
        }

        public async Task<List<PurchaseInvoice>> GetPurchaseInvoicesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.PurchaseInvoices
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.Items).ThenInclude(d => d.Product)
                .Where(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate)
                .OrderByDescending(p => p.InvoiceDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalPurchaseAmountAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.PurchaseInvoices
                .AsNoTracking()
                .Where(p => p.InvoiceDate >= fromDate && p.InvoiceDate <= toDate && p.IsPosted)
                .SumAsync(p => p.NetTotal);
        }

        // =========================
        // Create / Update / Delete
        // =========================
        public async Task<PurchaseInvoice> CreatePurchaseInvoiceAsync(PurchaseInvoice invoice)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // توليد رقم الفاتورة
                invoice.InvoiceNumber = await GenerateInvoiceNumberAsync();
                invoice.CreatedDate   = DateTime.Now;
                invoice.Status        = InvoiceStatus.Draft;
                invoice.IsPosted      = false;

                // حفظ الفاتورة
                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                // تحديث رصيد المورد (نفترض أن Balance = مديونية علينا للمورد)
                var supplier = await _context.Suppliers.FindAsync(invoice.SupplierId);
                if (supplier != null)
                {
                    supplier.Balance += invoice.RemainingAmount;
                    _context.Suppliers.Update(supplier);
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();

                // إعادة القراءة مع العلاقات
                return await GetPurchaseInvoiceByIdAsync(invoice.PurchaseInvoiceId) ?? invoice;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<PurchaseInvoice> UpdatePurchaseInvoiceAsync(PurchaseInvoice invoice)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));

            var existing = await _context.PurchaseInvoices
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PurchaseInvoiceId == invoice.PurchaseInvoiceId);

            if (existing == null)
                throw new InvalidOperationException($"لا توجد فاتورة شراء بالرقم {invoice.PurchaseInvoiceId}.");

            if (existing.IsPosted)
                throw new InvalidOperationException("لا يمكن تعديل فاتورة تم ترحيلها.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var oldRemaining = existing.RemainingAmount;
                var oldSupplierId = existing.SupplierId;

                // تحديث رأس الفاتورة
                existing.InvoiceDate     = invoice.InvoiceDate;
                existing.SupplierId      = invoice.SupplierId;
                existing.SubTotal        = invoice.SubTotal;
                existing.TaxAmount       = invoice.TaxAmount;
                existing.DiscountAmount  = invoice.DiscountAmount;
                existing.NetTotal        = invoice.NetTotal;
                existing.PaidAmount      = invoice.PaidAmount;
                existing.RemainingAmount = invoice.RemainingAmount;
                existing.Notes           = invoice.Notes;

                // استبدال التفاصيل
                _context.PurchaseInvoiceItems.RemoveRange(existing.Items);
                foreach (var d in invoice.Items)
                {
                    d.PurchaseInvoiceId = existing.PurchaseInvoiceId;
                    _context.PurchaseInvoiceItems.Add(d);
                }

                await _context.SaveChangesAsync();

                // ضبط رصيد المورد
                if (oldSupplierId == existing.SupplierId)
                {
                    var supplier = await _context.Suppliers.FindAsync(existing.SupplierId);
                    if (supplier != null)
                    {
                        supplier.Balance = supplier.Balance - oldRemaining + existing.RemainingAmount;
                        _context.Suppliers.Update(supplier);
                    }
                }
                else
                {
                    var oldSupplier = await _context.Suppliers.FindAsync(oldSupplierId);
                    if (oldSupplier != null)
                    {
                        oldSupplier.Balance -= oldRemaining;
                        _context.Suppliers.Update(oldSupplier);
                    }

                    var newSupplier = await _context.Suppliers.FindAsync(existing.SupplierId);
                    if (newSupplier != null)
                    {
                        newSupplier.Balance += existing.RemainingAmount;
                        _context.Suppliers.Update(newSupplier);
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return await GetPurchaseInvoiceByIdAsync(existing.PurchaseInvoiceId) ?? existing;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeletePurchaseInvoiceAsync(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.PurchaseInvoiceId == id);

            if (invoice == null) return false;
            if (invoice.IsPosted)
                throw new InvalidOperationException("لا يمكن حذف فاتورة تم ترحيلها.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // تعديل رصيد المورد
                var supplier = await _context.Suppliers.FindAsync(invoice.SupplierId);
                if (supplier != null)
                {
                    supplier.Balance -= invoice.RemainingAmount;
                    _context.Suppliers.Update(supplier);
                }

                // حذف التفاصيل والرأس
                _context.PurchaseInvoiceItems.RemoveRange(invoice.Items);
                _context.PurchaseInvoices.Remove(invoice);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // =========================
        // Posting
        // =========================
        public async Task<bool> PostPurchaseInvoiceAsync(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .AsTracking()
                .Include(p => p.Items).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(p => p.PurchaseInvoiceId == id);

            if (invoice == null) return false;
            if (invoice.IsPosted)
                throw new InvalidOperationException("الفاتورة مُرحّلة مسبقًا.");

            // تحقق أساسي من البنود
            foreach (var d in invoice.Items)
            {
                if (d.Product is null)
                    throw new InvalidOperationException("بيانات المنتج غير مكتملة في تفاصيل الفاتورة.");
                if (d.Quantity <= 0)
                    throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");
                _ = await _context.Units.FindAsync(d.UnitId)
                    ?? throw new InvalidOperationException("الوحدة غير موجودة.");
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;

                foreach (var detail in invoice.Items)
                {
                    var product = detail.Product!;
                    // تأمين أحدث قيمة للمخزون
                    await _context.Entry(product).ReloadAsync();

                    // تحويل الكمية للوحدة الأساسية
                    var qtyInMain = await ConvertToMainUnitAsync(product, detail.Quantity, detail.UnitId ?? 0);

                    product.CurrentStock += qtyInMain;
                    _context.Products.Update(product);

                    // حركة مخزون (نخزن الكمية كما أدخلها المستخدم + المكافئ بالأساسية)
                    var movement = new StockMovement
                    {
                        ProductId          = detail.ProductId,
                        Product            = product,
                        MovementType       = StockMovementType.In,
                        Quantity           = detail.Quantity,   // كما أدخلها المستخدم
                        UnitId             = detail.UnitId,     // الوحدة المختارة
                        Unit               = await _context.Units.FindAsync(detail.UnitId)
                                              ?? throw new InvalidOperationException("الوحدة غير موجودة."),
                        QuantityInMainUnit = qtyInMain,        // المكافئ بالأساسية
                        ReferenceType      = "PurchaseInvoice",
                        ReferenceId        = invoice.PurchaseInvoiceId,
                        MovementDate       = now,
                        CreatedBy          = invoice.CreatedBy,
                        Notes              = $"مشتريات - فاتورة رقم {invoice.InvoiceNumber}"
                    };
                    await _context.StockMovements.AddAsync(movement);
                }

                // تحديث حالة الفاتورة
                invoice.Status   = InvoiceStatus.Confirmed;
                invoice.IsPosted = true;

                _context.PurchaseInvoices.Update(invoice);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // =========================
        // Helpers
        // =========================
        private async Task<string> GenerateInvoiceNumberAsync()
        {
            // شكل الرقم: PUR-YYYYMM-####  (مراعاة الثقافة اللغوية الثابتة)
            var prefix = "PUR-" + DateTime.Now.ToString("yyyyMM", CultureInfo.InvariantCulture) + "-";

            // آخر رقم بالبادئة الحالية
            var last = await _context.PurchaseInvoices
                .AsNoTracking()
                .Where(p => p.InvoiceNumber != null && p.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(p => p.InvoiceNumber)
                .Select(p => p.InvoiceNumber!)
                .FirstOrDefaultAsync();

            var next = 1;
            if (!string.IsNullOrWhiteSpace(last))
            {
                var numberPart = last.Substring(prefix.Length);
                if (int.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var current))
                    next = current + 1;
            }

            return $"{prefix}{next:D4}";
        }

        public async Task<bool> CancelPurchaseInvoiceAsync(int invoiceId)
        {
            var invoice = await _context.PurchaseInvoices.FirstOrDefaultAsync(p => p.PurchaseInvoiceId == invoiceId);
            if (invoice == null || invoice.Status == InvoiceStatus.Cancelled)
                return false;

            // إلغاء فقط للحالة غير المرحلة (لو محتاج عكس مخزون عند الإلغاء بعد الترحيل، نضيف منطق رجوع)
            if (invoice.IsPosted)
                throw new InvalidOperationException("لا يمكن إلغاء فاتورة مُرحّلة. (يمكنك تنفيذ عكس/مرتجع مشتريات بدلاً من ذلك)");

            invoice.Status = InvoiceStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<decimal> ConvertToMainUnitAsync(Product product, decimal quantity, int fromUnitId)
        {
            if (fromUnitId == product.MainUnitId) return quantity;

            var productUnit = await _context.ProductUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(pu => pu.ProductId == product.ProductId &&
                                           pu.UnitId    == fromUnitId &&
                                           pu.IsActive);
            if (productUnit == null)
                throw new InvalidOperationException("لا توجد علاقة تحويل للوحدة المختارة.");

            if (productUnit.ConversionFactor <= 0)
                throw new InvalidOperationException("معامل التحويل غير صالح.");

            // الكمية بالأساسية = الكمية * معامل التحويل
            return quantity * productUnit.ConversionFactor;
        }
    }
}
