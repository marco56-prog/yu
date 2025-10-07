using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Models;

// نموذج قواعد الخصم
[Index(nameof(DiscountName), IsUnique = true)]
public class DiscountRule
{
    [Key]
    public int DiscountId { get; set; }

    [Required, StringLength(100)]
    public required string DiscountName { get; set; }

    [StringLength(300)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinQuantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxQuantity { get; set; } = decimal.MaxValue;

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FixedDiscountAmount { get; set; }

    public DateTime ValidFrom { get; set; } = DateTime.Now;
    public DateTime ValidTo { get; set; } = DateTime.Now.AddYears(1);

    public bool IsActive { get; set; } = true;
    public bool ApplyToAllProducts { get; set; }

    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // العلاقات
    public virtual ICollection<Product> ApplicableProducts { get; set; } = new List<Product>();
}

// ملاحظة: تم نقل تعريفات Warehouse و ProductStock إلى WarehouseModels.cs لتجنب التعريف المكرر

// نموذج مستويات الأسعار
public class PriceLevel
{
    [Key]
    public int PriceLevelId { get; set; }

    [Required, StringLength(50)]
    public required string LevelName { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumOrderAmount { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // العلاقات
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}

// نموذج تاريخ أسعار المنتجات
public class ProductPriceHistory
{
    [Key]
    public int PriceHistoryId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OldPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NewPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OldCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NewCost { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }

    public DateTime ChangeDate { get; set; } = DateTime.Now;

    [StringLength(50)]
    public string ChangedBy { get; set; } = string.Empty;

    // العلاقات
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;
}

// نموذج العروض والتخفيضات الموسمية
public class Promotion
{
    [Key]
    public int PromotionId { get; set; }

    [Required, StringLength(100)]
    public required string PromotionName { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal FixedDiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumPurchaseAmount { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);

    public bool IsActive { get; set; } = true;
    public bool AutoApply { get; set; } = true;

    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // العلاقات
    public virtual ICollection<Product> ApplicableProducts { get; set; } = new List<Product>();
    public virtual ICollection<Customer> ApplicableCustomers { get; set; } = new List<Customer>();
}