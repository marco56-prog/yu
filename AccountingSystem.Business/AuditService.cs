using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Business
{
    /// <summary>
    /// واجهة خدمة تتبع العمليات
    /// </summary>
    public interface IAuditService
    {
        Task LogAsync(string operation, string? tableName, int? recordId, object? oldValues, object? newValues, 
            int userId, string username, string? ipAddress = null, string? details = null, AuditSeverity severity = AuditSeverity.Medium);
        
        Task LogSuccessAsync(string operation, int userId, string username, string? details = null);
        Task LogFailureAsync(string operation, int userId, string username, string errorMessage, string? details = null);
        
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null, 
            int? userId = null, string? operation = null, int pageSize = 50, int pageNumber = 1);
        
        Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string tableName, int recordId);
        Task<AuditStatistics> GetAuditStatisticsAsync(DateTime fromDate, DateTime toDate);
        
        Task CleanupOldLogsAsync(int retentionDays = 365);
    }

    /// <summary>
    /// تنفيذ خدمة تتبع العمليات
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// تسجيل عملية في سجل التتبع
        /// </summary>
        public async Task LogAsync(string operation, string? tableName, int? recordId, object? oldValues, object? newValues,
            int userId, string username, string? ipAddress = null, string? details = null, AuditSeverity severity = AuditSeverity.Medium)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Action = operation,
                    TableName = tableName,
                    RecordId = recordId,
                    OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, GetJsonOptions()) : null,
                    NewValues = newValues != null ? JsonSerializer.Serialize(newValues, GetJsonOptions()) : null,
                    UserId = userId,
                    Username = username,
                    Timestamp = DateTime.Now,
                    IpAddress = ipAddress,
                    Details = details,
                    Severity = severity.ToString(),
                    Status = AuditStatus.Success.ToString()
                };

                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                await _unitOfWork.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit entry for operation {Operation}", operation);
            }
        }

        /// <summary>
        /// تسجيل عملية ناجحة
        /// </summary>
        public async Task LogSuccessAsync(string operation, int userId, string username, string? details = null)
        {
            await LogAsync(operation, null, null, null, null, userId, username, 
                details: details, severity: AuditSeverity.Low);
        }

        /// <summary>
        /// تسجيل عملية فاشلة
        /// </summary>
        public async Task LogFailureAsync(string operation, int userId, string username, string errorMessage, string? details = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    Action = operation,
                    UserId = userId,
                    Username = username,
                    Timestamp = DateTime.Now,
                    Details = details,
                    Severity = AuditSeverity.High.ToString(),
                    Status = AuditStatus.Failed.ToString(),
                    ErrorMessage = errorMessage
                };

                await _unitOfWork.AuditLogs.AddAsync(auditLog);
                await _unitOfWork.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log failure audit entry for operation {Operation}", operation);
            }
        }

        /// <summary>
        /// الحصول على سجلات التتبع مع التصفية
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(DateTime? fromDate = null, DateTime? toDate = null,
            int? userId = null, string? operation = null, int pageSize = 50, int pageNumber = 1)
        {
            // استخدام Repository pattern مع LINQ filtering
            var logs = await _unitOfWork.AuditLogs.GetAllAsync();
            
            IEnumerable<AuditLog> filteredLogs = logs;
            
            if (fromDate.HasValue)
                filteredLogs = filteredLogs.Where(a => a.Timestamp >= fromDate.Value);

            if (toDate.HasValue)
                filteredLogs = filteredLogs.Where(a => a.Timestamp <= toDate.Value);

            if (userId.HasValue)
                filteredLogs = filteredLogs.Where(a => a.UserId == userId.Value);

            if (!string.IsNullOrEmpty(operation))
                filteredLogs = filteredLogs.Where(a => a.Action != null && a.Action.Contains(operation));

            var result = filteredLogs
                .OrderByDescending(a => a.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return result;
        }

        /// <summary>
        /// الحصول على تاريخ كيان محدد
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string tableName, int recordId)
        {
            return await _unitOfWork.AuditLogs
                .FindAsync(a => a.TableName == tableName && a.RecordId == recordId);
        }

        /// <summary>
        /// إحصائيات سجل التتبع
        /// </summary>
        public async Task<AuditStatistics> GetAuditStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            var logs = await _unitOfWork.AuditLogs
                .FindAsync(a => a.Timestamp >= fromDate && a.Timestamp <= toDate);

            var logsList = logs.ToList();

            return new AuditStatistics
            {
                TotalOperations = logsList.Count,
                SuccessfulOperations = logsList.Count(l => l.Status == AuditStatus.Success.ToString()),
                FailedOperations = logsList.Count(l => l.Status == AuditStatus.Failed.ToString()),
                UniqueUsers = logsList.Select(l => l.UserId).Distinct().Count(),
                MostActiveUser = logsList.GroupBy(l => l.Username)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "غير محدد",
                MostCommonOperation = logsList.GroupBy(l => l.Action ?? "غير محدد")
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "غير محدد",
                CriticalOperations = logsList.Count(l => l.Severity == AuditSeverity.Critical.ToString()),
                FromDate = fromDate,
                ToDate = toDate
            };
        }

        /// <summary>
        /// تنظيف السجلات القديمة
        /// </summary>
        public async Task CleanupOldLogsAsync(int retentionDays = 365)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var oldLogs = await _unitOfWork.AuditLogs
                    .FindAsync(a => a.Timestamp < cutoffDate);

                var oldLogsList = oldLogs.ToList();
                if (oldLogsList.Count > 0)
                {
                    _unitOfWork.AuditLogs.RemoveRange(oldLogsList);
                    await _unitOfWork.SaveAsync();

                    _logger.LogInformation("Cleaned up {Count} old audit log entries", oldLogsList.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old audit logs");
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }
    }

    /// <summary>
    /// إحصائيات سجل التتبع
    /// </summary>
    public class AuditStatistics
    {
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public int UniqueUsers { get; set; }
        public string MostActiveUser { get; set; } = string.Empty;
        public string MostCommonOperation { get; set; } = string.Empty;
        public int CriticalOperations { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal SuccessRate => TotalOperations > 0 ? 
            Math.Round((decimal)SuccessfulOperations / TotalOperations * 100, 2) : 0;
    }

    /// <summary>
    /// Extension Methods للتسهيل
    /// </summary>
    public static class AuditExtensions
    {
        /// <summary>
        /// تسجيل إنشاء كيان
        /// </summary>
        public static async Task LogCreateAsync<T>(this IAuditService auditService, T entity, int userId, string username)
            where T : class
        {
            var tableName = typeof(T).Name;
            var recordId = GetEntityId(entity);
            
            await auditService.LogAsync(
                AuditOperations.CreateProduct, // استخدام عملية موجودة
                tableName,
                recordId,
                null,
                entity,
                userId,
                username,
                severity: AuditSeverity.Low
            );
        }

        /// <summary>
        /// تسجيل تعديل كيان
        /// </summary>
        public static async Task LogUpdateAsync<T>(this IAuditService auditService, T oldEntity, T newEntity, int userId, string username)
            where T : class
        {
            var tableName = typeof(T).Name;
            var recordId = GetEntityId(newEntity);

            await auditService.LogAsync(
                AuditOperations.UpdateProduct, // استخدام عملية موجودة
                tableName,
                recordId,
                oldEntity,
                newEntity,
                userId,
                username,
                severity: AuditSeverity.Medium
            );
        }

        /// <summary>
        /// تسجيل حذف كيان
        /// </summary>
        public static async Task LogDeleteAsync<T>(this IAuditService auditService, T entity, int userId, string username)
            where T : class
        {
            var tableName = typeof(T).Name;
            var recordId = GetEntityId(entity);

            await auditService.LogAsync(
                AuditOperations.DeleteProduct, // استخدام عملية موجودة
                tableName,
                recordId,
                entity,
                null,
                userId,
                username,
                severity: AuditSeverity.High
            );
        }

        private static int? GetEntityId<T>(T entity) where T : class
        {
            var idProperty = typeof(T).GetProperty($"{typeof(T).Name}Id");
            if (idProperty != null && idProperty.PropertyType == typeof(int))
            {
                return (int?)idProperty.GetValue(entity);
            }
            return null;
        }
    }
}