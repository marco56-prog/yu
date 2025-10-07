using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.Diagnostics.Models;
using AccountingSystem.Diagnostics.Core;

namespace AccountingSystem.Diagnostics.HealthChecks
{
    /// <summary>
    /// فحص الاتصال بقاعدة البيانات
    /// </summary>
    public class DatabaseConnectionCheck : IHealthCheck
    {
        private readonly AccountingDbContext _context;
        private readonly ILogger<DatabaseConnectionCheck> _logger;

        public string Name => "فحص الاتصال بقاعدة البيانات";
        public string Category => "database";
        public string Description => "التأكد من صحة الاتصال بقاعدة البيانات والقدرة على تنفيذ الاستعلامات";
        public int Priority => 1; // أولوية عالية
        public bool IsEnabled => true;

        public DatabaseConnectionCheck(AccountingDbContext context, ILogger<DatabaseConnectionCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // اختبار الاتصال الأساسي
                await _context.Database.OpenConnectionAsync(cancellationToken);
                await _context.Database.CloseConnectionAsync();

                // اختبار استعلام بسيط
                var count = await _context.Users.CountAsync(cancellationToken);
                
                stopwatch.Stop();
                
                return HealthCheckResult.Ok(Name, 
                    $"الاتصال سليم - يحتوي على {count} مستخدم", 
                    stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "فشل في الاتصال بقاعدة البيانات");
                
                return HealthCheckResult.Failed(Name, 
                    "فشل في الاتصال بقاعدة البيانات", 
                    stopwatch.Elapsed, 
                    ex, 
                    "تحقق من سلسلة الاتصال وتأكد من تشغيل خدمة SQL Server");
            }
        }
    }

    /// <summary>
    /// فحص الترحيلات المعلقة
    /// </summary>
    public class PendingMigrationsCheck : IHealthCheck
    {
        private readonly AccountingDbContext _context;
        private readonly ILogger<PendingMigrationsCheck> _logger;

        public string Name => "فحص الترحيلات المعلقة";
        public string Category => "database";
        public string Description => "التأكد من عدم وجود ترحيلات معلقة في قاعدة البيانات";
        public int Priority => 2;
        public bool IsEnabled => true;

        public PendingMigrationsCheck(AccountingDbContext context, ILogger<PendingMigrationsCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
                var pendingList = pendingMigrations.ToList();
                
                stopwatch.Stop();

                if (!pendingList.Any())
                {
                    return HealthCheckResult.Ok(Name, 
                        "جميع الترحيلات مطبقة بنجاح", 
                        stopwatch.Elapsed);
                }

                var result = HealthCheckResult.Failed(Name, 
                    $"يوجد {pendingList.Count} ترحيل معلق: {string.Join(", ", pendingList)}", 
                    stopwatch.Elapsed, 
                    null, 
                    "قم بتشغيل Update-Database أو dotnet ef database update");
                result.CanAutoFix = true;
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "فشل في فحص الترحيلات");
                
                return HealthCheckResult.Failed(Name, 
                    "فشل في فحص الترحيلات", 
                    stopwatch.Elapsed, 
                    ex);
            }
        }
    }

    /// <summary>
    /// فحص القيود المرجعية
    /// </summary>
    public class ReferentialIntegrityCheck : IHealthCheck
    {
        private readonly AccountingDbContext _context;
        private readonly ILogger<ReferentialIntegrityCheck> _logger;

        public string Name => "فحص القيود المرجعية";
        public string Category => "database";
        public string Description => "التأكد من سلامة القيود المرجعية والعلاقات بين الجداول";
        public int Priority => 3;
        public bool IsEnabled => true;

        public ReferentialIntegrityCheck(AccountingDbContext context, ILogger<ReferentialIntegrityCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var issues = new List<string>();

            try
            {
                // فحص فواتير البيع بدون عملاء
                var salesInvoicesWithoutCustomers = await _context.SalesInvoices
                    .Where(si => !_context.Customers.Any(c => c.CustomerId == si.CustomerId))
                    .CountAsync(cancellationToken);

                if (salesInvoicesWithoutCustomers > 0)
                    issues.Add($"{salesInvoicesWithoutCustomers} فاتورة بيع بدون عميل صحيح");

                // فحص فواتير الشراء بدون موردين
                var purchaseInvoicesWithoutSuppliers = await _context.PurchaseInvoices
                    .Where(pi => !_context.Suppliers.Any(s => s.SupplierId == pi.SupplierId))
                    .CountAsync(cancellationToken);

                if (purchaseInvoicesWithoutSuppliers > 0)
                    issues.Add($"{purchaseInvoicesWithoutSuppliers} فاتورة شراء بدون مورد صحيح");

                // فحص بنود الفواتير بدون منتجات
                var itemsWithoutProducts = await _context.SalesInvoiceItems
                    .Where(item => !_context.Products.Any(p => p.ProductId == item.ProductId))
                    .CountAsync(cancellationToken);

                if (itemsWithoutProducts > 0)
                    issues.Add($"{itemsWithoutProducts} بند فاتورة بدون منتج صحيح");

                stopwatch.Stop();

                if (!issues.Any())
                {
                    return HealthCheckResult.Ok(Name, 
                        "جميع القيود المرجعية سليمة", 
                        stopwatch.Elapsed);
                }

                var result = HealthCheckResult.Failed(Name, 
                    $"تم اكتشاف {issues.Count} مشكلة في القيود المرجعية", 
                    stopwatch.Elapsed, 
                    null, 
                    "قم بمراجعة وإصلاح البيانات المفقودة أو الحذف المناسب");
                result.Details = string.Join("\n", issues);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "فشل في فحص القيود المرجعية");
                
                return HealthCheckResult.Failed(Name, 
                    "فشل في فحص القيود المرجعية", 
                    stopwatch.Elapsed, 
                    ex);
            }
        }
    }

    /// <summary>
    /// فحص أداء الاستعلامات الأساسية
    /// </summary>
    public class DatabasePerformanceCheck : IHealthCheck
    {
        private readonly AccountingDbContext _context;
        private readonly ILogger<DatabasePerformanceCheck> _logger;

        public string Name => "فحص أداء قاعدة البيانات";
        public string Category => "database";
        public string Description => "قياس أداء الاستعلامات الأساسية والتأكد من وجود الفهارس المطلوبة";
        public int Priority => 4;
        public bool IsEnabled => true;

        public DatabasePerformanceCheck(AccountingDbContext context, ILogger<DatabasePerformanceCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var performanceIssues = new List<string>();

            try
            {
                // اختبار سرعة استعلام العملاء
                var customerQueryTime = await MeasureQueryTime(async () => 
                    await _context.Customers.Take(100).ToListAsync(cancellationToken));

                if (customerQueryTime.TotalMilliseconds > 1000)
                    performanceIssues.Add($"استعلام العملاء بطيء: {customerQueryTime.TotalMilliseconds:F0}ms");

                // اختبار سرعة استعلام الفواتير
                var invoiceQueryTime = await MeasureQueryTime(async () => 
                    await _context.SalesInvoices.Include(si => si.Customer).Take(50).ToListAsync(cancellationToken));

                if (invoiceQueryTime.TotalMilliseconds > 2000)
                    performanceIssues.Add($"استعلام الفواتير بطيء: {invoiceQueryTime.TotalMilliseconds:F0}ms");

                // اختبار سرعة استعلام المنتجات
                var productQueryTime = await MeasureQueryTime(async () => 
                    await _context.Products.Include(p => p.Category).Take(100).ToListAsync(cancellationToken));

                if (productQueryTime.TotalMilliseconds > 1500)
                    performanceIssues.Add($"استعلام المنتجات بطيء: {productQueryTime.TotalMilliseconds:F0}ms");

                stopwatch.Stop();

                if (!performanceIssues.Any())
                {
                    return HealthCheckResult.Ok(Name, 
                        "أداء قاعدة البيانات ممتاز", 
                        stopwatch.Elapsed);
                }

                var result = HealthCheckResult.Warning(Name, 
                    $"تم اكتشاف {performanceIssues.Count} مشكلة أداء", 
                    stopwatch.Elapsed, 
                    "فكر في إضافة فهارس أو تحسين الاستعلامات");
                result.Details = string.Join("\n", performanceIssues);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "فشل في فحص أداء قاعدة البيانات");
                
                return HealthCheckResult.Failed(Name, 
                    "فشل في فحص أداء قاعدة البيانات", 
                    stopwatch.Elapsed, 
                    ex);
            }
        }

        private async Task<TimeSpan> MeasureQueryTime(Func<Task> query)
        {
            var sw = Stopwatch.StartNew();
            await query();
            sw.Stop();
            return sw.Elapsed;
        }
    }
}