using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Business
{
    /// <summary>
    /// خدمة فحص صحة قاعدة البيانات والتحقق من الاتصال
    /// </summary>
    public interface IDatabaseHealthService
    {
        Task<DatabaseHealthResult> CheckDatabaseHealthAsync();
        Task<bool> CanConnectAsync();
        Task<bool> TestConnectionAsync(string connectionString);
        Task<MigrationStatus> CheckMigrationsAsync();
        Task<bool> ApplyPendingMigrationsAsync();
        Task<DatabaseStatistics> GetDatabaseStatisticsAsync();
    }

    /// <summary>
    /// تنفيذ خدمة فحص صحة قاعدة البيانات
    /// </summary>
    public class DatabaseHealthService : IDatabaseHealthService
    {
        private readonly AccountingDbContext _dbContext;
        private readonly ILogger<DatabaseHealthService> _logger;

        public DatabaseHealthService(AccountingDbContext dbContext, ILogger<DatabaseHealthService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// فحص شامل لصحة قاعدة البيانات
        /// </summary>
        public async Task<DatabaseHealthResult> CheckDatabaseHealthAsync()
        {
            var result = new DatabaseHealthResult
            {
                CheckedAt = DateTime.Now
            };

            try
            {
                // 1. اختبار الاتصال
                result.CanConnect = await CanConnectAsync();
                if (!result.CanConnect)
                {
                    result.IsHealthy = false;
                    result.Issues.Add("فشل الاتصال بقاعدة البيانات");
                    return result;
                }

                // 2. التحقق من وجود قاعدة البيانات
                result.DatabaseExists = await _dbContext.Database.CanConnectAsync();
                if (!result.DatabaseExists)
                {
                    result.IsHealthy = false;
                    result.Issues.Add("قاعدة البيانات غير موجودة");
                    return result;
                }

                // 3. التحقق من Migrations
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                result.HasPendingMigrations = pendingMigrations.Any();
                result.PendingMigrations = pendingMigrations.ToList();

                if (result.HasPendingMigrations)
                {
                    result.Warnings.Add($"يوجد {result.PendingMigrations.Count} migration معلق");
                }

                // 4. التحقق من الجداول الأساسية
                result.TablesExist = await CheckCriticalTablesAsync();
                if (!result.TablesExist)
                {
                    result.IsHealthy = false;
                    result.Issues.Add("بعض الجداول الأساسية مفقودة");
                }

                // 5. التحقق من البيانات الأساسية
                result.HasSeedData = await CheckSeedDataAsync();
                if (!result.HasSeedData)
                {
                    result.Warnings.Add("البيانات الأولية مفقودة أو غير كاملة");
                }

                // 6. فحص الفهارس
                var indexIssues = await CheckIndexesAsync();
                if (indexIssues.Any())
                {
                    result.Warnings.AddRange(indexIssues);
                }

                // 7. فحص Foreign Keys
                var fkIssues = await CheckForeignKeysAsync();
                if (fkIssues.Any())
                {
                    result.Issues.AddRange(fkIssues);
                    result.IsHealthy = false;
                }

                // تحديد الحالة العامة
                result.IsHealthy = !result.Issues.Any() && result.CanConnect && result.DatabaseExists && result.TablesExist;
                
                _logger.LogInformation("Database health check completed. Status: {Status}", 
                    result.IsHealthy ? "Healthy" : "Unhealthy");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في فحص صحة قاعدة البيانات");
                result.IsHealthy = false;
                result.Issues.Add($"خطأ في الفحص: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// اختبار الاتصال بقاعدة البيانات
        /// </summary>
        public async Task<bool> CanConnectAsync()
        {
            try
            {
                await _dbContext.Database.OpenConnectionAsync();
                await _dbContext.Database.CloseConnectionAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل الاتصال بقاعدة البيانات");
                return false;
            }
        }

        /// <summary>
        /// اختبار سلسلة اتصال معينة
        /// </summary>
        public async Task<bool> TestConnectionAsync(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                await connection.CloseAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل اختبار سلسلة الاتصال");
                return false;
            }
        }

        /// <summary>
        /// التحقق من حالة Migrations
        /// </summary>
        public async Task<MigrationStatus> CheckMigrationsAsync()
        {
            var status = new MigrationStatus();

            try
            {
                status.AppliedMigrations = (await _dbContext.Database.GetAppliedMigrationsAsync()).ToList();
                status.PendingMigrations = (await _dbContext.Database.GetPendingMigrationsAsync()).ToList();
                status.AllMigrations = _dbContext.Database.GetMigrations().ToList();

                status.IsUpToDate = !status.PendingMigrations.Any();
                status.LastAppliedMigration = status.AppliedMigrations.LastOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من حالة Migrations");
                status.Error = ex.Message;
            }

            return status;
        }

        /// <summary>
        /// تطبيق Migrations المعلقة
        /// </summary>
        public async Task<bool> ApplyPendingMigrationsAsync()
        {
            try
            {
                var pending = await _dbContext.Database.GetPendingMigrationsAsync();
                if (!pending.Any())
                {
                    _logger.LogInformation("لا توجد migrations معلقة");
                    return true;
                }

                _logger.LogInformation("تطبيق {Count} migration معلق", pending.Count());
                await _dbContext.Database.MigrateAsync();
                _logger.LogInformation("تم تطبيق Migrations بنجاح");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل تطبيق Migrations");
                return false;
            }
        }

        /// <summary>
        /// الحصول على إحصائيات قاعدة البيانات
        /// </summary>
        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync()
        {
            var stats = new DatabaseStatistics();

            try
            {
                // عدد السجلات في الجداول الرئيسية
                stats.CustomersCount = await _dbContext.Customers.CountAsync();
                stats.SuppliersCount = await _dbContext.Suppliers.CountAsync();
                stats.ProductsCount = await _dbContext.Products.CountAsync();
                stats.SalesInvoicesCount = await _dbContext.SalesInvoices.CountAsync();
                stats.PurchaseInvoicesCount = await _dbContext.PurchaseInvoices.CountAsync();
                stats.UsersCount = await _dbContext.Users.CountAsync();

                // حجم قاعدة البيانات
                stats.DatabaseSize = await GetDatabaseSizeAsync();

                // آخر تحديث
                stats.LastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الحصول على إحصائيات قاعدة البيانات");
            }

            return stats;
        }

        #region Private Helper Methods

        /// <summary>
        /// التحقق من وجود الجداول الأساسية
        /// </summary>
        private async Task<bool> CheckCriticalTablesAsync()
        {
            try
            {
                var criticalTables = new[]
                {
                    "Users", "Customers", "Suppliers", "Products", "Categories", 
                    "Units", "SalesInvoices", "PurchaseInvoices", "SystemSettings"
                };

                foreach (var tableName in criticalTables)
                {
                    var exists = await TableExistsAsync(tableName);
                    if (!exists)
                    {
                        _logger.LogWarning("الجدول {TableName} غير موجود", tableName);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من الجداول");
                return false;
            }
        }

        /// <summary>
        /// التحقق من وجود جدول
        /// </summary>
        private async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = @TableName";

                var count = await _dbContext.Database
                    .ExecuteSqlRawAsync(sql, new SqlParameter("@TableName", tableName));

                return count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// التحقق من البيانات الأولية
        /// </summary>
        private async Task<bool> CheckSeedDataAsync()
        {
            try
            {
                // التحقق من وجود بيانات أولية أساسية
                var hasUsers = await _dbContext.Users.AnyAsync();
                var hasSettings = await _dbContext.SystemSettings.AnyAsync();
                var hasNumberSequences = await _dbContext.NumberSequences.AnyAsync();

                return hasUsers && hasSettings && hasNumberSequences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من البيانات الأولية");
                return false;
            }
        }

        /// <summary>
        /// فحص الفهارس
        /// </summary>
        private async Task<List<string>> CheckIndexesAsync()
        {
            var issues = new List<string>();

            try
            {
                // يمكن إضافة فحوصات مخصصة للفهارس هنا
                // مثل: التحقق من الفهارس المفقودة أو المكسورة
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في فحص الفهارس");
                issues.Add("خطأ في فحص الفهارس");
            }

            return issues;
        }

        /// <summary>
        /// فحص Foreign Keys
        /// </summary>
        private async Task<List<string>> CheckForeignKeysAsync()
        {
            var issues = new List<string>();

            try
            {
                // التحقق من Foreign Keys المكسورة
                var sql = @"
                    SELECT 
                        fk.name AS ForeignKeyName,
                        OBJECT_NAME(fk.parent_object_id) AS TableName,
                        COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
                        OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
                        COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
                    FROM sys.foreign_keys fk
                    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                    WHERE fk.is_disabled = 1";

                using var command = _dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                await _dbContext.Database.OpenConnectionAsync();

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var fkName = reader["ForeignKeyName"].ToString();
                    issues.Add($"Foreign Key معطل: {fkName}");
                }

                await _dbContext.Database.CloseConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في فحص Foreign Keys");
            }

            return issues;
        }

        /// <summary>
        /// الحصول على حجم قاعدة البيانات
        /// </summary>
        private async Task<string> GetDatabaseSizeAsync()
        {
            try
            {
                var sql = @"
                    SELECT 
                        SUM(size) * 8 / 1024 AS SizeMB
                    FROM sys.database_files";

                using var command = _dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = sql;
                await _dbContext.Database.OpenConnectionAsync();

                var result = await command.ExecuteScalarAsync();
                await _dbContext.Database.CloseConnectionAsync();

                if (result != null && int.TryParse(result.ToString(), out var sizeMB))
                {
                    return $"{sizeMB} MB";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الحصول على حجم قاعدة البيانات");
            }

            return "غير معروف";
        }

        #endregion
    }

    #region Result Classes

    /// <summary>
    /// نتيجة فحص صحة قاعدة البيانات
    /// </summary>
    public class DatabaseHealthResult
    {
        public bool IsHealthy { get; set; }
        public DateTime CheckedAt { get; set; }
        public bool CanConnect { get; set; }
        public bool DatabaseExists { get; set; }
        public bool TablesExist { get; set; }
        public bool HasSeedData { get; set; }
        public bool HasPendingMigrations { get; set; }
        public List<string> PendingMigrations { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public string GetSummary()
        {
            if (IsHealthy)
                return "قاعدة البيانات بحالة جيدة";

            if (!CanConnect)
                return "فشل الاتصال بقاعدة البيانات";

            if (!DatabaseExists)
                return "قاعدة البيانات غير موجودة";

            if (Issues.Any())
                return $"يوجد {Issues.Count} مشكلة في قاعدة البيانات";

            if (Warnings.Any())
                return $"يوجد {Warnings.Count} تحذير";

            return "حالة غير معروفة";
        }
    }

    /// <summary>
    /// حالة Migrations
    /// </summary>
    public class MigrationStatus
    {
        public List<string> AppliedMigrations { get; set; } = new();
        public List<string> PendingMigrations { get; set; } = new();
        public List<string> AllMigrations { get; set; } = new();
        public bool IsUpToDate { get; set; }
        public string? LastAppliedMigration { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// إحصائيات قاعدة البيانات
    /// </summary>
    public class DatabaseStatistics
    {
        public int CustomersCount { get; set; }
        public int SuppliersCount { get; set; }
        public int ProductsCount { get; set; }
        public int SalesInvoicesCount { get; set; }
        public int PurchaseInvoicesCount { get; set; }
        public int UsersCount { get; set; }
        public string DatabaseSize { get; set; } = "غير معروف";
        public DateTime LastUpdated { get; set; }
    }

    #endregion
}
