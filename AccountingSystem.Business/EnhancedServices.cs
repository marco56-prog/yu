using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    // =========================
    // CategoryService
    // =========================
    public class CategoryService : ICategoryService
    {
        private readonly AccountingDbContext _context;

        public CategoryService(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int categoryId)
        {
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        public async Task<Category> CreateCategoryAsync(Category category)
        {
            // التحقق من الاسم وتوحيده
            var name = (category.CategoryName ?? string.Empty).Trim();
            var nameNorm = name.ToLowerInvariant();

            var exists = await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.CategoryName.Equals(nameNorm, StringComparison.OrdinalIgnoreCase));

            if (exists)
                throw new InvalidOperationException("اسم الفئة موجود بالفعل.");

            category.CategoryName = name;
            if (!category.IsActive) category.IsActive = true;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            var existing = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == category.CategoryId);

            if (existing == null)
                throw new InvalidOperationException("الفئة غير موجودة.");

            var newName = (category.CategoryName ?? string.Empty).Trim();

            var duplicate = await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.CategoryId != category.CategoryId && c.CategoryName.Equals(newName, StringComparison.OrdinalIgnoreCase));

            if (duplicate)
                throw new InvalidOperationException("اسم الفئة موجود بالفعل.");

            existing.CategoryName = newName;
            existing.Description  = category.Description;
            existing.IsActive     = category.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null) return false;

            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == categoryId);

            if (hasProducts)
            {
                // في حالة وجود منتجات: إلغاء التفعيل بدل الحذف
                if (category.IsActive)
                {
                    category.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }

    // =========================
    // UnitService
    // =========================
    public class UnitService : IUnitService
    {
        private readonly AccountingDbContext _context;

        public UnitService(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Unit>> GetAllUnitsAsync()
        {
            return await _context.Units
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.UnitName)
                .ToListAsync();
        }

        public async Task<Unit?> GetUnitByIdAsync(int unitId)
        {
            return await _context.Units
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UnitId == unitId);
        }

        public async Task<Unit> CreateUnitAsync(Unit unit)
        {
            var name   = (unit.UnitName   ?? string.Empty).Trim();
            var symbol = (unit.UnitSymbol ?? string.Empty).Trim();

            var nameNorm   = name.ToLowerInvariant();
            var symbolNorm = symbol.ToLowerInvariant();

            var dup = await _context.Units
                .AsNoTracking()
                .AnyAsync(u =>
                    u.UnitName.Equals(nameNorm, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(symbol) && u.UnitSymbol != null && u.UnitSymbol.Equals(symbolNorm, StringComparison.OrdinalIgnoreCase)));

            if (dup)
                throw new InvalidOperationException("الاسم/الرمز مستخدم بالفعل.");

            unit.UnitName = name;
            unit.UnitSymbol = symbol;
            if (!unit.IsActive) unit.IsActive = true;

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task<Unit> UpdateUnitAsync(Unit unit)
        {
            var existing = await _context.Units
                .FirstOrDefaultAsync(u => u.UnitId == unit.UnitId);

            if (existing == null)
                throw new InvalidOperationException("الوحدة غير موجودة.");

            var name   = (unit.UnitName   ?? string.Empty).Trim();
            var symbol = (unit.UnitSymbol ?? string.Empty).Trim();

            var nameNorm   = name.ToLowerInvariant();
            var symbolNorm = symbol.ToLowerInvariant();

            var dup = await _context.Units
                .AsNoTracking()
                .AnyAsync(u => u.UnitId != unit.UnitId &&
                               (u.UnitName.Equals(nameNorm, StringComparison.OrdinalIgnoreCase) ||
                                (!string.IsNullOrEmpty(symbol) && u.UnitSymbol != null && u.UnitSymbol.Equals(symbolNorm, StringComparison.OrdinalIgnoreCase))));

            if (dup)
                throw new InvalidOperationException("الاسم/الرمز مستخدم بالفعل.");

            existing.UnitName = name;
            existing.UnitSymbol = symbol;
            existing.IsActive = unit.IsActive;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteUnitAsync(int unitId)
        {
            var unit = await _context.Units
                .FirstOrDefaultAsync(u => u.UnitId == unitId);

            if (unit == null) return false;

            // تحقق من الاستخدامات المرتبطة
            var usedAsMainUnit = await _context.Products.AnyAsync(p => p.MainUnitId == unitId);
            var usedInProductUnits = await _context.ProductUnits.AnyAsync(pu => pu.UnitId == unitId);
            var usedInInvoices = await _context.SalesInvoiceItems.AnyAsync(d => d.UnitId == unitId) ||
                                 await _context.PurchaseInvoiceItems.AnyAsync(d => d.UnitId == unitId);

            if (usedAsMainUnit || usedInProductUnits || usedInInvoices)
            {
                // إلغاء التفعيل بدل الحذف للحفاظ على التكامل المرجعي
                if (unit.IsActive)
                {
                    unit.IsActive = false;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                _context.Units.Remove(unit);
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }

    // =========================
    // EnhancedProductService
    // =========================
    // ملاحظة: يعتمد على IEnhancedProductService بالإضافة إلى IProductService.
    public class EnhancedProductService : IEnhancedProductService
    {
        private readonly AccountingDbContext _context;
        private readonly IProductService _productService;

        public EnhancedProductService(AccountingDbContext context, IProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        // ==== IProductService delegation ====
        public Task<IEnumerable<Product>> GetAllProductsAsync() => _productService.GetAllProductsAsync();

        public Task<Product?> GetProductByIdAsync(int id) => _productService.GetProductByIdAsync(id);

        // Implement IProductService-compatible method via delegation when requested by consumers
        public Task<Result<Product>> GetProductByCodeAsync(string code)
        {
            // Delegate to underlying product service if available, otherwise perform lookup
            if (_productService != null)
                return _productService.GetProductByCodeAsync(code);
            return GetProductByCodeInternalAsync(code);
        }

        // Internal helper that returns Result<Product>
        private async Task<Result<Product>> GetProductByCodeInternalAsync(string code)
        {
            code = (code ?? string.Empty).Trim();
            if (code == "") return Result.Failure<Product>("كود المنتج فارغ");

            var codeNorm = code.ToLowerInvariant();

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductCode.Equals(codeNorm, StringComparison.OrdinalIgnoreCase));

            if (product == null) return Result.Failure<Product>("لم يتم العثور على المنتج");
            return Result.Success(product);
        }

        // If a consumer expects a nullable Product (legacy), they can call GetProductByCodeInternalAsync and
        // read Data. We keep the Result<T>-based public method for consistency.

        public Task<Result<Product>> CreateProductAsync(Product product) => _productService.CreateProductAsync(product);

        public Task<Product> UpdateProductAsync(Product product) => _productService.UpdateProductAsync(product);

        public Task<bool> DeleteProductAsync(int id) => _productService.DeleteProductAsync(id);

        public Task<int> GetLowStockCountAsync(CancellationToken cancellationToken = default) => 
            _productService.GetLowStockCountAsync(cancellationToken);

        // ==== مخزون ====
        public async Task<decimal> GetProductStockAsync(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);
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
            // تحويل الكمية لوحدة الصنف الرئيسية
            var qtyInMain = await ConvertToMainUnitAsync(productId, quantity, unitId);
            return await UpdateStockAsync(productId, qtyInMain, movementType, notes, referenceType, referenceId);
        }

        public async Task<decimal> ConvertToMainUnitAsync(int productId, decimal quantity, int unitId)
        {
            // ConversionFactor = عدد وحدات "الرئيسية" المقابلة لوحدة الإدخال
            // => الكمية بوحدة الأساسية = quantity * factor
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.MainUnit)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) throw new InvalidOperationException("المنتج غير موجود.");

            if (unitId == product.MainUnitId) return quantity;

            var productUnit = await _context.ProductUnits
                .AsNoTracking()
                .Include(pu => pu.Unit)
                .FirstOrDefaultAsync(pu => pu.ProductId == productId && pu.UnitId == unitId && pu.IsActive);

            if (productUnit == null)
                throw new InvalidOperationException("لا توجد علاقة تحويل معتمدة لهذه الوحدة.");

            var factor = productUnit.ConversionFactor;
            if (factor <= 0) throw new InvalidOperationException("معامل التحويل غير صالح.");

            return quantity * factor;
        }

        // نسخة بسيطة للتحديث بدون تفاصيل إضافية
        public async Task<bool> UpdateStockAsync(int productId, decimal quantity, StockMovementType movementType)
            => await UpdateStockAsync(productId, quantity, movementType, notes: $"حركة مخزون - {movementType}");

        private async Task<bool> UpdateStockAsync(
            int productId,
            decimal quantityInMainUnit,
            StockMovementType movementType,
            string notes,
            string? referenceType = null,
            int? referenceId = null)
        {
            // إدارة المعاملة لضمان الاتساق
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                    throw new InvalidOperationException("المنتج غير موجود.");

                // جلب الوحدة الرئيسية (للتسجيل في الحركة)
                var unit = await _context.Units.FirstOrDefaultAsync(u => u.UnitId == product.MainUnitId);
                if (unit == null)
                    throw new InvalidOperationException("الوحدة الرئيسية غير موجودة.");

                switch (movementType)
                {
                    case StockMovementType.In:
                        product.CurrentStock += quantityInMainUnit;
                        break;

                    case StockMovementType.Out:
                        if (product.CurrentStock < quantityInMainUnit)
                            throw new InvalidOperationException("الكمية المطلوبة غير متاحة في المخزون.");
                        product.CurrentStock -= quantityInMainUnit;
                        break;

                    case StockMovementType.Adjustment:
                        product.CurrentStock = quantityInMainUnit;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(movementType));
                }

                // مبدئيًا نستخدم رقم مستخدم ثابت إلى أن يتم ربط نظام المستخدمين
                var userId = 1;

                var movement = new StockMovement
                {
                    ProductId = productId,
                    Product = product,
                    MovementType = movementType,
                    Quantity = quantityInMainUnit,         // الكمية بوحدة الإدخال (هنا نفس الرئيسية)
                    UnitId = product.MainUnitId,
                    Unit = unit,
                    QuantityInMainUnit = quantityInMainUnit,
                    MovementDate = DateTime.Now,
                    CreatedBy = userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Notes = string.IsNullOrWhiteSpace(notes) ? $"حركة مخزون - {movementType}" : notes,
                    ReferenceType = referenceType ?? string.Empty,
                    ReferenceId = referenceId
                };

                _context.StockMovements.Add(movement);

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

        // ==== استعلامات إضافية ====
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.MainUnit)
                .Where(p => p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.MainUnit)
                .Where(p => p.IsActive && p.CurrentStock <= p.MinimumStock)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<decimal> GetProductStockValueAsync(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return 0m;

            return product.CurrentStock * product.PurchasePrice;
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        {
            searchTerm = (searchTerm ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(searchTerm))
                return await GetAllProductsAsync();

            // استخدام Like لتحسين الأداء مع الفهارس
            var pattern = $"%{searchTerm}%";

            return await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.MainUnit)
                .Where(p => p.IsActive &&
                            (EF.Functions.Like(p.ProductName, pattern) ||
                             EF.Functions.Like(p.ProductCode, pattern) ||
                             (p.Description != null && EF.Functions.Like(p.Description, pattern))))
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }
    }
}
