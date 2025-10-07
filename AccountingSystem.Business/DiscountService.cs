using AccountingSystem.Models;
using AccountingSystem.Data;

namespace AccountingSystem.Business
{
    /// <summary>
    /// خدمة إدارة الخصومات والعروض
    /// </summary>
    public interface IDiscountService
    {
        Task<IEnumerable<Discount>> GetAllDiscountsAsync();
        Task<IEnumerable<Discount>> GetActiveDiscountsAsync();
        Task<Discount?> GetDiscountByIdAsync(int id);
        Task<Discount?> GetDiscountByCodeAsync(string code);
        Task<bool> CreateDiscountAsync(Discount discount);
        Task<bool> UpdateDiscountAsync(Discount discount);
        Task<bool> DeleteDiscountAsync(int id);
        Task<bool> ActivateDiscountAsync(int id);
        Task<bool> DeactivateDiscountAsync(int id);
        Task<bool> ValidateDiscountAsync(int discountId, decimal transactionAmount, int? productId = null, int? categoryId = null);
        Task<decimal> CalculateDiscountAsync(int discountId, decimal amount);
        Task<bool> IncrementUsageAsync(int discountId);
    }

    public class DiscountService : IDiscountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DiscountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Discount>> GetAllDiscountsAsync()
        {
            var repo = _unitOfWork.Repository<Discount>();
            return await repo.GetAllAsync();
        }

        public async Task<IEnumerable<Discount>> GetActiveDiscountsAsync()
        {
            var repo = _unitOfWork.Repository<Discount>();
            var now = DateTime.Now;

            // فعّالة + ضمن الفترة + لم تتجاوز حد الاستخدام
            return await repo.FindAsync(d =>
                d.IsActive &&
                (!d.StartDate.HasValue || d.StartDate <= now) &&
                (!d.EndDate.HasValue || d.EndDate >= now) &&
                (d.UsageLimit == 0 || d.TimesUsed < d.UsageLimit));
        }

        public async Task<Discount?> GetDiscountByIdAsync(int id)
        {
            var repo = _unitOfWork.Repository<Discount>();
            return await repo.GetByIdAsync(id);
        }

        public async Task<Discount?> GetDiscountByCodeAsync(string code)
        {
            var repo = _unitOfWork.Repository<Discount>();
            var normalized = (code ?? string.Empty).Trim();
            var items = await repo.FindAsync(d => d.DiscountCode.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            return items.FirstOrDefault();
        }

        public async Task<bool> CreateDiscountAsync(Discount discount)
        {
            try
            {
                if (discount == null) return false;

                // تنظيف الحقول
                discount.DiscountCode = (discount.DiscountCode ?? string.Empty).Trim();
                discount.ApplicableOn = (discount.ApplicableOn ?? string.Empty).Trim();
                discount.DiscountType = (discount.DiscountType ?? string.Empty).Trim();

                // عدم تكرار الكود
                var existing = await GetDiscountByCodeAsync(discount.DiscountCode);
                if (existing != null) return false;

                // حماية قيم أساسية
                if (discount.DiscountValue < 0) discount.DiscountValue = 0;
                if (discount.MaximumDiscount.HasValue && discount.MaximumDiscount < 0)
                    discount.MaximumDiscount = 0;

                var repo = _unitOfWork.Repository<Discount>();
                await repo.AddAsync(discount);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateDiscountAsync(Discount discount)
        {
            try
            {
                if (discount == null) return false;

                // تنظيف الحقول
                discount.DiscountCode = (discount.DiscountCode ?? string.Empty).Trim();
                discount.ApplicableOn = (discount.ApplicableOn ?? string.Empty).Trim();
                discount.DiscountType = (discount.DiscountType ?? string.Empty).Trim();

                // حماية القيم
                if (discount.DiscountValue < 0) discount.DiscountValue = 0;
                if (discount.MaximumDiscount.HasValue && discount.MaximumDiscount < 0)
                    discount.MaximumDiscount = 0;
                if (discount.UsageLimit < 0) discount.UsageLimit = 0;
                if (discount.TimesUsed < 0) discount.TimesUsed = 0;

                // منع تجاوز الاستخدام
                if (discount.UsageLimit > 0 && discount.TimesUsed > discount.UsageLimit)
                    discount.TimesUsed = discount.UsageLimit;

                var repo = _unitOfWork.Repository<Discount>();
                repo.Update(discount);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteDiscountAsync(int id)
        {
            try
            {
                var discount = await GetDiscountByIdAsync(id);
                if (discount == null) return false;

                // إن كان تم استخدامه من الأفضل إلغاء تفعيله بدل الحذف (لتجنب كسر المراجع)
                if (discount.TimesUsed > 0)
                {
                    discount.IsActive = false;
                    return await UpdateDiscountAsync(discount);
                }

                var repo = _unitOfWork.Repository<Discount>();
                repo.Remove(discount);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActivateDiscountAsync(int id)
        {
            try
            {
                var discount = await GetDiscountByIdAsync(id);
                if (discount == null) return false;

                discount.IsActive = true;
                return await UpdateDiscountAsync(discount);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeactivateDiscountAsync(int id)
        {
            try
            {
                var discount = await GetDiscountByIdAsync(id);
                if (discount == null) return false;

                discount.IsActive = false;
                return await UpdateDiscountAsync(discount);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateDiscountAsync(int discountId, decimal transactionAmount, int? productId = null, int? categoryId = null)
        {
            try
            {
                var discount = await GetDiscountByIdAsync(discountId);
                if (discount == null || !discount.IsActive) return false;

                var now = DateTime.Now;

                // ضمن نطاق التاريخ
                if (discount.StartDate.HasValue && discount.StartDate > now) return false;
                if (discount.EndDate.HasValue && discount.EndDate < now) return false;

                // حد أدنى للمبلغ
                if (discount.MinimumAmount.HasValue && transactionAmount < discount.MinimumAmount.Value)
                    return false;

                // حد الاستخدام
                if (discount.UsageLimit > 0 && discount.TimesUsed >= discount.UsageLimit)
                    return false;

                // قابلية التطبيق
                if (discount.ApplicableOn == "منتج" && discount.ProductId != productId)
                    return false;

                if (discount.ApplicableOn == "فئة" && discount.CategoryId != categoryId)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<decimal> CalculateDiscountAsync(int discountId, decimal amount)
        {
            try
            {
                if (amount <= 0) return 0m;

                var discount = await GetDiscountByIdAsync(discountId);
                if (discount == null || !discount.IsActive) return 0m;

                decimal discountAmount = 0m;

                // نسبة/قيمة ثابتة
                if (discount.DiscountType == "نسبة")
                {
                    var pct = discount.DiscountValue;
                    if (pct < 0) pct = 0;
                    discountAmount = amount * (pct / 100m);
                }
                else
                {
                    var val = discount.DiscountValue;
                    if (val < 0) val = 0;
                    discountAmount = val;
                }

                // حد أقصى للخصم
                if (discount.MaximumDiscount.HasValue && discount.MaximumDiscount.Value >= 0 && discountAmount > discount.MaximumDiscount.Value)
                    discountAmount = discount.MaximumDiscount.Value;

                // لا نتجاوز قيمة العملية
                if (discountAmount > amount) discountAmount = amount;

                // لا نعيد قيم سالبة
                if (discountAmount < 0) discountAmount = 0;

                return discountAmount;
            }
            catch
            {
                return 0m;
            }
        }

        public async Task<bool> IncrementUsageAsync(int discountId)
        {
            try
            {
                var discount = await GetDiscountByIdAsync(discountId);
                if (discount == null) return false;

                // عدم تجاوز حد الاستخدام
                if (discount.UsageLimit > 0 && discount.TimesUsed >= discount.UsageLimit)
                    return false;

                discount.TimesUsed++;
                return await UpdateDiscountAsync(discount);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// خدمة نقاط الولاء
    /// </summary>
    public interface ILoyaltyService
    {
        Task<IEnumerable<LoyaltyProgram>> GetActiveProgramsAsync();
        Task<LoyaltyProgram?> GetProgramByIdAsync(int id);
        Task<CustomerLoyaltyPoint?> GetCustomerPointsAsync(int customerId, int programId);
        Task<bool> EarnPointsAsync(int customerId, int programId, decimal purchaseAmount, int? posTransactionId = null);
        Task<bool> RedeemPointsAsync(int customerId, int programId, int points, int? posTransactionId = null);
        Task<bool> CanRedeemPointsAsync(int customerId, int programId, int points);
        Task<decimal> CalculatePointsValueAsync(int programId, int points);
        Task<int> CalculateEarnedPointsAsync(int programId, decimal amount);
    }

    public class LoyaltyService : ILoyaltyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LoyaltyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<LoyaltyProgram>> GetActiveProgramsAsync()
        {
            var repo = _unitOfWork.Repository<LoyaltyProgram>();
            var now = DateTime.Now;

            return await repo.FindAsync(p =>
                p.IsActive &&
                (!p.StartDate.HasValue || p.StartDate <= now) &&
                (!p.EndDate.HasValue || p.EndDate >= now));
        }

        public async Task<LoyaltyProgram?> GetProgramByIdAsync(int id)
        {
            var repo = _unitOfWork.Repository<LoyaltyProgram>();
            return await repo.GetByIdAsync(id);
        }

        public async Task<CustomerLoyaltyPoint?> GetCustomerPointsAsync(int customerId, int programId)
        {
            var repo = _unitOfWork.Repository<CustomerLoyaltyPoint>();
            var list = await repo.FindAsync(p => p.CustomerId == customerId && p.LoyaltyProgramId == programId);
            return list.FirstOrDefault();
        }

        public async Task<bool> EarnPointsAsync(int customerId, int programId, decimal purchaseAmount, int? posTransactionId = null)
        {
            try
            {
                if (purchaseAmount <= 0) return true;

                var program = await GetProgramByIdAsync(programId);
                if (program == null || !program.IsActive) return false;

                // حساب النقاط المكتسبة
                var pointsToEarn = await CalculateEarnedPointsAsync(programId, purchaseAmount);
                if (pointsToEarn <= 0) return true;

                _unitOfWork.BeginTransaction();
                try
                {
                    var repoPts = _unitOfWork.Repository<CustomerLoyaltyPoint>();
                    var customerPoints = await GetCustomerPointsAsync(customerId, programId);

                    if (customerPoints == null)
                    {
                        customerPoints = new CustomerLoyaltyPoint
                        {
                            CustomerId = customerId,
                            LoyaltyProgramId = programId,
                            PointsEarned = pointsToEarn,
                            PointsBalance = pointsToEarn,
                            LastEarnedDate = DateTime.Now
                        };
                        await repoPts.AddAsync(customerPoints);
                        await _unitOfWork.SaveChangesAsync(); // للحصول على Id قبل إنشاء حركة
                    }
                    else
                    {
                        customerPoints.PointsEarned += pointsToEarn;
                        customerPoints.PointsBalance += pointsToEarn;
                        customerPoints.LastEarnedDate = DateTime.Now;
                        repoPts.Update(customerPoints);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    // تسجيل حركة الولاء
                    var transRepo = _unitOfWork.Repository<LoyaltyTransaction>();
                    var transaction = new LoyaltyTransaction
                    {
                        CustomerLoyaltyPointId = customerPoints.Id,
                        POSTransactionId = posTransactionId,
                        TransactionType = "كسب",
                        Points = pointsToEarn,
                        MonetaryValue = purchaseAmount
                    };
                    await transRepo.AddAsync(transaction);

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
            catch
            {
                return false;
            }
        }

        public async Task<bool> RedeemPointsAsync(int customerId, int programId, int points, int? posTransactionId = null)
        {
            try
            {
                if (points <= 0) return false;

                if (!await CanRedeemPointsAsync(customerId, programId, points))
                    return false;

                _unitOfWork.BeginTransaction();
                try
                {
                    var repoPts = _unitOfWork.Repository<CustomerLoyaltyPoint>();
                    var customerPoints = await GetCustomerPointsAsync(customerId, programId);
                    if (customerPoints == null) { _unitOfWork.RollbackTransaction(); return false; }

                    customerPoints.PointsRedeemed += points;
                    customerPoints.PointsBalance  -= points;
                    customerPoints.LastRedeemedDate = DateTime.Now;

                    repoPts.Update(customerPoints);
                    await _unitOfWork.SaveChangesAsync();

                    // تسجيل حركة الاستبدال
                    var value = await CalculatePointsValueAsync(programId, points);
                    var transRepo = _unitOfWork.Repository<LoyaltyTransaction>();
                    var transaction = new LoyaltyTransaction
                    {
                        CustomerLoyaltyPointId = customerPoints.Id,
                        POSTransactionId = posTransactionId,
                        TransactionType = "استبدال",
                        Points = points,
                        MonetaryValue = value
                    };
                    await transRepo.AddAsync(transaction);

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
            catch
            {
                return false;
            }
        }

        public async Task<bool> CanRedeemPointsAsync(int customerId, int programId, int points)
        {
            try
            {
                if (points <= 0) return false;

                var program = await GetProgramByIdAsync(programId);
                if (program == null || !program.IsActive) return false;

                if (points < program.MinimumRedemption) return false;

                var customerPoints = await GetCustomerPointsAsync(customerId, programId);
                return customerPoints != null && customerPoints.PointsBalance >= points;
            }
            catch
            {
                return false;
            }
        }

        public async Task<decimal> CalculatePointsValueAsync(int programId, int points)
        {
            try
            {
                if (points <= 0) return 0m;

                var program = await GetProgramByIdAsync(programId);
                if (program == null || !program.IsActive) return 0m;

                var pv = program.PointValue;
                if (pv < 0) pv = 0;

                return points * pv;
            }
            catch
            {
                return 0m;
            }
        }

        public async Task<int> CalculateEarnedPointsAsync(int programId, decimal amount)
        {
            try
            {
                if (amount <= 0) return 0;

                var program = await GetProgramByIdAsync(programId);
                if (program == null || !program.IsActive) return 0;

                var per = program.AmountPerPoint;
                if (per <= 0) return 0; // منع القسمة على صفر

                // حساب نقاط صحيحة لأسفل (يمكنك تغيير المنطق لاحقًا)
                var points = (int)(amount / per);
                if (points < 0) points = 0;

                return points;
            }
            catch
            {
                return 0;
            }
        }
    }
}
