using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models
{
    /// <summary>
    /// أنواع الأخطاء المختلفة في النظام
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// خطأ في واجهة المستخدم
        /// </summary>
        UIError = 1,

        /// <summary>
        /// خطأ في قاعدة البيانات
        /// </summary>
        DatabaseError = 2,

        /// <summary>
        /// خطأ في منطق الأعمال
        /// </summary>
        BusinessLogicError = 3,

        /// <summary>
        /// خطأ في التحقق من صحة البيانات
        /// </summary>
        ValidationError = 4,

        /// <summary>
        /// خطأ في الشبكة أو الاتصال
        /// </summary>
        NetworkError = 5,

        /// <summary>
        /// خطأ في النظام العام
        /// </summary>
        SystemError = 6,

        /// <summary>
        /// خطأ في الأمان والحماية
        /// </summary>
        SecurityError = 7,

        /// <summary>
        /// خطأ في العمليات المالية
        /// </summary>
        FinancialError = 8,

        /// <summary>
        /// خطأ في المخزون
        /// </summary>
        InventoryError = 9,

        /// <summary>
        /// خطأ في التقارير
        /// </summary>
        ReportError = 10,

        /// <summary>
        /// سجل تدقيق - لتسجيل العمليات المهمة
        /// </summary>
        AuditLog = 11
    }

    /// <summary>
    /// مستوى خطورة الخطأ
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// معلومة فقط - لا يؤثر على النظام
        /// </summary>
        Info = 1,

        /// <summary>
        /// تحذير - قد يسبب مشاكل
        /// </summary>
        Warning = 2,

        /// <summary>
        /// خطأ - يؤثر على العملية الحالية
        /// </summary>
        Error = 3,

        /// <summary>
        /// خطأ خطير - يؤثر على جزء كبير من النظام
        /// </summary>
        Critical = 4,

        /// <summary>
        /// خطأ قاتل - يؤدي إلى توقف النظام
        /// </summary>
        Fatal = 5
    }

    /// <summary>
    /// حالة الخطأ (هل تم إصلاحه أم لا)
    /// </summary>
    public enum ErrorStatus
    {
        /// <summary>
        /// جديد - لم يتم التعامل معه
        /// </summary>
        New = 1,

        /// <summary>
        /// قيد المراجعة
        /// </summary>
        UnderReview = 2,

        /// <summary>
        /// قيد الإصلاح
        /// </summary>
        InProgress = 3,

        /// <summary>
        /// تم الإصلاح
        /// </summary>
        Resolved = 4,

        /// <summary>
        /// مُغلق
        /// </summary>
        Closed = 5,

        /// <summary>
        /// مُعاد فتحه
        /// </summary>
        Reopened = 6
    }

    /// <summary>
    /// نموذج سجل الأخطاء الرئيسي
    /// </summary>
    [Table("ErrorLogs")]
    public class ErrorLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// معرف فريد للخطأ
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ErrorId { get; set; } = Guid.NewGuid().ToString("N")[..12];

        /// <summary>
        /// نوع الخطأ
        /// </summary>
        [Required]
        public ErrorType ErrorType { get; set; }

        /// <summary>
        /// مستوى خطورة الخطأ
        /// </summary>
        [Required]
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// حالة الخطأ
        /// </summary>
        [Required]
        public ErrorStatus Status { get; set; } = ErrorStatus.New;

        /// <summary>
        /// عنوان الخطأ
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// رسالة الخطأ
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// تفاصيل الخطأ الكاملة
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Stack Trace الخطأ
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Inner Exception إن وجدت
        /// </summary>
        public string? InnerException { get; set; }

        /// <summary>
        /// مصدر الخطأ (ملف، كلاس، دالة)
        /// </summary>
        [MaxLength(500)]
        public string? Source { get; set; }

        /// <summary>
        /// اسم الطريقة التي حدث فيها الخطأ
        /// </summary>
        [MaxLength(200)]
        public string? MethodName { get; set; }

        /// <summary>
        /// رقم السطر إن أمكن
        /// </summary>
        public int? LineNumber { get; set; }

        /// <summary>
        /// اسم الملف الذي حدث فيه الخطأ
        /// </summary>
        [MaxLength(500)]
        public string? FileName { get; set; }

        /// <summary>
        /// معرف المستخدم الذي كان يستخدم النظام عند حدوث الخطأ
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// اسم المستخدم
        /// </summary>
        [MaxLength(200)]
        public string? Username { get; set; }

        /// <summary>
        /// عنوان IP للمستخدم
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent (إذا كان متاحاً)
        /// </summary>
        [MaxLength(1000)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// الجلسة أو Session ID
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// النشاط أو العملية التي كانت تجري عند حدوث الخطأ
        /// </summary>
        [MaxLength(200)]
        public string? Activity { get; set; }

        /// <summary>
        /// المعاملات أو البيانات التي تم إرسالها
        /// </summary>
        public string? RequestData { get; set; }

        /// <summary>
        /// البيانات الإضافية (JSON)
        /// </summary>
        public string? AdditionalData { get; set; }

        /// <summary>
        /// تاريخ ووقت حدوث الخطأ (UTC)
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// تاريخ آخر تحديث (UTC)
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// تاريخ الحل (UTC)
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// معرف المستخدم الذي قام بحل المشكلة
        /// </summary>
        public int? ResolvedBy { get; set; }

        /// <summary>
        /// ملاحظات الحل
        /// </summary>
        public string? ResolutionNotes { get; set; }

        /// <summary>
        /// توقيع الخطأ لتجميع التكرارات (SHA-256)
        /// </summary>
        [MaxLength(64)]
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        /// عدد مرات تكرار نفس الخطأ
        /// </summary>
        public int OccurrenceCount { get; set; } = 1;

        /// <summary>
        /// آخر مرة تكرر فيها الخطأ (UTC)
        /// </summary>
        public DateTime? LastOccurrence { get; set; }

        /// <summary>
        /// معرف الارتباط لتتبع العمليات المترابطة
        /// </summary>
        [MaxLength(100)]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// هل تم إرسال تنبيه للمطورين؟
        /// </summary>
    public bool NotificationSent { get; set; }

        /// <summary>
        /// هل الخطأ محل إهتمام؟
        /// </summary>
    public bool IsStarred { get; set; }

        /// <summary>
        /// ملاحظات إضافية
        /// </summary>
        public string? Comments { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("ResolvedBy")]
        public virtual User? ResolverUser { get; set; }

        public virtual ICollection<ErrorLogComment> ErrorComments { get; set; } = new List<ErrorLogComment>();
    }

    /// <summary>
    /// تعليقات على سجلات الأخطاء
    /// </summary>
    [Table("ErrorLogComments")]
    public class ErrorLogComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ErrorLogId { get; set; }

        // جعلناه nullable للسماح بـ SetNull عند حذف المستخدم
        public int? UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Comment { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("ErrorLogId")]
        public virtual ErrorLog ErrorLog { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// إحصائيات الأخطاء
    /// </summary>
    public class ErrorStatistics
    {
        /// <summary>
        /// إجمالي عدد الأخطاء
        /// </summary>
        public int TotalErrors { get; set; }

        /// <summary>
        /// عدد الأخطاء الجديدة
        /// </summary>
        public int NewErrors { get; set; }

        /// <summary>
        /// عدد الأخطاء المحلولة
        /// </summary>
        public int ResolvedErrors { get; set; }

        /// <summary>
        /// عدد الأخطاء الحرجة
        /// </summary>
        public int CriticalErrors { get; set; }

        /// <summary>
        /// إحصائيات الأخطاء حسب النوع
        /// </summary>
        public Dictionary<ErrorType, int> ErrorsByType { get; set; } = new();

        /// <summary>
        /// إحصائيات الأخطاء حسب الخطورة
        /// </summary>
        public Dictionary<ErrorSeverity, int> ErrorsBySeverity { get; set; } = new();

        /// <summary>
        /// إحصائيات الأخطاء حسب التاريخ (آخر 7 أيام)
        /// </summary>
        public Dictionary<DateTime, int> ErrorsByDate { get; set; } = new();

        /// <summary>
        /// أكثر الأخطاء تكراراً
        /// </summary>
        public List<TopErrorInfo> TopErrors { get; set; } = new();

        /// <summary>
        /// معدل الأخطاء اليومي
        /// </summary>
        public double DailyErrorRate { get; set; }

        /// <summary>
        /// نسبة الحل
        /// </summary>
        public double ResolutionRate { get; set; }

        /// <summary>
        /// متوسط وقت الحل (بالساعات)
        /// </summary>
        public double AverageResolutionTime { get; set; }
    }

    /// <summary>
    /// معلومات أكثر الأخطاء تكراراً
    /// </summary>
    public class TopErrorInfo
    {
        public string ErrorId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public ErrorType ErrorType { get; set; }
        public ErrorSeverity Severity { get; set; }
        public int Count { get; set; }
        public DateTime LastOccurrence { get; set; }
    }

    /// <summary>
    /// نموذج البحث في سجلات الأخطاء
    /// </summary>
    public class ErrorSearchRequest
    {
        /// <summary>
        /// البحث في النص
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// نوع الخطأ
        /// </summary>
        public ErrorType? ErrorType { get; set; }

        /// <summary>
        /// مستوى الخطورة
        /// </summary>
        public ErrorSeverity? Severity { get; set; }

        /// <summary>
        /// حالة الخطأ
        /// </summary>
        public ErrorStatus? Status { get; set; }

        /// <summary>
        /// معرف المستخدم
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// من تاريخ
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// إلى تاريخ
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// الأخطاء المميزة فقط
        /// </summary>
        public bool? OnlyStarred { get; set; }

        /// <summary>
        /// عدد العناصر في الصفحة
        /// </summary>
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// رقم الصفحة
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// ترتيب النتائج
        /// </summary>
        public string? OrderBy { get; set; } = "CreatedAt";

        /// <summary>
        /// ترتيب تنازلي؟
        /// </summary>
        public bool OrderDescending { get; set; } = true;
    }

    /// <summary>
    /// تقرير الأخطاء
    /// </summary>
    public class ErrorReport
    {
        public string Title { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public ErrorStatistics Statistics { get; set; } = new();
        public List<ErrorLog> ErrorDetails { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// نتيجة عملية البحث في الأخطاء
    /// </summary>
    public class ErrorSearchResult
    {
        public List<ErrorLog> Errors { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }
}