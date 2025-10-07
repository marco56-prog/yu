using System;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models;

namespace AccountingSystem.Business
{
    public interface IPriceListService
    {
        Task<decimal> GetCustomerPriceAsync(int customerId, int productId);
        Task<bool> HasCustomerPriceAsync(int customerId, int productId);
        Task SetCustomerPriceAsync(int customerId, int productId, decimal price, int userId = 1);
    }

    public class PriceListService : IPriceListService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PriceListService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// يرجّع سعر بيع المنتج. حاليًا: السعر العام (SalePrice) لعدم وجود جدول أسعار لكل عميل.
        /// TODO: لو هتضيف جدول مثل CustomerProductPrice، اعمل Lookup هنا أولاً ثم ارجع للسعر العام كـ fallback.
        /// </summary>
        public async Task<decimal> GetCustomerPriceAsync(int customerId, int productId)
        {
            var productRepo = _unitOfWork.Repository<Product>();
            var product = await productRepo.GetByIdAsync(productId);
            return product?.SalePrice ?? 0m;
        }

        /// <summary>
        /// يتحقق هل للعميل سعر خاص للمنتج.
        /// حاليًا: يرجّع false لعدم وجود جدول مخصص.
        /// TODO: بدّل المنطق لما تضيف جدول للأسعار الخاصة (مثلاً CustomerProductPrice).
        /// </summary>
        public async Task<bool> HasCustomerPriceAsync(int customerId, int productId)
        {
            await Task.CompletedTask;
            return false;
        }

        /// <summary>
        /// يعيّن سعرًا (حاليًا عام) للمنتج.
        /// ملاحظة: ده بيغير SalePrice العام على مستوى المنتج كله، مش سعر خاص بعميل بعينه.
        /// TODO: لو أضفت جدول أسعار عملاء، خزّن/حدّث السجل هناك بدل تغيير SalePrice.
        /// </summary>
        public async Task SetCustomerPriceAsync(int customerId, int productId, decimal price, int userId = 1)
        {
            if (price < 0m)
                throw new ArgumentOutOfRangeException(nameof(price), "السعر لا يمكن أن يكون سالبًا.");

            _unitOfWork.BeginTransaction();
            try
            {
                var productRepo = _unitOfWork.Repository<Product>();
                var product = await productRepo.GetByIdAsync(productId);
                if (product == null)
                    throw new InvalidOperationException("المنتج غير موجود.");

                // لو نفس السعر، مفيش داعي لحفظ
                if (product.SalePrice == price)
                {
                    await _unitOfWork.CommitTransactionAsync();
                    return;
                }

                product.SalePrice = price;
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
