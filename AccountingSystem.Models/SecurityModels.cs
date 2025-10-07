using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Models;

// نموذج الأدوار والصلاحيات
public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required, StringLength(50)]
    public required string RoleName { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public bool IsSystemRole { get; set; } = false; // الأدوار الأساسية للنظام
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // العلاقات
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

// نموذج الصلاحيات
public class Permission
{
    [Key]
    public int PermissionId { get; set; }

    [Required, StringLength(100)]
    public required string PermissionName { get; set; }

    [Required, StringLength(50)]
    public required string PermissionCode { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = "General"; // Sales, Inventory, Reports, etc.

    public bool IsSystemPermission { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // العلاقات
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

// ربط الأدوار بالصلاحيات
[Index(nameof(RoleId), nameof(PermissionId), IsUnique = true)]
public class RolePermission
{
    [Key]
    public int RolePermissionId { get; set; }

    [Required]
    public int RoleId { get; set; }

    [Required]
    public int PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public DateTime GrantedDate { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string GrantedBy { get; set; } = string.Empty;

    // العلاقات
    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey(nameof(PermissionId))]
    public virtual Permission Permission { get; set; } = null!;
}

// نموذج سجل العمليات (Audit Log)
public class AuditLog
{
    [Key]
    public int AuditId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required, StringLength(50)]
    public required string Action { get; set; } // Create, Update, Delete, View, Login, etc.
    
    // إضافة الخصائص المطلوبة للتوافق مع AuditService
    [Required, StringLength(100)]
    public string Operation => Action; // Alias للـ Action
    
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [StringLength(50)]
    public string? TableName { get; set; }

    public int? RecordId { get; set; }

    [StringLength(2000)] // زيادة الحجم للـ JSON
    public string? OldValues { get; set; } // JSON

    [StringLength(2000)] // زيادة الحجم للـ JSON
    public string? NewValues { get; set; } // JSON

    [StringLength(1000)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string? IpAddress { get; set; }
    
    [StringLength(20)]
    public string Severity { get; set; } = "Medium";
    
    [StringLength(20)]
    public string Status { get; set; } = "Success";

    [StringLength(200)]
    public string? UserAgent { get; set; }

    [StringLength(100)]
    public string? MachineName { get; set; }

    public bool IsSuccessful { get; set; } = true;

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    // العلاقات
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}

// نموذج جلسات المستخدمين
public class UserSession
{
    [Key]
    public int SessionId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required, StringLength(500)]
    public required string SessionToken { get; set; }

    public DateTime LoginTime { get; set; } = DateTime.Now;
    public DateTime? LogoutTime { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string? IPAddress { get; set; }

    [StringLength(200)]
    public string? UserAgent { get; set; }

    [StringLength(100)]
    public string? MachineName { get; set; }

    public bool IsActive { get; set; } = true;
    public bool ForceLogout { get; set; } = false;

    [StringLength(200)]
    public string? LogoutReason { get; set; }

    // العلاقات
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}

// نموذج أمان الوصول للبيانات
public class DataSecurity
{
    [Key]
    public int SecurityId { get; set; }

    [Required, StringLength(50)]
    public required string TableName { get; set; }

    public int? RecordId { get; set; }

    [Required]
    public int UserId { get; set; }

    [StringLength(50)]
    public string AccessType { get; set; } = "Read"; // Read, Write, Delete

    public bool IsAllowed { get; set; } = true;

    [StringLength(200)]
    public string? Conditions { get; set; } // JSON للشروط

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ExpiryDate { get; set; }

    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    // العلاقات
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}

// نموذج النسخ الاحتياطية
public class BackupInfo
{
    [Key]
    public int BackupId { get; set; }

    [Required, StringLength(200)]
    public required string BackupName { get; set; }

    [Required, StringLength(500)]
    public required string FilePath { get; set; }

    [Column(TypeName = "bigint")]
    public long FileSizeBytes { get; set; }

    public DateTime BackupDate { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string BackupType { get; set; } = "Full"; // Full, Incremental, Differential

    public bool IsCompressed { get; set; } = true;
    public bool IsEncrypted { get; set; } = false;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsSuccessful { get; set; } = true;

    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedDate { get; set; }

    // إحصائيات النسخة الاحتياطية
    public int TablesCount { get; set; }
    public int RecordsCount { get; set; }
    public TimeSpan BackupDuration { get; set; }

    [StringLength(100)]
    public string? ChecksumMD5 { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}

// نموذج إعدادات الأمان
public class SecuritySettings
{
    [Key]
    public int SettingId { get; set; }

    [Required, StringLength(100)]
    public required string SettingKey { get; set; }

    [Required, StringLength(500)]
    public required string SettingValue { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string Category { get; set; } = "General"; // Password, Session, Backup, etc.

    public bool IsSystemSetting { get; set; } = false;
    public bool RequiresRestart { get; set; } = false;

    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string UpdatedBy { get; set; } = string.Empty;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}

// نموذج المحاولات المشبوهة
public class SecurityIncident
{
    [Key]
    public int IncidentId { get; set; }

    [StringLength(50)]
    public string IncidentType { get; set; } = string.Empty; // "FailedLogin", "UnauthorizedAccess", "DataBreach"

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? IPAddress { get; set; }

    [StringLength(100)]
    public string? UserName { get; set; }

    [StringLength(200)]
    public string? UserAgent { get; set; }

    public DateTime IncidentTime { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical

    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedDate { get; set; }

    [StringLength(50)]
    public string? ResolvedBy { get; set; }

    [StringLength(500)]
    public string? Resolution { get; set; }

    [StringLength(1000)]
    public string? AdditionalData { get; set; } // JSON للبيانات الإضافية
}

// تحديث نموذج المستخدم لدعم الميزات الجديدة
public partial class User
{
    public int? RoleId { get; set; }

    public DateTime? LastPasswordChange { get; set; }
    public bool MustChangePassword { get; set; } = false;

    public bool TwoFactorEnabled { get; set; } = false;

    [StringLength(100)]
    public string? TwoFactorSecret { get; set; }

    public int MaxConcurrentSessions { get; set; } = 1;

    // العلاقات الجديدة (تجنب تكرار خاصية Role)
    [ForeignKey(nameof(RoleId))]
    public virtual Role? UserRole { get; set; }

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<DataSecurity> DataSecurities { get; set; } = new List<DataSecurity>();
}

// العمليات في سجل التتبع - موحدة وشاملة
public static class AuditOperations
{
    // عمليات تسجيل الدخول والأمان
    public const string UserLogin = "USER_LOGIN";
    public const string UserLogout = "USER_LOGOUT";
    public const string UserManagement = "USER_MANAGEMENT";
    public const string SecurityChange = "SECURITY_CHANGE";

    // عمليات الفواتير
    public const string CreateInvoice = "CREATE_INVOICE";
    public const string UpdateInvoice = "UPDATE_INVOICE";
    public const string DeleteInvoice = "DELETE_INVOICE";
    public const string CreateSalesInvoice = "CREATE_SALES_INVOICE";
    public const string UpdateSalesInvoice = "UPDATE_SALES_INVOICE";
    public const string DeleteSalesInvoice = "DELETE_SALES_INVOICE";
    public const string CreatePurchaseInvoice = "CREATE_PURCHASE_INVOICE";
    public const string UpdatePurchaseInvoice = "UPDATE_PURCHASE_INVOICE";
    public const string DeletePurchaseInvoice = "DELETE_PURCHASE_INVOICE";

    // عمليات المنتجات
    public const string CreateProduct = "CREATE_PRODUCT";
    public const string UpdateProduct = "UPDATE_PRODUCT";
    public const string DeleteProduct = "DELETE_PRODUCT";

    // عمليات العملاء والموردين
    public const string CreateCustomer = "CREATE_CUSTOMER";
    public const string UpdateCustomer = "UPDATE_CUSTOMER";
    public const string DeleteCustomer = "DELETE_CUSTOMER";
    public const string CreateSupplier = "CREATE_SUPPLIER";
    public const string UpdateSupplier = "UPDATE_SUPPLIER";
    public const string DeleteSupplier = "DELETE_SUPPLIER";

    // عمليات المخزون
    public const string StockMovement = "STOCK_MOVEMENT";
    public const string StockAdjustment = "STOCK_ADJUSTMENT";

    // عمليات النسخ الاحتياطي والاستعادة - المطلوبة في BackupService
    public const string BackupCreated = "BACKUP_CREATED";
    public const string BackupRestored = "BACKUP_RESTORED";
    public const string BackupDeleted = "BACKUP_DELETED";
    public const string SystemBackup = "SYSTEM_BACKUP";
    public const string SystemRestore = "SYSTEM_RESTORE";

    // عمليات التقارير
    public const string ReportGeneration = "REPORT_GENERATION";
    public const string ReportExport = "REPORT_EXPORT";

    // عمليات نقاط البيع
    public const string POSTransaction = "POS_TRANSACTION";
    public const string POSReturn = "POS_RETURN";
    public const string POSVoid = "POS_VOID";

    // عمليات عامة
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
    public const string View = "VIEW";
    public const string Export = "EXPORT";
    public const string Import = "IMPORT";
}

// مستوى خطورة العملية
public enum AuditSeverity
{
    Low = 1,        // عمليات عادية
    Medium = 2,     // عمليات مهمة
    High = 3,       // عمليات حساسة
    Critical = 4    // عمليات بالغة الخطورة
}

// حالة العملية في سجل التتبع
public enum AuditStatus
{
    Success,    // نجحت
    Failed,     // فشلت
    Pending     // في الانتظار
}