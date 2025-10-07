using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AccountingSystem.Data;
using AccountingSystem.Business;
using AccountingSystem.WPF.Views;
using AccountingSystem.WPF.ViewModels;
using AccountingSystem.WPF.Configuration;
using AccountingSystem.WPF.Helpers;
// Diagnostics temporarily disabled
// using AccountingSystem.Diagnostics.Core;
using Serilog;

namespace AccountingSystem.WPF
{
    public partial class App : Application
    {
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // مسارات سجلات آمنة
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AccountingSystem");
        private static readonly string LogsDir = Path.Combine(AppDataDir, ConfigurationKeys.LogsDirectory);
        private static readonly string StartupLogPath = Path.Combine(LogsDir, ConfigurationKeys.StartupLogFile);
        private static readonly string StartupErrorLogPath = Path.Combine(LogsDir, ConfigurationKeys.StartupErrorLogFile);

        private static void SafeAppend(string path, string text)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception ex)
            {
                // تجاهل أخطاء الكتابة في الملفات لتجنب حلقات الأخطاء اللانهائية
                System.Diagnostics.Debug.WriteLine($"SafeAppend failed: {ex.Message}");
            }
        }

        private static string FlattenException(Exception ex)
        {
            if (ex is AggregateException ag)
                return string.Join(Environment.NewLine + "---" + Environment.NewLine,
                    ag.InnerExceptions.Select(FlattenException));
            return ex.ToString();
        }

        static App()
        {
            // فرض UTF-8 على مستوى التطبيق
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.InputEncoding = System.Text.Encoding.UTF8;
            }
            catch
            {
                // تجاهل أخطاء Console encoding في WPF apps
            }
            
            SetupEarlyErrorHandling();
        }

        private bool _mainWindowShown;
        private IServiceProvider? _serviceProvider;
        private IConfiguration? _configuration;
        private Mutex? _singleInstanceMutex;

        public static IServiceProvider ServiceProvider => ((App)Current)._serviceProvider!;

        private static void SetupEarlyErrorHandling()
        {
            try
            {
                Directory.CreateDirectory(LogsDir);

                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    var exception = e.ExceptionObject as Exception;
                    var message =
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] UNHANDLED (IsTerminating={e.IsTerminating}){Environment.NewLine}" +
                        $"{FlattenException(exception!)}{Environment.NewLine}================================{Environment.NewLine}{Environment.NewLine}";
                    SafeAppend(StartupErrorLogPath, message);
                    Console.WriteLine(message);
                };

                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    var message =
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] UNOBSERVED TASK EXCEPTION{Environment.NewLine}" +
                        $"{FlattenException(e.Exception)}{Environment.NewLine}================================{Environment.NewLine}{Environment.NewLine}";
                    SafeAppend(StartupErrorLogPath, message);
                    Console.WriteLine(message);
                    e.SetObserved();
                };

                SafeAppend(StartupErrorLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Early error handlers wired.{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Early error handling failed: {ex.Message}");
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // UI thread exceptions
            this.DispatcherUnhandledException += (sender, args) =>
            {
                var message =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] UI THREAD EXCEPTION{Environment.NewLine}" +
                    $"{FlattenException(args.Exception)}{Environment.NewLine}================================{Environment.NewLine}{Environment.NewLine}";
                SafeAppend(StartupErrorLogPath, message);
                Console.WriteLine(message);

                var fatal = args.Exception is OutOfMemoryException
                         || args.Exception is StackOverflowException
                         || args.Exception is TypeInitializationException;

                if (fatal)
                {
                    MessageBox.Show("حدث خطأ جسيم وسيتم إغلاق التطبيق.", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    args.Handled = false;
                }
                else
                {
                    ShowFriendlyError(args.Exception);
                    args.Handled = true;
                }
            };

            try
            {
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 🚀 Startup begin{Environment.NewLine}");

                // 1) Logging
                ConfigureComprehensiveLogging();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ✅ Logging configured{Environment.NewLine}");

                // 2) Single instance (أخرج بدري لو في نسخة شغالة)
                if (!EnsureSingleInstance())
                {
                    TryBringExistingInstanceToFront();
                    Shutdown();
                    return;
                }
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ✅ Single-instance OK{Environment.NewLine}");

                // 3) Culture + RTL
                ConfigureCulture();
                SetupRtlSupport();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ✅ Culture/RTL set{Environment.NewLine}");

                // 4) DI
                ConfigureDependencyInjection();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ✅ DI built{Environment.NewLine}");

                // 5) Health check
                PerformHealthCheck();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ✅ Health check passed{Environment.NewLine}");

                // 6) DB init (خليه في الخلفية لكن برسائل واضحة)
                _ = InitializeDatabase(); // fire-and-forget مع retry داخلي
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 🗄 DB init kicked off{Environment.NewLine}");

                // 7) Initialize Theme Manager
                InitializeThemeManager();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ✅ Theme Manager initialized{Environment.NewLine}");

                // 8) Show main window directly without diagnostic window
                ShowMainWindowDirectly();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ✅ MainWindow shown directly{Environment.NewLine}");

                base.OnStartup(e);
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ✅ Startup done{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                var message =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Startup failed{Environment.NewLine}{FlattenException(ex)}{Environment.NewLine}================================{Environment.NewLine}{Environment.NewLine}";
                SafeAppend(StartupErrorLogPath, message);
                ShowCriticalError("فشل في بدء تشغيل التطبيق", ex);
                Current.Shutdown(1);
            }
        }

        private static void SetupRtlSupport()
        {
            try
            {
                // فرض استخدام العربية في كل التطبيق
                var arabicLanguage = XmlLanguage.GetLanguage("ar-EG");
                
                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(arabicLanguage));

                FrameworkElement.FlowDirectionProperty.OverrideMetadata(
                    typeof(FrameworkElement),
                    new FrameworkPropertyMetadata(FlowDirection.RightToLeft));

                // فرض الخط العربي على كل النصوص
                TextElement.FontFamilyProperty.OverrideMetadata(
                    typeof(TextElement),
                    new FrameworkPropertyMetadata(
                        new FontFamily("Segoe UI, Tahoma, Traditional Arabic, Arial Unicode MS, Arial")));

                Control.FontFamilyProperty.OverrideMetadata(
                    typeof(Control),
                    new FrameworkPropertyMetadata(
                        new FontFamily("Segoe UI, Tahoma, Traditional Arabic, Arial Unicode MS, Arial")));

                SafeAppend(StartupLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] RTL ready with Arabic fonts{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                SafeAppend(StartupErrorLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] RTL setup failed: {ex.Message}{Environment.NewLine}");
            }
        }

        private static void ShowFriendlyError(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine("حدث خطأ غير متوقع:");
            sb.AppendLine(ex.Message);
            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine("الخطأ الداخلي:");
                sb.AppendLine(ex.InnerException.Message);
            }

            try 
            { 
                System.Windows.Clipboard.SetText(ex.ToString()); 
            } 
            catch (Exception clipEx) 
            {
                // تجاهل أخطاء نسخ النص للحافظة - غير ضروري لعمل التطبيق
                System.Diagnostics.Debug.WriteLine($"Clipboard failed: {clipEx.Message}");
            }

            MessageBox.Show(sb + "\n\n(تم نسخ التفاصيل للحافظة)", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static void ConfigureComprehensiveLogging()
        {
            try
            {
                Directory.CreateDirectory(LogsDir);

                Serilog.Debugging.SelfLog.Enable(msg =>
                {
                    SafeAppend(Path.Combine(LogsDir, "serilog-selflog.txt"), msg);
                });

                var cfg = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "AccountingSystem")
                    .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown")
                    .Enrich.WithProperty("MachineName", Environment.MachineName)
                    .Enrich.WithProperty("UserName", Environment.UserName)
                    .WriteTo.File(
                        path: Path.Combine(LogsDir, "application-.log"),
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 30,
                        fileSizeLimitBytes: 50 * 1024 * 1024,
                        encoding: Encoding.UTF8,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        path: Path.Combine(LogsDir, "errors-.log"),
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: 90,
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
                        fileSizeLimitBytes: 100 * 1024 * 1024,
                        encoding: Encoding.UTF8,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}");

                if (Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true)
                {
                    cfg = cfg.WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
                }

                Log.Logger = cfg.CreateLogger();
                Log.Information("Serilog configured.");
            }
            catch
            {
                // تأكد إن عندك Logger مهما حصل
                Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            }
        }

        private bool EnsureSingleInstance()
        {
            // جرّب Global أولًا؛ لو صلاحيات مش كافية استخدم Local
            bool createdNew;
            try
            {
                _singleInstanceMutex = new Mutex(true, $"Global\\{ConfigurationKeys.ApplicationGuid}", out createdNew);
                if (!createdNew)
                {
                    MessageBox.Show("التطبيق يعمل بالفعل.\nلا يمكن فتح أكثر من نسخة.", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                return true;
            }
            catch
            {
                _singleInstanceMutex = new Mutex(true, $"Local\\{ConfigurationKeys.ApplicationGuid}", out createdNew);
                if (!createdNew)
                {
                    MessageBox.Show("التطبيق يعمل بالفعل.\nلا يمكن فتح أكثر من نسخة.", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                return true;
            }
        }

        private static void TryBringExistingInstanceToFront()
        {
            try
            {
                var current = Process.GetCurrentProcess();
                foreach (var p in Process.GetProcessesByName(current.ProcessName))
                {
                    if (p.Id == current.Id) continue;
                    if (p.MainWindowHandle == IntPtr.Zero) continue;
                    ShowWindow(p.MainWindowHandle, 9 /*SW_RESTORE*/);
                    SetForegroundWindow(p.MainWindowHandle);
                    break;
                }
            }
            catch (Exception ex)
            {
                // تجاهل أخطاء العثور على النوافذ الأخرى - هذا متوقع في بعض الحالات
                System.Diagnostics.Debug.WriteLine($"TryBringExistingInstanceToFront failed: {ex.Message}");
            }
        }

        private static void ConfigureCulture()
        {
            try
            {
                var culture = new CultureInfo(ConfigurationKeys.CultureCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                SafeAppend(StartupLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Culture: {culture.Name}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                SafeAppend(StartupErrorLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Culture setup failed: {ex.Message}{Environment.NewLine}");
            }
        }

        private void ConfigureDependencyInjection()
        {
            var services = new ServiceCollection();

            // Configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();
            services.AddSingleton<IConfiguration>(_configuration);

            // Database
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ??
                "Server=(localdb)\\MSSQLLocalDB;Database=AccountingSystemDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

            services.AddDbContext<AccountingDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(60);
                    sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
                });

                var isDev = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true;
                if (isDev)
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            ConfigureServices(services);

            // Logging provider
            services.AddLogging(b =>
            {
                b.ClearProviders();
                b.AddSerilog();
            });

            _serviceProvider = services.BuildServiceProvider();
            SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DI container built.{Environment.NewLine}");
        }

        private void PerformHealthCheck()
        {
            try
            {
                using var scope = _serviceProvider!.CreateScope();
                scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
                scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Health check OK.{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                SafeAppend(StartupErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Health check failed: {ex.Message}{Environment.NewLine}");
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // DAL
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Business
            services.AddScoped<INumberSequenceService, NumberSequenceService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IEnhancedProductService, EnhancedProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IUnitService, UnitService>();
            services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
            services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<ReportService>(); // optional direct injection
            services.AddScoped<IReportsService, ReportsService>();
            services.AddScoped<ReportsService>(); // optional direct injection
            services.AddScoped<ISecurityService, SecurityService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPriceHistoryService, PriceHistoryService>();
            services.AddScoped<IErrorLoggingService, ErrorLoggingService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IBackupService, BackupService>();
            services.AddScoped<ISystemSettingsService, SystemSettingsService>();
            services.AddScoped<ICashierService, CashierService>();
            services.AddScoped<IPosService, PosService>();
            
            // Cash Drawer Service - استخدام التنفيذ المؤقت لمنع أخطاء الحقن
            services.AddScoped<ICashDrawerService, AccountingSystem.Business.Services.NullCashDrawerService>();

            // Enhanced
            services.AddScoped<IAdvancedAnalyticsService, AdvancedAnalyticsService>();
            services.AddScoped<IAdvancedReportsService, AdvancedReportsService>();
            services.AddScoped<IDiscountService, DiscountService>();

            // Diagnostics System (temporarily disabled)
            // services.AddScoped<HealthCheckRunner>();

            // Windows
            services.AddTransient<MainWindow>();
            services.AddTransient<DashboardWindow>();
            services.AddTransient<CustomersWindow>();
            services.AddTransient<SuppliersWindow>();
            services.AddTransient<ProductsWindow>();
            services.AddTransient<CategoriesWindow>();
            services.AddTransient<UnitsWindow>();
            services.AddTransient<SalesInvoiceWindow>();
            services.AddTransient<Views.SalesReturnWindow>();
            services.AddTransient<StockMovementsWindow>();
            services.AddTransient<SalesReportsWindow>();
            services.AddTransient<InventoryReportsWindow>();
            services.AddTransient<SystemSettingsWindow>();
            services.AddTransient<CashBoxWindow>();
            services.AddTransient<SalesInvoicesListWindow>();
            
            // Doctor System ViewModels
            services.AddTransient<SimpleDoctorViewModel>();
            services.AddTransient<Views.PurchaseInvoiceWindow>();
            services.AddTransient<Views.PurchaseInvoicesListWindow>();
            services.AddTransient<Views.PurchaseReturnWindow>();

            // VMs & Helpers
            services.AddScoped<DashboardViewModel>();
            services.AddScoped<WindowHelper>();
            
            // MVVM Services and ViewModels
            services.AddAccountingSystemServices();
            
            // Additional Windows called via GetRequiredService in MainWindow
            services.AddTransient<ChartsWindow>();
            services.AddTransient<Views.ReportsWindow>();
            services.AddTransient<DraftInvoicesWindow>();
            services.AddTransient<AdvancedReportsWindow>();
            services.AddTransient<AdvancedFinancialReportsWindow>();
            services.AddTransient<Views.AdvancedUserManagementWindow>();
            services.AddTransient<UsersPermissionsWindow>();
            services.AddTransient<DiscountManagementWindow>();
            services.AddTransient<LoyaltyProgramWindow>();
            services.AddTransient<BackupRestoreWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<SystemMonitoringWindow>();
        }

        private static void InitializeThemeManager()
        {
            try
            {
                // تهيئة مدير الثيمات وتحميل الثيم المحفوظ
                AccountingSystem.WPF.Services.ThemeManager.Instance.Initialize();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Theme Manager initialized with saved theme.{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                SafeAppend(StartupErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Theme Manager init failed: {ex.Message}{Environment.NewLine}");
                // تطبيق الثيم الافتراضي في حالة الفشل
                try
                {
                    AccountingSystem.WPF.Services.ThemeManager.Instance.SetTheme(AccountingSystem.WPF.Services.AppTheme.Light);
                }
                catch
                {
                    // تجاهل أخطاء الثيم الافتراضي - سيستخدم التطبيق الثيم المدمج
                }
            }
        }

        private async Task InitializeDatabase()
        {
            try
            {
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DB init started.{Environment.NewLine}");
                var retries = 3;
                var delay = TimeSpan.FromSeconds(3);

                for (var attempt = 1; attempt <= retries; attempt++)
                {
                    try
                    {
                        using var scope = _serviceProvider!.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
                        await db.Database.MigrateAsync();
                        SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DB migrate OK (attempt {attempt}).{Environment.NewLine}");
                        break;
                    }
                    catch (Exception ex) when (attempt < retries)
                    {
                        SafeAppend(StartupErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DB migrate failed (attempt {attempt}): {ex.Message}{Environment.NewLine}");
                        await Task.Delay(delay);
                    }
                }

                // إضافة البيانات الأولية يمكن تنفيذها هنا في المستقبل إذا لزم الأمر

                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DB init finished.{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                SafeAppend(StartupErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DB init fatal: {ex.Message}{Environment.NewLine}");
                // نسيب التطبيق يكمل—لو محتاج توقف، ارمِ الاستثناء هنا.
            }
        }

        private void ShowMainWindowDirectly()
        {
            if (_mainWindowShown) return;

            try
            {
                // محاولة إنشاء MainWindow الأصلية
                var main = _serviceProvider!.GetRequiredService<MainWindow>();
                MainWindow = main;
                
                // التأكد من أن النافذة تظهر بشكل صحيح
                main.Show();
                main.WindowState = WindowState.Maximized;
                main.Activate();
                main.Focus();
                
                _mainWindowShown = true;
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] MainWindow shown successfully.{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                // تشخيص مفصل للـ XAML Parse Exception
                string detailedError = ex.Message;
                if (ex is System.Windows.Markup.XamlParseException xamlEx)
                {
                    detailedError = $"XAML Parse Error at Line {xamlEx.LineNumber}, Position {xamlEx.LinePosition}:\n{xamlEx.Message}";
                    if (xamlEx.InnerException != null)
                    {
                        detailedError += $"\nInner Exception: {xamlEx.InnerException.Message}";
                    }
                }
                
                SafeAppend(StartupErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] MainWindow creation failed: {detailedError}{Environment.NewLine}");
                SafeAppend(StartupErrorLogPath, $"Stack trace: {ex.StackTrace}{Environment.NewLine}");
                
                // عرض رسالة خطأ مفيدة للمستخدم
                MessageBox.Show(
                    "حدث خطأ في تحميل النافذة الرئيسية:\n\n" + 
                    detailedError + "\n\n" +
                    "تم تحسين رسائل الخطأ للتشخيص الدقيق.",
                    "خطأ في النظام", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                
                // إغلاق التطبيق بدلاً من استخدام نافذة بديلة
                Current.Shutdown(1);
            }
        }

        private static void ShowCriticalError(string title, Exception ex)
        {
            var message = $"{title}\n\n{ex.Message}";
            if (ex.InnerException != null)
                message += $"\n\nالسبب: {ex.InnerException.Message}";
            MessageBox.Show(message, "خطأ حرج", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] App exiting.{Environment.NewLine}");
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
                Log.CloseAndFlush();
                SafeAppend(StartupLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] App exited cleanly.{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                SafeAppend(StartupErrorLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Exit error: {ex.Message}{Environment.NewLine}");
            }

            base.OnExit(e);
        }
    }
}
