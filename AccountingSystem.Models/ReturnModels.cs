using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models
{
    // مرتجع بيع
    public class SalesReturn
    {
        [Key]
        public int SalesReturnId { get; set; }

        [Required]
        public DateTime ReturnDate { get; set; } = DateTime.Now;

        [Required]
        public int CustomerId { get; set; }

        public int OriginalSalesInvoiceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetTotal { get; set; }

        public string? Notes { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public int CreatedBy { get; set; } = 1;

        // Navigation
        public virtual Customer? Customer { get; set; }
        public virtual SalesInvoice? OriginalSalesInvoice { get; set; }
        public virtual ICollection<SalesReturnDetail> SalesReturnDetails { get; set; } = new List<SalesReturnDetail>();
    }

    public class SalesReturnDetail
    {
        [Key]
        public int SalesReturnDetailId { get; set; }

        [Required]
        public int SalesReturnId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? UnitId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(300)]
        public string? ReturnReason { get; set; }

        public int? OriginalInvoiceDetailId { get; set; }

        // Navigation
        public virtual SalesReturn SalesReturn { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Unit? Unit { get; set; }
    }

    // مرتجع شراء
    public class PurchaseReturn
    {
        [Key]
        public int PurchaseReturnId { get; set; }

        [Required]
        public DateTime ReturnDate { get; set; } = DateTime.Now;

        [Required]
        public int SupplierId { get; set; }

        public int OriginalPurchaseInvoiceId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetTotal { get; set; }

        public string? Notes { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        public int CreatedBy { get; set; } = 1;

        // Navigation
        public virtual Supplier? Supplier { get; set; }
        public virtual PurchaseInvoice? OriginalPurchaseInvoice { get; set; }
        public virtual ICollection<PurchaseReturnDetail> PurchaseReturnDetails { get; set; } = new List<PurchaseReturnDetail>();
    }

    public class PurchaseReturnDetail
    {
        [Key]
        public int PurchaseReturnDetailId { get; set; }

        [Required]
        public int PurchaseReturnId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? UnitId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(300)]
        public string? ReturnReason { get; set; }

        public int? OriginalInvoiceDetailId { get; set; }

        // Navigation
        public virtual PurchaseReturn PurchaseReturn { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Unit? Unit { get; set; }
    }
}
