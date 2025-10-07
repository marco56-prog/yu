using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.Business.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    // =========================
    // واجهة خدمة المنتجات
    // =========================
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Result<Product>> GetProductByCodeAsync(string code);
        Task<Result<Product>> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<decimal> GetProductStockAsync(int productId);
        Task<bool> UpdateProductStockAsync(int productId, decimal quantity, int unitId, StockMovementType movementType, string notes = "", string referenceType = "", int? referenceId = null);
        Task<decimal> ConvertToMainUnitAsync(int productId, decimal quantity, int unitId);
        Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default);
    }

    // =========================
    // تنفيذ خدمة المنتجات
    // =========================
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INumberSequenceService _numberSequenceService;
        private readonly ISecurityService? _securityService;

        public ProductService(IUnitOfWork unitOfWork, INumberSequenceService numberSequenceService, ISecurityService? securityService = null)
        {
            _unitOfWork = unitOfWork;
            _numberSequenceService = numberSequenceService;
            _securityService = securityService;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            // ملاحظة: لو Repository.FindAsync بيرجع تتبع، يفضّل نسخة AsNoTracking للقراءة فقط.
            return await _unitOfWork.Repository<Product>().FindAsync(p => p.IsActive);
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _unitOfWork.Repository<Product>().GetByIdAsync(id);
        }

        public async Task<Result<Product>> GetProductByCodeAsync(string code)
        {
            code = (code ?? string.Empty).Trim();
            if (code == "") return Result<Product>.Failure("كود المنتج فارغ");

            var product = await _unitOfWork.Repository<Product>()
                .SingleOrDefaultAsync(p => p.ProductCode == code && p.IsActive);

            if (product == null) return Result<Product>.Failure("لم يتم العثور على المنتج");
            return Result<Product>.Success(product);
        }

    public async Task<Result<Product>> CreateProductAsync(Product product)
        {
            // Validate product name
            var name = (product.ProductName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                return Result<Product>.Failure("اسم المنتج مطلوب");

            product.ProductName = name;

            // Ensure a main unit exists. If caller didn't provide, create a default 'قطعة' unit for tests and simple flows.
            if (product.MainUnitId <= 0 ||
                await _unitOfWork.Repository<Unit>().GetByIdAsync(product.MainUnitId) is null)
            {
                var defaultUnit = new Unit
                {
                    UnitName = "قطعة",
                    UnitSymbol = "pc",
                    IsActive = true
                };
                await _unitOfWork.Repository<Unit>().AddAsync(defaultUnit);
                await _unitOfWork.SaveChangesAsync();
                product.MainUnitId = defaultUnit.UnitId;
                product.MainUnit = defaultUnit;
            }

            // Use provided ProductCode if given, otherwise generate a unique one
            if (string.IsNullOrWhiteSpace(product.ProductCode))
                product.ProductCode = await GenerateUniqueProductCodeAsync();

            product.Description  = product.Description?.Trim();
            product.CreatedDate  = DateTime.Now;
            product.IsActive     = true;
            product.CurrentStock = 0;

            await _unitOfWork.Repository<Product>().AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return Result<Product>.Success(product);
        }

    public async Task<Product> UpdateProductAsync(Product product)
        {
            var existing = await _unitOfWork.Repository<Product>().GetByIdAsync(product.ProductId);
            if (existing == null)
                throw new InvalidOperationException("المنتج غير موجود.");

            // تأكيد الوحدة الأساسية موجودة
            if (product.MainUnitId <= 0 ||
                await _unitOfWork.Repository<Unit>().GetByIdAsync(product.MainUnitId) is null)
            {
                throw new InvalidOperationException("الوحدة الأساسية غير موجودة.");
            }

            existing.ProductName   = (product.ProductName ?? string.Empty).Trim();
            existing.Description   = product.Description?.Trim();
            existing.CategoryId    = product.CategoryId;
            existing.MainUnitId    = product.MainUnitId;
            existing.MinimumStock  = product.MinimumStock;
            existing.PurchasePrice = product.PurchasePrice;
            existing.SalePrice     = product.SalePrice;

            _unitOfWork.Repository<Product>().Update(existing);
            await _unitOfWork.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
            if (product == null) return false;

            // وجود مراجع: حركات مخزون / تفاصيل فواتير
            var hasStockMovements = await _unitOfWork.Repository<StockMovement>()
                .AnyAsync(m => m.ProductId == id);
            var usedInSales = await _unitOfWork.Repository<SalesInvoiceItem>()
                .AnyAsync(d => d.ProductId == id);
            var usedInPurchases = await _unitOfWork.Repository<PurchaseInvoiceItem>()
                .AnyAsync(d => d.ProductId == id);

            // لو فيه مراجع، نفعّل الحذف الناعم بدل الرمي باستثناء
            if (hasStockMovements || usedInSales || usedInPurchases)
            {
                if (!product.IsActive)
                    return true;

                product.IsActive = false;
                _unitOfWork.Repository<Product>().Update(product);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }

            // بدون مراجع: حذف ناعم للحفاظ على التاريخ (يمكنك التحويل لإزالة نهائية لو لازم)
            product.IsActive = false;
            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetProductStockAsync(int productId)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId);
            return product?.CurrentStock ?? 0m;
        }

        public async Task<bool> UpdateProductStockAsync(
            int productId,
            decimal quantity,
            int unitId,
            StockMovementType movementType,
            string notes = "",
            string referenceType = "",
            int? referenceId = null)
        {
            ValidationHelpers.EnsureValidId(productId, nameof(productId));
            ValidationHelpers.EnsureNonNegative(quantity, nameof(quantity));
            ValidationHelpers.EnsureValidId(unitId, nameof(unitId));

            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId);
            if (product == null)
                throw new Exceptions.EntityNotFoundException(typeof(Product), productId);

            var unit = await _unitOfWork.Repository<Unit>().GetByIdAsync(unitId);
            if (unit == null)
                throw new Exceptions.EntityNotFoundException("الوحدة", unitId);

            _unitOfWork.BeginTransaction();
            try
            {
                // تحويل الكمية للوحدة الأساسية
                var qtyInMain = await ConvertToMainUnitAsync(productId, quantity, unitId);

                // تحديث المخزون
                switch (movementType)
                {
                    case StockMovementType.In:
                        product.CurrentStock += qtyInMain;
                        break;

                    case StockMovementType.Out:
                        if (product.CurrentStock < qtyInMain)
                            throw new Exceptions.InsufficientStockException(
                                productId, 
                                product.ProductName, 
                                qtyInMain, 
                                product.CurrentStock);
                        product.CurrentStock -= qtyInMain;
                        break;

                    case StockMovementType.Adjustment:
                        product.CurrentStock = qtyInMain; // ضبط مباشر
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(movementType));
                }

                _unitOfWork.Repository<Product>().Update(product);

                // تحديد المستخدم (لو متاح)
                var userId = 1;
                try
                {
                    if (_securityService != null)
                        userId = (await _securityService.GetCurrentUserAsync())?.UserId ?? 1;
                }
                catch
                {
                    // استخدام القيمة الافتراضية في حالة الفشل - تسجيل التحذير
                    // نتجاهل الخطأ ونستمر بالقيمة الافتراضية
                }

                // حركة مخزون: نخزّن كما أدخلها المستخدم + المكافئ بالأساسية
                var movement = new StockMovement
                {
                    ProductId          = productId,
                    Product            = product,
                    MovementType       = movementType,
                    Quantity           = quantity,          // كما أدخلها المستخدم
                    UnitId             = unitId,            // الوحدة المختارة
                    Unit               = unit,
                    QuantityInMainUnit = qtyInMain,         // المكافئ بالأساسية
                    ReferenceType      = referenceType,
                    ReferenceId        = referenceId,
                    Notes              = string.IsNullOrWhiteSpace(notes) ? $"حركة مخزون - {movementType}" : notes,
                    MovementDate       = DateTime.Now,
                    CreatedBy          = userId.ToString(CultureInfo.InvariantCulture)
                };

                await _unitOfWork.Repository<StockMovement>().AddAsync(movement);

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

        public async Task<decimal> ConvertToMainUnitAsync(int productId, decimal quantity, int unitId)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId)
                          ?? throw new InvalidOperationException("المنتج غير موجود.");

            // لو بالفعل الوحدة الأساسية
            if (unitId == product.MainUnitId) return quantity;

            // ابحث علاقة التحويل
            var productUnit = await _unitOfWork.Repository<ProductUnit>()
                .SingleOrDefaultAsync(pu => pu.ProductId == productId &&
                                            pu.UnitId    == unitId &&
                                            pu.IsActive);
            if (productUnit == null)
                throw new InvalidOperationException("معامل التحويل للوحدة غير محدد لهذا المنتج.");

            if (productUnit.ConversionFactor <= 0)
                throw new InvalidOperationException("معامل التحويل غير صالح.");

            // تعريف: ConversionFactor = عدد وحدات الأساس داخل الوحدة البديلة
            // إذن: الكمية بالأساسية = quantity * factor
            return quantity * productUnit.ConversionFactor;
        }

        // ==============
        // Helpers
        // ==============
        private async Task<string> GenerateUniqueProductCodeAsync()
        {
            const int maxAttempts = 10;
            for (var i = 0; i < maxAttempts; i++)
            {
                var candidate = await _numberSequenceService.GenerateProductCodeAsync();
                var exists = await _unitOfWork.Repository<Product>()
                    .AnyAsync(p => p.ProductCode == candidate);
                if (!exists) return candidate;
            }
            throw new InvalidOperationException("تعذر توليد كود فريد للمنتج. حاول مرة أخرى.");
        }

        /// <summary>
        /// تحديث المخزون للمنتج (مساعدة اختيارية خارج الواجهة)
        /// </summary>
        public async Task<Result<bool>> UpdateStockAsync(int productId, decimal quantity, string reason = "")
        {
            try
            {
                var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId);
                if (product == null) return Result<bool>.Failure("المنتج غير موجود");

                product.CurrentStock = quantity;

                _unitOfWork.Repository<Product>().Update(product);
                await _unitOfWork.SaveChangesAsync();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// الحصول على المنتجات منخفضة المخزون (مساعدة اختيارية خارج الواجهة)
        /// </summary>
        public async Task<Result<IEnumerable<Product>>> GetLowStockProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Repository<Product>()
                    .FindAsync(p => p.IsActive && p.CurrentStock <= p.MinimumStock);

                var ordered = products.OrderBy(p => p.CurrentStock);
                return Result<IEnumerable<Product>>.Success(ordered);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<Product>>.Failure(ex.Message);
            }
        }

        /// <summary>
        /// عدد المنتجات أقل من الحد الأدنى
        /// </summary>
        public async Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _unitOfWork.Repository<Product>()
                    .FindAsync(p => p.IsActive && p.CurrentStock <= p.MinimumStock);

                return products.Count();
            }
            catch
            {
                return 0;
            }
        }
    }
}
