using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Models
{
    /// <summary>
    /// نموذج المستودع
    /// </summary>
    public class Warehouse
    {
        [Key]
        public int WarehouseId { get; set; }

        [Required, MaxLength(50)]
        public string WarehouseCode { get; set; } = "";

        [Required, MaxLength(200)]
        public string WarehouseName { get; set; } = "";

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? ManagerName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[8];

        // Navigation Properties
        public virtual ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
        public virtual ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();
    }

    /// <summary>
    /// نموذج المندوب
    /// </summary>
    public class Representative
    {
        [Key]
        public int RepresentativeId { get; set; }

        [Required, MaxLength(50)]
        public string RepresentativeCode { get; set; } = "";

        [Required, MaxLength(200)]
        public string RepresentativeName { get; set; } = "";

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[8];

        // Navigation Properties
        public virtual ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    }

    /// <summary>
    /// نموذج مخزون المنتج في المستودع
    /// </summary>
    [Index(nameof(ProductId), nameof(WarehouseId), IsUnique = true)]
    public class ProductStock
    {
        [Key]
        public int ProductStockId { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Required]
        public int WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal ReservedQuantity { get; set; } = 0;

        [NotMapped]
        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        [Column(TypeName = "decimal(18,4)")]
        public decimal MinimumStock { get; set; } = 0;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[8];
    }
}