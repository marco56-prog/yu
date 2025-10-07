using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Models
{
    // حالة الفاتورة
    public enum InvoiceStatus
    {
        Draft = 1,
        Confirmed = 2,
        Posted = 3,
        Cancelled = 4
    }

    // نوع حركة المخزون
    public enum StockMovementType
    {
        In = 1,
        Out = 2,
        Adjustment = 3
    }

    // فاتورة بيع
    [Index(nameof(InvoiceNumber), IsUnique = true)]
    public class SalesInvoice
    {
        [Key]
        public int SalesInvoiceId { get; set; }

        [Required, StringLength(20)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public int? WarehouseId { get; set; }

        public int? RepresentativeId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public bool IsPosted { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Compatibility for tests
        [NotMapped]
        public DateTime CreatedAt
        {
            get => CreatedDate;
            set => CreatedDate = value;
        }

    // CreatedBy already defined above (compatibility alias preserved)

        // Older tests expect IsDraft boolean
        [NotMapped]
        public bool IsDraft
        {
            get => Status == InvoiceStatus.Draft;
            set => Status = value ? InvoiceStatus.Draft : InvoiceStatus.Confirmed;
        }

        // TaxRate used in tests to compute TaxAmount
        [NotMapped]
        public decimal TaxRate { get; set; }

        [StringLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual Warehouse? Warehouse { get; set; }
        public virtual Representative? Representative { get; set; }
        public virtual ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();

        // Backwards-compatible alias expected by some tests/code
        [NotMapped]
        public ICollection<SalesInvoiceItem> SalesInvoiceItems => Items;
    }

    // بند فاتورة البيع
    public class SalesInvoiceItem
    {
        [Key]
        public int SalesInvoiceItemId { get; set; }

        [Required]
        public int SalesInvoiceId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        // Compatibility alias used in tests
        [NotMapped]
        public decimal Discount
        {
            get => DiscountAmount;
            set => DiscountAmount = value;
        }

        public int? UnitId { get; set; }

        [StringLength(200)]
        public string Notes { get; set; } = string.Empty;

        // خصائص إضافية للعرض
        [NotMapped]
        public string ProductName => Product?.ProductName ?? string.Empty;

        [NotMapped]
        public string UnitName => Product?.MainUnit?.UnitName ?? "قطعة";

        // Navigation properties
        public virtual SalesInvoice SalesInvoice { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Unit? Unit { get; set; }
    }

    // فاتورة شراء
    [Index(nameof(InvoiceNumber), IsUnique = true)]
    public class PurchaseInvoice
    {
        [Key]
        public int PurchaseInvoiceId { get; set; }

        [Required, StringLength(20)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int SupplierId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public bool IsPosted { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public virtual Supplier Supplier { get; set; } = null!;
        public virtual ICollection<PurchaseInvoiceItem> Items { get; set; } = new List<PurchaseInvoiceItem>();
    }

    // بند فاتورة الشراء
    public class PurchaseInvoiceItem
    {
        [Key]
        public int PurchaseInvoiceItemId { get; set; }

        [Required]
        public int PurchaseInvoiceId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public int? UnitId { get; set; }

        [StringLength(200)]
        public string Notes { get; set; } = string.Empty;

        // Navigation properties
        public virtual PurchaseInvoice PurchaseInvoice { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Unit? Unit { get; set; }
    }

    // حركة المخزون
    public class StockMovement
    {
        [Key]
        public int StockMovementId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public StockMovementType MovementType { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Required]
        public DateTime MovementDate { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string ReferenceType { get; set; } = string.Empty;

        public int? ReferenceId { get; set; }

        public int? UnitId { get; set; }

        // مستودع الحركة (اختياري)
        public int? WarehouseId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityInMainUnit { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual Unit? Unit { get; set; }
        public virtual Warehouse? Warehouse { get; set; }
    }
}
