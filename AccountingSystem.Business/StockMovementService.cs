using System;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    public interface IStockMovementService
    {
        Task RecordSalesMovementAsync(int productId, decimal quantity, int unitId, int salesInvoiceId, string notes = "");
        Task RecordPurchaseMovementAsync(int productId, decimal quantity, int unitId, int purchaseInvoiceId, string notes = "");
        Task RecordAdjustmentMovementAsync(int productId, decimal quantity, int unitId, string notes = "", int userId = 1);
        Task UpdateProductStockAsync(int productId);
        Task<decimal> GetCurrentStockAsync(int productId);
    }

    /// <summary>
    /// خدمة إدارة حركات المخزون
    /// - نخزّن Quantity بالوحدة المدخلة، و QuantityInMainUnit بالمكافئ للوحدة الأساسية.
    /// - Adjustment يُعامل كفارق (delta): موجب للزيادة وسالب للنقص.
    /// - كل عملية داخل Transaction لضمان الاتساق.
    /// </summary>
    public class StockMovementService : IStockMovementService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StockMovementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task RecordSalesMovementAsync(int productId, decimal quantity, int unitId, int salesInvoiceId, string notes = "")
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "الكمية يجب أن تكون أكبر من صفر.");

            var (product, unit) = await GetProductAndUnitOrThrow(productId, unitId);
            var qtyMain = await ConvertToMainUnitStrictAsync(product, quantity, unitId);

            // تحقق توفر الكمية قبل الخصم
            if (product.CurrentStock < qtyMain)
                throw new InvalidOperationException($"الكمية المطلوبة غير متوفرة. المتاح: {product.CurrentStock}");

            _unitOfWork.BeginTransaction();
            try
            {
                var movement = new StockMovement
                {
                    ProductId = productId,
                    Product = product,
                    MovementType = StockMovementType.Out,
                    Quantity = quantity,  // كما أدخلها المستخدم
                    UnitId = unitId,
                    Unit = unit,
                    QuantityInMainUnit = qtyMain,   // مكافئ الأساسية
                    ReferenceType = "SalesInvoice",
                    ReferenceId = salesInvoiceId,
                    Notes = string.IsNullOrWhiteSpace(notes) ? $"صرف مبيعات - فاتورة رقم {salesInvoiceId}" : notes,
                    MovementDate = DateTime.UtcNow,
                    CreatedBy = "1"
                };

                await _unitOfWork.Repository<StockMovement>().AddAsync(movement);

                // خصم من رصيد المنتج
                product.CurrentStock -= qtyMain;
                _unitOfWork.Repository<Product>().Update(product);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        public async Task RecordPurchaseMovementAsync(int productId, decimal quantity, int unitId, int purchaseInvoiceId, string notes = "")
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "الكمية يجب أن تكون أكبر من صفر.");

            var (product, unit) = await GetProductAndUnitOrThrow(productId, unitId);
            var qtyMain = await ConvertToMainUnitStrictAsync(product, quantity, unitId);

            _unitOfWork.BeginTransaction();
            try
            {
                var movement = new StockMovement
                {
                    ProductId = productId,
                    Product = product,
                    MovementType = StockMovementType.In,
                    Quantity = quantity,
                    UnitId = unitId,
                    Unit = unit,
                    QuantityInMainUnit = qtyMain,
                    ReferenceType = "PurchaseInvoice",
                    ReferenceId = purchaseInvoiceId,
                    Notes = string.IsNullOrWhiteSpace(notes) ? $"وارد مشتريات - فاتورة رقم {purchaseInvoiceId}" : notes,
                    MovementDate = DateTime.UtcNow,
                    CreatedBy = "1"
                };

                await _unitOfWork.Repository<StockMovement>().AddAsync(movement);

                // زيادة رصيد المنتج
                product.CurrentStock += qtyMain;
                _unitOfWork.Repository<Product>().Update(product);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// Adjustment هنا هو تغيير بالفرق (delta) على المخزون.
        /// مثال: لخفض المخزون 5 من عبوة (unitId)، أرسل quantity = -5.
        /// </summary>
        public async Task RecordAdjustmentMovementAsync(int productId, decimal quantity, int unitId, string notes = "", int userId = 1)
        {
            if (quantity == 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "لا يوجد تغيير على المخزون (الكمية = 0).");

            var (product, unit) = await GetProductAndUnitOrThrow(productId, unitId);

            // نحسب بالمطلق ثم نعيد الإشارة
            var qtyMainAbs = await ConvertToMainUnitStrictAsync(product, Math.Abs(quantity), unitId);
            var qtyMain = quantity < 0 ? -qtyMainAbs : qtyMainAbs;

            // منع الوصول لرصيد سالب
            var newStock = product.CurrentStock + qtyMain;
            if (newStock < 0)
                throw new InvalidOperationException($"لا يمكن أن يكون رصيد المخزون سالباً. الناتج سيكون {newStock}.");

            _unitOfWork.BeginTransaction();
            try
            {
                var movement = new StockMovement
                {
                    ProductId = productId,
                    Product = product,
                    MovementType = StockMovementType.Adjustment,
                    Quantity = quantity,
                    UnitId = unitId,
                    Unit = unit,
                    QuantityInMainUnit = qtyMain,
                    ReferenceType = "Adjustment",
                    ReferenceId = null,
                    Notes = string.IsNullOrWhiteSpace(notes) ? "تسوية مخزون" : notes,
                    MovementDate = DateTime.UtcNow,
                    CreatedBy = userId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                };

                await _unitOfWork.Repository<StockMovement>().AddAsync(movement);

                product.CurrentStock = newStock;
                _unitOfWork.Repository<Product>().Update(product);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }

        /// <summary>
        /// يعيد احتساب CurrentStock من جدول الحركات (In - Out + Adjustment) مباشرة من قاعدة البيانات.
        /// مفيد للمزامنة في حال تغييرات قديمة.
        /// </summary>
        public async Task UpdateProductStockAsync(int productId)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId)
                          ?? throw new ArgumentException($"المنتج برقم {productId} غير موجود", nameof(productId));

            // استعلام واحد أساسي ثم فلاتر، مع AsNoTracking لتخفيف الحمل
            var baseQ = _unitOfWork.Context.StockMovements
                .AsNoTracking()
                .Where(m => m.ProductId == productId);

            var inSum = await baseQ.Where(m => m.MovementType == StockMovementType.In)
                                    .SumAsync(m => (decimal?)m.QuantityInMainUnit) ?? 0m;

            var outSum = await baseQ.Where(m => m.MovementType == StockMovementType.Out)
                                    .SumAsync(m => (decimal?)m.QuantityInMainUnit) ?? 0m;

            var adjSum = await baseQ.Where(m => m.MovementType == StockMovementType.Adjustment)
                                    .SumAsync(m => (decimal?)m.QuantityInMainUnit) ?? 0m;

            product.CurrentStock = inSum - outSum + adjSum;
            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<decimal> GetCurrentStockAsync(int productId)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId);
            return product?.CurrentStock ?? 0m;
        }

        // ===================== Helpers =====================

        private async Task<(Product product, Unit unit)> GetProductAndUnitOrThrow(int productId, int unitId)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId)
                          ?? throw new ArgumentException($"المنتج برقم {productId} غير موجود", nameof(productId));

            var unit = await _unitOfWork.Repository<Unit>().GetByIdAsync(unitId)
                       ?? throw new ArgumentException($"الوحدة برقم {unitId} غير موجودة", nameof(unitId));

            if (!product.IsActive) throw new InvalidOperationException("المنتج غير نشط.");
            if (!unit.IsActive) throw new InvalidOperationException("الوحدة غير نشطة.");

            return (product, unit);
        }

        /// <summary>
        /// تحويل الكمية إلى الوحدة الأساسية للمنتج بشكل صارم:
        /// - لو كانت نفس الوحدة، يرجع نفس الكمية.
        /// - لو لا توجد علاقة تحويل نشطة، يرمي استثناء (لا يعتمد 1 افتراضياً).
        /// </summary>
        private async Task<decimal> ConvertToMainUnitStrictAsync(Product product, decimal quantity, int unitId)
        {
            if (unitId == product.MainUnitId) return quantity;

            var productUnit = await _unitOfWork.Repository<ProductUnit>()
                .SingleOrDefaultAsync(pu => pu.ProductId == product.ProductId &&
                                            pu.UnitId == unitId &&
                                            pu.IsActive);

            if (productUnit == null || productUnit.ConversionFactor <= 0)
                throw new InvalidOperationException("لا توجد علاقة تحويل صالحة للوحدة المختارة لهذا المنتج.");

            return quantity * productUnit.ConversionFactor;
        }
    }
}
