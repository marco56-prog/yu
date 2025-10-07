using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AccountingSystem.Models;

namespace AccountingSystem.Business
{
    // =========================
    // الفئات والوحدات
    // =========================

    /// <summary>
    /// واجهة خدمة إدارة الفئات (Category).
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>إرجاع جميع الفئات النشطة (أو حسب منطقك الداخلي).</summary>
        Task<IEnumerable<Category>> GetAllCategoriesAsync();

        /// <summary>جلب فئة حسب المعرّف.</summary>
        Task<Category?> GetCategoryByIdAsync(int categoryId);

        /// <summary>إنشاء فئة جديدة.</summary>
        Task<Category> CreateCategoryAsync(Category category);

        /// <summary>تحديث فئة موجودة.</summary>
        Task<Category> UpdateCategoryAsync(Category category);

        /// <summary>حذف/تعطيل فئة حسب المعرّف (بحسب منطقك).</summary>
        Task<bool> DeleteCategoryAsync(int categoryId);
    }

    /// <summary>
    /// واجهة خدمة إدارة الوحدات (Unit).
    /// </summary>
    public interface IUnitService
    {
        /// <summary>إرجاع جميع الوحدات النشطة.</summary>
        Task<IEnumerable<Unit>> GetAllUnitsAsync();

        /// <summary>جلب وحدة بالمعرّف.</summary>
        Task<Unit?> GetUnitByIdAsync(int unitId);

        /// <summary>إنشاء وحدة جديدة.</summary>
        Task<Unit> CreateUnitAsync(Unit unit);

        /// <summary>تحديث وحدة موجودة.</summary>
        Task<Unit> UpdateUnitAsync(Unit unit);

        /// <summary>حذف/تعطيل وحدة (بحسب منطقك).</summary>
        Task<bool> DeleteUnitAsync(int unitId);
    }

    // =========================
    // المنتجات (المحسّن) - يرث من IProductService المعرّف في ProductService.cs
    // =========================

    /// <summary>
    /// واجهة خدمة المنتجات المحسّنة. ترث من <c>IProductService</c> وتضيف عمليات مخزون/بحث إضافية.
    /// ملاحظة: استخدمت <c>new</c> فقط لإظهار التوسعة بدون تغيير توقيعات الأساس.
    /// </summary>
    public interface IEnhancedProductService : IProductService
    {
        // الموجودة عندك أصلاً:
        /// <summary>إرجاع المنتجات ضمن فئة محددة.</summary>
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);

        /// <summary>إرجاع المنتجات منخفضة المخزون (CurrentStock &lt;= MinimumStock).</summary>
        Task<IEnumerable<Product>> GetLowStockProductsAsync();

        /// <summary>تحديث المخزون مباشرة بوحدة الصنف الرئيسية.</summary>
        Task<bool> UpdateStockAsync(int productId, decimal quantity, StockMovementType movementType);

        /// <summary>احتساب القيمة المخزنية للصنف (مثلاً CurrentStock × PurchasePrice).</summary>
        Task<decimal> GetProductStockValueAsync(int productId);

        /// <summary>بحث عام عن المنتجات (بالاسم/الكود/الوصف).</summary>
        Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);

    // ==== توسعة مفيدة (متوافقة مع الخدمات اللي بعتهالك) ====
    // لا تعيد تعاريف للتواقيع الموجودة في IProductService هنا لتجنب الازدواج.
    }

    // =========================
    // تاريخ الأسعار للعميل – واجهة خفيفة مطلوبة للنافذة
    // =========================

    /// <summary>
    /// واجهة خدمة تاريخ أسعار بيع المنتج لعميل معيّن.
    /// </summary>
    public interface ICustomerPriceHistoryService
    {
        /// <summary>
        /// إحضار آخر سعر بيع لعميل/منتج/وحدة معيّنة، إن وجد.
        /// </summary>
        Task<CustomerProductPriceInfo?> GetCustomerLastPriceAsync(int customerId, int productId, int unitId);
    }

    /// <summary>
    /// معلومات آخر سعر بيع لعميل/منتج/وحدة.
    /// </summary>
    public class CustomerProductPriceInfo
    {
        /// <summary>آخر سعر بيع مسجّل.</summary>
        public decimal LastSalePrice { get; set; }

        /// <summary>تاريخ آخر عملية بيع عند توفره.</summary>
        public DateTime? LastSaleDate { get; set; }
    }
}
