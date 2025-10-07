using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models
{
    /// <summary>
    /// نموذج الكاشير
    /// </summary>
    public class Cashier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "كود الكاشير مطلوب")]
        [MaxLength(20)]
        public string CashierCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم الكاشير مطلوب")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string Status { get; set; } = "نشط"; // نشط، غير نشط، إجازة

        // معلومات تسجيل الدخول
        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(255)]
        public string? PasswordHash { get; set; }

        public DateTime? LastLoginTime { get; set; }

        // الصلاحيات
        public bool CanOpenCashDrawer { get; set; } = true;
        public bool CanProcessReturns { get; set; }
        public bool CanApplyDiscounts { get; set; }
        public bool CanVoidTransactions { get; set; }
        public bool CanViewReports { get; set; }
        public bool CanManageInventory { get; set; }
        public bool CanAccessSettings { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal MaxDiscountPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxDiscountAmount { get; set; }

        // معلومات التدقيق
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // العلاقات
        public virtual ICollection<CashierSession> Sessions { get; set; } = new List<CashierSession>();
        public virtual ICollection<POSTransaction> Transactions { get; set; } = new List<POSTransaction>();
    }

    /// <summary>
    /// جلسة الكاشير (الوردية)
    /// </summary>
    public class CashierSession
    {
        public int Id { get; set; }

        [Required]
        public int CashierId { get; set; }

        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ClosingBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedClosingBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashSalesTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CardSalesTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalReturns { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDiscounts { get; set; }

        public int TransactionsCount { get; set; }
        public int ReturnsCount { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "مفتوحة"; // مفتوحة، مغلقة، معلقة

        [MaxLength(500)]
        public string? Notes { get; set; }

    public bool IsClosed { get; set; }

        // العلاقات
        [ForeignKey("CashierId")]
        public virtual Cashier Cashier { get; set; } = null!;

        public virtual ICollection<POSTransaction> Transactions { get; set; } = new List<POSTransaction>();
        public virtual ICollection<CashDrawerOperation> CashDrawerOperations { get; set; } = new List<CashDrawerOperation>();
    }

    /// <summary>
    /// معاملة نقطة البيع
    /// </summary>
    public class POSTransaction
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string TransactionNumber { get; set; } = string.Empty;

        [Required]
        public int CashierId { get; set; }

        public int? SessionId { get; set; }

        public int? CustomerId { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string TransactionType { get; set; } = "بيع"; // بيع، مرتجع، إلغاء

        [Column(TypeName = "decimal(18,3)")]
        public decimal Subtotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeAmount { get; set; } = 0;

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "نقداً"; // نقداً، بطاقة، تحويل، آجل

        [MaxLength(100)]
        public string? PaymentReference { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "مكتملة"; // مكتملة، معلقة، ملغية، مرتجعة

        [MaxLength(500)]
        public string? Notes { get; set; }

    public bool IsPrinted { get; set; }
    public bool IsVoided { get; set; }
        public DateTime? VoidedAt { get; set; }
        public string? VoidReason { get; set; }

        // العلاقات
        [ForeignKey("CashierId")]
        public virtual Cashier Cashier { get; set; } = null!;

        [ForeignKey("SessionId")]
        public virtual CashierSession? Session { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public virtual ICollection<POSTransactionItem> Items { get; set; } = new List<POSTransactionItem>();
        public virtual ICollection<POSPayment> Payments { get; set; } = new List<POSPayment>();
    }

    /// <summary>
    /// صنف معاملة نقطة البيع
    /// </summary>
    public class POSTransactionItem
    {
        public int Id { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; } = 0;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // العلاقات
        [ForeignKey("TransactionId")]
        public virtual POSTransaction Transaction { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }

    /// <summary>
    /// دفعة نقطة البيع (للدفع المتعدد)
    /// </summary>
    public class POSPayment
    {
        public int Id { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "نقداً";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0;

        [MaxLength(100)]
        public string? Reference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime PaymentTime { get; set; } = DateTime.Now;

        // العلاقات
        [ForeignKey("TransactionId")]
        public virtual POSTransaction Transaction { get; set; } = null!;
    }

    /// <summary>
    /// عمليات الخزينة
    /// </summary>
    public class CashDrawerOperation
    {
        public int Id { get; set; }

        [Required]
        public int CashierId { get; set; }

        public int? SessionId { get; set; }

        public DateTime OperationTime { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string OperationType { get; set; } = "إيداع"; // إيداع، سحب، فتح، إغلاق

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceBefore { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; } = 0;

        [Required]
        [MaxLength(200)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string? Reference { get; set; }

        // العلاقات
        [ForeignKey("CashierId")]
        public virtual Cashier Cashier { get; set; } = null!;

        [ForeignKey("SessionId")]
        public virtual CashierSession? Session { get; set; }
    }

    /// <summary>
    /// خصم أو عرض
    /// </summary>
    public class Discount
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string DiscountCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string DiscountType { get; set; } = "نسبة"; // نسبة، مبلغ ثابت

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscount { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(50)]
        public string ApplicableOn { get; set; } = "إجمالي"; // إجمالي، منتج، فئة

        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }

        public int UsageLimit { get; set; } // 0 = بلا حدود
        public int TimesUsed { get; set; }

        public bool RequiresApproval { get; set; }

        // العلاقات
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
    }

    /// <summary>
    /// نقاط الولاء
    /// </summary>
    public class LoyaltyProgram
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPerPoint { get; set; } = 1; // مبلغ لكل نقطة

        [Column(TypeName = "decimal(18,2)")]
        public decimal PointValue { get; set; } = 0.01m; // قيمة النقطة

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinimumRedemption { get; set; } = 100; // أقل نقاط للاستبدال

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // العلاقات
        public virtual ICollection<CustomerLoyaltyPoint> CustomerPoints { get; set; } = new List<CustomerLoyaltyPoint>();
    }

    /// <summary>
    /// نقاط ولاء العميل
    /// </summary>
    public class CustomerLoyaltyPoint
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int LoyaltyProgramId { get; set; }

        public int PointsEarned { get; set; }
        public int PointsRedeemed { get; set; }
        public int PointsBalance { get; set; }

        public DateTime LastEarnedDate { get; set; } = DateTime.Now;
        public DateTime? LastRedeemedDate { get; set; }

        // العلاقات
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey("LoyaltyProgramId")]
        public virtual LoyaltyProgram LoyaltyProgram { get; set; } = null!;

        public virtual ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
    }

    /// <summary>
    /// معاملات نقاط الولاء
    /// </summary>
    public class LoyaltyTransaction
    {
        public int Id { get; set; }

        [Required]
        public int CustomerLoyaltyPointId { get; set; }

        public int? POSTransactionId { get; set; }

        [MaxLength(50)]
        public string TransactionType { get; set; } = "كسب"; // كسب، استبدال، انتهاء

        public int Points { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MonetaryValue { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // العلاقات
        [ForeignKey("CustomerLoyaltyPointId")]
        public virtual CustomerLoyaltyPoint CustomerLoyaltyPoint { get; set; } = null!;

        [ForeignKey("POSTransactionId")]
        public virtual POSTransaction? POSTransaction { get; set; }
    }
}