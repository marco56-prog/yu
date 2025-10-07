using Microsoft.EntityFrameworkCore;
using AccountingSystem.Models;
using AccountingSystem.Data.Extensions;

namespace AccountingSystem.Data;

public class AccountingDbContext : DbContext
{
    public AccountingDbContext(DbContextOptions<AccountingDbContext> options) : base(options)
    {
    }

    // توحيد الدقّات الافتراضية: كل decimal = (18,2)،
    // والاستثناءات (الكميات 18,4) بتتظبط في ConfigurePrecision أدناه.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        
        // ضمان استخدام Unicode لكل النصوص (لدعم العربية)
        configurationBuilder.Properties<string>().AreUnicode(true);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // تعطيل تحذير EF التالي للراحة أثناء التطوير (EF Core 8.0)
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    #region DbSets - تعريف جداول قاعدة البيانات

    // الجداول الأساسية
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Product> Products { get; set; }

    // جداول الوحدات المتعددة للمنتجات
    public DbSet<ProductUnit> ProductUnits { get; set; }

    // جداول الفواتير
    public DbSet<SalesInvoice> SalesInvoices { get; set; }
    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; }
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }

    // جداول الكاشير ونقطة البيع
    public DbSet<Cashier> Cashiers { get; set; }
    public DbSet<CashierSession> CashierSessions { get; set; }
    public DbSet<POSTransaction> POSTransactions { get; set; }
    public DbSet<POSTransactionItem> POSTransactionItems { get; set; }
    public DbSet<POSPayment> POSPayments { get; set; }
    public DbSet<CashDrawerOperation> CashDrawerOperations { get; set; }

    // جداول الخصومات ونقاط الولاء
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }
    public DbSet<CustomerLoyaltyPoint> CustomerLoyaltyPoints { get; set; }
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }






    // جداول حركات المخزون والمعاملات
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<CashBox> CashBoxes { get; set; }
    public DbSet<CashTransaction> CashTransactions { get; set; }
    public DbSet<CustomerTransaction> CustomerTransactions { get; set; }
    public DbSet<Representative> Representatives { get; set; }
    public DbSet<SalesReturn> SalesReturns { get; set; }
    public DbSet<SalesReturnDetail> SalesReturnDetails { get; set; }
    public DbSet<PurchaseReturn> PurchaseReturns { get; set; }
    public DbSet<PurchaseReturnDetail> PurchaseReturnDetails { get; set; }
    public DbSet<SupplierTransaction> SupplierTransactions { get; set; }

    // الإعدادات والنظام
    public DbSet<SystemSettings> SystemSettings { get; set; }
    public DbSet<NumberSequence> NumberSequences { get; set; }
    public DbSet<User> Users { get; set; }

    // الكيانات المتقدمة الجديدة
    public DbSet<DiscountRule> DiscountRules { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<ProductStock> ProductStocks { get; set; }
    public DbSet<PriceLevel> PriceLevels { get; set; }
    public DbSet<ProductPriceHistory> ProductPriceHistories { get; set; }
    public DbSet<Promotion> Promotions { get; set; }

    // نماذج الإشعارات
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationAction> NotificationActions { get; set; }
    public DbSet<NotificationSettings> NotificationSettings { get; set; }
    public DbSet<NotificationRule> NotificationRules { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    // نماذج الأمان المتقدم
    public DbSet<Role> Roles { get; set; }
    public DbSet<PermissionEntity> Permissions { get; set; }
    public DbSet<RolePermissionLink> RolePermissions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<DataSecurity> DataSecurities { get; set; }
    public DbSet<BackupInfo> BackupInfos { get; set; }
    public DbSet<SecuritySettings> SecuritySettings { get; set; }
    public DbSet<SecurityIncident> SecurityIncidents { get; set; }

    // سجلات الأخطاء الشاملة
    public DbSet<ErrorLog> ErrorLogs { get; set; }
    public DbSet<ErrorLogComment> ErrorLogComments { get; set; }

    // التقارير المخصصة
    // public DbSet<CustomReport> CustomReports { get; set; } // معطل مؤقتاً لحل مشاكل FK

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ضمان دعم النصوص العربية
        modelBuilder.ConfigureArabicTextSupport();
        
        ConfigureIndexes(modelBuilder);
        ConfigurePrecision(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureTableNames(modelBuilder);
        ConfigureConstraints(modelBuilder); // تم تفعيلها
        ConfigureErrorLogging(modelBuilder); // إضافة تكوين سجلات الأخطاء

        SeedInitialData(modelBuilder);
    }

    #region Configuration Methods - طرق التكوين

    /// <summary>
    /// تكوين الفهارس للأداء الأمثل
    /// </summary>
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // العملاء
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.CustomerName)
            .HasDatabaseName("IX_Customer_Name");

        // الموردين
        modelBuilder.Entity<Supplier>()
            .HasIndex(s => s.SupplierName)
            .HasDatabaseName("IX_Supplier_Name");

        // المنتجات (فريدة)
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.ProductCode)
            .IsUnique()
            .HasDatabaseName("IX_Product_Code");

        // الفئات (فريدة)
        modelBuilder.Entity<Category>()
            .HasIndex(c => c.CategoryName)
            .IsUnique()
            .HasDatabaseName("IX_Category_Name");

        // الوحدات (فريدة)
        modelBuilder.Entity<Unit>()
            .HasIndex(u => u.UnitName)
            .IsUnique()
            .HasDatabaseName("IX_Unit_Name");

        // المستخدمون (UserName فريد)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique()
            .HasDatabaseName("IX_User_UserName");

        // Email فريد مع فلتر (يسمح بتكرار NULL في SQL Server)
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL")
            .HasDatabaseName("IX_User_Email");

        // إعدادات النظام (فريدة)
        modelBuilder.Entity<SystemSettings>()
            .HasIndex(s => s.SettingKey)
            .IsUnique()
            .HasDatabaseName("IX_SystemSettings_Key");

        // تسلسل الأرقام (فريد)
        modelBuilder.Entity<NumberSequence>()
            .HasIndex(n => n.SequenceType)
            .IsUnique()
            .HasDatabaseName("IX_NumberSequence_Type");

        // الفواتير
        modelBuilder.Entity<SalesInvoice>()
            .HasIndex(si => si.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("IX_SalesInvoice_Number");

        modelBuilder.Entity<PurchaseInvoice>()
            .HasIndex(pi => pi.InvoiceNumber)
            .IsUnique()
            .HasDatabaseName("IX_PurchaseInvoice_Number");

        // فهارس الكاشير ونقطة البيع
        modelBuilder.Entity<Cashier>()
            .HasIndex(c => c.CashierCode)
            .IsUnique()
            .HasDatabaseName("IX_Cashier_Code");

        modelBuilder.Entity<Cashier>()
            .HasIndex(c => c.Username)
            .IsUnique()
            .HasDatabaseName("IX_Cashier_Username")
            .HasFilter("[Username] IS NOT NULL");

        modelBuilder.Entity<POSTransaction>()
            .HasIndex(t => t.TransactionNumber)
            .IsUnique()
            .HasDatabaseName("IX_POSTransaction_Number");

        modelBuilder.Entity<POSTransaction>()
            .HasIndex(t => t.TransactionDate)
            .HasDatabaseName("IX_POSTransaction_Date");

        modelBuilder.Entity<CashierSession>()
            .HasIndex(s => new { s.CashierId, s.StartTime })
            .HasDatabaseName("IX_CashierSession_CashierDate");

        modelBuilder.Entity<Discount>()
            .HasIndex(d => d.DiscountCode)
            .IsUnique()
            .HasDatabaseName("IX_Discount_Code");

        // المعاملات المالية
        modelBuilder.Entity<CashTransaction>()
            .HasIndex(ct => ct.TransactionNumber)
            .IsUnique()
            .HasDatabaseName("IX_CashTransaction_Number");

        modelBuilder.Entity<CustomerTransaction>()
            .HasIndex(ct => ct.TransactionNumber)
            .IsUnique()
            .HasDatabaseName("IX_CustomerTransaction_Number");

        modelBuilder.Entity<SupplierTransaction>()
            .HasIndex(st => st.TransactionNumber)
            .IsUnique()
            .HasDatabaseName("IX_SupplierTransaction_Number");
    }

    /// <summary>
    /// تكوين دقة الأرقام العشرية
    /// </summary>
    private static void ConfigurePrecision(ModelBuilder modelBuilder)
    {
        // استثناءات (الكميات 18,4)
        var quantityEntities = new[]
        {
            typeof(Product).GetProperty(nameof(Product.CurrentStock)),
            typeof(Product).GetProperty(nameof(Product.MinimumStock)),
            typeof(ProductUnit).GetProperty(nameof(ProductUnit.ConversionFactor)),
            typeof(SalesInvoiceItem).GetProperty(nameof(SalesInvoiceItem.Quantity)),
            typeof(PurchaseInvoiceItem).GetProperty(nameof(PurchaseInvoiceItem.Quantity)),
            typeof(StockMovement).GetProperty(nameof(StockMovement.Quantity))
        };

        foreach (var property in quantityEntities.Where(p => p != null))
        {
            modelBuilder.Entity(property!.DeclaringType!)
                .Property(property.Name)
                .HasPrecision(18, 4);
        }

        // مبالغ سطر الفاتورة (18,2)
        var unitPriceEntities = new[]
        {
            typeof(SalesInvoiceItem).GetProperty(nameof(SalesInvoiceItem.UnitPrice)),
            typeof(SalesInvoiceItem).GetProperty(nameof(SalesInvoiceItem.LineTotal)),
            typeof(PurchaseInvoiceItem).GetProperty(nameof(PurchaseInvoiceItem.UnitCost)),
            typeof(PurchaseInvoiceItem).GetProperty(nameof(PurchaseInvoiceItem.LineTotal))
        };

        foreach (var property in unitPriceEntities.Where(p => p != null))
        {
            modelBuilder.Entity(property!.DeclaringType!)
                .Property(property.Name)
                .HasPrecision(18, 2);
        }
    }

    /// <summary>
    /// تكوين العلاقات لتجنّب Cascade غير المرغوب
    /// </summary>
    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // المنتج ← الفئة
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // المنتج ← الوحدة الأساسية
        modelBuilder.Entity<Product>()
            .HasOne(p => p.MainUnit)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.MainUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // وحدات المنتج
        modelBuilder.Entity<ProductUnit>()
            .HasOne(pu => pu.Product)
            .WithMany(p => p.ProductUnits)
            .HasForeignKey(pu => pu.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductUnit>()
            .HasOne(pu => pu.Unit)
            .WithMany(u => u.ProductUnits)
            .HasForeignKey(pu => pu.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // فواتير البيع
        modelBuilder.Entity<SalesInvoice>()
            .HasOne(si => si.Customer)
            .WithMany(c => c.SalesInvoices)
            .HasForeignKey(si => si.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesInvoiceItem>()
            .HasOne(sid => sid.SalesInvoice)
            .WithMany(si => si.Items)
            .HasForeignKey(sid => sid.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SalesInvoiceItem>()
            .HasOne(sid => sid.Product)
            .WithMany(p => p.SalesInvoiceItems)
            .HasForeignKey(sid => sid.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // فواتير الشراء
        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(pi => pi.Supplier)
            .WithMany(s => s.PurchaseInvoices)
            .HasForeignKey(pi => pi.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoiceItem>()
            .HasOne(pid => pid.PurchaseInvoice)
            .WithMany(pi => pi.Items)
            .HasForeignKey(pid => pid.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseInvoiceItem>()
            .HasOne(pid => pid.Product)
            .WithMany(p => p.PurchaseInvoiceItems)
            .HasForeignKey(pid => pid.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // حركات المخزون
        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.Product)
            .WithMany(p => p.StockMovements)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // المعاملات المالية
        modelBuilder.Entity<CashTransaction>()
            .HasOne(ct => ct.CashBox)
            .WithMany(cb => cb.CashTransactions)
            .HasForeignKey(ct => ct.CashBoxId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerTransaction>()
            .HasOne(ct => ct.Customer)
            .WithMany(c => c.CustomerTransactions)
            .HasForeignKey(ct => ct.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupplierTransaction>()
            .HasOne(st => st.Supplier)
            .WithMany(s => s.SupplierTransactions)
            .HasForeignKey(st => st.SupplierId)
            .OnDelete(DeleteBehavior.Cascade);

        // CustomReport - معطل مؤقتاً لحل مشاكل FK
        // modelBuilder.Entity<CustomReport>()
        //     .HasOne(cr => cr.Creator)
        //     .WithMany()
        //     .HasForeignKey(cr => cr.CreatedBy)
        //     .HasPrincipalKey(u => u.UserName)
        //     .OnDelete(DeleteBehavior.SetNull);

        // علاقات الكاشير ونقطة البيع
        modelBuilder.Entity<CashierSession>()
            .HasOne(cs => cs.Cashier)
            .WithMany(c => c.Sessions)
            .HasForeignKey(cs => cs.CashierId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<POSTransaction>()
            .HasOne(pt => pt.Cashier)
            .WithMany(c => c.Transactions)
            .HasForeignKey(pt => pt.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<POSTransaction>()
            .HasOne(pt => pt.Session)
            .WithMany(cs => cs.Transactions)
            .HasForeignKey(pt => pt.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<POSTransactionItem>()
            .HasOne(pti => pti.Transaction)
            .WithMany(pt => pt.Items)
            .HasForeignKey(pti => pti.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<POSPayment>()
            .HasOne(pp => pp.Transaction)
            .WithMany(pt => pt.Payments)
            .HasForeignKey(pp => pp.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CashDrawerOperation>()
            .HasOne(cdo => cdo.Cashier)
            .WithMany()
            .HasForeignKey(cdo => cdo.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CashDrawerOperation>()
            .HasOne(cdo => cdo.Session)
            .WithMany(cs => cs.CashDrawerOperations)
            .HasForeignKey(cdo => cdo.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CustomerLoyaltyPoint>()
            .HasOne(clp => clp.Customer)
            .WithMany()
            .HasForeignKey(clp => clp.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerLoyaltyPoint>()
            .HasOne(clp => clp.LoyaltyProgram)
            .WithMany(lp => lp.CustomerPoints)
            .HasForeignKey(clp => clp.LoyaltyProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoyaltyTransaction>()
            .HasOne(lt => lt.CustomerLoyaltyPoint)
            .WithMany(clp => clp.Transactions)
            .HasForeignKey(lt => lt.CustomerLoyaltyPointId)
            .OnDelete(DeleteBehavior.Cascade);

    }

    /// <summary>
    /// تسمية الجداول
    /// </summary>
    private static void ConfigureTableNames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Supplier>().ToTable("Suppliers");
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Unit>().ToTable("Units");
        modelBuilder.Entity<Product>().ToTable("Products");
        modelBuilder.Entity<ProductUnit>().ToTable("ProductUnits");
        modelBuilder.Entity<SalesInvoice>().ToTable("SalesInvoices");
        modelBuilder.Entity<SalesInvoiceItem>().ToTable("SalesInvoiceItems");
        modelBuilder.Entity<PurchaseInvoice>().ToTable("PurchaseInvoices");
        modelBuilder.Entity<PurchaseInvoiceItem>().ToTable("PurchaseInvoiceItems");

        modelBuilder.Entity<StockMovement>().ToTable("StockMovements");
        modelBuilder.Entity<CashBox>().ToTable("CashBoxes");
        modelBuilder.Entity<CashTransaction>().ToTable("CashTransactions");
        modelBuilder.Entity<CustomerTransaction>().ToTable("CustomerTransactions");
        modelBuilder.Entity<SupplierTransaction>().ToTable("SupplierTransactions");
        modelBuilder.Entity<SystemSettings>().ToTable("SystemSettings");
        modelBuilder.Entity<NumberSequence>().ToTable("NumberSequences");
        modelBuilder.Entity<User>().ToTable("Users");
        
        // جداول الكاشير ونقطة البيع
        modelBuilder.Entity<Cashier>().ToTable("Cashiers");
        modelBuilder.Entity<CashierSession>().ToTable("CashierSessions");
        modelBuilder.Entity<POSTransaction>().ToTable("POSTransactions");
        modelBuilder.Entity<POSTransactionItem>().ToTable("POSTransactionItems");
        modelBuilder.Entity<POSPayment>().ToTable("POSPayments");
        modelBuilder.Entity<CashDrawerOperation>().ToTable("CashDrawerOperations");
        
        // جداول الخصومات ونقاط الولاء
        modelBuilder.Entity<Discount>().ToTable("Discounts");
        modelBuilder.Entity<LoyaltyProgram>().ToTable("LoyaltyPrograms");
        modelBuilder.Entity<CustomerLoyaltyPoint>().ToTable("CustomerLoyaltyPoints");
        modelBuilder.Entity<LoyaltyTransaction>().ToTable("LoyaltyTransactions");
    }

    /// <summary>
    /// قيود تحقق (CHECK) على المستوى القاعدي
    /// </summary>
    private static void ConfigureConstraints(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint("CK_Product_Prices", "[PurchasePrice] >= 0 AND [SalePrice] >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Product_Stock", "[CurrentStock] >= 0 AND [MinimumStock] >= 0"));
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint("CK_Customer_Name", "LEN([CustomerName]) > 0"));
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint("CK_Supplier_Name", "LEN([SupplierName]) > 0"));
        });

        modelBuilder.Entity<CashBox>(entity =>
        {
            entity.ToTable(t => t.HasCheckConstraint("CK_CashBox_Balance", "[CurrentBalance] >= 0"));
        });
    }

    /// <summary>
    /// تكوين جداول سجلات الأخطاء
    /// </summary>
    private static void ConfigureErrorLogging(ModelBuilder modelBuilder)
    {
        // تكوين جدول ErrorLog
        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.ToTable("ErrorLogs");
            
            // الفهارس للأداء
            entity.HasIndex(e => e.ErrorId).IsUnique();
            entity.HasIndex(e => e.ErrorType);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.CreatedAt, e.ErrorType });
            entity.HasIndex(e => new { e.Status, e.Severity });

            // العلاقات
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ResolverUser)
                  .WithMany()
                  .HasForeignKey(e => e.ResolvedBy)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasMany(e => e.ErrorComments)
                  .WithOne(c => c.ErrorLog)
                  .HasForeignKey(c => c.ErrorLogId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // تكوين جدول ErrorLogComment
        modelBuilder.Entity<ErrorLogComment>(entity =>
        {
            entity.ToTable("ErrorLogComments");
            
            // الفهارس
            entity.HasIndex(e => e.ErrorLogId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);

            // العلاقات
            entity.HasOne(c => c.User)
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);

        SeedUnits(modelBuilder);
        SeedCategories(modelBuilder);
        SeedSystemSettings(modelBuilder);
        SeedNumberSequences(modelBuilder);
        SeedCashBoxes(modelBuilder, baseDate);
        SeedUsers(modelBuilder, baseDate);      // ← محدث لـ PBKDF2
        SeedSampleData(modelBuilder, baseDate);
    }

    private static void SeedUnits(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Unit>().HasData(
            new Unit { UnitId = 1, UnitName = "قطعة", UnitSymbol = "قطعة", IsActive = true },
            new Unit { UnitId = 2, UnitName = "كيلو جرام", UnitSymbol = "كجم", IsActive = true },
            new Unit { UnitId = 3, UnitName = "لتر", UnitSymbol = "لتر", IsActive = true },
            new Unit { UnitId = 4, UnitName = "متر", UnitSymbol = "م", IsActive = true },
            new Unit { UnitId = 5, UnitName = "علبة", UnitSymbol = "علبة", IsActive = true },
            new Unit { UnitId = 6, UnitName = "كرتونة", UnitSymbol = "كرتونة", IsActive = true },
            new Unit { UnitId = 7, UnitName = "جرام", UnitSymbol = "جم", IsActive = true },
            new Unit { UnitId = 8, UnitName = "طن", UnitSymbol = "طن", IsActive = true }
        );
    }

    private static void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, CategoryName = "عام", Description = "فئة عامة لجميع المنتجات", IsActive = true },
            new Category { CategoryId = 2, CategoryName = "مواد غذائية", Description = "المواد الغذائية والمشروبات والبقالة", IsActive = true },
            new Category { CategoryId = 3, CategoryName = "مواد تنظيف", Description = "مواد التنظيف والمطهرات", IsActive = true },
            new Category { CategoryId = 4, CategoryName = "أدوات منزلية", Description = "الأدوات والأجهزة المنزلية", IsActive = true },
            new Category { CategoryId = 5, CategoryName = "أجهزة إلكترونية", Description = "الأجهزة الإلكترونية والكهربائية", IsActive = true },
            new Category { CategoryId = 6, CategoryName = "ملابس", Description = "الملابس والأزياء", IsActive = true },
            new Category { CategoryId = 7, CategoryName = "أدوية", Description = "الأدوية والمستحضرات الطبية", IsActive = true },
            new Category { CategoryId = 8, CategoryName = "مستحضرات تجميل", Description = "مستحضرات التجميل والعناية الشخصية", IsActive = true }
        );
    }

    private static void SeedSystemSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemSettings>().HasData(
            new SystemSettings { SettingId = 1, SettingKey = "CompanyName", SettingValue = "النظام المحاسبي الشامل", Description = "اسم الشركة الرسمي" },
            new SystemSettings { SettingId = 2, SettingKey = "CompanyAddress", SettingValue = "جمهورية مصر العربية", Description = "عنوان الشركة" },
            new SystemSettings { SettingId = 3, SettingKey = "CompanyPhone", SettingValue = "+20-000-000-0000", Description = "هاتف الشركة" },
            new SystemSettings { SettingId = 4, SettingKey = "CompanyEmail", SettingValue = "info@company.com", Description = "بريد الشركة الإلكتروني" },

            new SystemSettings { SettingId = 5, SettingKey = "TaxRate", SettingValue = "14", Description = "نسبة ضريبة القيمة المضافة %" },
            new SystemSettings { SettingId = 6, SettingKey = "TaxNumber", SettingValue = "123-456-789", Description = "الرقم الضريبي للشركة" },

            new SystemSettings { SettingId = 7, SettingKey = "Currency", SettingValue = "ج.م", Description = "العملة الأساسية - الجنيه المصري" },
            new SystemSettings { SettingId = 8, SettingKey = "CurrencyName", SettingValue = "الجنيه المصري", Description = "اسم العملة الكامل" },
            new SystemSettings { SettingId = 9, SettingKey = "CurrencySymbol", SettingValue = "ج.م", Description = "رمز العملة المختصر" },
            new SystemSettings { SettingId = 10, SettingKey = "CurrencyFormat", SettingValue = "#,##0.00 'ج.م'", Description = "تنسيق عرض العملة" },

            new SystemSettings { SettingId = 11, SettingKey = "DecimalPlaces", SettingValue = "2", Description = "عدد الخانات العشرية للعملة" },
            new SystemSettings { SettingId = 12, SettingKey = "DateFormat", SettingValue = "dd/MM/yyyy", Description = "تنسيق التاريخ" },
            new SystemSettings { SettingId = 13, SettingKey = "Language", SettingValue = "ar-EG", Description = "لغة النظام" },
            new SystemSettings { SettingId = 14, SettingKey = "BackupPath", SettingValue = "C:\\Backups\\Accounting", Description = "مسار النسخ الاحتياطية" },
            new SystemSettings { SettingId = 15, SettingKey = "AutoBackup", SettingValue = "true", Description = "تفعيل النسخ الاحتياطية التلقائية" }
        );
    }

    private static void SeedNumberSequences(ModelBuilder modelBuilder)
    {
        var currentDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);

        modelBuilder.Entity<NumberSequence>().HasData(
            new NumberSequence { SequenceId = 1, SequenceType = "SalesInvoice", Prefix = "S",  CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate },
            new NumberSequence { SequenceId = 2, SequenceType = "PurchaseInvoice", Prefix = "P", CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate },
            new NumberSequence { SequenceId = 3, SequenceType = "Customer", Prefix = "C", CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate },
            new NumberSequence { SequenceId = 4, SequenceType = "Supplier", Prefix = "SP", CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate },
            new NumberSequence { SequenceId = 5, SequenceType = "Product", Prefix = "PR", CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate },
            new NumberSequence { SequenceId = 6, SequenceType = "CashTransaction", Prefix = "CT", CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate },
            new NumberSequence { SequenceId = 7, SequenceType = "CustomerTransaction", Prefix = "CuT", CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate },
            new NumberSequence { SequenceId = 8, SequenceType = "SupplierTransaction", Prefix = "SuT", CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate },
            new NumberSequence { SequenceId = 9, SequenceType = "StockMovement", Prefix = "SM", CurrentNumber = 0, NumberLength = 6, UpdatedDate = currentDate }
        );
    }

    private static void SeedCashBoxes(ModelBuilder modelBuilder, DateTime baseDate)
    {
        modelBuilder.Entity<CashBox>().HasData(
            new CashBox
            {
                CashBoxId = 1,
                CashBoxName = "الخزنة الرئيسية",
                Description = "الخزنة الرئيسية للشركة - جنيه مصري",
                CurrentBalance = 10000.00m,
                IsActive = true,
                CreatedDate = baseDate
            },
            new CashBox
            {
                CashBoxId = 2,
                CashBoxName = "خزنة الفرع الثاني",
                Description = "خزنة فرعية للعمليات اليومية",
                CurrentBalance = 5000.00m,
                IsActive = true,
                CreatedDate = baseDate
            }
        );
    }

    private static void SeedUsers(ModelBuilder modelBuilder, DateTime baseDate)
    {
        // كلمات مرور تجريبية:
        // admin => admin
        // accountant => admin  
        // الصيغة: PBKDF2:Iterations:SaltBase64:HashBase64
        const string adminHash      = "PBKDF2:120000:c6c26yJQmQpsRPYGDmB5nQ==:V5IsjDNiy2POmseczguUobxGvG3j0EJkMTU1W0zH5PM=";
        const string accountantHash = "PBKDF2:120000:c6c26yJQmQpsRPYGDmB5nQ==:V5IsjDNiy2POmseczguUobxGvG3j0EJkMTU1W0zH5PM=";

        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserId = 1,
                UserName = "admin",
                FullName = "مدير النظام",
                Email = "admin@system.com",
                PasswordHash = adminHash,
                Phone = "01000000000",
                Role = "admin",
                IsActive = true,
                CreatedDate = baseDate
            },
            new User
            {
                UserId = 2,
                UserName = "accountant",
                FullName = "المحاسب الرئيسي",
                Email = "accountant@system.com",
                PasswordHash = accountantHash,
                Phone = "01100000000",
                Role = "accountant",
                IsActive = true,
                CreatedDate = baseDate
            }
        );
    }

    private static void SeedSampleData(ModelBuilder modelBuilder, DateTime baseDate)
    {
        SeedSampleProducts(modelBuilder, baseDate);
        SeedSampleCustomers(modelBuilder, baseDate);
        SeedSampleSuppliers(modelBuilder, baseDate);
        SeedSampleProductUnits(modelBuilder);
        SeedSampleInvoices(modelBuilder, baseDate);
        SeedSampleNotifications(modelBuilder, baseDate);
        SeedSampleStockMovements(modelBuilder, baseDate);
    }

    private static void SeedSampleProducts(ModelBuilder modelBuilder, DateTime baseDate)
    {
        modelBuilder.Entity<Product>().HasData(
            // مواد غذائية أساسية
            new Product { ProductId = 1, ProductCode = "FD001", ProductName = "أرز أبيض مصري", Description = "أرز أبيض من النوع الممتاز - منتج محلي", CategoryId = 2, MainUnitId = 2, CurrentStock = 500, MinimumStock = 50, PurchasePrice = 28.50m, SalePrice = 35.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 2, ProductCode = "FD002", ProductName = "زيت طعام سولنزا", Description = "زيت طعام سولنزا - 1 لتر", CategoryId = 2, MainUnitId = 3, CurrentStock = 200, MinimumStock = 20, PurchasePrice = 48.00m, SalePrice = 58.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 3, ProductCode = "FD003", ProductName = "سكر أبيض مصري", Description = "سكر أبيض ناعم - منتج محلي", CategoryId = 2, MainUnitId = 2, CurrentStock = 300, MinimumStock = 30, PurchasePrice = 22.00m, SalePrice = 28.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 4, ProductCode = "FD004", ProductName = "شاي أحمد", Description = "شاي أحمد إيرل جراي - 100 كيس", CategoryId = 2, MainUnitId = 5, CurrentStock = 150, MinimumStock = 15, PurchasePrice = 42.00m, SalePrice = 52.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 5, ProductCode = "FD005", ProductName = "مكرونة الوادي", Description = "مكرونة الوادي بنة - 500 جرام", CategoryId = 2, MainUnitId = 5, CurrentStock = 400, MinimumStock = 40, PurchasePrice = 8.50m, SalePrice = 12.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 6, ProductCode = "FD006", ProductName = "عدس أحمر", Description = "عدس أحمر مستورد - درجة أولى", CategoryId = 2, MainUnitId = 2, CurrentStock = 180, MinimumStock = 25, PurchasePrice = 35.00m, SalePrice = 42.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 7, ProductCode = "FD007", ProductName = "ملح طعام جوهرة", Description = "ملح طعام مكرر - 1 كيلو", CategoryId = 2, MainUnitId = 5, CurrentStock = 250, MinimumStock = 30, PurchasePrice = 5.00m, SalePrice = 7.50m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 8, ProductCode = "FD008", ProductName = "عسل نحل طبيعي", Description = "عسل نحل طبيعي من المناحل المصرية - 1 كيلو", CategoryId = 2, MainUnitId = 5, CurrentStock = 80, MinimumStock = 10, PurchasePrice = 180.00m, SalePrice = 225.00m, IsActive = true, CreatedDate = baseDate },

            // مواد تنظيف
            new Product { ProductId = 9, ProductCode = "CL001", ProductName = "صابون أريال", Description = "مسحوق غسيل أريال - 3 كيلو", CategoryId = 3, MainUnitId = 5, CurrentStock = 100, MinimumStock = 10, PurchasePrice = 75.00m, SalePrice = 90.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 10, ProductCode = "CL002", ProductName = "منظف فيري", Description = "سائل تنظيف أطباق فيري - 1 لتر", CategoryId = 3, MainUnitId = 5, CurrentStock = 80, MinimumStock = 8, PurchasePrice = 32.00m, SalePrice = 42.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 11, ProductCode = "CL003", ProductName = "معقم ديتول", Description = "معقم ديتول متعدد الاستخدامات - 750 مل", CategoryId = 3, MainUnitId = 5, CurrentStock = 120, MinimumStock = 15, PurchasePrice = 28.00m, SalePrice = 38.00m, IsActive = true, CreatedDate = baseDate },

            // أدوات منزلية
            new Product { ProductId = 12, ProductCode = "HW001", ProductName = "مقلاة تيفال", Description = "مقلاة تيفال غير لاصقة - 26 سم", CategoryId = 4, MainUnitId = 1, CurrentStock = 25, MinimumStock = 5, PurchasePrice = 180.00m, SalePrice = 230.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 13, ProductCode = "HW002", ProductName = "خلاط موليكس", Description = "خلاط موليكس 3 سرعات - 400 واط", CategoryId = 4, MainUnitId = 1, CurrentStock = 15, MinimumStock = 3, PurchasePrice = 420.00m, SalePrice = 520.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 14, ProductCode = "HW003", ProductName = "طقم أكواب زجاجية", Description = "طقم 6 أكواب زجاجية شفافة", CategoryId = 4, MainUnitId = 1, CurrentStock = 35, MinimumStock = 8, PurchasePrice = 85.00m, SalePrice = 120.00m, IsActive = true, CreatedDate = baseDate },

            // أجهزة إلكترونية
            new Product { ProductId = 15, ProductCode = "EL001", ProductName = "تليفزيون سامسونج", Description = "تليفزيون سامسونج LED 43 بوصة", CategoryId = 5, MainUnitId = 1, CurrentStock = 10, MinimumStock = 2, PurchasePrice = 8500.00m, SalePrice = 10200.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 16, ProductCode = "EL002", ProductName = "موبايل سامسونج", Description = "سامسونج جالاكسي A54 - 128 جيجا", CategoryId = 5, MainUnitId = 1, CurrentStock = 20, MinimumStock = 5, PurchasePrice = 12000.00m, SalePrice = 14500.00m, IsActive = true, CreatedDate = baseDate },
            new Product { ProductId = 17, ProductCode = "EL003", ProductName = "لاب توب ديل", Description = "لاب توب ديل Inspiron 15 - Intel i5", CategoryId = 5, MainUnitId = 1, CurrentStock = 8, MinimumStock = 2, PurchasePrice = 18000.00m, SalePrice = 22000.00m, IsActive = true, CreatedDate = baseDate },

            // منتجات إضافية لمحاكاة مخزون كبير
            new Product { ProductId = 18, ProductCode = "FD009", ProductName = "قهوة محمصة", Description = "قهوة عربية محمصة - 250 جرام", CategoryId = 2, MainUnitId = 5, CurrentStock = 5, MinimumStock = 20, PurchasePrice = 45.00m, SalePrice = 60.00m, IsActive = true, CreatedDate = baseDate }, // مخزون منخفض
            new Product { ProductId = 19, ProductCode = "FD010", ProductName = "جبنة رومي", Description = "جبنة رومي مصرية - 1 كيلو", CategoryId = 2, MainUnitId = 2, CurrentStock = 8, MinimumStock = 15, PurchasePrice = 120.00m, SalePrice = 155.00m, IsActive = true, CreatedDate = baseDate }, // مخزون منخفض
            new Product { ProductId = 20, ProductCode = "HW004", ProductName = "ماكينة قهوة", Description = "ماكينة قهوة إسبريسو كهربائية", CategoryId = 4, MainUnitId = 1, CurrentStock = 12, MinimumStock = 3, PurchasePrice = 850.00m, SalePrice = 1100.00m, IsActive = true, CreatedDate = baseDate }
        );
    }

    private static void SeedSampleCustomers(ModelBuilder modelBuilder, DateTime baseDate)
    {
        modelBuilder.Entity<Customer>().HasData(
            new Customer { CustomerId = 1, CustomerName = "أحمد محمد عبد الله", Address = "القاهرة - مدينة نصر - شارع مصطفى النحاس", Phone = "01012345678", Email = "ahmed.mohamed@gmail.com", Balance = 1500.00m, CreatedDate = baseDate, IsActive = true },
            new Customer { CustomerId = 2, CustomerName = "فاطمة علي حسن", Address = "الإسكندرية - سموحة - شارع الحجاز", Phone = "01123456789", Email = "fatma.ali@yahoo.com", Balance = 0.00m, CreatedDate = baseDate, IsActive = true },
            new Customer { CustomerId = 3, CustomerName = "محمد حسن إبراهيم", Address = "الجيزة - الدقي - شارع النيل", Phone = "01234567890", Email = "mohamed.hassan@hotmail.com", Balance = -850.00m, CreatedDate = baseDate, IsActive = true },
            new Customer { CustomerId = 4, CustomerName = "مريم أحمد سالم", Address = "القاهرة - المعادي - كورنيش النيل", Phone = "01098765432", Email = "maryam.ahmed@gmail.com", Balance = 2200.00m, CreatedDate = baseDate, IsActive = true },
            new Customer { CustomerId = 5, CustomerName = "عمر محمود طه", Address = "الإسكندرية - العجمي - طريق الساحل الشمالي", Phone = "01156789012", Email = "omar.mahmoud@live.com", Balance = 0.00m, CreatedDate = baseDate, IsActive = true },
            new Customer { CustomerId = 6, CustomerName = "نور الدين عبد الرحمن", Address = "الجيزة - الهرم - شارع الهرم الرئيسي", Phone = "01267890123", Email = "nour.eldin@outlook.com", Balance = 650.00m, CreatedDate = baseDate, IsActive = true }
        );
    }

    private static void SeedSampleSuppliers(ModelBuilder modelBuilder, DateTime baseDate)
    {
        modelBuilder.Entity<Supplier>().HasData(
            new Supplier { SupplierId = 1, SupplierName = "شركة الأهرام للمواد الغذائية", Address = "القاهرة - العبور - المنطقة الصناعية الأولى", Phone = "0223456789", Email = "info@ahram-foods.com", Balance = 15000.00m, CreatedDate = baseDate, IsActive = true },
            new Supplier { SupplierId = 2, SupplierName = "مؤسسة النيل للتجارة والاستيراد", Address = "الإسكندرية - الورديان - المنطقة الحرة", Phone = "0334567890", Email = "orders@nile-trading.com", Balance = 8500.00m, CreatedDate = baseDate, IsActive = true },
            new Supplier { SupplierId = 3, SupplierName = "شركة الدلتا للأجهزة المنزلية", Address = "طنطا - المنطقة الصناعية - شارع المصنع", Phone = "0405678901", Email = "sales@delta-appliances.com", Balance = 22000.00m, CreatedDate = baseDate, IsActive = true },
            new Supplier { SupplierId = 4, SupplierName = "مجموعة الشرق الأوسط للإلكترونيات", Address = "القاهرة - مدينة العبور - المنطقة الصناعية الثالثة", Phone = "0226789012", Email = "contact@mideast-electronics.com", Balance = 45000.00m, CreatedDate = baseDate, IsActive = true },
            new Supplier { SupplierId = 5, SupplierName = "شركة المصرية لمواد التنظيف", Address = "الإسكندرية - برج العرب الجديدة - القطعة الصناعية 15", Phone = "0337890123", Email = "orders@egyptian-cleaning.com", Balance = 6800.00m, CreatedDate = baseDate, IsActive = true }
        );
    }

    private static void SeedSampleProductUnits(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductUnit>().HasData(
            // وحدات بديلة للأرز
            new ProductUnit { ProductUnitId = 1, ProductId = 1, UnitId = 5, ConversionFactor = 25.0m, IsActive = true },  // كيس 25 كيلو
            new ProductUnit { ProductUnitId = 2, ProductId = 1, UnitId = 6, ConversionFactor = 1000.0m, IsActive = true }, // كرتونة 1000 كيلو

            // وحدات بديلة للسكر
            new ProductUnit { ProductUnitId = 3, ProductId = 3, UnitId = 5, ConversionFactor = 50.0m, IsActive = true },  // كيس 50 كيلو
            new ProductUnit { ProductUnitId = 4, ProductId = 3, UnitId = 7, ConversionFactor = 0.001m, IsActive = true }, // بالجرام

            // وحدات بديلة لصابون الغسيل
            new ProductUnit { ProductUnitId = 5, ProductId = 5, UnitId = 6, ConversionFactor = 12.0m, IsActive = true },  // كرتونة 12 علبة
            new ProductUnit { ProductUnitId = 6, ProductId = 5, UnitId = 2, ConversionFactor = 3.0m, IsActive = true }    // بالكيلو
        );
    }

    private static void SeedSampleInvoices(ModelBuilder modelBuilder, DateTime baseDate)
    {
        // إضافة فواتير بيع متنوعة عبر الشهر الماضي
        modelBuilder.Entity<SalesInvoice>().HasData(
            // فواتير الأسبوع الأول
            new SalesInvoice 
            { 
                SalesInvoiceId = 1, 
                InvoiceNumber = "S-2024-001", 
                CustomerId = 1, 
                InvoiceDate = baseDate.AddDays(-25), 
                SubTotal = 500.00m, 
                TaxAmount = 75.00m, 
                DiscountAmount = 25.00m, 
                TotalAmount = 550.00m,
                Status = InvoiceStatus.Confirmed, 
                CreatedDate = baseDate.AddDays(-25) 
            },
            new SalesInvoice 
            { 
                SalesInvoiceId = 2, 
                InvoiceNumber = "S-2024-002", 
                CustomerId = 2, 
                InvoiceDate = baseDate.AddDays(-23), 
                SubTotal = 1200.00m, 
                TaxAmount = 180.00m, 
                DiscountAmount = 0.00m, 
                TotalAmount = 1380.00m,
                Status = InvoiceStatus.Confirmed, 
                CreatedDate = baseDate.AddDays(-23) 
            },
            
            // فواتير الأسبوع الثاني
            new SalesInvoice 
            { 
                SalesInvoiceId = 3, 
                InvoiceNumber = "S-2024-003", 
                CustomerId = 3, 
                InvoiceDate = baseDate.AddDays(-18), 
                SubTotal = 750.00m, 
                TaxAmount = 112.50m, 
                DiscountAmount = 50.00m, 
                TotalAmount = 812.50m,
                Status = InvoiceStatus.Confirmed, 
                CreatedDate = baseDate.AddDays(-18) 
            },
            new SalesInvoice 
            { 
                SalesInvoiceId = 4, 
                InvoiceNumber = "S-2024-004", 
                CustomerId = 4, 
                InvoiceDate = baseDate.AddDays(-15), 
                SubTotal = 2200.00m, 
                TaxAmount = 330.00m, 
                DiscountAmount = 100.00m, 
                TotalAmount = 2430.00m,
                Status = InvoiceStatus.Confirmed, 
                CreatedDate = baseDate.AddDays(-15) 
            },
            
            // فواتير الأسبوع الثالث
            new SalesInvoice 
            { 
                SalesInvoiceId = 5, 
                InvoiceNumber = "S-2024-005", 
                CustomerId = 5, 
                InvoiceDate = baseDate.AddDays(-12), 
                SubTotal = 850.00m, 
                TaxAmount = 127.50m, 
                DiscountAmount = 30.00m, 
                TotalAmount = 947.50m,
                Status = InvoiceStatus.Confirmed, 
                CreatedDate = baseDate.AddDays(-12) 
            },
            new SalesInvoice 
            { 
                SalesInvoiceId = 6, 
                InvoiceNumber = "S-2024-006", 
                CustomerId = 6, 
                InvoiceDate = baseDate.AddDays(-10), 
                SubTotal = 3500.00m, 
                TaxAmount = 525.00m, 
                DiscountAmount = 200.00m, 
                TotalAmount = 3825.00m,
                Status = InvoiceStatus.Confirmed, 
                CreatedDate = baseDate.AddDays(-10) 
            },
            
            // فواتير الأسبوع الحالي
            new SalesInvoice 
            { 
                SalesInvoiceId = 7, 
                InvoiceNumber = "S-2024-007", 
                CustomerId = 1, 
                InvoiceDate = baseDate.AddDays(-3), 
                SubTotal = 420.00m, 
                TaxAmount = 63.00m, 
                DiscountAmount = 0.00m, 
                TotalAmount = 483.00m,
                Status = InvoiceStatus.Confirmed, 
                CreatedDate = baseDate.AddDays(-3) 
            },
            new SalesInvoice 
            { 
                SalesInvoiceId = 8, 
                InvoiceNumber = "S-2024-008", 
                CustomerId = 2, 
                InvoiceDate = baseDate.AddDays(-1), 
                SubTotal = 18500.00m, 
                TaxAmount = 2775.00m, 
                DiscountAmount = 500.00m, 
                TotalAmount = 20775.00m,
                Status = InvoiceStatus.Draft, 
                CreatedDate = baseDate.AddDays(-1) 
            }
        );

        // إضافة أصناف الفواتير بتفصيل أكبر
        modelBuilder.Entity<SalesInvoiceItem>().HasData(
            // فاتورة 1 - مواد غذائية متنوعة
            new SalesInvoiceItem { SalesInvoiceItemId = 1, SalesInvoiceId = 1, ProductId = 1, Quantity = 5.0m, UnitPrice = 35.00m, DiscountAmount = 10.00m, LineTotal = 165.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 2, SalesInvoiceId = 1, ProductId = 3, Quantity = 3.0m, UnitPrice = 28.00m, DiscountAmount = 0.00m, LineTotal = 84.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 3, SalesInvoiceId = 1, ProductId = 4, Quantity = 2.0m, UnitPrice = 52.00m, DiscountAmount = 4.00m, LineTotal = 100.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 4, SalesInvoiceId = 1, ProductId = 7, Quantity = 20.0m, UnitPrice = 7.50m, DiscountAmount = 0.00m, LineTotal = 150.00m },

            // فاتورة 2 - خليط من المواد الغذائية والتنظيف
            new SalesInvoiceItem { SalesInvoiceItemId = 5, SalesInvoiceId = 2, ProductId = 2, Quantity = 8.0m, UnitPrice = 58.00m, DiscountAmount = 0.00m, LineTotal = 464.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 6, SalesInvoiceId = 2, ProductId = 5, Quantity = 10.0m, UnitPrice = 12.00m, DiscountAmount = 0.00m, LineTotal = 120.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 7, SalesInvoiceId = 2, ProductId = 9, Quantity = 4.0m, UnitPrice = 90.00m, DiscountAmount = 0.00m, LineTotal = 360.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 8, SalesInvoiceId = 2, ProductId = 8, Quantity = 1.0m, UnitPrice = 225.00m, DiscountAmount = 0.00m, LineTotal = 225.00m },

            // فاتورة 3 - مواد تنظيف ومنزلية
            new SalesInvoiceItem { SalesInvoiceItemId = 9, SalesInvoiceId = 3, ProductId = 10, Quantity = 5.0m, UnitPrice = 42.00m, DiscountAmount = 10.00m, LineTotal = 200.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 10, SalesInvoiceId = 3, ProductId = 11, Quantity = 8.0m, UnitPrice = 38.00m, DiscountAmount = 0.00m, LineTotal = 304.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 11, SalesInvoiceId = 3, ProductId = 14, Quantity = 2.0m, UnitPrice = 120.00m, DiscountAmount = 0.00m, LineTotal = 240.00m },

            // فاتورة 4 - أجهزة منزلية
            new SalesInvoiceItem { SalesInvoiceItemId = 12, SalesInvoiceId = 4, ProductId = 12, Quantity = 3.0m, UnitPrice = 230.00m, DiscountAmount = 30.00m, LineTotal = 660.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 13, SalesInvoiceId = 4, ProductId = 13, Quantity = 2.0m, UnitPrice = 520.00m, DiscountAmount = 20.00m, LineTotal = 1020.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 14, SalesInvoiceId = 4, ProductId = 20, Quantity = 1.0m, UnitPrice = 1100.00m, DiscountAmount = 50.00m, LineTotal = 1050.00m },

            // فاتورة 5 - مواد غذائية متنوعة
            new SalesInvoiceItem { SalesInvoiceItemId = 15, SalesInvoiceId = 5, ProductId = 6, Quantity = 4.0m, UnitPrice = 42.00m, DiscountAmount = 8.00m, LineTotal = 160.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 16, SalesInvoiceId = 5, ProductId = 18, Quantity = 5.0m, UnitPrice = 60.00m, DiscountAmount = 0.00m, LineTotal = 300.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 17, SalesInvoiceId = 5, ProductId = 19, Quantity = 2.0m, UnitPrice = 155.00m, DiscountAmount = 10.00m, LineTotal = 300.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 18, SalesInvoiceId = 5, ProductId = 1, Quantity = 3.0m, UnitPrice = 35.00m, DiscountAmount = 5.00m, LineTotal = 100.00m },

            // فاتورة 6 - أجهزة إلكترونية
            new SalesInvoiceItem { SalesInvoiceItemId = 19, SalesInvoiceId = 6, ProductId = 15, Quantity = 1.0m, UnitPrice = 10200.00m, DiscountAmount = 200.00m, LineTotal = 10000.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 20, SalesInvoiceId = 6, ProductId = 16, Quantity = 2.0m, UnitPrice = 14500.00m, DiscountAmount = 0.00m, LineTotal = 29000.00m },

            // فاتورة 7 - مواد أساسية
            new SalesInvoiceItem { SalesInvoiceItemId = 21, SalesInvoiceId = 7, ProductId = 1, Quantity = 2.0m, UnitPrice = 35.00m, DiscountAmount = 0.00m, LineTotal = 70.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 22, SalesInvoiceId = 7, ProductId = 3, Quantity = 5.0m, UnitPrice = 28.00m, DiscountAmount = 0.00m, LineTotal = 140.00m },
            new SalesInvoiceItem { SalesInvoiceItemId = 23, SalesInvoiceId = 7, ProductId = 4, Quantity = 4.0m, UnitPrice = 52.00m, DiscountAmount = 0.00m, LineTotal = 208.00m },

            // فاتورة 8 - أجهزة إلكترونية (مسودة)
            new SalesInvoiceItem { SalesInvoiceItemId = 24, SalesInvoiceId = 8, ProductId = 17, Quantity = 1.0m, UnitPrice = 22000.00m, DiscountAmount = 500.00m, LineTotal = 21500.00m }
        );
    }

    private static void SeedSampleNotifications(ModelBuilder modelBuilder, DateTime baseDate)
    {
        modelBuilder.Entity<Notification>().HasData(
            new Notification 
            { 
                NotificationId = 1, 
                Title = "تحذير: نفاد مخزون", 
                Message = "مخزون المنتج 'قهوة محمصة' أصبح أقل من الحد الأدنى (5 من أصل 20)", 
                Type = NotificationType.LowStock, 
                Priority = NotificationPriority.High,
                Status = NotificationStatus.Unread,
                ReferenceType = "Product",
                ReferenceId = 18,
                CreatedDate = baseDate.AddHours(-2) 
            },
            new Notification 
            { 
                NotificationId = 2, 
                Title = "تحذير: نفاد مخزون", 
                Message = "مخزون المنتج 'جبنة رومي' أصبح أقل من الحد الأدنى (8 من أصل 15)", 
                Type = NotificationType.LowStock, 
                Priority = NotificationPriority.Medium,
                Status = NotificationStatus.Unread,
                ReferenceType = "Product",
                ReferenceId = 19,
                CreatedDate = baseDate.AddHours(-5) 
            },
            new Notification 
            { 
                NotificationId = 3, 
                Title = "فاتورة جديدة", 
                Message = "تم إنشاء فاتورة مبيعات جديدة S-2024-008 بقيمة 20,775 جنيه", 
                Type = NotificationType.SalesTarget, 
                Priority = NotificationPriority.Medium,
                Status = NotificationStatus.Read,
                ReferenceType = "SalesInvoice",
                ReferenceId = 8,
                CreatedDate = baseDate.AddDays(-1), 
                ReadDate = baseDate.AddHours(-1) 
            },
            new Notification 
            { 
                NotificationId = 4, 
                Title = "دفعة مستحقة", 
                Message = "يوجد مبلغ مستحق للمورد 'شركة الأهرام للمواد الغذائية' قدره 15,000 جنيه", 
                Type = NotificationType.SupplierPayment, 
                Priority = NotificationPriority.High,
                Status = NotificationStatus.Acknowledged,
                ReferenceType = "Supplier",
                ReferenceId = 1,
                CreatedDate = baseDate.AddDays(-7) 
            },
            new Notification 
            { 
                NotificationId = 5, 
                Title = "هدف المبيعات", 
                Message = "تم تحقيق 85% من هدف مبيعات الشهر الحالي. المطلوب: 50,000 ج.م، المُحقق: 42,500 ج.م", 
                Type = NotificationType.SalesTarget, 
                Priority = NotificationPriority.Low,
                Status = NotificationStatus.Read,
                CreatedDate = baseDate.AddDays(-3), 
                ReadDate = baseDate.AddDays(-2) 
            },
            new Notification 
            { 
                NotificationId = 6, 
                Title = "نشاط المستخدم", 
                Message = "المستخدم 'admin' قام بتسجيل الدخول من عنوان IP جديد", 
                Type = NotificationType.UserActivity, 
                Priority = NotificationPriority.Low,
                Status = NotificationStatus.Dismissed,
                CreatedDate = baseDate.AddDays(-1) 
            }
        );
    }

    private static void SeedSampleStockMovements(ModelBuilder modelBuilder, DateTime baseDate)
    {
        modelBuilder.Entity<StockMovement>().HasData(
            // حركات بيع (خروج)
            new StockMovement 
            { 
                StockMovementId = 1, 
                ProductId = 1, 
                MovementType = StockMovementType.Out, 
                Quantity = -5.0m, 
                UnitCost = 28.50m,  // سعر التكلفة
                ReferenceNumber = "S-2024-001",
                ReferenceType = "SalesInvoice",
                ReferenceId = 1,
                MovementDate = baseDate.AddDays(-25),
                Notes = "بيع فاتورة S-2024-001" 
            },
            new StockMovement 
            { 
                StockMovementId = 2, 
                ProductId = 2, 
                MovementType = StockMovementType.Out, 
                Quantity = -8.0m, 
                UnitCost = 48.00m,
                ReferenceNumber = "S-2024-002",
                ReferenceType = "SalesInvoice",
                ReferenceId = 2,
                MovementDate = baseDate.AddDays(-23),
                Notes = "بيع فاتورة S-2024-002" 
            },
            new StockMovement 
            { 
                StockMovementId = 3, 
                ProductId = 15, 
                MovementType = StockMovementType.Out, 
                Quantity = -1.0m, 
                UnitCost = 8500.00m,
                ReferenceNumber = "S-2024-006",
                ReferenceType = "SalesInvoice",
                ReferenceId = 6,
                MovementDate = baseDate.AddDays(-10),
                Notes = "بيع تليفزيون سامسونج" 
            },
            
            // حركات شراء (دخول)
            new StockMovement 
            { 
                StockMovementId = 4, 
                ProductId = 18, 
                MovementType = StockMovementType.In, 
                Quantity = 50.0m, 
                UnitCost = 45.00m,
                ReferenceNumber = "P-2024-001",
                ReferenceType = "Purchase",
                MovementDate = baseDate.AddDays(-30),
                Notes = "شراء قهوة محمصة من المورد" 
            },
            new StockMovement 
            { 
                StockMovementId = 5, 
                ProductId = 18, 
                MovementType = StockMovementType.Out, 
                Quantity = -45.0m, 
                UnitCost = 45.00m,
                ReferenceNumber = "Multiple Sales",
                ReferenceType = "Sales",
                MovementDate = baseDate.AddDays(-10),
                Notes = "مبيعات متعددة خلال الشهر" 
            },
            
            // حركات تسوية المخزون
            new StockMovement 
            { 
                StockMovementId = 6, 
                ProductId = 19, 
                MovementType = StockMovementType.Adjustment, 
                Quantity = -2.0m, 
                UnitCost = 120.00m,
                ReferenceNumber = "ADJ-001",
                ReferenceType = "Adjustment",
                MovementDate = baseDate.AddDays(-5),
                Notes = "تسوية: جبنة منتهية الصلاحية" 
            }
        );
    }

    #endregion
}
