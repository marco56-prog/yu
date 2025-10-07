using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace AccountingSystem.Business
{
    /// <summary>
    /// واجهة خدمة تسجيل الأخطاء الشاملة
    /// </summary>
    public interface IErrorLoggingService
    {
        // تسجيل الأخطاء الأساسي
        Task<string> LogErrorAsync(Exception exception, ErrorType errorType = ErrorType.SystemError, 
            ErrorSeverity severity = ErrorSeverity.Error, int? userId = null, string? username = null);

        Task<string> LogErrorAsync(string message, string? details = null, ErrorType errorType = ErrorType.SystemError,
            ErrorSeverity severity = ErrorSeverity.Error, int? userId = null, string? username = null);

        // تسجيل أنواع محددة من الأخطاء
        Task<string> LogUIErrorAsync(Exception exception, int? userId = null, string? username = null);
        Task<string> LogDatabaseErrorAsync(Exception exception, int? userId = null, string? username = null);
        Task<string> LogBusinessLogicErrorAsync(Exception exception, int? userId = null, string? username = null);
        Task<string> LogValidationErrorAsync(string message, int? userId = null, string? username = null);
        Task<string> LogSecurityErrorAsync(Exception exception, int? userId = null, string? username = null);
        Task<string> LogFinancialErrorAsync(Exception exception, int? userId = null, string? username = null);
        Task<string> LogInventoryErrorAsync(Exception exception, int? userId = null, string? username = null);
        Task<string> LogReportErrorAsync(Exception exception, string reportName, int? userId = null, string? username = null);

        // إدارة سجلات الأخطاء
        Task<ErrorSearchResult> SearchErrorsAsync(ErrorSearchRequest request);
        Task<ErrorLog?> GetErrorByIdAsync(int id);
        Task<ErrorLog?> GetErrorByErrorIdAsync(string errorId);
        Task<bool> UpdateErrorStatusAsync(int errorId, ErrorStatus status, int resolvedBy, string? resolutionNotes = null);
        Task<bool> AddCommentAsync(int errorId, int userId, string username, string comment);
        Task<bool> ToggleStarAsync(int errorId, bool isStarred);

        // إحصائيات
        Task<ErrorStatistics> GetErrorStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 10);
        Task<List<ErrorLog>> GetCriticalErrorsAsync();
        Task<int> GetUnresolvedErrorsCountAsync();

        // تنظيف السجلات القديمة
        Task<int> CleanupOldLogsAsync(int retentionDays = 90);

        // تقارير الأخطاء
        Task<ErrorReport> GenerateErrorReportAsync(DateTime fromDate, DateTime toDate, 
            ErrorType? errorType = null, ErrorSeverity? severity = null);
    }

    /// <summary>
    /// تنفيذ خدمة تسجيل الأخطاء الشاملة المحصنة - Thread-safe, UTC, Deduplication, High Performance
    /// </summary>
    public class ErrorLoggingService : IErrorLoggingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AccountingDbContext _db;
        private readonly ILogger<ErrorLoggingService> _logger;

        // Logger Message Delegates للأداء
        private static readonly Action<Microsoft.Extensions.Logging.ILogger, string, ErrorStatus, Exception?> LogStatusUpdated =
            LoggerMessage.Define<string, ErrorStatus>(LogLevel.Information, new EventId(3001, "ErrorStatusUpdated"),
                "تم تحديث حالة الخطأ {ErrorId} إلى {Status}");

        private static readonly Action<Microsoft.Extensions.Logging.ILogger, int, Exception?> LogCommentAdded =
            LoggerMessage.Define<int>(LogLevel.Information, new EventId(3002, "CommentAdded"),
                "تم إضافة تعليق للخطأ {ErrorId}");

        private static readonly Action<Microsoft.Extensions.Logging.ILogger, int, Exception?> LogStarToggled =
            LoggerMessage.Define<int>(LogLevel.Information, new EventId(3003, "StarToggled"),
                "تم تحديث علامة النجمة للخطأ {ErrorId}");

        public ErrorLoggingService(IUnitOfWork unitOfWork, AccountingDbContext db, ILogger<ErrorLoggingService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region تسجيل الأخطاء الأساسي

        public async Task<string> LogErrorAsync(Exception exception, ErrorType errorType = ErrorType.SystemError,
            ErrorSeverity severity = ErrorSeverity.Error, int? userId = null, string? username = null)
        {
            try
            {
                var errorLog = CreateHardenedErrorLogFromException(exception, errorType, severity, userId, username);

                // Signature-based deduplication - البحث عن خطأ مماثل خلال آخر 5 دقائق
                var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
                var existingError = await _db.Set<ErrorLog>()
                    .AsNoTracking()
                    .Where(e => e.Signature == errorLog.Signature && e.CreatedAt >= cutoffTime)
                    .OrderByDescending(e => e.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingError != null)
                {
                    // تحديث العداد مع Concurrency handling
                    var updateResult = await _db.Database.ExecuteSqlRawAsync(
                        "UPDATE ErrorLogs SET OccurrenceCount = OccurrenceCount + 1, LastOccurrence = @lastOccurrence WHERE Id = @id",
                        new Microsoft.Data.SqlClient.SqlParameter("@lastOccurrence", DateTime.UtcNow),
                        new Microsoft.Data.SqlClient.SqlParameter("@id", existingError.Id));

                    if (updateResult > 0)
                    {
                        using (LogContext.PushProperty("ErrorId", existingError.ErrorId))
                        using (LogContext.PushProperty("ErrorType", errorType.ToString()))
                        using (LogContext.PushProperty("Severity", severity.ToString()))
                        using (LogContext.PushProperty("UserId", userId))
                        using (LogContext.PushProperty("Username", username))
                        using (LogContext.PushProperty("CorrelationId", Activity.Current?.Id))
                        {
                            Log.Warning("🔄 Duplicate error aggregated: {ErrorId} - Count incremented", 
                                existingError.ErrorId);
                        }

                        return existingError.ErrorId;
                    }
                }

                // إضافة الخطأ الجديد بـ UTC timestamps
                await _unitOfWork.Repository<ErrorLog>().AddAsync(errorLog);
                await _unitOfWork.SaveAsync();

                // Enhanced Serilog logging
                LogToSerilogEnhanced(errorLog, exception);

                return errorLog.ErrorId;
            }
            catch (Exception ex)
            {
                // Fallback logging مع context محسّن
                using (LogContext.PushProperty("OriginalException", exception?.Message))
                using (LogContext.PushProperty("ErrorType", errorType.ToString()))
                using (LogContext.PushProperty("Severity", severity.ToString()))
                {
                    Log.Fatal(ex, "💥 Critical failure in error logging - Original: {OriginalException}", 
                        exception?.Message);
                }
                return Guid.NewGuid().ToString("N")[..8]; // Shorter fallback ID
            }
        }

        public async Task<string> LogErrorAsync(string message, string? details = null, 
            ErrorType errorType = ErrorType.SystemError, ErrorSeverity severity = ErrorSeverity.Error,
            int? userId = null, string? username = null)
        {
            try
            {
                var errorLog = new ErrorLog
                {
                    ErrorId = Guid.NewGuid().ToString("N")[..8], // Shorter ID
                    ErrorType = errorType,
                    Severity = severity,
                    Title = TruncateString(message, 500),
                    Message = message,
                    Details = details,
                    UserId = userId,
                    Username = username ?? Environment.UserName,
                    CreatedAt = DateTime.UtcNow, // UTC timestamp
                    LastOccurrence = DateTime.UtcNow,
                    Signature = ComputeMessageSignature(message, details), // Signature for dedup
                    CorrelationId = Activity.Current?.Id ?? Environment.CurrentManagedThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture)
                };

                await _unitOfWork.Repository<ErrorLog>().AddAsync(errorLog);
                await _unitOfWork.SaveAsync();

                // Enhanced Serilog logging
                LogToSerilogEnhanced(errorLog, null);

                return errorLog.ErrorId;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "فشل في تسجيل رسالة الخطأ: {Message}", message);
                return Guid.NewGuid().ToString("N")[..12];
            }
        }

        #endregion

        #region تسجيل أنواع محددة من الأخطاء

        public async Task<string> LogUIErrorAsync(Exception exception, int? userId = null, string? username = null)
        {
            return await LogErrorAsync(exception, ErrorType.UIError, ErrorSeverity.Error, userId, username);
        }

        public async Task<string> LogDatabaseErrorAsync(Exception exception, int? userId = null, string? username = null)
        {
            return await LogErrorAsync(exception, ErrorType.DatabaseError, ErrorSeverity.Critical, userId, username);
        }

        public async Task<string> LogBusinessLogicErrorAsync(Exception exception, int? userId = null, string? username = null)
        {
            return await LogErrorAsync(exception, ErrorType.BusinessLogicError, ErrorSeverity.Error, userId, username);
        }

        public async Task<string> LogValidationErrorAsync(string message, int? userId = null, string? username = null)
        {
            return await LogErrorAsync(message, null, ErrorType.ValidationError, ErrorSeverity.Warning, userId, username);
        }

        public async Task<string> LogSecurityErrorAsync(Exception exception, int? userId = null, string? username = null)
        {
            return await LogErrorAsync(exception, ErrorType.SecurityError, ErrorSeverity.Critical, userId, username);
        }

        public async Task<string> LogFinancialErrorAsync(Exception exception, int? userId = null, string? username = null)
        {
            return await LogErrorAsync(exception, ErrorType.FinancialError, ErrorSeverity.Critical, userId, username);
        }

        public async Task<string> LogInventoryErrorAsync(Exception exception, int? userId = null, string? username = null)
        {
            return await LogErrorAsync(exception, ErrorType.InventoryError, ErrorSeverity.Error, userId, username);
        }

        public async Task<string> LogReportErrorAsync(Exception exception, string reportName, 
            int? userId = null, string? username = null)
        {
            return await LogErrorAsync(exception, ErrorType.ReportError, ErrorSeverity.Error, userId, username);
        }

        #endregion

        #region إدارة سجلات الأخطاء

        public async Task<ErrorSearchResult> SearchErrorsAsync(ErrorSearchRequest request)
        {
            var errors = await _unitOfWork.Repository<ErrorLog>().GetAllAsync();
            var query = errors.AsQueryable();

            // التصفية
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                query = query.Where(e => e.Title.Contains(request.SearchText) ||
                                       e.Message.Contains(request.SearchText) ||
                                       (e.Details != null && e.Details.Contains(request.SearchText)));
            }

            if (request.ErrorType.HasValue)
                query = query.Where(e => e.ErrorType == request.ErrorType);

            if (request.Severity.HasValue)
                query = query.Where(e => e.Severity == request.Severity);

            if (request.Status.HasValue)
                query = query.Where(e => e.Status == request.Status);

            if (request.UserId.HasValue)
                query = query.Where(e => e.UserId == request.UserId);

            if (request.FromDate.HasValue)
                query = query.Where(e => e.CreatedAt >= request.FromDate);

            if (request.ToDate.HasValue)
                query = query.Where(e => e.CreatedAt <= request.ToDate);

            if (request.OnlyStarred.HasValue && request.OnlyStarred.Value)
                query = query.Where(e => e.IsStarred);

            // إجمالي العدد
            var totalCount = query.Count();

            // الترتيب
            query = request.OrderBy?.ToLower() switch
            {
                "title" => request.OrderDescending ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
                "severity" => request.OrderDescending ? query.OrderByDescending(e => e.Severity) : query.OrderBy(e => e.Severity),
                "status" => request.OrderDescending ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
                "errortype" => request.OrderDescending ? query.OrderByDescending(e => e.ErrorType) : query.OrderBy(e => e.ErrorType),
                _ => request.OrderDescending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt)
            };

            // التصفح
            var resultErrors = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new ErrorSearchResult
            {
                Errors = resultErrors,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<ErrorLog?> GetErrorByIdAsync(int id)
        {
            return await _unitOfWork.Repository<ErrorLog>().GetByIdAsync(id);
        }

        public async Task<ErrorLog?> GetErrorByErrorIdAsync(string errorId)
        {
            return await _unitOfWork.Repository<ErrorLog>().SingleOrDefaultAsync(e => e.ErrorId == errorId);
        }

        public async Task<bool> UpdateErrorStatusAsync(int errorId, ErrorStatus status, int resolvedBy, string? resolutionNotes = null)
        {
            try
            {
                var error = await _unitOfWork.Repository<ErrorLog>().GetByIdAsync(errorId);
                if (error == null) return false;

                error.Status = status;
                error.UpdatedAt = DateTime.Now;

                if (status == ErrorStatus.Resolved)
                {
                    error.ResolvedAt = DateTime.Now;
                    error.ResolvedBy = resolvedBy;
                    error.ResolutionNotes = resolutionNotes;
                }

                _unitOfWork.Repository<ErrorLog>().Update(error);
                await _unitOfWork.SaveAsync();

                LogStatusUpdated(_logger, error.ErrorId, status, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تحديث حالة الخطأ {ErrorId}", errorId);
                return false;
            }
        }

        public async Task<bool> AddCommentAsync(int errorId, int userId, string username, string comment)
        {
            try
            {
                var errorComment = new ErrorLogComment
                {
                    ErrorLogId = errorId,
                    UserId = userId,
                    Username = username,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };

                await _unitOfWork.Repository<ErrorLogComment>().AddAsync(errorComment);
                await _unitOfWork.SaveAsync();

                LogCommentAdded(_logger, errorId, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في إضافة تعليق للخطأ {ErrorId}", errorId);
                return false;
            }
        }

        public async Task<bool> ToggleStarAsync(int errorId, bool isStarred)
        {
            try
            {
                var error = await _unitOfWork.Repository<ErrorLog>().GetByIdAsync(errorId);
                if (error == null) return false;

                error.IsStarred = isStarred;
                error.UpdatedAt = DateTime.Now;

                _unitOfWork.Repository<ErrorLog>().Update(error);
                await _unitOfWork.SaveAsync();

                LogStarToggled(_logger, errorId, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تحديث علامة النجمة للخطأ {ErrorId}", errorId);
                return false;
            }
        }

        #endregion

        #region إحصائيات

        public async Task<ErrorStatistics> GetErrorStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            fromDate ??= DateTime.Now.AddDays(-30);
            toDate ??= DateTime.Now;

            var allErrors = await _unitOfWork.Repository<ErrorLog>().GetAllAsync();
            var errors = allErrors.Where(e => e.CreatedAt >= fromDate && e.CreatedAt <= toDate).ToList();

            var statistics = new ErrorStatistics
            {
                TotalErrors = errors.Count,
                NewErrors = errors.Count(e => e.Status == ErrorStatus.New),
                ResolvedErrors = errors.Count(e => e.Status == ErrorStatus.Resolved),
                CriticalErrors = errors.Count(e => e.Severity == ErrorSeverity.Critical || e.Severity == ErrorSeverity.Fatal),
                ErrorsByType = errors.GroupBy(e => e.ErrorType).ToDictionary(g => g.Key, g => g.Count()),
                ErrorsBySeverity = errors.GroupBy(e => e.Severity).ToDictionary(g => g.Key, g => g.Count()),
                ErrorsByDate = errors.GroupBy(e => e.CreatedAt.Date).ToDictionary(g => g.Key, g => g.Count()),
                TopErrors = errors
                    .GroupBy(e => new { e.ErrorId, e.Title, e.ErrorType, e.Severity })
                    .Select(g => new TopErrorInfo
                    {
                        ErrorId = g.Key.ErrorId,
                        Title = g.Key.Title,
                        ErrorType = g.Key.ErrorType,
                        Severity = g.Key.Severity,
                        Count = g.Sum(e => e.OccurrenceCount),
                        LastOccurrence = g.Max(e => e.LastOccurrence ?? e.CreatedAt)
                    })
                    .OrderByDescending(e => e.Count)
                    .Take(10)
                    .ToList()
            };

            // حساب معدلات الأداء
            if (statistics.TotalErrors > 0)
            {
                statistics.ResolutionRate = (double)statistics.ResolvedErrors / statistics.TotalErrors * 100;
                
                var resolvedErrors = errors.Where(e => e.ResolvedAt.HasValue && e.CreatedAt != e.ResolvedAt);
                if (resolvedErrors.Any())
                {
                    statistics.AverageResolutionTime = resolvedErrors
                        .Average(e => (e.ResolvedAt!.Value - e.CreatedAt).TotalHours);
                }
            }

            var days = (toDate.Value - fromDate.Value).Days;
            statistics.DailyErrorRate = days > 0 ? (double)statistics.TotalErrors / days : 0;

            return statistics;
        }

        public async Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 10)
        {
            var allErrors = await _unitOfWork.Repository<ErrorLog>().GetAllAsync();
            return allErrors.OrderByDescending(e => e.CreatedAt).Take(count).ToList();
        }

        public async Task<List<ErrorLog>> GetCriticalErrorsAsync()
        {
            var allErrors = await _unitOfWork.Repository<ErrorLog>().GetAllAsync();
            return allErrors.Where(e => (e.Severity == ErrorSeverity.Critical || e.Severity == ErrorSeverity.Fatal) &&
                                       e.Status != ErrorStatus.Resolved && e.Status != ErrorStatus.Closed)
                             .OrderByDescending(e => e.CreatedAt).ToList();
        }

        public async Task<int> GetUnresolvedErrorsCountAsync()
        {
            var allErrors = await _unitOfWork.Repository<ErrorLog>().GetAllAsync();
            return allErrors.Count(e => e.Status != ErrorStatus.Resolved && e.Status != ErrorStatus.Closed);
        }

        #endregion

        #region تنظيف السجلات القديمة

        public async Task<int> CleanupOldLogsAsync(int retentionDays = 90)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var allErrors = await _unitOfWork.Repository<ErrorLog>().GetAllAsync();
                
                var oldErrors = allErrors.Where(e => e.CreatedAt < cutoffDate &&
                                                    (e.Status == ErrorStatus.Resolved || e.Status == ErrorStatus.Closed) &&
                                                    !e.IsStarred).ToList();

                var count = oldErrors.Count;
                if (count > 0)
                {
                    foreach (var error in oldErrors)
                    {
                        _unitOfWork.Repository<ErrorLog>().Remove(error);
                    }
                    
                    await _unitOfWork.SaveAsync();
                    Log.Information("تم حذف {Count} من سجلات الأخطاء القديمة", count);
                }

                return count;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تنظيف سجلات الأخطاء القديمة");
                return 0;
            }
        }

        #endregion

        #region تقارير الأخطاء

        public async Task<ErrorReport> GenerateErrorReportAsync(DateTime fromDate, DateTime toDate,
            ErrorType? errorType = null, ErrorSeverity? severity = null)
        {
            var allErrors = await _unitOfWork.Repository<ErrorLog>().GetAllAsync();
            var filteredErrors = allErrors.Where(e => e.CreatedAt >= fromDate && e.CreatedAt <= toDate);

            if (errorType.HasValue)
                filteredErrors = filteredErrors.Where(e => e.ErrorType == errorType);

            if (severity.HasValue)
                filteredErrors = filteredErrors.Where(e => e.Severity == severity);

            var errors = filteredErrors.ToList();
            var statistics = await GetErrorStatisticsAsync(fromDate, toDate);

            var report = new ErrorReport
            {
                Title = $"تقرير الأخطاء من {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}",
                GeneratedAt = DateTime.Now,
                FromDate = fromDate,
                ToDate = toDate,
                Statistics = statistics,
                ErrorDetails = errors,
                Recommendations = GenerateRecommendations(statistics)
            };

            return report;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// إنشاء سجل خطأ محصن مع Signature وUTC timestamps
        /// </summary>
        private static ErrorLog CreateHardenedErrorLogFromException(Exception exception, ErrorType errorType,
            ErrorSeverity severity, int? userId, string? username)
        {
            // Enhanced StackTrace - البحث عن أول Frame مفيد
            var stackTrace = new StackTrace(exception, true);
            var frame = stackTrace.GetFrames()?
                .FirstOrDefault(f => f.GetFileLineNumber() > 0) ?? stackTrace.GetFrame(0);

            // حساب Signature للتجميع
            var signature = ComputeErrorSignature(exception);

            // الحصول على CorrelationId من Activity الحالي
            var correlationId = Activity.Current?.Id ?? Environment.CurrentManagedThreadId.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var errorLog = new ErrorLog
            {
                ErrorId = Guid.NewGuid().ToString("N")[..8], // Shorter ID
                ErrorType = errorType,
                Severity = severity,
                Title = TruncateString(exception.GetType().Name, 500),
                Message = exception.Message,
                Details = exception.ToString(),
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.ToString(),
                Source = exception.Source,
                MethodName = frame?.GetMethod()?.Name,
                LineNumber = frame?.GetFileLineNumber(),
                FileName = frame?.GetFileName(),
                UserId = userId,
                Username = username ?? Environment.UserName,
                CreatedAt = DateTime.UtcNow, // UTC timestamp
                LastOccurrence = DateTime.UtcNow,
                Signature = signature,
                CorrelationId = correlationId
            };

            return errorLog;
        }

        /// <summary>
        /// حساب توقيع الخطأ للتجميع (SHA-256)
        /// </summary>
        private static string ComputeErrorSignature(Exception ex)
        {
            var signatureComponents = $"{ex.GetType().FullName}|{ex.Message}|{ex.Source}|{ex.TargetSite?.Name}|{ex.StackTrace}";
            
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(signatureComponents));
            return Convert.ToHexString(hashBytes); // .NET 5+ method
        }

        /// <summary>
        /// حساب توقيع رسالة الخطأ للتجميع
        /// </summary>
        private static string ComputeMessageSignature(string message, string? details)
        {
            var signatureComponents = $"MESSAGE|{message}|{details}";
            
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(signatureComponents));
            return Convert.ToHexString(hashBytes);
        }

        private async Task<ErrorLog?> FindSimilarRecentErrorAsync(string title, string message)
        {
            var cutoffTime = DateTime.Now.AddMinutes(-5); // آخر 5 دقائق
            var allErrors = await _unitOfWork.Repository<ErrorLog>().GetAllAsync();

            return allErrors.Where(e => e.Title == title && e.Message == message && e.CreatedAt >= cutoffTime)
                           .OrderByDescending(e => e.CreatedAt)
                           .FirstOrDefault();
        }

        /// <summary>
        /// Enhanced Serilog logging مع context محسّن وstructured data
        /// </summary>
        private static void LogToSerilogEnhanced(ErrorLog errorLog, Exception? exception)
        {
            using (LogContext.PushProperty("ErrorId", errorLog.ErrorId))
            using (LogContext.PushProperty("ErrorType", errorLog.ErrorType.ToString()))
            using (LogContext.PushProperty("Severity", errorLog.Severity.ToString()))
            using (LogContext.PushProperty("UserId", errorLog.UserId))
            using (LogContext.PushProperty("Username", errorLog.Username))
            using (LogContext.PushProperty("CorrelationId", errorLog.CorrelationId))
            using (LogContext.PushProperty("Signature", errorLog.Signature))
            using (LogContext.PushProperty("OccurrenceCount", errorLog.OccurrenceCount))
            {
                var messageTemplate = errorLog.Severity switch
                {
                    ErrorSeverity.Info => "ℹ️ Info logged: {ErrorId} - {Message}",
                    ErrorSeverity.Warning => "⚠️ Warning: {ErrorId} - {Message}",
                    ErrorSeverity.Error => "❌ Error: {ErrorId} - {Message}",
                    ErrorSeverity.Critical => "🚨 Critical: {ErrorId} - {Message}",
                    ErrorSeverity.Fatal => "💀 Fatal: {ErrorId} - {Message}",
                    _ => "❓ Unknown: {ErrorId} - {Message}"
                };

                switch (errorLog.Severity)
                {
                    case ErrorSeverity.Info:
                        Log.Information(messageTemplate, errorLog.ErrorId, errorLog.Message);
                        break;
                    case ErrorSeverity.Warning:
                        Log.Warning(messageTemplate, errorLog.ErrorId, errorLog.Message);
                        break;
                    case ErrorSeverity.Error:
                        if (exception != null)
                            Log.Error(exception, messageTemplate, errorLog.ErrorId, errorLog.Message);
                        else
                            Log.Error(messageTemplate, errorLog.ErrorId, errorLog.Message);
                        break;
                    case ErrorSeverity.Critical:
                    case ErrorSeverity.Fatal:
                        if (exception != null)
                            Log.Fatal(exception, messageTemplate, errorLog.ErrorId, errorLog.Message);
                        else
                            Log.Fatal(messageTemplate, errorLog.ErrorId, errorLog.Message);
                        break;
                }
            }
        }

        private static List<string> GenerateRecommendations(ErrorStatistics statistics)
        {
            var recommendations = new List<string>();

            if (statistics.CriticalErrors > 0)
            {
                recommendations.Add($"يوجد {statistics.CriticalErrors} خطأ حرج يتطلب اهتماماً فورياً");
            }

            if (statistics.ResolutionRate < 50)
            {
                recommendations.Add($"معدل حل الأخطاء منخفض ({statistics.ResolutionRate:F1}%)، يُنصح بمراجعة عمليات المعالجة");
            }

            if (statistics.DailyErrorRate > 10)
            {
                recommendations.Add($"معدل الأخطاء اليومي مرتفع ({statistics.DailyErrorRate:F1} خطأ/يوم)");
            }

            var topErrorType = statistics.ErrorsByType.OrderByDescending(x => x.Value).FirstOrDefault();
            if (topErrorType.Value > statistics.TotalErrors * 0.3)
            {
                recommendations.Add($"أكثر أنواع الأخطاء شيوعاً: {topErrorType.Key} ({topErrorType.Value} خطأ)");
            }

            return recommendations;
        }

        private static string TruncateString(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Length > maxLength ? input[..(maxLength - 3)] + "..." : input;
        }

        #endregion
    }

    /// <summary>
    /// Extension Methods لتسهيل استخدام خدمة تسجيل الأخطاء
    /// </summary>
    public static class ErrorLoggingExtensions
    {
        public static async Task<string> LogErrorSafeAsync(this IErrorLoggingService errorLoggingService,
            Exception exception, ErrorType errorType = ErrorType.SystemError,
            [CallerMemberName] string? callerMethod = null,
            [CallerFilePath] string? callerFile = null,
            [CallerLineNumber] int callerLine = 0)
        {
            try
            {
                return await errorLoggingService.LogErrorAsync(exception, errorType);
            }
            catch
            {
                // في حالة فشل تسجيل الخطأ، عُد بـ ID مؤقت
                return Guid.NewGuid().ToString("N")[..12];
            }
        }

        public static async Task<string> LogMessageSafeAsync(this IErrorLoggingService errorLoggingService,
            string message, ErrorSeverity severity = ErrorSeverity.Error,
            [CallerMemberName] string? callerMethod = null,
            [CallerFilePath] string? callerFile = null,
            [CallerLineNumber] int callerLine = 0)
        {
            try
            {
                return await errorLoggingService.LogErrorAsync(message, severity: severity);
            }
            catch
            {
                return Guid.NewGuid().ToString("N")[..12];
            }
        }

        /// <summary>
        /// تسجيل شامل لكافة العمليات المحاسبية (ليس فقط الأخطاء)
        /// </summary>
        public static Task LogBusinessOperationAsync(string operation, string details, 
            int? userId = null, string? username = null, bool isSuccess = true)
        {
            try
            {
                var logContext = Log.ForContext("BusinessOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("UserId", userId)
                                   .ForContext("Username", username)
                                   .ForContext("IsSuccess", isSuccess);

                if (isSuccess)
                {
                    logContext.Information("✅ عملية محاسبية ناجحة: {Operation} - المستخدم: {Username} - التفاصيل: {Details}", 
                           operation, username, details);
                }
                else
                {
                    logContext.Warning("⚠️ فشل في عملية محاسبية: {Operation} - المستخدم: {Username} - التفاصيل: {Details}", 
                           operation, username, details);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تسجيل العملية المحاسبية: {Operation}", operation);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// تسجيل شامل لعمليات قاعدة البيانات
        /// </summary>
        public static Task LogDatabaseOperationAsync(string operation, string tableName, 
            string? details = null, int? recordId = null, bool isSuccess = true)
        {
            try
            {
                var logContext = Log.ForContext("DatabaseOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("TableName", tableName)
                                   .ForContext("RecordId", recordId)
                                   .ForContext("IsSuccess", isSuccess);

                if (isSuccess)
                {
                    logContext.Information("🗄️ عملية قاعدة بيانات ناجحة: {Operation} على {TableName} - سجل: {RecordId} - {Details}", 
                           operation, tableName, recordId, details);
                }
                else
                {
                    logContext.Error("❌ فشل في عملية قاعدة بيانات: {Operation} على {TableName} - سجل: {RecordId} - {Details}", 
                           operation, tableName, recordId, details);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تسجيل عملية قاعدة البيانات: {Operation}", operation);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// تسجيل شامل لعمليات المستخدمين
        /// </summary>
        public static Task LogUserOperationAsync(string operation, string username, 
            int? userId = null, string? details = null, bool isSuccess = true)
        {
            try
            {
                var logContext = Log.ForContext("UserOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("Username", username)
                                   .ForContext("UserId", userId)
                                   .ForContext("IsSuccess", isSuccess);

                if (isSuccess)
                {
                    logContext.Information("👤 عملية مستخدم ناجحة: {Operation} - المستخدم: {Username} - {Details}", 
                           operation, username, details);
                }
                else
                {
                    logContext.Warning("⚠️ فشل في عملية مستخدم: {Operation} - المستخدم: {Username} - {Details}", 
                           operation, username, details);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تسجيل عملية المستخدم: {Operation} للمستخدم: {Username}", operation, username);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// تسجيل شامل لعمليات الأمان
        /// </summary>
        public static Task LogSecurityOperationAsync(string operation, string? username = null, 
            string? ipAddress = null, string? details = null, bool isSuccess = true, 
            SecurityEventType eventType = SecurityEventType.General)
        {
            try
            {
                var logContext = Log.ForContext("SecurityOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("Username", username)
                                   .ForContext("IpAddress", ipAddress)
                                   .ForContext("EventType", eventType)
                                   .ForContext("IsSuccess", isSuccess);

                if (isSuccess)
                {
                    logContext.Information("🔒 عملية أمان ناجحة: {Operation} - المستخدم: {Username} - IP: {IpAddress} - {Details}", 
                           operation, username, ipAddress, details);
                }
                else
                {
                    logContext.Warning("🚨 محاولة أمان فاشلة: {Operation} - المستخدم: {Username} - IP: {IpAddress} - {Details}", 
                           operation, username, ipAddress, details);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تسجيل عملية الأمان: {Operation}", operation);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// أنواع أحداث الأمان
    /// </summary>
    public enum SecurityEventType
    {
        General,
        Login,
        Logout,
        FailedLogin,
        AccessDenied,
        PasswordChange,
        PermissionChange,
        DataAccess
    }
}