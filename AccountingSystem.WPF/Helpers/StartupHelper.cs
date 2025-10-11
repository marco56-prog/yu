using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using AccountingSystem.WPF.Constants;
using AccountingSystem.WPF.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.WPF.Helpers
{
    /// <summary>
    /// مساعد لعمليات بدء تشغيل التطبيق
    /// </summary>
    public class StartupHelper
    {
        private readonly ILogger<StartupHelper>? _logger;
        private readonly AppConfiguration _appConfig;

        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string LogsDir = Path.Combine(BaseDir, ConfigurationKeys.LogsDirectory);
        private static readonly string StartupLogPath = Path.Combine(LogsDir, ConfigurationKeys.StartupLogFile);
        private static readonly string StartupErrorLogPath = Path.Combine(LogsDir, ConfigurationKeys.StartupErrorLogFile);

        public StartupHelper(ILogger<StartupHelper>? logger = null, AppConfiguration? appConfig = null)
        {
            _logger = logger;
            _appConfig = appConfig ?? new AppConfiguration();
        }

        /// <summary>
        /// تهيئة مجلدات التطبيق
        /// </summary>
        public void InitializeDirectories()
        {
            try
            {
                Directory.CreateDirectory(LogsDir);
                SafeAppend(StartupLogPath, $"[{GetCurrentTime()}] تم إنشاء مجلدات التطبيق بنجاح\n");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "فشل في إنشاء مجلدات التطبيق");
                throw;
            }
        }

        /// <summary>
        /// تطبيق الثقافة العربية
        /// </summary>
        public void ApplyArabicCulture()
        {
            try
            {
                var culture = new CultureInfo(ConfigurationKeys.CultureCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                SafeAppend(StartupLogPath, $"[{GetCurrentTime()}] تم تطبيق الثقافة العربية بنجاح\n");
                _logger?.LogInformation("تم تطبيق الثقافة العربية: {Culture}", ConfigurationKeys.CultureCode);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "فشل في تطبيق الثقافة العربية");
                throw;
            }
        }

        /// <summary>
        /// التحقق من وجود نسخة واحدة فقط من التطبيق
        /// </summary>
        /// <param name="mutex">المؤشر للتحكم في النسخة الواحدة</param>
        /// <returns>true إذا كان هذا التطبيق هو النسخة الوحيدة</returns>
        public bool EnsureSingleInstance(out Mutex mutex)
        {
            var createdNew = false;
            mutex = new Mutex(true, $"Global\\{ConfigurationKeys.ApplicationGuid}", out createdNew);

            if (!createdNew)
            {
                SafeAppend(StartupLogPath, $"[{GetCurrentTime()}] اكتشاف نسخة أخرى تعمل بالفعل\n");
                _logger?.LogWarning("محاولة تشغيل نسخة ثانية من التطبيق");
                return false;
            }

            SafeAppend(StartupLogPath, $"[{GetCurrentTime()}] تم التأكد من عدم وجود نسخ أخرى\n");
            return true;
        }

        /// <summary>
        /// تحميل الأنماط الموحدة
        /// </summary>
        public void LoadUnifiedStyles()
        {
            try
            {
                // التحقق من عدم تحميل المورد مسبقاً
                if (Application.Current.Resources.MergedDictionaries.Any(d =>
                    string.Equals(d.Source?.OriginalString, ConfigurationKeys.UnifiedStylesPath, StringComparison.OrdinalIgnoreCase)))
                {
                    SafeAppend(StartupLogPath, $"[{GetCurrentTime()}] UnifiedStyles سبق تحميله - تخطي التحميل المكرر\n");
                    return;
                }

                // تحميل قاموس الموارد
                var dict = new ResourceDictionary();
                dict.Source = new Uri(ConfigurationKeys.UnifiedStylesPath, UriKind.Relative);

                // فحص المفاتيح المتضاربة
                var conflictingKeys = dict.Keys.Cast<object>()
                    .Where(key => Application.Current.Resources.Contains(key) ||
                                 Application.Current.Resources.MergedDictionaries.Any(md => md.Contains(key)))
                    .ToList();

                if (conflictingKeys.Count > 0)
                {
                    SafeAppend(StartupLogPath,
                        $"[{GetCurrentTime()}] تم العثور على {conflictingKeys.Count} مفاتيح متضاربة: {string.Join(", ", conflictingKeys)}\n");

                    // إزالة المفاتيح المتضاربة
                    foreach (var key in conflictingKeys)
                    {
                        try { dict.Remove(key); } catch { /* تجاهل أخطاء الإزالة الفردية */ }
                    }
                }

                // إضافة القاموس إذا كان يحتوي على موارد
                if (dict.Keys.Count > 0)
                {
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                    SafeAppend(StartupLogPath, $"[{GetCurrentTime()}] تم تحميل UnifiedStyles بنجاح\n");
                }
                else
                {
                    SafeAppend(StartupLogPath, $"[{GetCurrentTime()}] لا توجد موارد جديدة لتحميلها من UnifiedStyles\n");
                }
            }
            catch (Exception ex)
            {
                SafeWrite(StartupErrorLogPath, $"[{GetCurrentTime()}] فشل تحميل UnifiedStyles: {ex}\n\n");
                _logger?.LogError(ex, "فشل في تحميل الأنماط الموحدة");
                throw;
            }
        }

        /// <summary>
        /// التحقق من بيئة التطوير
        /// </summary>
        /// <param name="configuration">التكوين</param>
        /// <returns>true إذا كانت بيئة التطوير</returns>
        public static bool IsDevelopmentEnvironment(IConfiguration configuration)
        {
            var env = configuration?[ConfigurationKeys.EnvironmentKey] ??
                      Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                      ConfigurationKeys.ProductionEnvironment;

            return env.Equals(ConfigurationKeys.DevelopmentEnvironment, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// التحقق من إعداد تجاوز تسجيل الدخول
        /// </summary>
        /// <param name="configuration">التكوين</param>
        /// <returns>true إذا كان تجاوز تسجيل الدخول مفعلاً</returns>
        public static bool ShouldSkipLogin(IConfiguration configuration)
        {
            return configuration?.GetValue<bool>(ConfigurationKeys.SkipLoginKey) ?? false;
        }

        /// <summary>
        /// الحصول على الوقت الحالي بتنسيق منسق
        /// </summary>
        /// <returns>الوقت الحالي</returns>
        private static string GetCurrentTime() =>
            DateTime.Now.ToString(ConfigurationKeys.DateTimeFormat, CultureInfo.InvariantCulture);

        /// <summary>
        /// كتابة آمنة للملف (إضافة)
        /// </summary>
        /// <param name="path">مسار الملف</param>
        /// <param name="text">النص المراد إضافته</param>
        private static void SafeAppend(string path, string text)
        {
            try { File.AppendAllText(path, text, Encoding.UTF8); }
            catch { /* تجاهل آمن */ }
        }

        /// <summary>
        /// كتابة آمنة للملف (استبدال)
        /// </summary>
        /// <param name="path">مسار الملف</param>
        /// <param name="text">النص المراد كتابته</param>
        private static void SafeWrite(string path, string text)
        {
            try { File.WriteAllText(path, text, Encoding.UTF8); }
            catch { /* تجاهل آمن */ }
        }
    }
}