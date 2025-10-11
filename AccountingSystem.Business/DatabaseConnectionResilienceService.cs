using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AccountingSystem.Data;

namespace AccountingSystem.Business
{
    /// <summary>
    /// خدمة مرونة الاتصال بقاعدة البيانات - توفر آليات إعادة المحاولة والتعافي
    /// </summary>
    public interface IDatabaseConnectionResilienceService
    {
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, int delayMs = 1000);
        Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3, int delayMs = 1000);
        Task<bool> EnsureConnectionAsync();
        Task<bool> RecreateConnectionAsync();
    }

    /// <summary>
    /// تنفيذ خدمة مرونة الاتصال بقاعدة البيانات
    /// </summary>
    public class DatabaseConnectionResilienceService : IDatabaseConnectionResilienceService
    {
        private readonly AccountingDbContext _dbContext;
        private readonly ILogger<DatabaseConnectionResilienceService> _logger;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        public DatabaseConnectionResilienceService(
            AccountingDbContext dbContext,
            ILogger<DatabaseConnectionResilienceService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// تنفيذ إجراء مع إعادة المحاولة في حالة فشل الاتصال
        /// </summary>
        public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, int delayMs = 1000)
        {
            int retryCount = 0;
            Exception? lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    // التأكد من الاتصال قبل التنفيذ
                    await EnsureConnectionAsync();

                    // تنفيذ الإجراء
                    return await action();
                }
                catch (Exception ex) when (IsTransientError(ex) && retryCount < maxRetries - 1)
                {
                    lastException = ex;
                    retryCount++;

                    _logger.LogWarning(
                        "Transient database error detected. Retry {RetryCount}/{MaxRetries}. Error: {Error}",
                        retryCount, maxRetries, ex.Message);

                    // انتظار قبل إعادة المحاولة مع زيادة تدريجية (exponential backoff)
                    var delay = delayMs * (int)Math.Pow(2, retryCount - 1);
                    await Task.Delay(delay);

                    // محاولة إعادة إنشاء الاتصال
                    await RecreateConnectionAsync();
                }
                catch (Exception ex)
                {
                    // خطأ غير قابل للإعادة
                    _logger.LogError(ex, "Non-transient database error occurred");
                    throw;
                }
            }

            // فشلت جميع المحاولات
            _logger.LogError(lastException, "All retry attempts failed");
            throw new InvalidOperationException(
                $"فشلت العملية بعد {maxRetries} محاولة",
                lastException);
        }

        /// <summary>
        /// تنفيذ إجراء مع إعادة المحاولة (بدون قيمة إرجاع)
        /// </summary>
        public async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3, int delayMs = 1000)
        {
            await ExecuteWithRetryAsync(async () =>
            {
                await action();
                return true;
            }, maxRetries, delayMs);
        }

        /// <summary>
        /// التأكد من صلاحية الاتصال
        /// </summary>
        public async Task<bool> EnsureConnectionAsync()
        {
            try
            {
                await _connectionLock.WaitAsync();

                // التحقق من حالة الاتصال
                var connection = _dbContext.Database.GetDbConnection();

                if (connection.State != ConnectionState.Open)
                {
                    _logger.LogInformation("Opening database connection");
                    await _dbContext.Database.OpenConnectionAsync();
                }

                // اختبار الاتصال بـ ping
                await TestConnectionAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure database connection");
                return false;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// إعادة إنشاء الاتصال
        /// </summary>
        public async Task<bool> RecreateConnectionAsync()
        {
            try
            {
                await _connectionLock.WaitAsync();

                _logger.LogInformation("Recreating database connection");

                // إغلاق الاتصال الحالي
                var connection = _dbContext.Database.GetDbConnection();
                if (connection.State != ConnectionState.Closed)
                {
                    await _dbContext.Database.CloseConnectionAsync();
                }

                // انتظار قصير
                await Task.Delay(500);

                // فتح اتصال جديد
                await _dbContext.Database.OpenConnectionAsync();

                // اختبار الاتصال
                await TestConnectionAsync();

                _logger.LogInformation("Database connection recreated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recreate database connection");
                return false;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// اختبار الاتصال بقاعدة البيانات
        /// </summary>
        private async Task TestConnectionAsync()
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                throw;
            }
        }

        /// <summary>
        /// تحديد إذا كان الخطأ مؤقت (قابل لإعادة المحاولة)
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
            // أخطاء SQL Server المؤقتة
            if (ex is SqlException sqlException)
            {
                // أكواد الأخطاء المؤقتة في SQL Server
                int[] transientErrorNumbers = {
                    -1,     // Timeout
                    -2,     // Connection timeout
                    1205,   // Deadlock
                    1222,   // Lock request timeout
                    3960,   // Snapshot isolation transaction aborted
                    4060,   // Cannot open database
                    40197,  // Service unavailable
                    40501,  // Service is busy
                    40613,  // Database unavailable
                    49918,  // Cannot process request
                    49919,  // Cannot process create or update request
                    49920   // Cannot process delete request
                };

                foreach (SqlError error in sqlException.Errors)
                {
                    if (Array.IndexOf(transientErrorNumbers, error.Number) >= 0)
                    {
                        return true;
                    }
                }
            }

            // أخطاء Entity Framework المؤقتة
            if (ex is DbUpdateException)
            {
                return IsTransientError(ex.InnerException!);
            }

            // أخطاء الشبكة
            if (ex is System.Net.Sockets.SocketException)
            {
                return true;
            }

            if (ex is TimeoutException)
            {
                return true;
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// Extension Methods لتسهيل استخدام خدمة المرونة
    /// </summary>
    public static class DatabaseConnectionResilienceExtensions
    {
        /// <summary>
        /// تنفيذ استعلام مع معالجة الأخطاء وإعادة المحاولة التلقائية
        /// </summary>
        public static async Task<T?> ExecuteQueryWithResilienceAsync<T>(
            this IDatabaseConnectionResilienceService resilienceService,
            Func<Task<T>> query,
            T? defaultValue = default,
            int maxRetries = 3)
        {
            try
            {
                return await resilienceService.ExecuteWithRetryAsync(query, maxRetries);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// تنفيذ أمر مع معالجة الأخطاء وإعادة المحاولة التلقائية
        /// </summary>
        public static async Task<bool> ExecuteCommandWithResilienceAsync(
            this IDatabaseConnectionResilienceService resilienceService,
            Func<Task> command,
            int maxRetries = 3)
        {
            try
            {
                await resilienceService.ExecuteWithRetryAsync(command, maxRetries);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
