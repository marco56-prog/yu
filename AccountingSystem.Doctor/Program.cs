using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AccountingSystem.Data;
using AccountingSystem.Business;
using AccountingSystem.Diagnostics;
using AccountingSystem.Diagnostics.HealthChecks;
using AccountingSystem.Diagnostics.Fixers;
using Newtonsoft.Json;
using Serilog;

namespace AccountingSystem.Doctor
{
    /// <summary>
    /// أداة تشخيص النظام المحاسبي - Console Application
    /// </summary>
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("🩺 طبيب النظام المحاسبي - AccountingSystem Doctor v1.0");
            Console.WriteLine("=" * 60);

            try
            {
                var rootCommand = CreateRootCommand();
                return await rootCommand.InvokeAsync(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطأ غير متوقع: {ex.Message}");
                Console.WriteLine($"التفاصيل: {ex}");
                return 1;
            }
        }

        private static RootCommand CreateRootCommand()
        {
            var rootCommand = new RootCommand("أداة تشخيص وإصلاح النظام المحاسبي الشامل");

            // الخيارات الأساسية
            var headlessOption = new Option<bool>("--headless", "تشغيل بدون واجهة تفاعلية");
            var allOption = new Option<bool>("--all", "تشغيل جميع الفحوصات");
            var categoriesOption = new Option<string[]>("--categories", "فئات محددة للفحص (db,ui,resources,performance)");
            var applyOption = new Option<bool>("--apply", "تطبيق الإصلاحات التلقائية");
            var policyOption = new Option<string>("--policy", "سياسة الإصلاح: safe, ask, none") { IsRequired = false };
            var exportOption = new Option<string>("--export", "مسار تصدير التقرير والملفات");
            var timeoutOption = new Option<int>("--timeout", () => 300, "مهلة زمنية بالثواني");
            var verboseOption = new Option<bool>("--verbose", "إخراج تفصيلي");

            rootCommand.AddOption(headlessOption);
            rootCommand.AddOption(allOption);
            rootCommand.AddOption(categoriesOption);
            rootCommand.AddOption(applyOption);
            rootCommand.AddOption(policyOption);
            rootCommand.AddOption(exportOption);
            rootCommand.AddOption(timeoutOption);
            rootCommand.AddOption(verboseOption);

            rootCommand.SetHandler(async (headless, all, categories, apply, policy, export, timeout, verbose) =>
            {
                var options = new DiagnosticsOptions
                {
                    Headless = headless,
                    IncludeAll = all || (categories?.Length == 0),
                    Categories = categories ?? Array.Empty<string>(),
                    FixPolicy = ParseFixPolicy(policy, apply),
                    Timeout = TimeSpan.FromSeconds(timeout),
                    ExportPath = export
                };

                await RunDiagnosticsAsync(options, verbose);
            }, headlessOption, allOption, categoriesOption, applyOption, policyOption, exportOption, timeoutOption, verboseOption);

            return rootCommand;
        }

        private static FixPolicy ParseFixPolicy(string? policy, bool apply)
        {
            if (!apply) return FixPolicy.None;

            return policy?.ToLower() switch
            {
                "safe" => FixPolicy.Safe,
                "ask" => FixPolicy.Ask,
                "none" => FixPolicy.None,
                _ => FixPolicy.Safe // افتراضي آمن
            };
        }

        private static async Task RunDiagnosticsAsync(DiagnosticsOptions options, bool verbose)
        {
            // إعداد التكوين والخدمات
            var host = CreateHost(verbose);
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var runner = host.Services.GetRequiredService<HealthCheckRunner>();

            logger.LogInformation("🚀 بدء التشخيص مع الخيارات: {@Options}", options);

            if (!options.Headless)
            {
                Console.WriteLine($"📋 فئات الفحص: {(options.IncludeAll ? "الكل" : string.Join(", ", options.Categories))}");
                Console.WriteLine($"🔧 سياسة الإصلاح: {options.FixPolicy}");
                Console.WriteLine($"⏱️ المهلة الزمنية: {options.Timeout.TotalSeconds} ثانية");
                Console.WriteLine();
            }

            try
            {
                // تشغيل التشخيص
                var report = await runner.RunAllChecksAsync(options, CancellationToken.None);

                // عرض النتائج
                if (!options.Headless)
                {
                    DisplayResults(report);
                }

                // تصدير التقارير والملفات
                if (!string.IsNullOrEmpty(options.ExportPath))
                {
                    await ExportDiagnosticsBundle(report, options.ExportPath, host.Services);
                }

                // إنهاء بكود الخروج المناسب
                Environment.Exit(report.IsHealthy ? 0 : 1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "فشل في تشغيل التشخيص");
                
                if (!options.Headless)
                {
                    Console.WriteLine($"❌ فشل في التشخيص: {ex.Message}");
                }
                
                Environment.Exit(2);
            }
        }

        private static IHost CreateHost(bool verbose)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // قاعدة البيانات
                    var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                        "Server=(localdb)\\MSSQLLocalDB;Database=AccountingSystemDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

                    services.AddDbContext<AccountingDbContext>(options =>
                        options.UseSqlServer(connectionString));

                    // UnitOfWork
                    services.AddScoped<IUnitOfWork, UnitOfWork>();

                    // خدمات الأعمال الأساسية
                    services.AddScoped<INumberSequenceService, NumberSequenceService>();

                    // التشخيص
                    services.AddSingleton<HealthCheckRunner>();
                    
                    // فحوصات
                    services.AddTransient<IHealthCheck, DatabaseConnectionCheck>();
                    services.AddTransient<IHealthCheck, PendingMigrationsCheck>();
                    services.AddTransient<IHealthCheck, ReferentialIntegrityCheck>();
                    services.AddTransient<IHealthCheck, DatabasePerformanceCheck>();
                    services.AddTransient<IHealthCheck, ResourcesAndThemesCheck>();
                    services.AddTransient<IHealthCheck, DataBindingCheck>();

                    // مصلحات
                    services.AddTransient<IFixAction, PendingMigrationsFixer>();
                    services.AddTransient<IFixAction, MissingDirectoriesFixer>();
                    services.AddTransient<IFixAction, MissingConfigurationFixer>();
                    services.AddTransient<IFixAction, TempFileCleanupFixer>();
                })
                .UseSerilog((context, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .MinimumLevel.Is(verbose ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
                        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
                });

            var host = hostBuilder.Build();

            // تسجيل الفحوصات والمصلحات
            RegisterHealthChecksAndFixers(host.Services);

            return host;
        }

        private static void RegisterHealthChecksAndFixers(IServiceProvider services)
        {
            var runner = services.GetRequiredService<HealthCheckRunner>();

            // تسجيل الفحوصات
            var healthChecks = services.GetServices<IHealthCheck>();
            foreach (var check in healthChecks)
            {
                runner.RegisterCheck(check);
            }

            // تسجيل المصلحات
            var fixers = services.GetServices<IFixAction>();
            foreach (var fixer in fixers)
            {
                runner.RegisterFixer(fixer);
            }
        }

        private static void DisplayResults(DiagnosticsReport report)
        {
            Console.WriteLine("📊 نتائج التشخيص:");
            Console.WriteLine("=" * 40);

            // ملخص عام
            var okCount = report.Results.Count(r => r.Status == HealthStatus.Ok);
            var warningCount = report.Results.Count(r => r.Status == HealthStatus.Warning);
            var failedCount = report.Results.Count(r => r.Status == HealthStatus.Failed);

            Console.WriteLine($"✅ سليم: {okCount}");
            Console.WriteLine($"⚠️ تحذيرات: {warningCount}");
            Console.WriteLine($"❌ أخطاء: {failedCount}");
            Console.WriteLine($"⏱️ إجمالي الوقت: {report.TotalDuration.TotalSeconds:F1} ثانية");
            Console.WriteLine();

            // النتائج بالتفصيل
            foreach (var categoryGroup in report.ResultsByCategory)
            {
                Console.WriteLine($"📂 {categoryGroup.Key.ToUpper()}:");
                
                foreach (var result in categoryGroup.Value.OrderBy(r => r.Status))
                {
                    var icon = result.Status switch
                    {
                        HealthStatus.Ok => "✅",
                        HealthStatus.Warning => "⚠️",
                        HealthStatus.Failed => "❌",
                        _ => "❓"
                    };

                    Console.WriteLine($"  {icon} {result.CheckName}: {result.Message}");
                    
                    if (!string.IsNullOrEmpty(result.Details))
                    {
                        Console.WriteLine($"     التفاصيل: {result.Details}");
                    }
                    
                    if (!string.IsNullOrEmpty(result.RecommendedAction))
                    {
                        Console.WriteLine($"     الإجراء المقترح: {result.RecommendedAction}");
                    }
                }
                
                Console.WriteLine();
            }

            // نتائج الإصلاحات
            if (report.FixResults?.Any() == true)
            {
                Console.WriteLine("🔧 نتائج الإصلاحات:");
                Console.WriteLine("-" * 20);

                foreach (var fixResult in report.FixResults)
                {
                    var icon = fixResult.Success ? "✅" : "❌";
                    Console.WriteLine($"  {icon} {fixResult.Message}");
                }
                
                Console.WriteLine();
            }

            // الحالة النهائية
            if (report.IsHealthy)
            {
                Console.WriteLine("🎉 النظام سليم وجاهز للعمل!");
            }
            else
            {
                Console.WriteLine($"⚠️ النظام يحتاج انتباه - يوجد {failedCount} مشكلة حرجة");
            }
        }

        private static async Task ExportDiagnosticsBundle(DiagnosticsReport report, string exportPath, IServiceProvider services)
        {
            try
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("📦 إنشاء حزمة التشخيص في: {ExportPath}", exportPath);

                // إنشاء مجلد مؤقت
                var tempDir = Path.Combine(Path.GetTempPath(), $"accounting-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // تصدير التقرير JSON
                    var jsonReport = JsonConvert.SerializeObject(report, Formatting.Indented);
                    await File.WriteAllTextAsync(Path.Combine(tempDir, "diagnostics-report.json"), jsonReport);

                    // تصدير التقرير HTML
                    var htmlReport = GenerateHtmlReport(report);
                    await File.WriteAllTextAsync(Path.Combine(tempDir, "diagnostics-report.html"), htmlReport);

                    // نسخ ملفات اللوق
                    await CopyLogFiles(tempDir, logger);

                    // نسخ ملفات التكوين (مع إخفاء الأسرار)
                    await CopyConfigurationFiles(tempDir, logger);

                    // إنشاء معلومات النظام
                    await CreateSystemInfo(tempDir);

                    // ضغط كل شيء
                    if (File.Exists(exportPath))
                        File.Delete(exportPath);

                    ZipFile.CreateFromDirectory(tempDir, exportPath);

                    Console.WriteLine($"📦 تم إنشاء حزمة التشخيص: {exportPath}");
                    logger.LogInformation("تم إنشاء حزمة التشخيص بنجاح");
                }
                finally
                {
                    // تنظيف المجلد المؤقت
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ فشل في إنشاء حزمة التشخيص: {ex.Message}");
            }
        }

        private static string GenerateHtmlReport(DiagnosticsReport report)
        {
            var html = $@"<!DOCTYPE html>
<html dir=""rtl"" lang=""ar"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>تقرير تشخيص النظام المحاسبي</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Arial, sans-serif; margin: 20px; direction: rtl; }}
        .header {{ background: #1565C0; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; }}
        .summary {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin-bottom: 30px; }}
        .summary-card {{ background: #f8f9fa; border: 1px solid #e9ecef; border-radius: 8px; padding: 15px; text-align: center; }}
        .ok {{ border-left: 4px solid #28a745; }}
        .warning {{ border-left: 4px solid #ffc107; }}
        .failed {{ border-left: 4px solid #dc3545; }}
        .results {{ margin-bottom: 30px; }}
        .category {{ background: #f8f9fa; border-radius: 8px; padding: 15px; margin-bottom: 15px; }}
        .check-item {{ margin: 10px 0; padding: 10px; border-radius: 4px; }}
        .check-ok {{ background: #d4edda; }}
        .check-warning {{ background: #fff3cd; }}
        .check-failed {{ background: #f8d7da; }}
        .timestamp {{ color: #6c757d; font-size: 0.9em; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🩺 تقرير تشخيص النظام المحاسبي</h1>
        <p>تم الإنشاء في: {report.StartTime:yyyy-MM-dd HH:mm:ss}</p>
        <p>المدة الإجمالية: {report.TotalDuration.TotalSeconds:F1} ثانية</p>
    </div>

    <div class=""summary"">
        <div class=""summary-card ok"">
            <h3>✅ سليم</h3>
            <h2>{report.Results.Count(r => r.Status == HealthStatus.Ok)}</h2>
        </div>
        <div class=""summary-card warning"">
            <h3>⚠️ تحذيرات</h3>
            <h2>{report.Results.Count(r => r.Status == HealthStatus.Warning)}</h2>
        </div>
        <div class=""summary-card failed"">
            <h3>❌ أخطاء</h3>
            <h2>{report.Results.Count(r => r.Status == HealthStatus.Failed)}</h2>
        </div>
    </div>

    <div class=""results"">";

            foreach (var categoryGroup in report.ResultsByCategory)
            {
                html += $@"
        <div class=""category"">
            <h3>📂 {categoryGroup.Key}</h3>";

                foreach (var result in categoryGroup.Value.OrderBy(r => r.Status))
                {
                    var cssClass = result.Status.ToString().ToLower();
                    var icon = result.Status switch
                    {
                        HealthStatus.Ok => "✅",
                        HealthStatus.Warning => "⚠️",
                        HealthStatus.Failed => "❌",
                        _ => "❓"
                    };

                    html += $@"
            <div class=""check-item check-{cssClass}"">
                <h4>{icon} {result.CheckName}</h4>
                <p><strong>الرسالة:</strong> {result.Message}</p>
                {(!string.IsNullOrEmpty(result.Details) ? $"<p><strong>التفاصيل:</strong> {result.Details}</p>" : "")}
                {(!string.IsNullOrEmpty(result.RecommendedAction) ? $"<p><strong>الإجراء المقترح:</strong> {result.RecommendedAction}</p>" : "")}
                <p class=""timestamp"">المدة: {result.Duration.TotalMilliseconds:F0}ms | الوقت: {result.Timestamp:HH:mm:ss}</p>
            </div>";
                }

                html += "</div>";
            }

            html += @"
    </div>
</body>
</html>";

            return html;
        }

        private static async Task CopyLogFiles(string tempDir, ILogger logger)
        {
            try
            {
                var logsDir = Path.Combine(tempDir, "logs");
                Directory.CreateDirectory(logsDir);

                var sourceLogsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                
                if (Directory.Exists(sourceLogsDir))
                {
                    var logFiles = Directory.GetFiles(sourceLogsDir, "*.log");
                    
                    foreach (var logFile in logFiles.Take(5)) // أحدث 5 ملفات لوق
                    {
                        var destFile = Path.Combine(logsDir, Path.GetFileName(logFile));
                        File.Copy(logFile, destFile, true);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "فشل في نسخ ملفات اللوق");
            }
        }

        private static async Task CopyConfigurationFiles(string tempDir, ILogger logger)
        {
            try
            {
                var configDir = Path.Combine(tempDir, "config");
                Directory.CreateDirectory(configDir);

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var configFiles = new[] { "appsettings.json", "serilog.json" };

                foreach (var configFile in configFiles)
                {
                    var sourcePath = Path.Combine(baseDir, configFile);
                    if (File.Exists(sourcePath))
                    {
                        var content = await File.ReadAllTextAsync(sourcePath);
                        
                        // إخفاء معلومات حساسة
                        content = content.Replace(
                            "Server=(localdb)\\MSSQLLocalDB;Database=AccountingSystemDB;Trusted_Connection=true",
                            "Server=***MASKED***;Database=***MASKED***;Trusted_Connection=true"
                        );

                        await File.WriteAllTextAsync(Path.Combine(configDir, configFile), content);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "فشل في نسخ ملفات التكوين");
            }
        }

        private static async Task CreateSystemInfo(string tempDir)
        {
            var systemInfo = $@"نظام التشغيل: {Environment.OSVersion}
إصدار .NET: {Environment.Version}
معرف الجهاز: {Environment.MachineName}
المستخدم الحالي: {Environment.UserName}
مجلد العمل: {Environment.CurrentDirectory}
الذاكرة المستخدمة: {GC.GetTotalMemory(false) / (1024 * 1024):F1} MB
عدد المعالجات: {Environment.ProcessorCount}
وقت التشغيل: {Environment.TickCount / 1000 / 60:F1} دقيقة
المنطقة الزمنية: {TimeZoneInfo.Local.DisplayName}
التاريخ والوقت: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

إصدار التطبيق: 1.0.0
مسار التطبيق: {AppDomain.CurrentDomain.BaseDirectory}
";

            await File.WriteAllTextAsync(Path.Combine(tempDir, "system-info.txt"), systemInfo);
        }
    }
}