using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    /// <summary>
    /// خدمة إدارة الكاشير
    /// </summary>
    public interface ICashierService
    {
        // إدارة الكاشير
        Task<IEnumerable<Cashier>> GetAllCashiersAsync();
        Task<Cashier?> GetCashierByIdAsync(int id);
        Task<Cashier?> GetCashierByCodeAsync(string code);
        Task<Cashier?> GetCashierByUsernameAsync(string username);
        Task<bool> CreateCashierAsync(Cashier cashier);
        Task<bool> UpdateCashierAsync(Cashier cashier);
        Task<bool> DeleteCashierAsync(int id);
        Task<bool> ActivateCashierAsync(int id);
        Task<bool> DeactivateCashierAsync(int id);
        Task<bool> ValidateCredentialsAsync(string username, string password);
        Task<Cashier?> AuthenticateCashierAsync(string username, string password);
        Task<bool> UpdateLastLoginAsync(int cashierId);

        // إدارة الجلسات
        Task<CashierSession?> StartSessionAsync(int cashierId, decimal openingBalance);
        Task<bool> EndSessionAsync(int sessionId, decimal closingBalance);
        Task<CashierSession?> GetActiveSessionAsync(int cashierId);

        // إدارة الصلاحيات
        Task<bool> UpdateCashierPermissionsAsync(int cashierId, CashierPermissions permissions);
        Task<CashierPermissions> GetCashierPermissionsAsync(int cashierId);
    }

    public class CashierService : ICashierService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CashierService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Cashier>> GetAllCashiersAsync()
        {
            var repo = _unitOfWork.Repository<Cashier>();
            return await repo.GetAllAsync();
        }

        public async Task<Cashier?> GetCashierByIdAsync(int id)
        {
            var repo = _unitOfWork.Repository<Cashier>();
            return await repo.GetByIdAsync(id);
        }

        public async Task<Cashier?> GetCashierByCodeAsync(string code)
        {
            var repo = _unitOfWork.Repository<Cashier>();
            var normalized = (code ?? string.Empty).Trim();
            var cashiers = await repo.FindAsync(c => c.CashierCode.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            return cashiers.FirstOrDefault();
        }

        public async Task<Cashier?> GetCashierByUsernameAsync(string username)
        {
            var repo = _unitOfWork.Repository<Cashier>();
            var normalized = (username ?? string.Empty).Trim();
            var cashiers = await repo.FindAsync(c => c.Username != null && c.Username.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            return cashiers?.FirstOrDefault();
        }

        public async Task<bool> CreateCashierAsync(Cashier cashier)
        {
            try
            {
                if (cashier == null) return false;

                // توحيد الحقول النصية الحساسة
                cashier.CashierCode = (cashier.CashierCode ?? string.Empty).Trim();
                cashier.Username   = (cashier.Username   ?? string.Empty).Trim().ToLowerInvariant();

                // التحقق من عدم تكرار الكود
                if (!string.IsNullOrWhiteSpace(cashier.CashierCode))
                {
                    var existingByCode = await GetCashierByCodeAsync(cashier.CashierCode);
                    if (existingByCode != null) return false;
                }

                // التحقق من عدم تكرار اسم المستخدم
                if (!string.IsNullOrEmpty(cashier.Username))
                {
                    var existingByUsername = await GetCashierByUsernameAsync(cashier.Username);
                    if (existingByUsername != null) return false;
                }

                // تشفير كلمة المرور لو كانت مُرسلة كنص واضح (وليس Hash مسبقًا)
                if (!string.IsNullOrWhiteSpace(cashier.PasswordHash))
                {
                    // Bcrypt يبدأ عادة بـ $2a$ أو $2b$ أو $2y$
                    var ph = cashier.PasswordHash.Trim();
                    if (!(ph.StartsWith("$2a$", StringComparison.Ordinal) || ph.StartsWith("$2b$", StringComparison.Ordinal) || ph.StartsWith("$2y$", StringComparison.Ordinal)))
                    {
                        cashier.PasswordHash = BCrypt.Net.BCrypt.HashPassword(ph);
                    }
                }

                var repo = _unitOfWork.Repository<Cashier>();
                await repo.AddAsync(cashier);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCashierAsync(Cashier cashier)
        {
            try
            {
                if (cashier == null) return false;

                cashier.UpdatedAt = DateTime.Now;

                // توحيد الحقول الحساسة
                cashier.CashierCode = (cashier.CashierCode ?? string.Empty).Trim();
                cashier.Username   = (cashier.Username   ?? string.Empty).Trim().ToLowerInvariant();

                // لو الـPasswordHash موجودة وواضحة (مش Bcrypt)، أعد تشفيرها
                if (!string.IsNullOrWhiteSpace(cashier.PasswordHash))
                {
                    var ph = cashier.PasswordHash.Trim();
                    if (!(ph.StartsWith("$2a$", StringComparison.Ordinal) || ph.StartsWith("$2b$", StringComparison.Ordinal) || ph.StartsWith("$2y$", StringComparison.Ordinal)))
                    {
                        cashier.PasswordHash = BCrypt.Net.BCrypt.HashPassword(ph);
                    }
                }

                var repo = _unitOfWork.Repository<Cashier>();
                repo.Update(cashier);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCashierAsync(int id)
        {
            try
            {
                var cashier = await GetCashierByIdAsync(id);
                if (cashier == null) return false;

                var repo = _unitOfWork.Repository<Cashier>();
                repo.Remove(cashier);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActivateCashierAsync(int id)
        {
            try
            {
                var cashier = await GetCashierByIdAsync(id);
                if (cashier == null) return false;

                cashier.IsActive = true;
                cashier.Status = "نشط";

                return await UpdateCashierAsync(cashier);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeactivateCashierAsync(int id)
        {
            try
            {
                var cashier = await GetCashierByIdAsync(id);
                if (cashier == null) return false;

                cashier.IsActive = false;
                cashier.Status = "غير نشط";

                return await UpdateCashierAsync(cashier);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return false;

                var cashier = await GetCashierByUsernameAsync(username);
                if (cashier == null || !cashier.IsActive) return false;

                if (string.IsNullOrWhiteSpace(cashier.PasswordHash)) return false;

                return BCrypt.Net.BCrypt.Verify(password, cashier.PasswordHash);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int cashierId)
        {
            try
            {
                var cashier = await GetCashierByIdAsync(cashierId);
                if (cashier == null) return false;

                cashier.LastLoginTime = DateTime.Now;
                return await UpdateCashierAsync(cashier);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCashierPermissionsAsync(int cashierId, CashierPermissions permissions)
        {
            try
            {
                var cashier = await GetCashierByIdAsync(cashierId);
                if (cashier == null) return false;

                // نسخ الصلاحيات للكيان
                cashier.CanOpenCashDrawer  = permissions.CanOpenCashDrawer;
                cashier.CanProcessReturns  = permissions.CanProcessReturns;
                cashier.CanApplyDiscounts  = permissions.CanApplyDiscounts;
                cashier.CanVoidTransactions = permissions.CanVoidTransactions;
                cashier.CanViewReports     = permissions.CanViewReports;
                cashier.CanManageInventory = permissions.CanManageInventory;
                cashier.MaxDiscountPercent = permissions.MaxDiscountPercent;
                cashier.MaxDiscountAmount  = permissions.MaxDiscountAmount;

                return await UpdateCashierAsync(cashier);
            }
            catch
            {
                return false;
            }
        }

        public async Task<CashierPermissions> GetCashierPermissionsAsync(int cashierId)
        {
            var cashier = await GetCashierByIdAsync(cashierId);
            if (cashier == null)
                return new CashierPermissions();

            return new CashierPermissions
            {
                CanOpenCashDrawer  = cashier.CanOpenCashDrawer,
                CanProcessReturns  = cashier.CanProcessReturns,
                CanApplyDiscounts  = cashier.CanApplyDiscounts,
                CanVoidTransactions = cashier.CanVoidTransactions,
                CanViewReports     = cashier.CanViewReports,
                CanManageInventory = cashier.CanManageInventory,
                MaxDiscountPercent = cashier.MaxDiscountPercent,
                MaxDiscountAmount  = cashier.MaxDiscountAmount
            };
        }

        public async Task<Cashier?> AuthenticateCashierAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return null;

                var cashier = await GetCashierByUsernameAsync(username);
                if (cashier == null || !cashier.IsActive) return null;

                if (string.IsNullOrWhiteSpace(cashier.PasswordHash)) return null;

                bool isValid = BCrypt.Net.BCrypt.Verify(password, cashier.PasswordHash);
                if (isValid)
                {
                    await UpdateLastLoginAsync(cashier.Id);
                    return cashier;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<CashierSession?> StartSessionAsync(int cashierId, decimal openingBalance)
        {
            try
            {
                // اتساق: حالة الجلسة المفتوحة "مفتوحة"
                var session = new CashierSession
                {
                    CashierId = cashierId,
                    StartTime = DateTime.Now,
                    OpeningBalance = openingBalance,
                    Status = "مفتوحة"
                };

                var repo = _unitOfWork.Repository<CashierSession>();
                await repo.AddAsync(session);
                await _unitOfWork.SaveChangesAsync();

                return session;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> EndSessionAsync(int sessionId, decimal closingBalance)
        {
            try
            {
                var repo = _unitOfWork.Repository<CashierSession>();
                var session = await repo.GetByIdAsync(sessionId);
                if (session == null) return false;

                session.EndTime = DateTime.Now;
                session.ClosingBalance = closingBalance;
                session.Status = "مغلقة";
                session.IsClosed = true;

                repo.Update(session);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<CashierSession?> GetActiveSessionAsync(int cashierId)
        {
            try
            {
                var repo = _unitOfWork.Repository<CashierSession>();
                var sessions = await repo.FindAsync(s => s.CashierId == cashierId && !s.IsClosed);
                return sessions.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// صلاحيات الكاشير
    /// </summary>
    public class CashierPermissions
    {
        public bool CanOpenCashDrawer { get; set; } = true;
        public bool CanProcessReturns { get; set; } = false;
        public bool CanApplyDiscounts { get; set; } = false;
        public bool CanVoidTransactions { get; set; } = false;
        public bool CanViewReports { get; set; } = false;
        public bool CanManageInventory { get; set; } = false;
        public decimal MaxDiscountPercent { get; set; } = 0;
        public decimal MaxDiscountAmount { get; set; } = 0;
    }

    /// <summary>
    /// خدمة إدارة الوردية
    /// </summary>
    public interface IShiftService
    {
        Task<CashierSession?> GetActiveSessionAsync(int cashierId);
        Task<CashierSession?> OpenShiftAsync(int cashierId, decimal openingBalance);
        Task<bool> CloseShiftAsync(int sessionId, decimal closingBalance, string? notes = null);
        Task<IEnumerable<CashierSession>> GetCashierSessionsAsync(int cashierId, DateTime? from = null, DateTime? to = null);
        Task<CashierSession?> GetSessionByIdAsync(int sessionId);
        Task<bool> UpdateSessionTotalsAsync(int sessionId);
    }

    public class ShiftService : IShiftService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShiftService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CashierSession?> GetActiveSessionAsync(int cashierId)
        {
            var repo = _unitOfWork.Repository<CashierSession>();
            var sessions = await repo.FindAsync(s => s.CashierId == cashierId && !s.IsClosed);
            return sessions.FirstOrDefault();
        }

        public async Task<CashierSession?> OpenShiftAsync(int cashierId, decimal openingBalance)
        {
            try
            {
                // التحقق من عدم وجود وردية مفتوحة
                var activeSession = await GetActiveSessionAsync(cashierId);
                if (activeSession != null) return null;

                var session = new CashierSession
                {
                    CashierId = cashierId,
                    StartTime = DateTime.Now,
                    OpeningBalance = openingBalance,
                    Status = "مفتوحة"
                };

                var repo = _unitOfWork.Repository<CashierSession>();
                await repo.AddAsync(session);
                await _unitOfWork.SaveChangesAsync();

                // تسجيل عملية فتح الخزينة
                var drawerService = new CashDrawerService(_unitOfWork);
                await drawerService.RecordOperationAsync(cashierId, session.Id, "فتح", openingBalance, 0, openingBalance, "فتح الوردية");

                return session;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> CloseShiftAsync(int sessionId, decimal closingBalance, string? notes = null)
        {
            try
            {
                var session = await GetSessionByIdAsync(sessionId);
                if (session == null || session.IsClosed) return false;

                // تحديث إجماليات الوردية قبل الإغلاق
                await UpdateSessionTotalsAsync(sessionId);

                session.EndTime = DateTime.Now;
                session.ClosingBalance = closingBalance;
                session.Status = "مغلقة";
                session.IsClosed = true;
                session.Notes = notes;

                var repo = _unitOfWork.Repository<CashierSession>();
                repo.Update(session);

                // تسجيل عملية إغلاق الخزينة
                var drawerService = new CashDrawerService(_unitOfWork);
                await drawerService.RecordOperationAsync(session.CashierId, sessionId, "إغلاق", 0, closingBalance, closingBalance, "إغلاق الوردية");

                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<CashierSession>> GetCashierSessionsAsync(int cashierId, DateTime? from = null, DateTime? to = null)
        {
            var repo = _unitOfWork.Repository<CashierSession>();
            var sessions = await repo.FindAsync(s => s.CashierId == cashierId);

            if (from.HasValue)
                sessions = sessions.Where(s => s.StartTime >= from.Value);
            if (to.HasValue)
                sessions = sessions.Where(s => s.StartTime <= to.Value);

            return sessions.OrderByDescending(s => s.StartTime);
        }

        public async Task<CashierSession?> GetSessionByIdAsync(int sessionId)
        {
            var repo = _unitOfWork.Repository<CashierSession>();
            return await repo.GetByIdAsync(sessionId);
        }

        public async Task<bool> UpdateSessionTotalsAsync(int sessionId)
        {
            try
            {
                var session = await GetSessionByIdAsync(sessionId);
                if (session == null) return false;

                var transactionRepo = _unitOfWork.Repository<POSTransaction>();
                var transactions = await transactionRepo.FindAsync(t => t.SessionId == sessionId && !t.IsVoided);

                // تقسيم العمليات
                var salesTransactions   = transactions.Where(t => t.TransactionType == "بيع");
                var returnTransactions  = transactions.Where(t => t.TransactionType == "مرتجع");

                // مجاميع محمية من Null
                session.TotalSales       = salesTransactions.Sum(t => t.Total);
                session.TotalReturns     = returnTransactions.Sum(t => t.Total);
                session.TotalDiscounts   = salesTransactions.Sum(t => t.DiscountAmount);
                session.CashSalesTotal   = salesTransactions.Where(t => t.PaymentMethod == "نقداً").Sum(t => t.Total);
                session.CardSalesTotal   = salesTransactions.Where(t => t.PaymentMethod == "بطاقة").Sum(t => t.Total);
                session.TransactionsCount = salesTransactions.Count();
                session.ReturnsCount      = returnTransactions.Count();

                // الرصيد المتوقع = رصيد بداية + مبيعات نقدية - المرتجعات
                session.ExpectedClosingBalance = session.OpeningBalance + session.CashSalesTotal - session.TotalReturns;

                var repo = _unitOfWork.Repository<CashierSession>();
                repo.Update(session);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// خدمة الخزينة
    /// </summary>
    public interface ICashDrawerService
    {
        Task<decimal> GetCurrentBalanceAsync(int cashierId);
        Task<bool> RecordOperationAsync(int cashierId, int? sessionId, string operationType, decimal amount, decimal balanceBefore, decimal balanceAfter, string reason, string? notes = null, string? reference = null);
        Task<IEnumerable<CashDrawerOperation>> GetOperationsAsync(int cashierId, DateTime? from = null, DateTime? to = null);
        Task<bool> AddCashAsync(int cashierId, int sessionId, decimal amount, string reason, string? notes = null);
        Task<bool> RemoveCashAsync(int cashierId, int sessionId, decimal amount, string reason, string? notes = null);
    }

    public class CashDrawerService : ICashDrawerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CashDrawerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<decimal> GetCurrentBalanceAsync(int cashierId)
        {
            var repo = _unitOfWork.Repository<CashDrawerOperation>();
            var operations = await repo.FindAsync(o => o.CashierId == cashierId);
            var lastOperation = operations.OrderByDescending(o => o.OperationTime).FirstOrDefault();

            return lastOperation?.BalanceAfter ?? 0m;
        }

        public async Task<bool> RecordOperationAsync(
            int cashierId,
            int? sessionId,
            string operationType,
            decimal amount,
            decimal balanceBefore,
            decimal balanceAfter,
            string reason,
            string? notes = null,
            string? reference = null)
        {
            try
            {
                var operation = new CashDrawerOperation
                {
                    CashierId = cashierId,
                    SessionId = sessionId,
                    OperationType = operationType,
                    Amount = amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    Reason = reason,
                    Notes = notes,
                    Reference = reference,
                    OperationTime = DateTime.Now
                };

                var repo = _unitOfWork.Repository<CashDrawerOperation>();
                await repo.AddAsync(operation);
                await _unitOfWork.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<CashDrawerOperation>> GetOperationsAsync(int cashierId, DateTime? from = null, DateTime? to = null)
        {
            var repo = _unitOfWork.Repository<CashDrawerOperation>();
            var operations = await repo.FindAsync(o => o.CashierId == cashierId);

            if (from.HasValue)
                operations = operations.Where(o => o.OperationTime >= from.Value);
            if (to.HasValue)
                operations = operations.Where(o => o.OperationTime <= to.Value);

            return operations.OrderByDescending(o => o.OperationTime);
        }

        public async Task<bool> AddCashAsync(int cashierId, int sessionId, decimal amount, string reason, string? notes = null)
        {
            try
            {
                var currentBalance = await GetCurrentBalanceAsync(cashierId);
                var newBalance = currentBalance + amount;

                return await RecordOperationAsync(cashierId, sessionId, "إيداع", amount, currentBalance, newBalance, reason, notes);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveCashAsync(int cashierId, int sessionId, decimal amount, string reason, string? notes = null)
        {
            try
            {
                var currentBalance = await GetCurrentBalanceAsync(cashierId);
                if (currentBalance < amount)
                    return false;

                var newBalance = currentBalance - amount;

                return await RecordOperationAsync(cashierId, sessionId, "سحب", amount, currentBalance, newBalance, reason, notes);
            }
            catch
            {
                return false;
            }
        }
    }
}
