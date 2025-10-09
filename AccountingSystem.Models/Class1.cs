using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Models;

// كائن العميل
public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required, StringLength(20), Unicode(false)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public required string CustomerName { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50), Unicode(false), Phone]
    public string? Phone { get; set; }

    [StringLength(100), Unicode(false), EmailAddress]
    public string? Email { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } // الرصيد التراكمي (قد يكون سالب للذمم)

    // خصائص جديدة
    public int? PriceLevelId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditLimit { get; set; } = 0;

    public DateTime? LastPurchaseDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPurchases { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Compatibility: some tests/code expect CreatedAt and CreatedBy
    [NotMapped]
    public DateTime CreatedAt
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    public virtual PriceLevel? PriceLevel { get; set; }
    public virtual ICollection<SalesInvoice> SalesInvoices { get; set; } = [];
    public virtual ICollection<CustomerTransaction> CustomerTransactions { get; set; } = [];
    
    // العلاقات الجديدة للعروض
    public virtual ICollection<Promotion> Promotions { get; set; } = [];
}

// كائن المورد
public class Supplier
{
    [Key]
    public int SupplierId { get; set; }

    [Required, StringLength(20), Unicode(false)]
    public string SupplierCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public required string SupplierName { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(50), Unicode(false), Phone]
    public string? Phone { get; set; }

    [StringLength(100), Unicode(false), EmailAddress]
    public string? Email { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; } // الرصيد التراكمي

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    public virtual ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = [];
    public virtual ICollection<SupplierTransaction> SupplierTransactions { get; set; } = [];
}

// كائن فئة المنتج
public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required, StringLength(100)]
    public required string CategoryName { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    public virtual ICollection<Product> Products { get; set; } = [];
}

// كائن الوحدة
public class Unit
{
    [Key]
    public int UnitId { get; set; }

    [Required, StringLength(50)]
    public required string UnitName { get; set; }

    [StringLength(20)]
    public string? UnitSymbol { get; set; }

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    public virtual ICollection<Product> Products { get; set; } = [];
    public virtual ICollection<ProductUnit> ProductUnits { get; set; } = [];
}

// كائن المنتج
public class Product : ICloneable
{
    [Key]
    public int ProductId { get; set; }

    [Required, StringLength(20), Unicode(false)]
    public required string ProductCode { get; set; } // كود تلقائي

    [Required, StringLength(200)]
    public required string ProductName { get; set; }

    [StringLength(50), Unicode(false)]
    public string? Barcode { get; set; } // باركود المنتج

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int MainUnitId { get; set; } // الوحدة الأساسية

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentStock { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinimumStock { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; }

    // خصائص إضافية للتوافق مع الكود الموجود
    [NotMapped]
    public decimal SellPrice
    {
        get => SalePrice;
        set => SalePrice = value;
    }

    [NotMapped]
    public decimal CostPrice
    {
        get => PurchasePrice;
        set => PurchasePrice = value;
    }

    // خصائص جديدة
    [StringLength(500)]
    public string? ImagePath { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int? WarehouseId { get; set; }

    public bool IsSerialNumberRequired { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public decimal Weight { get; set; }

    public bool HasExpiryDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Compatibility properties used by tests
    [NotMapped]
    public DateTime CreatedAt
    {
        get => CreatedDate;
        set => CreatedDate = value;
    }

    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    // Some older code/tests expect a Unit string property (human-readable)
    [NotMapped]
    public string? Unit { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    [ForeignKey(nameof(CategoryId))]
    public virtual Category? Category { get; set; }

    [ForeignKey(nameof(MainUnitId))]
    public virtual Unit? MainUnit { get; set; }

    public virtual ICollection<ProductUnit> ProductUnits { get; set; } = [];
    public virtual ICollection<StockMovement> StockMovements { get; set; } = [];
    public virtual ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } = [];
    public virtual ICollection<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = [];
    
    // العلاقات الجديدة للخصومات والعروض والمستودعات
    public virtual ICollection<DiscountRule> DiscountRules { get; set; } = [];
    public virtual ICollection<Promotion> Promotions { get; set; } = [];
    public virtual ICollection<ProductStock> ProductStocks { get; set; } = [];
    public virtual ICollection<ProductPriceHistory> PriceHistories { get; set; } = [];

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

// كائن وحدات المنتج (معاملات التحويل)
[Index(nameof(ProductId), nameof(UnitId), IsUnique = true, Name = "UX_ProductUnit_Product_Unit")]
public class ProductUnit
{
    [Key]
    public int ProductUnitId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    public int UnitId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ConversionFactor { get; set; } // معامل التحويل من الوحدة الأساسية

    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    // العلاقات
    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [ForeignKey(nameof(UnitId))]
    public virtual Unit? Unit { get; set; }
}
