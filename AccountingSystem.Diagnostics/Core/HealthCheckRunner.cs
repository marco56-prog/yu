using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AccountingSystem.Diagnostics.Models;
using AccountingSystem.Diagnostics.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.Diagnostics.Core
{
    /// <summary>
    /// منسق تشغيل فحوصات التشخيص
    /// </summary>
    public class HealthCheckRunner
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthCheckRunner> _logger;
        private readonly List<IHealthCheck> _healthChecks;
        private readonly List<IFixAction> _fixActions;

        public HealthCheckRunner(IServiceProvider serviceProvider, ILogger<HealthCheckRunner> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _healthChecks = new List<IHealthCheck>();
            _fixActions = new List<IFixAction>();
        }

        /// <summary>
        /// تسجيل فحص جديد
        /// </summary>
        public void RegisterCheck(IHealthCheck healthCheck)
        {
            _healthChecks.Add(healthCheck);
            _logger.LogDebug("تم تسجيل فحص: {CheckName} في فئة {Category}", healthCheck.Name, healthCheck.Category);
        }

        /// <summary>
        /// تسجيل إجراء إصلاح
        /// </summary>
        public void RegisterFixer(IFixAction fixAction)
        {
            _fixActions.Add(fixAction);
            _logger.LogDebug("تم تسجيل مصلح: {FixerName}", fixAction.Name);
        }

        /// <summary>
        /// تشغيل جميع الفحوصات
        /// </summary>
        public async Task<DiagnosticsReport> RunAllChecksAsync(DiagnosticsOptions options, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var report = new DiagnosticsReport
            {
                StartTime = DateTime.Now,
                Options = options
            };

            _logger.LogInformation("🩺 بدء التشخيص الشامل للنظام المحاسبي...");

            try
            {
                // فلترة الفحوصات حسب الفئات المطلوبة
                var checksToRun = GetChecksToRun(options);
                
                _logger.LogInformation("سيتم تشغيل {Count} فحص", checksToRun.Count());

                var results = new List<HealthCheckResult>();

                // تشغيل الفحوصات بالتسلسل (يمكن تحويلها للتوازي لاحقاً)
                foreach (var check in checksToRun.OrderBy(c => c.Priority))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        _logger.LogDebug("تشغيل فحص: {CheckName}", check.Name);
                        
                        using var timeout = new CancellationTokenSource(options.Timeout);
                        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
                        
                        var result = await check.CheckAsync(combined.Token);
                        result.Category = check.Category;
                        results.Add(result);

                        var statusEmoji = result.Status switch
                        {
                            HealthStatus.Ok => "✅",
                            HealthStatus.Warning => "⚠️",
                            HealthStatus.Failed => "❌",
                            _ => "❓"
                        };

                        _logger.LogInformation("{Emoji} {CheckName}: {Message} ({Duration}ms)", 
                            statusEmoji, check.Name, result.Message, (int)result.Duration.TotalMilliseconds);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("⏱️ انتهت مهلة فحص: {CheckName}", check.Name);
                        results.Add(HealthCheckResult.Failed(check.Name, "انتهت المهلة المحددة", TimeSpan.Zero));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ خطأ في فحص: {CheckName}", check.Name);
                        results.Add(HealthCheckResult.Failed(check.Name, $"خطأ غير متوقع: {ex.Message}", TimeSpan.Zero, ex));
                    }
                }

                report.Results = results;

                // تشغيل الإصلاحات التلقائية إذا طُلب ذلك
                if (options.FixPolicy != FixPolicy.None)
                {
                    await RunFixActionsAsync(report, options, cancellationToken);
                }

                stopwatch.Stop();
                report.EndTime = DateTime.Now;
                report.TotalDuration = stopwatch.Elapsed;

                LogSummary(report);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ فشل في تشغيل التشخيص");
                stopwatch.Stop();
                
                report.EndTime = DateTime.Now;
                report.TotalDuration = stopwatch.Elapsed;
                report.GlobalError = ex;
                
                return report;
            }
        }

        /// <summary>
        /// تشغيل إجراءات الإصلاح
        /// </summary>
        private async Task RunFixActionsAsync(DiagnosticsReport report, DiagnosticsOptions options, CancellationToken cancellationToken)
        {
            var failedResults = report.Results.Where(r => r.Status == HealthStatus.Failed && r.CanAutoFix).ToList();
            
            if (!failedResults.Any())
            {
                _logger.LogInformation("لا توجد مشاكل قابلة للإصلاح التلقائي");
                return;
            }

            _logger.LogInformation("🔧 بدء إجراءات الإصلاح التلقائي لـ {Count} مشكلة", failedResults.Count);

            var fixResults = new List<FixResult>();

            foreach (var failedResult in failedResults)
            {
                var fixer = _fixActions.FirstOrDefault(f => f.Name.Contains(failedResult.CheckName) || 
                                                          failedResult.CheckName.Contains(f.Name));
                
                if (fixer == null)
                    continue;

                // تحقق من السياسة
                if (options.FixPolicy == FixPolicy.Safe && !fixer.IsSafeForAutoFix)
                {
                    _logger.LogInformation("⏭️ تخطي إصلاح غير آمن: {FixerName}", fixer.Name);
                    continue;
                }

                try
                {
                    _logger.LogInformation("🔧 تشغيل مصلح: {FixerName}", fixer.Name);
                    var fixResult = await fixer.ExecuteAsync(cancellationToken);
                    fixResults.Add(fixResult);

                    var emoji = fixResult.Success ? "✅" : "❌";
                    _logger.LogInformation("{Emoji} {FixerName}: {Message}", emoji, fixer.Name, fixResult.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ فشل في تشغيل مصلح: {FixerName}", fixer.Name);
                    fixResults.Add(FixResult.Failure($"خطأ في التشغيل: {ex.Message}", TimeSpan.Zero, ex));
                }
            }

            report.FixResults = fixResults;
        }

        /// <summary>
        /// فلترة الفحوصات حسب الخيارات
        /// </summary>
        private IEnumerable<IHealthCheck> GetChecksToRun(DiagnosticsOptions options)
        {
            if (options.IncludeAll)
                return _healthChecks;

            if (options.Categories?.Any() == true)
            {
                return _healthChecks.Where(c => options.Categories.Contains(c.Category, StringComparer.OrdinalIgnoreCase));
            }

            return _healthChecks;
        }

        /// <summary>
        /// طباعة ملخص النتائج
        /// </summary>
        private void LogSummary(DiagnosticsReport report)
        {
            var okCount = report.Results.Count(r => r.Status == HealthStatus.Ok);
            var warningCount = report.Results.Count(r => r.Status == HealthStatus.Warning);
            var failedCount = report.Results.Count(r => r.Status == HealthStatus.Failed);

            _logger.LogInformation("📊 ملخص التشخيص:");
            _logger.LogInformation("   ✅ سليم: {OkCount}", okCount);
            _logger.LogInformation("   ⚠️ تحذيرات: {WarningCount}", warningCount);
            _logger.LogInformation("   ❌ أخطاء: {FailedCount}", failedCount);
            _logger.LogInformation("   ⏱️ المدة الإجمالية: {Duration:F2} ثانية", report.TotalDuration.TotalSeconds);

            if (report.FixResults?.Any() == true)
            {
                var fixedCount = report.FixResults.Count(f => f.Success);
                _logger.LogInformation("   🔧 تم إصلاح: {FixedCount} من أصل {TotalFixes}", fixedCount, report.FixResults.Count);
            }
        }

        /// <summary>
        /// الحصول على جميع الفحوصات المسجلة
        /// </summary>
        public IReadOnlyList<IHealthCheck> GetRegisteredChecks() => _healthChecks.AsReadOnly();

        /// <summary>
        /// الحصول على جميع المصلحات المسجلة
        /// </summary>
        public IReadOnlyList<IFixAction> GetRegisteredFixers() => _fixActions.AsReadOnly();
    }

    /// <summary>
    /// تقرير نتائج التشخيص
    /// </summary>
    public class DiagnosticsReport
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public DiagnosticsOptions Options { get; set; } = new();
        public List<HealthCheckResult> Results { get; set; } = new();
        public List<FixResult>? FixResults { get; set; }
        public Exception? GlobalError { get; set; }

        /// <summary>
        /// هل النظام سليم بشكل عام؟
        /// </summary>
        public bool IsHealthy => !Results.Any(r => r.Status == HealthStatus.Failed) && GlobalError == null;

        /// <summary>
        /// عدد المشاكل الحرجة
        /// </summary>
        public int CriticalIssuesCount => Results.Count(r => r.Status == HealthStatus.Failed);

        /// <summary>
        /// عدد التحذيرات
        /// </summary>
        public int WarningsCount => Results.Count(r => r.Status == HealthStatus.Warning);

        /// <summary>
        /// النتائج مجمعة حسب الفئة
        /// </summary>
        public Dictionary<string, List<HealthCheckResult>> ResultsByCategory =>
            Results.GroupBy(r => r.Category).ToDictionary(g => g.Key, g => g.ToList());
    }
}