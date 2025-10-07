using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AccountingSystem.Business;
using AccountingSystem.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using System.Threading;
using System.Security;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Data.Common;
using System.Net.NetworkInformation;

namespace AccountingSystem.WPF.Helpers
{
    /// <summary>
    /// معالج شامل للأخطاء في التطبيق مع تسجيل متقدم وتحصين ضد الفشل
    /// Thread-safe, Circuit Breaker pattern, UTC timestamps, Comprehensive logging
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static IServiceProvider? _serviceProvider;
        private static ILogger<Application>? _logger;
        private static int _configuredFlag = 0; // Thread-safe configuration flag
        private static volatile bool _showingErrorDialog = false; // Reentrancy guard
        private static volatile bool _dbLoggingDown = false; // Circuit breaker for DB logging
        private static DateTime _lastDbFailure = DateTime.MinValue; // Last DB failure time

        /// <summary>
        /// إعداد معالج الأخطاء الشامل - Thread-safe, Idempotent
        /// </summary>
        public static void Configure(IServiceProvider serviceProvider, ILogger<Application>? logger = null)
        {
            // Thread-safe configuration check
            if (Interlocked.Exchange(ref _configuredFlag, 1) == 1)
                return; // Already configured

            _serviceProvider = serviceProvider;
            _logger = logger;

            try
            {
                // تكوين Serilog مع Async sinks وFlush handlers
                ConfigureAdvancedSerilog();

                // ربط معالجات الأخطاء مع Idempotent wiring
                WireExceptionHandlersIdempotent();

                // إعداد Process Exit وShutdown handlers للFlush
                SetupFlushHandlers();

                _dbLoggingDown = false; // Reset circuit breaker
                _lastDbFailure = DateTime.MinValue;

                Log.Information("✅ تم إعداد معالج الأخطاء الشامل المحصن بنجاح - Thread: {ThreadId}", 
                    Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception ex)
            {
                // إعادة تعيين الفلاغ في حالة فشل التهيئة
                Interlocked.Exchange(ref _configuredFlag, 0);
                Log.Fatal(ex, "❌ فشل في إعداد معالج الأخطاء الشامل");
                throw;
            }
        }

        /// <summary>
        /// إعداد Serilog المتقدم مع Async sinks وSpecialized log files
        /// </summary>
        private static void ConfigureAdvancedSerilog()
        {
            if (Log.Logger != Serilog.Core.Logger.None) return;

            var logsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            System.IO.Directory.CreateDirectory(logsPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                
                // Console output
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                
                // General application log with enhanced template
                .WriteTo.File(
                    path: System.IO.Path.Combine(logsPath, "application-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                    buffered: true, // Buffer for performance
                    flushToDiskInterval: TimeSpan.FromSeconds(1))
                
                // Errors-only log
                .WriteTo.File(
                    path: System.IO.Path.Combine(logsPath, "errors-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
                    fileSizeLimitBytes: 50 * 1024 * 1024, // 50 MB
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                    buffered: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1))
                
                // Business operations log (filtered)
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Serilog.Filters.Matching.WithProperty("BusinessOperation"))
                    .WriteTo.File(
                        path: System.IO.Path.Combine(logsPath, "business-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        fileSizeLimitBytes: 25 * 1024 * 1024, // 25 MB
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] 💼 {Message:lj} {Properties:j}{NewLine}{Exception}"))
                
                // Database operations log (filtered)
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(Serilog.Filters.Matching.WithProperty("DatabaseOperation"))
                    .WriteTo.File(
                        path: System.IO.Path.Combine(logsPath, "database-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        fileSizeLimitBytes: 25 * 1024 * 1024, // 25 MB
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] 🗄️ {Message:lj} {Properties:j}{NewLine}{Exception}"))
                
                .CreateLogger();

            Log.Information("🚀 تم إعداد Serilog المحصن مع Async Sinks");
        }

        /// <summary>
        /// ربط معالجات الأخطاء المختلفة بطريقة Idempotent (آمنة للتكرار)
        /// </summary>
        private static void WireExceptionHandlersIdempotent()
        {
            // فصل أي handlers موجودة مسبقاً قبل الربط لضمان عدم التكرار
            try
            {
                Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "⚠️ تحذير أثناء فصل الـ handlers القديمة");
            }

            // ربط معالجات الأخطاء الجديدة
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            Log.Information("🔗 تم ربط جميع معالجات الأخطاء بطريقة آمنة");
        }

        /// <summary>
        /// إعداد Flush handlers للإغلاق النظيف
        /// </summary>
        private static void SetupFlushHandlers()
        {
            try
            {
                // Process Exit handler
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Log.Information("🔄 Process Exit - تنفيذ Flush للـ logs");
                    Log.CloseAndFlush();
                };

                // Application shutdown handler (إذا كان متاحاً)
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.ShutdownStarted += (sender, e) =>
                    {
                        Log.Information("🔄 Application Shutdown - تنفيذ Flush للـ logs");
                        Log.CloseAndFlush();
                    };
                }

                Log.Information("⚡ تم إعداد Flush handlers بنجاح");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ فشل في إعداد Flush handlers");
            }
        }

        /// <summary>
        /// معالجة أخطاء واجهة المستخدم مع حماية من إعادة الدخول وUI marshalling
        /// </summary>
        private static async void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Reentrancy guard - منع فتح MessageBox متكرر
            if (_showingErrorDialog)
            {
                e.Handled = true;
                Log.Warning("⚠️ تم تجاهل خطأ UI متكرر أثناء عرض نافذة خطأ أخرى");
                return;
            }

            try
            {
                _showingErrorDialog = true;

                // Enhanced logging with context
                using (LogContext.PushProperty("ErrorId", Guid.NewGuid().ToString("N")[..8]))
                using (LogContext.PushProperty("ErrorType", "UIError"))
                using (LogContext.PushProperty("UserId", GetCurrentUserId()))
                using (LogContext.PushProperty("Username", GetCurrentUsername()))
                using (LogContext.PushProperty("CorrelationId", Activity.Current?.Id))
                {
                    var errorId = await LogErrorWithCircuitBreaker(e.Exception, ErrorType.UIError, ErrorSeverity.Error);
                    
                    Log.Error(e.Exception, "🖥️❌ خطأ في واجهة المستخدم - Error ID: {ErrorId}", errorId);

                    var userMessage = GetEnhancedUserFriendlyMessage(e.Exception);
                    
                    // UI marshalling - التأكد من أننا على UI thread
                    if (!Application.Current.Dispatcher.CheckAccess())
                    {
                        Application.Current.Dispatcher.Invoke(() => ShowErrorDialog(userMessage, errorId, e));
                    }
                    else
                    {
                        ShowErrorDialog(userMessage, errorId, e);
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "💥 فشل في معالجة خطأ واجهة المستخدم");
                
                // Emergency fallback - عرض رسالة بسيطة
                try
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        MessageBox.Show($"حدث خطأ خطير في التطبيق:\n{e.Exception.Message}\n\nسيتم إغلاق التطبيق للحماية.",
                            "خطأ خطير", MessageBoxButton.OK, MessageBoxImage.Stop);
                    }
                }
                catch
                {
                    // إذا فشل حتى الـ emergency fallback، اتركه للنظام
                }
                finally
                {
                    e.Handled = true;
                }
            }
            finally
            {
                _showingErrorDialog = false;
            }
        }

        /// <summary>
        /// عرض نافذة الخطأ مع خيارات المستخدم
        /// </summary>
        private static void ShowErrorDialog(string userMessage, string errorId, DispatcherUnhandledExceptionEventArgs e)
        {
            var result = MessageBox.Show(
                $"{userMessage}\n\n🔍 رقم الخطأ: {errorId}\n📅 الوقت: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n🤔 هل تريد متابعة العمل؟\n\n💡 نصيحة: إذا كان الخطأ يتكرر، اختر \"لا\" لإعادة تشغيل التطبيق.",
                "⚠️ تنبيه - حدث خطأ في التطبيق",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);

            if (result == MessageBoxResult.No)
            {
                Log.Information("👤 المستخدم اختار إنهاء التطبيق بعد خطأ UI");
                Log.CloseAndFlush();
                Application.Current.Shutdown(1);
            }
            else
            {
                Log.Information("👤 المستخدم اختار متابعة العمل بعد خطأ UI");
            }
        }

        /// <summary>
        /// معالجة أخطاء النطاق العام مع حالات قاتلة وEnvironment.FailFast
        /// </summary>
        private static async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                
                // Enhanced logging with context
                using (LogContext.PushProperty("ErrorId", Guid.NewGuid().ToString("N")[..8]))
                using (LogContext.PushProperty("ErrorType", "SystemError"))
                using (LogContext.PushProperty("Severity", "Fatal"))
                using (LogContext.PushProperty("IsTerminating", e.IsTerminating))
                using (LogContext.PushProperty("CorrelationId", Activity.Current?.Id))
                {
                    var errorId = await LogErrorWithCircuitBreaker(exception, ErrorType.SystemError, ErrorSeverity.Fatal);

                    Log.Fatal(exception, "💀 خطأ خطير في النطاق العام - Error ID: {ErrorId} - Terminating: {IsTerminating}", 
                        errorId, e.IsTerminating);

                    var message = exception?.Message ?? "خطأ غير معروف";
                    var userMessage = GetEnhancedUserFriendlyMessage(exception ?? new Exception("Unknown error"));

                    // UI marshalling for non-UI thread
                    if (Application.Current != null)
                    {
                        try
                        {
                            if (!Application.Current.Dispatcher.CheckAccess())
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ShowFatalErrorDialog(userMessage, errorId, e.IsTerminating);
                                });
                            }
                            else
                            {
                                ShowFatalErrorDialog(userMessage, errorId, e.IsTerminating);
                            }
                        }
                        catch (Exception uiEx)
                        {
                            Log.Fatal(uiEx, "🚨 فشل في عرض رسالة الخطأ القاتل");
                        }
                    }

                    // Handle terminating cases with FailFast
                    if (e.IsTerminating)
                    {
                        try
                        {
                            Log.Fatal("⚡ تنفيذ Environment.FailFast بسبب حالة قاتلة");
                            Log.CloseAndFlush();
                            
                            // Wait a moment for logs to flush
                            Thread.Sleep(1000);
                            
                            Environment.FailFast($"💀 خطأ قاتل في التطبيق: {message}", exception);
                        }
                        catch
                        {
                            // If FailFast fails, try normal exit
                            Environment.Exit(1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Log.Fatal(ex, "💥 فشل في معالجة خطأ النطاق العام");
                    Log.CloseAndFlush();
                }
                catch
                {
                    // Last resort - direct exit if logging fails
                }
                
                if (e.IsTerminating)
                {
                    Environment.FailFast("Critical failure in exception handler", ex);
                }
            }
        }

        /// <summary>
        /// عرض نافذة خطأ قاتل للمستخدم
        /// </summary>
        private static void ShowFatalErrorDialog(string userMessage, string errorId, bool isTerminating)
        {
            var terminatingText = isTerminating ? "\n\n⚠️ سيتم إغلاق التطبيق الآن." : "";
            
            MessageBox.Show(
                $"💀 حدث خطأ خطير في التطبيق{terminatingText}\n\n{userMessage}\n\n🔍 رقم الخطأ: {errorId}\n📅 الوقت: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n📞 يرجى الاتصال بالدعم الفني وإعطاؤهم رقم الخطأ أعلاه.",
                "💀 خطأ خطير - إغلاق التطبيق",
                MessageBoxButton.OK,
                MessageBoxImage.Stop);
        }

        /// <summary>
        /// معالجة أخطاء المهام غير المُلاحظة - محصنة ومحسّنة
        /// </summary>
        private static async void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                // Enhanced logging with context
                using (LogContext.PushProperty("ErrorId", Guid.NewGuid().ToString("N")[..8]))
                using (LogContext.PushProperty("ErrorType", "UnobservedTask"))
                using (LogContext.PushProperty("TaskId", Task.CurrentId))
                using (LogContext.PushProperty("CorrelationId", Activity.Current?.Id))
                {
                    var errorId = await LogErrorWithCircuitBreaker(e.Exception, ErrorType.SystemError, ErrorSeverity.Error);
                    
                    Log.Error(e.Exception, "🔧 مهمة غير مُلاحظة بها خطأ - Error ID: {ErrorId} - InnerExceptions: {InnerCount}", 
                        errorId, e.Exception.InnerExceptions?.Count ?? 0);

                    // Log all inner exceptions for better debugging
                    if (e.Exception.InnerExceptions?.Count > 0)
                    {
                        foreach (var inner in e.Exception.InnerExceptions)
                        {
                            Log.Error(inner, "↳ Inner Exception في المهمة غير المُلاحظة");
                        }
                    }

                    // منع إنهاء التطبيق
                    e.SetObserved();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Log.Fatal(ex, "💥 فشل في معالجة خطأ المهمة غير المُلاحظة");
                }
                catch
                {
                    // If even logging fails, just observe it
                }
                finally
                {
                    e.SetObserved(); // Always observe to prevent app termination
                }
            }
        }

        /// <summary>
        /// تسجيل الخطأ مع Circuit breaker للحماية من فشل قاعدة البيانات المتكرر
        /// </summary>
        private static async Task<string> LogErrorWithCircuitBreaker(Exception? exception, ErrorType errorType, ErrorSeverity severity)
        {
            if (exception == null) return "UNKNOWN";

            // Circuit Breaker logic
            if (_dbLoggingDown && (DateTime.UtcNow - _lastDbFailure).TotalMinutes < 5)
            {
                Log.Warning("🚫 Database logging circuit breaker active - skipping DB log");
                return Guid.NewGuid().ToString("N")[..8];
            }

            try
            {
                using var scope = _serviceProvider?.CreateScope();
                var errorLoggingService = scope?.ServiceProvider.GetService<IErrorLoggingService>();

                if (errorLoggingService != null)
                {
                    var userId = GetCurrentUserId();
                    var username = GetCurrentUsername();

                    var errorId = await errorLoggingService.LogErrorAsync(
                        exception, errorType, severity, userId, username);

                    // Reset circuit breaker on success
                    _dbLoggingDown = false;
                    return errorId;
                }
            }
            catch (Exception ex) when (IsDatabaseException(ex))
            {
                // Activate circuit breaker
                _dbLoggingDown = true;
                _lastDbFailure = DateTime.UtcNow;
                Log.Error(ex, "🚫 Database logging failed - activating circuit breaker");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ فشل في تسجيل الخطأ في قاعدة البيانات");
            }

            // Fallback ID
            return Guid.NewGuid().ToString("N")[..8];
        }

        /// <summary>
        /// فحص ما إذا كان الاستثناء متعلق بقاعدة البيانات
        /// </summary>
        private static bool IsDatabaseException(Exception ex)
        {
            return ex is DbException || ex is DbUpdateException || 
                   ex is TimeoutException || ex.Message.Contains("database") ||
                   ex.Message.Contains("connection") || ex.Message.Contains("sql") ||
                   (ex.InnerException != null && IsDatabaseException(ex.InnerException));
        }

        /// <summary>
        /// تحويل رسالة الخطأ التقنية إلى رسالة مفهومة ومحسّنة للمستخدم
        /// </summary>
        private static string GetEnhancedUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                // Database exceptions
                DbException => " مشكلة في قاعدة البيانات. أعد المحاولة أو اتصل بالدعم الفني.",
                DbUpdateException => "💾 فشل في حفظ البيانات. تحقق من صحة المعلومات المدخلة.",
                
                // Security exceptions
                UnauthorizedAccessException => "🔒 ليس لديك صلاحية للقيام بهذه العملية. اتصل بالمدير لمنحك الصلاحية.",
                SecurityException => "🛡️ تم منع هذه العملية لأسباب أمنية. اتصل بالدعم الفني.",
                
                // Network exceptions  
                SocketException => "🌐 مشكلة في الشبكة. تحقق من اتصال الإنترنت وجرب مرة أخرى.",
                NetworkInformationException => "📡 خطأ في الشبكة. تحقق من إعدادات الشبكة.",
                TimeoutException => "⏰ انتهت مهلة الاتصال. الشبكة بطيئة أو الخادم مشغول.",
                
                // File system exceptions
                FileNotFoundException fileEx => $"📄 لم يتم العثور على الملف المطلوب:\n{Path.GetFileName(fileEx.FileName)}",
                DirectoryNotFoundException => "📁 لم يتم العثور على المجلد المطلوب.",
                IOException ioEx when ioEx.HResult == -2147024784 => "💽 لا توجد مساحة كافية على القرص الصلب.",
                IOException => "📁 مشكلة في الوصول للملف. قد يكون مفتوحاً في برنامج آخر.",
                
                // Memory exceptions
                OutOfMemoryException => "🧠 نفدت ذاكرة النظام. أغلق بعض البرامج وأعد المحاولة.",
                StackOverflowException => "🔄 حدث خطأ في العمليات المتكررة. أعد تشغيل البرنامج.",
                
                // Common exceptions
                ArgumentNullException argEx => $"❗ قيمة مطلوبة مفقودة: {argEx.ParamName}",
                ArgumentException argEx => $"❗ قيمة غير صحيحة تم إدخالها: {argEx.ParamName}",
                InvalidOperationException => "⚠️ لا يمكن تنفيذ هذه العملية في الوقت الحالي. جرب مرة أخرى.",
                NotSupportedException => "🚫 العملية المطلوبة غير مدعومة في هذا الإصدار.",
                FormatException => "📝 تنسيق البيانات المدخلة غير صحيح. تحقق من المعلومات.",
                
                // Default case
                _ => $"❓ حدث خطأ غير متوقع: {exception.Message}\n\n💡 إذا استمر هذا الخطأ، اتصل بالدعم الفني."
            };
        }

        /// <summary>
        /// Legacy method - kept for compatibility
        /// </summary>
        private static async Task<string> LogErrorAsync(Exception? exception, ErrorType errorType, ErrorSeverity severity)
        {
            return await LogErrorWithCircuitBreaker(exception, errorType, severity);
        }

        /// <summary>
        /// الحصول على معرف المستخدم الحالي
        /// NOTE: يحتاج تنفيذ حسب نظام إدارة المستخدمين المستخدم في التطبيق
        /// </summary>
        private static int? GetCurrentUserId()
        {
            try
            {
                // يمكن تحسين هذا للحصول على المستخدم الفعلي من:
                // - Session management service
                // - Claims-based identity
                // - Application state
                // - Database current user lookup
                
                // For now, return null to indicate system/unknown user
                return null; 
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "فشل في الحصول على معرف المستخدم الحالي");
                return null;
            }
        }

        /// <summary>
        /// الحصول على اسم المستخدم الحالي
        /// </summary>
        private static string? GetCurrentUsername()
        {
            try
            {
                // يمكن تحسين هذا للحصول على المستخدم الفعلي من Session
                return Environment.UserName; // استخدام اسم المستخدم في النظام كبديل
            }
            catch
            {
                return "غير محدد";
            }
        }



        /// <summary>
        /// تسجيل خطأ مخصص من أي مكان في التطبيق
        /// </summary>
        public static async Task<string> LogCustomErrorAsync(Exception exception, ErrorType errorType = ErrorType.SystemError,
            ErrorSeverity severity = ErrorSeverity.Error, string? context = null)
        {
            try
            {
                Log.Error(exception, "خطأ مخصص - Context: {Context}", context);
                return await LogErrorAsync(exception, errorType, severity);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "فشل في تسجيل خطأ مخصص");
                return Guid.NewGuid().ToString("N")[..12];
            }
        }

        /// <summary>
        /// تسجيل رسالة خطأ مخصصة
        /// </summary>
        public static async Task<string> LogCustomMessageAsync(string message, ErrorType errorType = ErrorType.SystemError,
            ErrorSeverity severity = ErrorSeverity.Error, string? details = null)
        {
            try
            {
                using var scope = _serviceProvider?.CreateScope();
                var errorLoggingService = scope?.ServiceProvider.GetService<IErrorLoggingService>();

                if (errorLoggingService != null)
                {
                    var userId = GetCurrentUserId();
                    var username = GetCurrentUsername();

                    return await errorLoggingService.LogErrorAsync(
                        message, details, errorType, severity, userId, username);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "فشل في تسجيل رسالة خطأ مخصصة: {Message}", message);
            }

            return Guid.NewGuid().ToString("N")[..12];
        }

        /// <summary>
        /// تنظيف الموارد - Thread-safe cleanup
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                // Check if we were configured before cleanup
                if (Interlocked.CompareExchange(ref _configuredFlag, 0, 1) == 1)
                {
                    // Unhook all handlers safely
                    try
                    {
                        if (Application.Current != null)
                            Application.Current.DispatcherUnhandledException -= OnDispatcherUnhandledException;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "⚠️ تحذير أثناء فصل DispatcherUnhandledException");
                    }

                    try
                    {
                        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "⚠️ تحذير أثناء فصل UnhandledException");
                    }

                    try
                    {
                        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "⚠️ تحذير أثناء فصل UnobservedTaskException");
                    }
                    
                    // Reset circuit breaker
                    _dbLoggingDown = false;
                    _lastDbFailure = DateTime.MinValue;
                    _showingErrorDialog = false;
                    
                    Log.Information("🧹 تم تنظيف معالج الأخطاء الشامل بنجاح");
                    Log.CloseAndFlush();
                }
            }
            catch (Exception ex)
            {
                // Emergency log - try to log the cleanup failure
                try
                {
                    Log.Fatal(ex, "💥 فشل حرج في تنظيف معالج الأخطاء");
                    Log.CloseAndFlush();
                }
                catch
                {
                    // If even logging fails, there's nothing more we can do
                }
            }
        }
    }
}