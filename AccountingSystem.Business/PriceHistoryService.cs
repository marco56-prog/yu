using AccountingSystem.Data;
using AccountingSystem.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    public interface IPriceHistoryService
    {
        Task<decimal> GetLastPriceAsync(int productId);
        Task UpdatePriceAsync(int productId, decimal newPrice, int userId = 1);
        
        /// <summary>
        /// يحصل على آخر سعر بيع للعميل لمنتج ووحدة معيّنة من تاريخ الفواتير
        /// إن لم يوجد يرجع سعر المنتج العام
        /// </summary>
        Task<decimal?> GetLastCustomerPriceAsync(int customerId, int productId, int unitId);
    }

    public class PriceHistoryService : IPriceHistoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PriceHistoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<decimal> GetLastPriceAsync(int productId)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId);
            return product?.SalePrice ?? 0m;
        }

        /// <summary>
        /// يحصل على آخر سعر بيع للعميل من تاريخ فواتير البيع
        /// يبحث في SalesInvoiceItems عن آخر سعر للعميل/المنتج/الوحدة
        /// إن لم يوجد يرجع سعر المنتج العام
        /// </summary>
        public async Task<decimal?> GetLastCustomerPriceAsync(int customerId, int productId, int unitId)
        {
            try
            {
                // استخدام Context مباشرة للحصول على آخر سعر بيع للعميل
                var lastSalePrice = await _unitOfWork.Context.SalesInvoiceItems
                    .Where(item => item.ProductId == productId && 
                                   item.UnitId == unitId &&
                                   item.SalesInvoice.CustomerId == customerId &&
                                   item.SalesInvoice.IsPosted)
                    .OrderByDescending(item => item.SalesInvoice.InvoiceDate)
                    .ThenByDescending(item => item.SalesInvoice.SalesInvoiceId)
                    .Select(item => (decimal?)item.UnitPrice)
                    .FirstOrDefaultAsync();
                
                // إن وُجد سعر سابق، أرجعه
                if (lastSalePrice.HasValue)
                    return lastSalePrice.Value;
                
                // وإلا أرجع السعر العام للمنتج
                var productRepo = _unitOfWork.Repository<Product>();
                var product = await productRepo.GetByIdAsync(productId);
                return product?.SalePrice;
            }
            catch
            {
                // في حالة خطأ، أرجع السعر العام للمنتج
                var productRepo = _unitOfWork.Repository<Product>();
                var product = await productRepo.GetByIdAsync(productId);
                return product?.SalePrice;
            }
        }

        public async Task UpdatePriceAsync(int productId, decimal newPrice, int userId = 1)
        {
            // تحقق أساسي
            if (newPrice < 0m) throw new ArgumentOutOfRangeException(nameof(newPrice), "السعر لا يمكن أن يكون سالباً.");

            _unitOfWork.BeginTransaction();
            try
            {
                var productRepo = _unitOfWork.Repository<Product>();
                var product = await productRepo.GetByIdAsync(productId);
                if (product == null)
                    throw new InvalidOperationException("المنتج غير موجود.");

                // لو مفيش تغيير فعلي، بلاش حفظ
                if (product.SalePrice == newPrice)
                {
                    await _unitOfWork.CommitTransactionAsync();
                    return;
                }

                product.SalePrice = newPrice;
                productRepo.Update(product);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                _unitOfWork.RollbackTransaction();
                throw;
            }
        }
    }
}
