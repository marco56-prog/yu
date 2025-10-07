using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Models;

// كائن الخزنة
public class CashBox
{
    [Key]
    public int CashBoxId { get; set; }

    [Required, StringLength(100)]
    public required string CashBoxName { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentBalance { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    public virtual ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}

// أنواع المعاملات النقدية
public enum TransactionType
{
    Income = 1,   // دخل
    Expense = 2,  // مصروف
    Transfer = 3  // تحويل
}

// كائن المعاملات النقدية
public class CashTransaction
{
    [Key]
    public int CashTransactionId { get; set; }

    [Required, StringLength(20), Unicode(false)]
    public required string TransactionNumber { get; set; } // رقم تلقائي

    [Required]
    public int CashBoxId { get; set; }

    [Required]
    public TransactionType TransactionType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, StringLength(200)]
    public required string Description { get; set; }

    [StringLength(100), Unicode(false)]
    public string? ReferenceType { get; set; } // نوع المرجع

    public int? ReferenceId { get; set; } // رقم المرجع

    public DateTime TransactionDate { get; set; } = DateTime.Now;

    public int CreatedBy { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    [ForeignKey(nameof(CashBoxId))]
    public virtual required CashBox CashBox { get; set; }
}

// كائن معاملات العملاء
public class CustomerTransaction
{
    [Key]
    public int CustomerTransactionId { get; set; }

    [Required, StringLength(20), Unicode(false)]
    public required string TransactionNumber { get; set; } // رقم تلقائي

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public TransactionType TransactionType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, StringLength(200)]
    public required string Description { get; set; }

    [StringLength(100), Unicode(false)]
    public string? ReferenceType { get; set; } // نوع المرجع (فاتورة، دفعة، إلخ)

    public int? ReferenceId { get; set; } // رقم المرجع

    public DateTime TransactionDate { get; set; } = DateTime.Now;

    public int CreatedBy { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    [ForeignKey(nameof(CustomerId))]
    public virtual required Customer Customer { get; set; }
}

// كائن معاملات الموردين
public class SupplierTransaction
{
    [Key]
    public int SupplierTransactionId { get; set; }

    [Required, StringLength(20), Unicode(false)]
    public required string TransactionNumber { get; set; } // رقم تلقائي

    [Required]
    public int SupplierId { get; set; }

    [Required]
    public TransactionType TransactionType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, StringLength(200)]
    public required string Description { get; set; }

    [StringLength(100), Unicode(false)]
    public string? ReferenceType { get; set; } // نوع المرجع

    public int? ReferenceId { get; set; } // رقم المرجع

    public DateTime TransactionDate { get; set; } = DateTime.Now;

    public int CreatedBy { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    [ForeignKey(nameof(SupplierId))]
    public virtual required Supplier Supplier { get; set; }
}

// كائن إعدادات النظام
public class SystemSettings
{
    [Key]
    public int SettingId { get; set; }

    [Required, StringLength(100), Unicode(false)]
    public required string SettingKey { get; set; }

    [Required, StringLength(500)]
    public required string SettingValue { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}

// كائن تسلسل الأرقام التلقائية
public class NumberSequence
{
    [Key]
    public int SequenceId { get; set; }

    [Required, StringLength(50), Unicode(false)]
    public required string SequenceType { get; set; } // نوع التسلسل (SalesInvoice, PurchaseInvoice, إلخ)

    [StringLength(20), Unicode(false)]
    public string? Prefix { get; set; } // بادئة

    public int CurrentNumber { get; set; }

    [StringLength(20), Unicode(false)]
    public string? Suffix { get; set; } // لاحقة

    public int NumberLength { get; set; } = 6; // طول الرقم

    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];
}

// كائن المستخدمين
public partial class User
{
    [Key]
    public int UserId { get; set; }

    [Required, StringLength(50), Unicode(false)]
    public required string UserName { get; set; }

    [Required, StringLength(100)]
    public required string FullName { get; set; }

    [Required, StringLength(100), Unicode(false), EmailAddress]
    public required string Email { get; set; }

    [Required, StringLength(255), Unicode(false)]
    public required string PasswordHash { get; set; }

    [StringLength(50), Unicode(false), Phone]
    public string? Phone { get; set; }

    [StringLength(50), Unicode(false)]
    public string? Role { get; set; } = "viewer";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? LastLoginDate { get; set; }

    // حقول القفل الأمني الدائم
    public int FailedAccessCount { get; set; }

    public DateTime? LockoutEnd { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;
}
