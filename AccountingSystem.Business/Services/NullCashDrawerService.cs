using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Models;

namespace AccountingSystem.Business.Services
{
    /// <summary>
    /// خدمة درج النقد - تنفيذ مؤقت لمنع أخطاء الحقن
    /// </summary>
    public class NullCashDrawerService : ICashDrawerService
    {
        public Task<decimal> GetCurrentBalanceAsync(int cashierId)
        {
            // تنفيذ مؤقت - يعيد صفر
            return Task.FromResult(0m);
        }

        public Task<bool> RecordOperationAsync(int cashierId, int? sessionId, string operationType, decimal amount, decimal balanceBefore, decimal balanceAfter, string reason, string? notes = null, string? reference = null)
        {
            // تنفيذ مؤقت - يعيد نجاح دون فعل شيء
            return Task.FromResult(true);
        }

        public Task<IEnumerable<CashDrawerOperation>> GetOperationsAsync(int cashierId, DateTime? from = null, DateTime? to = null)
        {
            // تنفيذ مؤقت - يعيد قائمة فارغة
            return Task.FromResult(Enumerable.Empty<CashDrawerOperation>());
        }

        public Task<bool> AddCashAsync(int cashierId, int sessionId, decimal amount, string reason, string? notes = null)
        {
            // تنفيذ مؤقت - يعيد نجاح دون فعل شيء
            return Task.FromResult(true);
        }

        public Task<bool> RemoveCashAsync(int cashierId, int sessionId, decimal amount, string reason, string? notes = null)
        {
            // تنفيذ مؤقت - يعيد نجاح دون فعل شيء
            return Task.FromResult(true);
        }
    }
}