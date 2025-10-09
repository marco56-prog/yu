using System;

namespace AccountingSystem.Diagnostics.Models
{
    /// <summary>
    /// نتيجة فحص صحة النظام
    /// </summary>
    public class HealthCheckResult
    {
        public string CheckName { get; set; } = "";
        public string Category { get; set; } = "";
        public HealthStatus Status { get; set; }
        public string Message { get; set; } = "";
        public string? Details { get; set; }
        public string? RecommendedAction { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;
        public Exception? Exception { get; set; }
        public object? Data { get; set; }

        /// <summary>
        /// أيقونة الحالة للعرض
        /// </summary>
        public string StatusIcon => Status switch
        {
            HealthStatus.Ok => "✅",
            HealthStatus.Warning => "⚠️",
            HealthStatus.Failed => "❌",
            _ => "❓"
        };

        /// <summary>
        /// نص وصف المدة
        /// </summary>
        public string DurationText => Duration.TotalMilliseconds < 1000 
            ? $"{Duration.TotalMilliseconds:F0}ms"
            : $"{Duration.TotalSeconds:F1}s";

        /// <summary>
        /// إنشاء نتيجة ناجحة
        /// </summary>
        public static HealthCheckResult Success(string checkName, string message, string category = "General")
        {
            return new HealthCheckResult
            {
                CheckName = checkName,
                Category = category,
                Status = HealthStatus.Ok,
                Message = message
            };
        }

        /// <summary>
        /// إنشاء نتيجة تحذير
        /// </summary>
        public static HealthCheckResult Warning(string checkName, string message, string category = "General", string? recommendedAction = null)
        {
            return new HealthCheckResult
            {
                CheckName = checkName,
                Category = category,
                Status = HealthStatus.Warning,
                Message = message,
                RecommendedAction = recommendedAction
            };
        }

        /// <summary>
        /// إنشاء نتيجة فشل
        /// </summary>
        public static HealthCheckResult Failure(string checkName, string message, string category = "General", Exception? exception = null, string? recommendedAction = null)
        {
            return new HealthCheckResult
            {
                CheckName = checkName,
                Category = category,
                Status = HealthStatus.Failed,
                Message = message,
                Exception = exception,
                RecommendedAction = recommendedAction
            };
        }

        /// <summary>
        /// إنشاء نتيجة ناجحة (للتوافق مع الكود القديم)
        /// </summary>
        public static HealthCheckResult Ok(string checkName, string message, TimeSpan duration)
        {
            return new HealthCheckResult
            {
                CheckName = checkName,
                Status = HealthStatus.Ok,
                Message = message,
                Duration = duration
            };
        }

        /// <summary>
        /// إنشاء نتيجة فشل (للتوافق مع الكود القديم)
        /// </summary>
        public static HealthCheckResult Failed(string checkName, string message, TimeSpan duration, Exception? exception = null, string? recommendedAction = null)
        {
            return new HealthCheckResult
            {
                CheckName = checkName,
                Status = HealthStatus.Failed,
                Message = message,
                Duration = duration,
                Exception = exception,
                RecommendedAction = recommendedAction
            };
        }
    }

    /// <summary>
    /// حالة الفحص الصحي
    /// </summary>
    public enum HealthStatus
    {
        Ok,
        Warning,
        Failed
    }

    /// <summary>
    /// نتيجة عملية الإصلاح
    /// </summary>
    public class FixResult
    {
        public string ActionName { get; set; } = "";
        public bool IsSuccessful { get; set; }
        public string Message { get; set; } = "";
        public string? Details { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime FixTime { get; set; } = DateTime.UtcNow;
        public Exception? Exception { get; set; }

        /// <summary>
        /// إنشاء نتيجة إصلاح ناجح
        /// </summary>
        public static FixResult Success(string actionName, string message)
        {
            return new FixResult
            {
                ActionName = actionName,
                IsSuccessful = true,
                Message = message
            };
        }

        /// <summary>
        /// إنشاء نتيجة إصلاح فاشل
        /// </summary>
        public static FixResult Failure(string actionName, string message, Exception? exception = null)
        {
            return new FixResult
            {
                ActionName = actionName,
                IsSuccessful = false,
                Message = message,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// خيارات النظام التشخيصي
    /// </summary>
    public class DiagnosticsOptions
    {
        /// <summary>
        /// تشغيل فحص سريع فقط
        /// </summary>
        public bool QuickCheckOnly { get; set; }

        /// <summary>
        /// تشغيل فحص شامل
        /// </summary>
        public bool ComprehensiveCheck { get; set; }

        /// <summary>
        /// تشغيل الإصلاح التلقائي
        /// </summary>
        public bool AutoFix { get; set; }

        /// <summary>
        /// الإصلاح الآمن فقط (لا تعديلات خطيرة)
        /// </summary>
        public bool SafeFixOnly { get; set; } = true;

        /// <summary>
        /// تشغيل اختبار الأداء
        /// </summary>
        public bool PerformanceTest { get; set; }

        /// <summary>
        /// تشغيل اختبار الانحدار البصري
        /// </summary>
        public bool VisualRegressionTest { get; set; }

        /// <summary>
        /// مهلة انتظار الفحص بالمللي ثانية
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;

        /// <summary>
        /// تضمين فحوصات الأمان
        /// </summary>
        public bool IncludeSecurityChecks { get; set; } = true;

        /// <summary>
        /// تضمين فحوصات قاعدة البيانات
        /// </summary>
        public bool IncludeDatabaseChecks { get; set; } = true;

        /// <summary>
        /// تضمين فحوصات الموارد
        /// </summary>
        public bool IncludeResourceChecks { get; set; } = true;

        /// <summary>
        /// مسار مجلد الإخراج
        /// </summary>
        public string OutputPath { get; set; } = "DiagnosticsOutput";

        /// <summary>
        /// تنسيق ملف التقرير
        /// </summary>
        public ReportFormat OutputFormat { get; set; } = ReportFormat.Json;

        /// <summary>
        /// اسم ملف التقرير
        /// </summary>
        public string? OutputFileName { get; set; }

        /// <summary>
        /// الحصول على الخيارات الافتراضية للفحص السريع
        /// </summary>
        public static DiagnosticsOptions QuickCheck()
        {
            return new DiagnosticsOptions
            {
                QuickCheckOnly = true,
                AutoFix = false,
                PerformanceTest = false,
                VisualRegressionTest = false,
                TimeoutMs = 10000
            };
        }

        /// <summary>
        /// الحصول على الخيارات الافتراضية للفحص الشامل
        /// </summary>
        public static DiagnosticsOptions Comprehensive()
        {
            return new DiagnosticsOptions
            {
                ComprehensiveCheck = true,
                AutoFix = true,
                SafeFixOnly = true,
                PerformanceTest = true,
                VisualRegressionTest = true,
                TimeoutMs = 300000 // 5 دقائق
            };
        }
    }

    /// <summary>
    /// تنسيق ملف التقرير
    /// </summary>
    public enum ReportFormat
    {
        Json,
        Html,
        Xml,
        Text
    }
}