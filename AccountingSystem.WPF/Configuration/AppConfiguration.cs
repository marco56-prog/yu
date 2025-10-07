namespace AccountingSystem.WPF.Configuration
{
    /// <summary>
    /// فئة تكوين مخصصة لتمثيل إعدادات التطبيق من appsettings.json
    /// </summary>
    public class AppConfiguration
    {
        public AppSettings App { get; set; } = new();
        public DatabaseSettings Database { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class AppSettings
    {
        public bool SkipLogin { get; set; } = false;
        public string Environment { get; set; } = "Production";
        public string Version { get; set; } = "2.0.0";
        public int DatabaseRetryAttempts { get; set; } = 3;
        public int DatabaseRetryDelayMs { get; set; } = 1500;
    }

    public class DatabaseSettings
    {
        public string DefaultConnection { get; set; } = string.Empty;
        public int CommandTimeout { get; set; } = 60;
        public bool EnableSensitiveDataLogging { get; set; } = false;
        public bool EnableDetailedErrors { get; set; } = false;
    }

    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information";
        public string LogPath { get; set; } = "logs";
        public int RetainedFileCountLimit { get; set; } = 7;
    }
}