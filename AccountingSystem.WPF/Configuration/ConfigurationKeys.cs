using System;

namespace AccountingSystem.WPF.Configuration
{
    /// <summary>
    /// مفاتيح التكوين والثوابت المستخدمة في التطبيق
    /// </summary>
    public static class ConfigurationKeys
    {
        // إعدادات التطبيق الأساسية
        public const string ApplicationGuid = "AccountingSystem-C84D8F2A-9E5F-4A3B-8C7D-1E4F9A5B2C8E";

        // إعدادات الثقافة واللغة
        public const string CultureCode = "ar-EG";

        // مسارات الملفات والمجلدات
        public const string LogsDirectory = "logs";
        public const string StartupLogFile = "startup.log";
        public const string StartupErrorLogFile = "startup_error.log";
        public const string ApplicationLogFile = "application.log";
        public const string ErrorLogFile = "errors.log";

        // إعدادات قاعدة البيانات
        public const string DefaultConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=AccountingSystemDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

        // إعدادات التسجيل
        public const int LogRetentionDays = 30;
        public const int ErrorLogRetentionDays = 90;
        public const long MaxLogFileSizeMB = 50;
        public const long MaxErrorLogFileSizeMB = 100;

        // إعدادات النوافذ
        public const int DefaultWindowWidth = 1400;
        public const int DefaultWindowHeight = 800;

        // إعدادات الأمان
        public const int MaxLoginAttempts = 5;
        public const int SessionTimeoutMinutes = 30;

        // إعدادات النسخ الاحتياطي
        public const string BackupDirectory = "backups";
        public const int BackupRetentionDays = 7;

        // إعدادات التقارير
        public const string ReportsDirectory = "reports";
        public const string TempDirectory = "temp";

        // إعدادات النظام
        public const string SystemName = "النظام المحاسبي الشامل";
        public const string SystemVersion = "1.0.0";

        // إعدادات التنسيق
        public const string DateFormat = "dd/MM/yyyy";
        public const string TimeFormat = "HH:mm:ss";
        public const string DateTimeFormat = "dd/MM/yyyy HH:mm:ss";
        public const string CurrencyFormat = "C";

        // إعدادات الأداء
        public const int DatabaseCommandTimeoutSeconds = 60;
        public const int DatabaseRetryAttempts = 5;
        public const int DatabaseRetryDelaySeconds = 5;

        // إعدادات إضافية مطلوبة في StartupHelper
        public const string UnifiedStylesPath = "Resources/MasterSafeResources.xaml";
        public const string EnvironmentKey = "Environment";
        public const string ProductionEnvironment = "Production";
        public const string DevelopmentEnvironment = "Development";
        public const string SkipLoginKey = "App:SkipLogin";
        public const string DefaultConnectionKey = "ConnectionStrings:DefaultConnection";
    }
}