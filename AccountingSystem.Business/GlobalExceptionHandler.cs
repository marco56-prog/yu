using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AccountingSystem.Models;

namespace AccountingSystem.Business
{
    /// <summary>
    /// معالج عام للاستثناءات - يوفر معالجة موحدة للأخطاء في كامل التطبيق
    /// </summary>
    public interface IGlobalExceptionHandler
    {
        Task<ErrorHandlingResult> HandleExceptionAsync(Exception exception, string? context = null);
        Task<T?> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> action, string? context = null);
        Task ExecuteWithErrorHandlingAsync(Func<Task> action, string? context = null);
        T? ExecuteWithErrorHandling<T>(Func<T> action, string? context = null);
        void ExecuteWithErrorHandling(Action action, string? context = null);
    }

    /// <summary>
    /// تنفيذ معالج الاستثناءات العام
    /// </summary>
    public class GlobalExceptionHandler : IGlobalExceptionHandler
    {
        private readonly IErrorLoggingService _errorLoggingService;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            IErrorLoggingService errorLoggingService,
            ILogger<GlobalExceptionHandler> logger)
        {
            _errorLoggingService = errorLoggingService ?? throw new ArgumentNullException(nameof(errorLoggingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// معالجة استثناء محدد
        /// </summary>
        public async Task<ErrorHandlingResult> HandleExceptionAsync(Exception exception, string? context = null)
        {
            var result = new ErrorHandlingResult
            {
                Exception = exception,
                Context = context ?? "Unknown",
                Timestamp = DateTime.Now
            };

            try
            {
                // تحديد نوع الخطأ وشدته
                var (errorType, severity) = ClassifyException(exception);
                result.ErrorType = errorType;
                result.Severity = severity;

                // تسجيل الخطأ
                result.ErrorId = await _errorLoggingService.LogErrorAsync(
                    exception,
                    errorType,
                    severity
                );

                // تحديد إمكانية الاسترداد
                result.IsRecoverable = IsRecoverableError(exception);

                // تحديد رسالة للمستخدم
                result.UserMessage = GenerateUserFriendlyMessage(exception, errorType);

                // اقتراح حل
                result.SuggestedAction = SuggestAction(exception, errorType);

                _logger.LogInformation(
                    "Exception handled: {ErrorId} - Type: {ErrorType} - Severity: {Severity}",
                    result.ErrorId, result.ErrorType, result.Severity);
            }
            catch (Exception ex)
            {
                // في حالة فشل المعالجة، سجل الخطأ الأصلي والخطأ الجديد
                _logger.LogCritical(ex, "Critical failure in exception handler while handling: {OriginalException}",
                    exception.Message);
                result.ErrorId = Guid.NewGuid().ToString();
                result.UserMessage = "حدث خطأ غير متوقع في النظام";
            }

            return result;
        }

        /// <summary>
        /// تنفيذ إجراء مع معالجة الأخطاء (async with return value)
        /// </summary>
        public async Task<T?> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> action, string? context = null)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                var result = await HandleExceptionAsync(ex, context);
                _logger.LogError(ex, "Error in {Context}: {Message}", context ?? "Unknown", ex.Message);
                return default;
            }
        }

        /// <summary>
        /// تنفيذ إجراء مع معالجة الأخطاء (async without return value)
        /// </summary>
        public async Task ExecuteWithErrorHandlingAsync(Func<Task> action, string? context = null)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                var result = await HandleExceptionAsync(ex, context);
                _logger.LogError(ex, "Error in {Context}: {Message}", context ?? "Unknown", ex.Message);
            }
        }

        /// <summary>
        /// تنفيذ إجراء مع معالجة الأخطاء (sync with return value)
        /// </summary>
        public T? ExecuteWithErrorHandling<T>(Func<T> action, string? context = null)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                // استدعاء غير متزامن في سياق متزامن
                var result = HandleExceptionAsync(ex, context).GetAwaiter().GetResult();
                _logger.LogError(ex, "Error in {Context}: {Message}", context ?? "Unknown", ex.Message);
                return default;
            }
        }

        /// <summary>
        /// تنفيذ إجراء مع معالجة الأخطاء (sync without return value)
        /// </summary>
        public void ExecuteWithErrorHandling(Action action, string? context = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                var result = HandleExceptionAsync(ex, context).GetAwaiter().GetResult();
                _logger.LogError(ex, "Error in {Context}: {Message}", context ?? "Unknown", ex.Message);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// تصنيف الاستثناء لتحديد النوع والشدة
        /// </summary>
        private static (ErrorType errorType, ErrorSeverity severity) ClassifyException(Exception exception)
        {
            return exception switch
            {
                // Database exceptions
                Microsoft.EntityFrameworkCore.DbUpdateException => (ErrorType.DatabaseError, ErrorSeverity.Critical),
                Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (ErrorType.DatabaseError, ErrorSeverity.Error),
                Microsoft.Data.SqlClient.SqlException => (ErrorType.DatabaseError, ErrorSeverity.Critical),
                
                // Validation exceptions
                ArgumentNullException => (ErrorType.ValidationError, ErrorSeverity.Warning),
                ArgumentException => (ErrorType.ValidationError, ErrorSeverity.Warning),
                InvalidOperationException => (ErrorType.BusinessLogicError, ErrorSeverity.Error),
                
                // Security exceptions
                UnauthorizedAccessException => (ErrorType.SecurityError, ErrorSeverity.Critical),
                System.Security.SecurityException => (ErrorType.SecurityError, ErrorSeverity.Critical),
                
                // System exceptions
                OutOfMemoryException => (ErrorType.SystemError, ErrorSeverity.Fatal),
                StackOverflowException => (ErrorType.SystemError, ErrorSeverity.Fatal),
                
                // Default
                _ => (ErrorType.SystemError, ErrorSeverity.Error)
            };
        }

        /// <summary>
        /// تحديد إذا كان الخطأ قابل للاسترداد
        /// </summary>
        private static bool IsRecoverableError(Exception exception)
        {
            return exception switch
            {
                OutOfMemoryException => false,
                StackOverflowException => false,
                System.Threading.ThreadAbortException => false,
                ArgumentNullException => true,
                ArgumentException => true,
                InvalidOperationException => true,
                _ => true
            };
        }

        /// <summary>
        /// توليد رسالة واضحة للمستخدم
        /// </summary>
        private static string GenerateUserFriendlyMessage(Exception exception, ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.DatabaseError => "حدث خطأ في الاتصال بقاعدة البيانات. يرجى المحاولة مرة أخرى.",
                ErrorType.ValidationError => $"بيانات غير صحيحة: {exception.Message}",
                ErrorType.SecurityError => "ليس لديك صلاحية للقيام بهذا الإجراء.",
                ErrorType.BusinessLogicError => "لا يمكن إتمام هذه العملية. يرجى مراجعة البيانات المدخلة.",
                ErrorType.UIError => "حدث خطأ في واجهة المستخدم.",
                ErrorType.FinancialError => "حدث خطأ في العملية المالية.",
                ErrorType.InventoryError => "حدث خطأ في إدارة المخزون.",
                ErrorType.ReportError => "فشل في إنشاء التقرير.",
                ErrorType.SystemError => "حدث خطأ في النظام. يرجى الاتصال بالدعم الفني.",
                _ => "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى."
            };
        }

        /// <summary>
        /// اقتراح إجراء للمستخدم
        /// </summary>
        private static string SuggestAction(Exception exception, ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.DatabaseError => "تحقق من اتصال الشبكة وحاول مرة أخرى. إذا استمرت المشكلة، اتصل بالدعم الفني.",
                ErrorType.ValidationError => "راجع البيانات المدخلة وتأكد من صحتها.",
                ErrorType.SecurityError => "اتصل بمدير النظام للحصول على الصلاحيات المطلوبة.",
                ErrorType.BusinessLogicError => "تحقق من أن جميع الحقول مملوءة بشكل صحيح.",
                ErrorType.UIError => "حاول إعادة تشغيل النافذة.",
                ErrorType.FinancialError => "راجع العملية المالية وتأكد من صحة المبالغ.",
                ErrorType.InventoryError => "تحقق من المخزون المتاح.",
                ErrorType.ReportError => "تأكد من توفر البيانات المطلوبة للتقرير.",
                ErrorType.SystemError => "أعد تشغيل التطبيق. إذا استمرت المشكلة، اتصل بالدعم الفني.",
                _ => "حاول مرة أخرى. إذا استمرت المشكلة، اتصل بالدعم الفني."
            };
        }

        #endregion
    }

    /// <summary>
    /// نتيجة معالجة الخطأ
    /// </summary>
    public class ErrorHandlingResult
    {
        public string ErrorId { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public ErrorType ErrorType { get; set; }
        public ErrorSeverity Severity { get; set; }
        public string Context { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRecoverable { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string SuggestedAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// Extension Methods لتسهيل استخدام معالج الأخطاء
    /// </summary>
    public static class GlobalExceptionHandlerExtensions
    {
        /// <summary>
        /// تنفيذ آمن مع معالجة الأخطاء وتسجيلها
        /// </summary>
        public static async Task<T?> TryExecuteAsync<T>(
            this IGlobalExceptionHandler handler,
            Func<Task<T>> action,
            T? defaultValue = default,
            string? context = null)
        {
            try
            {
                return await action();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// تنفيذ آمن مع إعادة محاولة في حالة الفشل
        /// </summary>
        public static async Task<T?> TryExecuteWithRetryAsync<T>(
            this IGlobalExceptionHandler handler,
            Func<Task<T>> action,
            int maxRetries = 3,
            int delayMs = 1000,
            string? context = null)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await action();
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    await handler.HandleExceptionAsync(ex, context);
                    return default;
                }
            }

            return default;
        }
    }
}
