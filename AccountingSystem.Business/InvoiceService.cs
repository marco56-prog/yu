using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    public interface IInvoiceService
    {
        Task<string> GenerateInvoiceNumberAsync(string invoiceType);
        Task<decimal> CalculateTaxAmountAsync(decimal subTotal);
        Task<bool> PostInvoiceAsync(int invoiceId, string invoiceType);
        Task<bool> CanDeleteInvoiceAsync(int invoiceId, string invoiceType);
    }

    public class InvoiceService : IInvoiceService
    {
        private readonly AccountingDbContext _context;
        private readonly INumberSequenceService _numberSequenceService;

        // مرجع ثابت لأسماء الأنواع والـ ReferenceType
        private const string SALES_INVOICE = "SalesInvoice";
        private const string PURCHASE_INVOICE = "PurchaseInvoice";

        public InvoiceService(AccountingDbContext context, INumberSequenceService numberSequenceService)
        {
            _context = context;
            _numberSequenceService = numberSequenceService;
        }

        public async Task<string> GenerateInvoiceNumberAsync(string invoiceType)
        {
            var key = (invoiceType ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("نوع الفاتورة غير صالح.", nameof(invoiceType));

            return await _numberSequenceService.GetNextNumberAsync(key);
        }

        public async Task<decimal> CalculateTaxAmountAsync(decimal subTotal)
        {
            if (subTotal <= 0) return 0m;

            var rate = await GetTaxRatePercentAsync();
            if (rate <= 0) return 0m;

            var tax = subTotal * (rate / 100m);
            return decimal.Round(tax, 2, MidpointRounding.AwayFromZero);
        }

        public async Task<bool> PostInvoiceAsync(int invoiceId, string invoiceType)
        {
            // توحيد نوع الفاتورة
            var type = (invoiceType ?? string.Empty).Trim();
            if (!type.Equals(SALES_INVOICE, StringComparison.OrdinalIgnoreCase) &&
                !type.Equals(PURCHASE_INVOICE, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var now = DateTime.Now;

                    if (type.Equals(SALES_INVOICE, StringComparison.OrdinalIgnoreCase))
                    {
                        var invoice = await _context.SalesInvoices
                            .AsTracking()
                            .Include(i => i.Items)
                                .ThenInclude(d => d.Product)
                            .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId);

                        if (invoice is null || invoice.IsPosted)
                            return false;

                        // تحقق أساسي للبنود
                        foreach (var d in invoice.Items)
                        {
                            if (d.Product is null)
                                throw new InvalidOperationException("بيانات المنتج غير مكتملة في تفاصيل الفاتورة.");
                            if (d.Quantity <= 0)
                                throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");
                            _ = await _context.Units.FindAsync(d.UnitId)
                                ?? throw new InvalidOperationException("الوحدة غير موجودة.");
                        }

                        // حركات المخزون (خروج)
                        foreach (var detail in invoice.Items)
                        {
                            // تأمين أحدث قيمة للمخزون
                            await _context.Entry(detail.Product!).ReloadAsync();

                            // تحويل الكمية للوحدة الأساسية
                            var qtyInMain = await ConvertToMainUnitAsync(detail.Product!, detail.Quantity, detail.UnitId ?? 0);

                            if (detail.Product!.CurrentStock < qtyInMain)
                                throw new InvalidOperationException($"الكمية المطلوبة من '{detail.Product.ProductName}' غير متوفرة.");

                            detail.Product.CurrentStock -= qtyInMain;

                            var stockMovement = new StockMovement
                            {
                                ProductId = detail.ProductId,
                                Product = detail.Product,
                                MovementType = StockMovementType.Out,
                                Quantity = qtyInMain,                // نخزّن دائماً بالأساسية
                                UnitId = detail.Product.MainUnitId,  // الوحدة الأساسية
                                Unit = await _context.Units.FindAsync(detail.Product.MainUnitId)
                                       ?? throw new InvalidOperationException("الوحدة الأساسية غير موجودة."),
                                QuantityInMainUnit = qtyInMain,
                                ReferenceType = SALES_INVOICE,
                                ReferenceId = invoice.SalesInvoiceId,
                                MovementDate = now,
                                CreatedBy = invoice.CreatedBy,
                                Notes = $"مبيعات - فاتورة رقم {invoice.InvoiceNumber}"
                            };
                            await _context.StockMovements.AddAsync(stockMovement);
                        }

                        // تحديث رصيد العميل + معاملة
                        var customer = await _context.Customers
                            .FirstOrDefaultAsync(c => c.CustomerId == invoice.CustomerId);

                        if (customer is not null)
                        {
                            // نفترض Balance = مديونية العميل (يزيد بالباقي غير المدفوع)
                            customer.Balance += invoice.RemainingAmount;

                            var txn = new CustomerTransaction
                            {
                                CustomerId = invoice.CustomerId,
                                Customer = customer,
                                TransactionNumber = await _numberSequenceService.GetNextNumberAsync("CustomerTransaction"),
                                TransactionType = TransactionType.Income, // حسب تصميمك
                                Amount = invoice.NetTotal,
                                Description = $"فاتورة بيع رقم {invoice.InvoiceNumber}",
                                ReferenceType = SALES_INVOICE,
                                ReferenceId = invoice.SalesInvoiceId,
                                TransactionDate = now,
                                CreatedBy = int.TryParse(invoice.CreatedBy, out var createdByInt) ? createdByInt : 1
                            };
                            await _context.CustomerTransactions.AddAsync(txn);
                        }

                        invoice.IsPosted = true;
                        invoice.Status = InvoiceStatus.Confirmed;
                    }
                    else // PURCHASE_INVOICE
                    {
                        var invoice = await _context.PurchaseInvoices
                            .AsTracking()
                            .Include(i => i.Items)
                                .ThenInclude(d => d.Product)
                            .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == invoiceId);

                        if (invoice is null || invoice.IsPosted)
                            return false;

                        foreach (var d in invoice.Items)
                        {
                            if (d.Product is null)
                                throw new InvalidOperationException("بيانات المنتج غير مكتملة في تفاصيل الفاتورة.");
                            if (d.Quantity <= 0)
                                throw new InvalidOperationException("الكمية يجب أن تكون أكبر من صفر.");
                            _ = await _context.Units.FindAsync(d.UnitId)
                                ?? throw new InvalidOperationException("الوحدة غير موجودة.");
                        }

                        // حركات المخزون (دخول)
                        foreach (var detail in invoice.Items)
                        {
                            await _context.Entry(detail.Product!).ReloadAsync();

                            var qtyInMain = await ConvertToMainUnitAsync(detail.Product!, detail.Quantity, detail.UnitId ?? 0);
                            detail.Product!.CurrentStock += qtyInMain;

                            var stockMovement = new StockMovement
                            {
                                ProductId = detail.ProductId,
                                Product = detail.Product,
                                MovementType = StockMovementType.In,
                                Quantity = qtyInMain,
                                UnitId = detail.Product.MainUnitId,
                                Unit = await _context.Units.FindAsync(detail.Product.MainUnitId)
                                       ?? throw new InvalidOperationException("الوحدة الأساسية غير موجودة."),
                                QuantityInMainUnit = qtyInMain,
                                ReferenceType = PURCHASE_INVOICE,
                                ReferenceId = invoice.PurchaseInvoiceId,
                                MovementDate = now,
                                CreatedBy = invoice.CreatedBy,
                                Notes = $"مشتريات - فاتورة رقم {invoice.InvoiceNumber}"
                            };
                            await _context.StockMovements.AddAsync(stockMovement);
                        }

                        // تحديث رصيد المورد (اختياري: تسجيل معاملة للمورد في جدول مستقل)
                        var supplier = await _context.Suppliers
                            .FirstOrDefaultAsync(s => s.SupplierId == invoice.SupplierId);
                        if (supplier is not null)
                        {
                            // نفترض Balance = مديونية علينا للمورد ⇒ يزيد بالباقي غير المدفوع
                            supplier.Balance += invoice.RemainingAmount;
                        }

                        invoice.IsPosted = true;
                        invoice.Status = InvoiceStatus.Confirmed;
                    }

                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    TryWriteInvoiceLog(ex);
                    try 
                    { 
                        await tx.RollbackAsync(); 
                    } 
                    catch (Exception rollbackEx) 
                    { 
                        // تسجيل فشل التراجع - نتجاهله ونستمر
                        System.Diagnostics.Debug.WriteLine($"فشل التراجع: {rollbackEx.Message}");
                    }
                    return false;
                }
            });
        }

        public async Task<bool> CanDeleteInvoiceAsync(int invoiceId, string invoiceType)
        {
            var type = (invoiceType ?? string.Empty).Trim();

            if (type.Equals(SALES_INVOICE, StringComparison.OrdinalIgnoreCase))
            {
                var invoice = await _context.SalesInvoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId);

                if (invoice is null || invoice.IsPosted) return false;

                var hasMovements = await _context.StockMovements
                    .AsNoTracking()
                    .AnyAsync(m => m.ReferenceType == SALES_INVOICE && m.ReferenceId == invoiceId);

                return !hasMovements;
            }
            else if (type.Equals(PURCHASE_INVOICE, StringComparison.OrdinalIgnoreCase))
            {
                var invoice = await _context.PurchaseInvoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == invoiceId);

                if (invoice is null || invoice.IsPosted) return false;

                var hasMovements = await _context.StockMovements
                    .AsNoTracking()
                    .AnyAsync(m => m.ReferenceType == PURCHASE_INVOICE && m.ReferenceId == invoiceId);

                return !hasMovements;
            }

            return false;
        }

        // =========================
        // Helpers
        // =========================
        private async Task<decimal> GetTaxRatePercentAsync()
        {
            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == "TaxRate");

            if (setting == null) return 0m;

            // جرّب Current ثم Invariant
            if (decimal.TryParse(setting.SettingValue, NumberStyles.Any, CultureInfo.CurrentCulture, out var r) ||
                decimal.TryParse(setting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out r))
            {
                if (r < 0) r = 0;
                return r;
            }
            return 0m;
        }

        private async Task<decimal> ConvertToMainUnitAsync(Product product, decimal quantity, int fromUnitId)
        {
            if (quantity < 0)
                throw new InvalidOperationException("الكمية لا يمكن أن تكون سالبة.");

            // لو نفس الوحدة الأساسية
            if (fromUnitId == product.MainUnitId) return quantity;

            var productUnit = await _context.ProductUnits
                .AsNoTracking()
                .Include(pu => pu.Unit)
                .FirstOrDefaultAsync(pu => pu.ProductId == product.ProductId &&
                                           pu.UnitId == fromUnitId &&
                                           pu.IsActive);

            if (productUnit == null)
                throw new InvalidOperationException("لا توجد علاقة تحويل للوحدة المختارة.");

            var factor = productUnit.ConversionFactor;
            if (factor <= 0)
                throw new InvalidOperationException("معامل التحويل غير صالح.");

            // الكمية بالأساسية = quantity * factor
            return quantity * factor;
        }

        private static void TryWriteInvoiceLog(Exception ex)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(baseDir, "invoice_error.log");
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
            }
            catch
            {
                // تجاهل أخطاء التسجيل
            }
        }
    }
}
