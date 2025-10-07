using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.Globalization;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    /// <summary>
    /// خيارات النسخ الاحتياطي
    /// </summary>
    public class BackupOptions
    {
        public bool IncludeData { get; set; } = true;
        public bool IncludeSchema { get; set; } = true;
        public bool CompressBackup { get; set; } = true;
        public string? Description { get; set; }
        public List<string> TablesToExclude { get; set; } = new();
    }

    /// <summary>
    /// خيارات الاستعادة
    /// </summary>
    public class RestoreOptions
    {
        public bool OverwriteExisting { get; set; } = false;
        public bool RestoreData { get; set; } = true;
        public bool RestoreSchema { get; set; } = true;
        public List<string> TablesToRestore { get; set; } = new();
    }

    /// <summary>
    /// نتيجة عملية النسخ الاحتياطي
    /// </summary>
    public class BackupResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public int TablesBackedUp { get; set; }
        public int RecordsBackedUp { get; set; }
    }

    /// <summary>
    /// نتيجة عملية الاستعادة
    /// </summary>
    public class RestoreResult
    {
        public bool Success { get; set; }
        public DateTime RestoredAt { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public int TablesRestored { get; set; }
        public int RecordsRestored { get; set; }
    }

    /// <summary>
    /// معلومات النسخة الاحتياطية
    /// </summary>
    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public long FileSize { get; set; }
        public string? Description { get; set; }
        public bool IsCompressed { get; set; }
        public string FormattedSize => FormatFileSize(FileSize);

        private static string FormatFileSize(long bytes)
        {
            const int scale = 1024;
            string[] orders = { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return string.Create(CultureInfo.InvariantCulture, $"{decimal.Divide(bytes, max):##.##} {order}");
                max /= scale;
            }
            return "0 Bytes";
        }
    }

    /// <summary>
    /// جدولة النسخ الاحتياطي التلقائي
    /// </summary>
    public class BackupSchedule
    {
        public TimeSpan Interval { get; set; }
        public BackupOptions Options { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// واجهة خدمة النسخ الاحتياطي
    /// </summary>
    public interface IBackupService
    {
        Task<BackupResult> CreateBackupAsync(BackupOptions options);
        Task<RestoreResult> RestoreBackupAsync(string backupPath, RestoreOptions options);
        Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync();
        Task<bool> DeleteBackupAsync(string backupPath);
        Task<bool> ValidateBackupAsync(string backupPath);
        Task ScheduleAutomaticBackupAsync(BackupSchedule schedule);
    }

    /// <summary>
    /// تنفيذ خدمة النسخ الاحتياطي
    /// </summary>
    public class BackupService : IBackupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BackupService> _logger;
        private readonly IAuditService _auditService;
        private readonly string _backupDirectory;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static readonly Action<ILogger, string, Exception?> LogBackupStarted =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(2001, "BackupStarted"),
                "بدء عملية النسخ الاحتياطي: {BackupType}");

        private static readonly Action<ILogger, string, string, Exception?> LogBackupCompleted =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(2002, "BackupCompleted"),
                "اكتملت عملية النسخ الاحتياطي: {BackupType} في المسار: {FilePath}");

        private static readonly Action<ILogger, string, string, Exception?> LogBackupError =
            LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(2003, "BackupError"),
                "فشل في عملية النسخ الاحتياطي {BackupType}: {ErrorMessage}");

        public BackupService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<BackupService> logger,
            IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _auditService = auditService;
            _backupDirectory = configuration.GetValue<string>("BackupSettings:BackupDirectory") 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AccountingBackups");

            // إنشاء مجلد النسخ الاحتياطي إذا لم يكن موجوداً
            Directory.CreateDirectory(_backupDirectory);
        }

        /// <summary>
        /// إنشاء نسخة احتياطية شاملة
        /// </summary>
        public async Task<BackupResult> CreateBackupAsync(BackupOptions options)
        {
            var startTime = DateTime.Now;
            var backupType = options.CompressBackup ? "Compressed" : "Standard";
            
            LogBackupStarted(_logger, backupType, null);

            try
            {
                var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                var backupPath = Path.Combine(_backupDirectory, $"{fileName}.json");
                
                var backupData = new
                {
                    CreatedAt = DateTime.Now,
                    Description = options.Description,
                    Version = "1.0",
                    Data = await ExtractDataAsync(options)
                };

                // كتابة البيانات
                await File.WriteAllTextAsync(backupPath, JsonSerializer.Serialize(backupData, JsonOptions), Encoding.UTF8);

                var fileInfo = new FileInfo(backupPath);
                var finalPath = backupPath;

                // ضغط النسخة الاحتياطية إذا كان مطلوباً
                if (options.CompressBackup)
                {
                    finalPath = Path.Combine(_backupDirectory, $"{fileName}.zip");
                    using var zipFile = ZipFile.Open(finalPath, ZipArchiveMode.Create);
                    zipFile.CreateEntryFromFile(backupPath, Path.GetFileName(backupPath));
                    File.Delete(backupPath);
                    fileInfo = new FileInfo(finalPath);
                }

                var result = new BackupResult
                {
                    Success = true,
                    FilePath = finalPath,
                    FileSize = fileInfo.Length,
                    CreatedAt = startTime,
                    Duration = DateTime.Now - startTime,
                    TablesBackedUp = await GetTableCountAsync(),
                    RecordsBackedUp = await GetTotalRecordsAsync()
                };

                LogBackupCompleted(_logger, backupType, finalPath, null);

                // تسجيل العملية في سجل التدقيق
                await _auditService.LogAsync(
                    AuditOperations.BackupCreated,
                    "System",
                    null, // recordId يجب أن يكون int? ليس string
                    null,
                    result,
                    1, // TODO: استخدام معرف المستخدم الحقيقي
                    "System",
                    severity: AuditSeverity.Medium
                );

                return result;
            }
            catch (Exception ex)
            {
                LogBackupError(_logger, backupType, ex.Message, ex);
                
                return new BackupResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    CreatedAt = startTime,
                    Duration = DateTime.Now - startTime
                };
            }
        }

        /// <summary>
        /// استعادة النسخة الاحتياطية
        /// </summary>
        public async Task<RestoreResult> RestoreBackupAsync(string backupPath, RestoreOptions options)
        {
            var startTime = DateTime.Now;

            try
            {
                if (!File.Exists(backupPath))
                    throw new FileNotFoundException($"ملف النسخة الاحتياطية غير موجود: {backupPath}");

                // التحقق من صحة النسخة الاحتياطية
                if (!await ValidateBackupAsync(backupPath))
                    throw new InvalidDataException("ملف النسخة الاحتياطية تالف أو غير صالح");

                var backupContent = await ReadBackupFileAsync(backupPath);
                var tablesRestored = 0;
                var recordsRestored = 0;

                if (options.RestoreData)
                {
                    var restoreStats = await RestoreDataAsync(backupContent, options);
                    tablesRestored = restoreStats.tablesRestored;
                    recordsRestored = restoreStats.recordsRestored;
                }

                var result = new RestoreResult
                {
                    Success = true,
                    RestoredAt = startTime,
                    Duration = DateTime.Now - startTime,
                    TablesRestored = tablesRestored,
                    RecordsRestored = recordsRestored
                };

                // تسجيل العملية في سجل التدقيق
                await _auditService.LogAsync(
                    AuditOperations.BackupRestored,
                    "System",
                    null, // recordId يجب أن يكون int? ليس string
                    null,
                    result,
                    1, // TODO: استخدام معرف المستخدم الحقيقي
                    "System",
                    severity: AuditSeverity.High
                );

                return result;
            }
            catch (Exception ex)
            {
                return new RestoreResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    RestoredAt = startTime,
                    Duration = DateTime.Now - startTime
                };
            }
        }

        /// <summary>
        /// الحصول على قائمة النسخ الاحتياطية المتاحة
        /// </summary>
        public async Task<IEnumerable<BackupInfo>> GetAvailableBackupsAsync()
        {
            var backups = new List<BackupInfo>();

            if (!Directory.Exists(_backupDirectory))
                return backups;

            var files = Directory.GetFiles(_backupDirectory, "backup_*.*");
            
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var backupInfo = new BackupInfo
                {
                    FileName = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    CreatedDate = fileInfo.CreationTime,
                    FileSize = fileInfo.Length,
                    IsCompressed = Path.GetExtension(file).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                };

                // محاولة قراءة الوصف من الملف
                try
                {
                    backupInfo.Description = await GetBackupDescriptionAsync(file);
                }
                catch
                {
                    backupInfo.Description = "غير متاح";
                }

                backups.Add(backupInfo);
            }

            return backups.OrderByDescending(b => b.CreatedDate);
        }

        /// <summary>
        /// حذف نسخة احتياطية
        /// </summary>
        public async Task<bool> DeleteBackupAsync(string backupPath)
        {
            try
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                    
                    // تسجيل العملية
                    await _auditService.LogAsync(
                        AuditOperations.BackupDeleted,
                        "System",
                        null, // recordId يجب أن يكون int? ليس string
                        null,
                        null,
                        1, // TODO: استخدام معرف المستخدم الحقيقي
                        "System",
                        severity: AuditSeverity.Medium
                    );
                    
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل في حذف النسخة الاحتياطية: {BackupPath}", backupPath);
                return false;
            }
        }

        /// <summary>
        /// التحقق من صحة النسخة الاحتياطية
        /// </summary>
        public async Task<bool> ValidateBackupAsync(string backupPath)
        {
            try
            {
                var content = await ReadBackupFileAsync(backupPath);
                
                // التحقق من وجود البيانات الأساسية
                return content != null && 
                       content.RootElement.TryGetProperty("CreatedAt", out _) &&
                       content.RootElement.TryGetProperty("Version", out _) &&
                       content.RootElement.TryGetProperty("Data", out _);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// جدولة النسخ الاحتياطي التلقائي
        /// </summary>
        public async Task ScheduleAutomaticBackupAsync(BackupSchedule schedule)
        {
            // TODO: تنفيذ الجدولة التلقائية باستخدام Background Service
            await Task.CompletedTask;
            throw new NotImplementedException("الجدولة التلقائية قيد التطوير");
        }

        #region Helper Methods

        private async Task<JsonElement> ExtractDataAsync(BackupOptions options)
        {
            var data = new Dictionary<string, object>();

            // نسخ احتياطي للعملاء
            if (!options.TablesToExclude.Contains("Customers"))
            {
                var customers = await _unitOfWork.Customers.GetAllAsync();
                data["Customers"] = customers.ToList();
            }

            // نسخ احتياطي للموردين
            if (!options.TablesToExclude.Contains("Suppliers"))
            {
                var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
                data["Suppliers"] = suppliers.ToList();
            }

            // نسخ احتياطي للمنتجات
            if (!options.TablesToExclude.Contains("Products"))
            {
                var products = await _unitOfWork.Products.GetAllAsync();
                data["Products"] = products.ToList();
            }

            // نسخ احتياطي لفواتير المبيعات
            if (!options.TablesToExclude.Contains("SalesInvoices"))
            {
                var salesInvoices = await _unitOfWork.SalesInvoices.GetAllAsync();
                data["SalesInvoices"] = salesInvoices.ToList();
            }

            // نسخ احتياطي لفواتير المشتريات
            if (!options.TablesToExclude.Contains("PurchaseInvoices"))
            {
                var purchaseInvoices = await _unitOfWork.PurchaseInvoices.GetAllAsync();
                data["PurchaseInvoices"] = purchaseInvoices.ToList();
            }

            var jsonString = JsonSerializer.Serialize(data, JsonOptions);
            var document = JsonDocument.Parse(jsonString);
            return document.RootElement;
        }

        private async Task<JsonDocument?> ReadBackupFileAsync(string backupPath)
        {
            try
            {
                string content;

                if (Path.GetExtension(backupPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using var archive = ZipFile.OpenRead(backupPath);
                    var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                    if (entry == null)
                        throw new InvalidDataException("لا يحتوي ملف ZIP على بيانات JSON");

                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    content = await reader.ReadToEndAsync();
                }
                else
                {
                    content = await File.ReadAllTextAsync(backupPath, Encoding.UTF8);
                }

                return JsonDocument.Parse(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل في قراءة ملف النسخة الاحتياطية: {BackupPath}", backupPath);
                return null;
            }
        }

        private async Task<(int tablesRestored, int recordsRestored)> RestoreDataAsync(JsonDocument? backupContent, RestoreOptions options)
        {
            if (backupContent == null)
                return (0, 0);

            var tablesRestored = 0;
            var recordsRestored = 0;

            if (!backupContent.RootElement.TryGetProperty("Data", out var dataElement))
                return (0, 0);

            // استعادة العملاء
            if (dataElement.TryGetProperty("Customers", out var customersElement))
            {
                var customers = JsonSerializer.Deserialize<List<Customer>>(customersElement.GetRawText());
                if (customers != null)
                {
                    foreach (var customer in customers)
                    {
                        await _unitOfWork.Customers.AddAsync(customer);
                        recordsRestored++;
                    }
                    tablesRestored++;
                }
            }

            // استعادة الموردين
            if (dataElement.TryGetProperty("Suppliers", out var suppliersElement))
            {
                var suppliers = JsonSerializer.Deserialize<List<Supplier>>(suppliersElement.GetRawText());
                if (suppliers != null)
                {
                    foreach (var supplier in suppliers)
                    {
                        await _unitOfWork.Suppliers.AddAsync(supplier);
                        recordsRestored++;
                    }
                    tablesRestored++;
                }
            }

            // استعادة المنتجات
            if (dataElement.TryGetProperty("Products", out var productsElement))
            {
                var products = JsonSerializer.Deserialize<List<Product>>(productsElement.GetRawText());
                if (products != null)
                {
                    foreach (var product in products)
                    {
                        await _unitOfWork.Products.AddAsync(product);
                        recordsRestored++;
                    }
                    tablesRestored++;
                }
            }

            await _unitOfWork.SaveAsync();
            return (tablesRestored, recordsRestored);
        }

        private async Task<string> GetBackupDescriptionAsync(string backupPath)
        {
            var document = await ReadBackupFileAsync(backupPath);
            if (document?.RootElement.TryGetProperty("Description", out var descElement) == true)
            {
                return descElement.GetString() ?? "غير متاح";
            }
            return "غير متاح";
        }

        private Task<int> GetTableCountAsync()
        {
            // حساب عدد الجداول المدعومة
            return Task.FromResult(5); // Customers, Suppliers, Products, SalesInvoices, PurchaseInvoices
        }

        private async Task<int> GetTotalRecordsAsync()
        {
            var customers = await _unitOfWork.Customers.GetAllAsync();
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            var products = await _unitOfWork.Products.GetAllAsync();
            var salesInvoices = await _unitOfWork.SalesInvoices.GetAllAsync();
            var purchaseInvoices = await _unitOfWork.PurchaseInvoices.GetAllAsync();

            return customers.Count() + suppliers.Count() + products.Count() + 
                   salesInvoices.Count() + purchaseInvoices.Count();
        }

        #endregion
    }
}