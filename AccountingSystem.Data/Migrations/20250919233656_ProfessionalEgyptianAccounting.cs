using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfessionalEgyptianAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashBoxes",
                columns: table => new
                {
                    CashBoxId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashBoxName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashBoxes", x => x.CashBoxId);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "NumberSequences",
                columns: table => new
                {
                    SequenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SequenceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CurrentNumber = table.Column<int>(type: "int", nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NumberLength = table.Column<int>(type: "int", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequences", x => x.SequenceId);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierId);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    SettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    UnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitSymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.UnitId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "CashTransactions",
                columns: table => new
                {
                    CashTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CashBoxId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashTransactions", x => x.CashTransactionId);
                    table.ForeignKey(
                        name: "FK_CashTransactions_CashBoxes_CashBoxId",
                        column: x => x.CashBoxId,
                        principalTable: "CashBoxes",
                        principalColumn: "CashBoxId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerTransactions",
                columns: table => new
                {
                    CustomerTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerTransactions", x => x.CustomerTransactionId);
                    table.ForeignKey(
                        name: "FK_CustomerTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                columns: table => new
                {
                    SalesInvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.SalesInvoiceId);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoices",
                columns: table => new
                {
                    PurchaseInvoiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseInvoices", x => x.PurchaseInvoiceId);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierTransactions",
                columns: table => new
                {
                    SupplierTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierTransactions", x => x.SupplierTransactionId);
                    table.ForeignKey(
                        name: "FK_SupplierTransactions_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "SupplierId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    MainUnitId = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Units_MainUnitId",
                        column: x => x.MainUnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductUnits",
                columns: table => new
                {
                    ProductUnitId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    ConversionFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnits", x => x.ProductUnitId);
                    table.ForeignKey(
                        name: "FK_ProductUnits_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductUnits_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoiceDetails",
                columns: table => new
                {
                    PurchaseInvoiceDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseInvoiceDetails", x => x.PurchaseInvoiceDetailId);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceDetails_PurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "PurchaseInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceDetails_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceDetails",
                columns: table => new
                {
                    SalesInvoiceDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesInvoiceId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceDetails", x => x.SalesInvoiceDetailId);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceDetails_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "SalesInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceDetails_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    StockMovementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitId = table.Column<int>(type: "int", nullable: false),
                    QuantityInMainUnit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.StockMovementId);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "UnitId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CashBoxes",
                columns: new[] { "CashBoxId", "CashBoxName", "CreatedDate", "CurrentBalance", "Description", "IsActive" },
                values: new object[,]
                {
                    { 1, "الخزنة الرئيسية", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 10000.00m, "الخزنة الرئيسية للشركة - جنيه مصري", true },
                    { 2, "خزنة الفرع الثاني", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 5000.00m, "خزنة فرعية للعمليات اليومية", true }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "Description", "IsActive" },
                values: new object[,]
                {
                    { 1, "عام", "فئة عامة لجميع المنتجات", true },
                    { 2, "مواد غذائية", "المواد الغذائية والمشروبات والبقالة", true },
                    { 3, "مواد تنظيف", "مواد التنظيف والمطهرات", true },
                    { 4, "أدوات منزلية", "الأدوات والأجهزة المنزلية", true },
                    { 5, "أجهزة إلكترونية", "الأجهزة الإلكترونية والكهربائية", true },
                    { 6, "ملابس", "الملابس والأزياء", true },
                    { 7, "أدوية", "الأدوية والمستحضرات الطبية", true },
                    { 8, "مستحضرات تجميل", "مستحضرات التجميل والعناية الشخصية", true }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "CustomerId", "Address", "Balance", "CreatedDate", "CustomerName", "Email", "IsActive", "Phone" },
                values: new object[,]
                {
                    { 1, "القاهرة - مدينة نصر - شارع مصطفى النحاس", 1500.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "أحمد محمد عبد الله", "ahmed.mohamed@gmail.com", true, "01012345678" },
                    { 2, "الإسكندرية - سموحة - شارع الحجاز", 0.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "فاطمة علي حسن", "fatma.ali@yahoo.com", true, "01123456789" },
                    { 3, "الجيزة - الدقي - شارع النيل", -850.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "محمد حسن إبراهيم", "mohamed.hassan@hotmail.com", true, "01234567890" },
                    { 4, "القاهرة - المعادي - كورنيش النيل", 2200.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "مريم أحمد سالم", "maryam.ahmed@gmail.com", true, "01098765432" },
                    { 5, "الإسكندرية - العجمي - طريق الساحل الشمالي", 0.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "عمر محمود طه", "omar.mahmoud@live.com", true, "01156789012" },
                    { 6, "الجيزة - الهرم - شارع الهرم الرئيسي", 650.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "نور الدين عبد الرحمن", "nour.eldin@outlook.com", true, "01267890123" }
                });

            migrationBuilder.InsertData(
                table: "NumberSequences",
                columns: new[] { "SequenceId", "CurrentNumber", "NumberLength", "Prefix", "SequenceType", "Suffix", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, 0, 6, "S", "SalesInvoice", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) },
                    { 2, 0, 6, "P", "PurchaseInvoice", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) },
                    { 3, 0, 6, "C", "Customer", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) },
                    { 4, 0, 6, "SP", "Supplier", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) },
                    { 5, 0, 6, "PR", "Product", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) },
                    { 6, 0, 6, "CT", "CashTransaction", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) },
                    { 7, 0, 6, "CuT", "CustomerTransaction", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) },
                    { 8, 0, 6, "SuT", "SupplierTransaction", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) },
                    { 9, 0, 6, "SM", "StockMovement", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local) }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "SupplierId", "Address", "Balance", "CreatedDate", "Email", "IsActive", "Phone", "SupplierName" },
                values: new object[,]
                {
                    { 1, "القاهرة - العبور - المنطقة الصناعية الأولى", 15000.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "info@ahram-foods.com", true, "0223456789", "شركة الأهرام للمواد الغذائية" },
                    { 2, "الإسكندرية - الورديان - المنطقة الحرة", 8500.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "orders@nile-trading.com", true, "0334567890", "مؤسسة النيل للتجارة والاستيراد" },
                    { 3, "طنطا - المنطقة الصناعية - شارع المصنع", 22000.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "sales@delta-appliances.com", true, "0405678901", "شركة الدلتا للأجهزة المنزلية" },
                    { 4, "القاهرة - مدينة العبور - المنطقة الصناعية الثالثة", 45000.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "contact@mideast-electronics.com", true, "0226789012", "مجموعة الشرق الأوسط للإلكترونيات" },
                    { 5, "الإسكندرية - برج العرب الجديدة - القطعة الصناعية 15", 6800.00m, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "orders@egyptian-cleaning.com", true, "0337890123", "شركة المصرية لمواد التنظيف" }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "SettingId", "Description", "SettingKey", "SettingValue", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, "اسم الشركة الرسمي", "CompanyName", "النظام المحاسبي الشامل", new DateTime(2025, 9, 20, 2, 36, 56, 274, DateTimeKind.Local).AddTicks(4801) },
                    { 2, "عنوان الشركة", "CompanyAddress", "جمهورية مصر العربية", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1732) },
                    { 3, "هاتف الشركة", "CompanyPhone", "+20-000-000-0000", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1752) },
                    { 4, "بريد الشركة الإلكتروني", "CompanyEmail", "info@company.com", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1756) },
                    { 5, "نسبة ضريبة القيمة المضافة %", "TaxRate", "14", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1758) },
                    { 6, "الرقم الضريبي للشركة", "TaxNumber", "123-456-789", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1761) },
                    { 7, "العملة الأساسية - الجنيه المصري", "Currency", "ج.م", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1763) },
                    { 8, "اسم العملة الكامل", "CurrencyName", "الجنيه المصري", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1765) },
                    { 9, "رمز العملة المختصر", "CurrencySymbol", "ج.م", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1767) },
                    { 10, "تنسيق عرض العملة", "CurrencyFormat", "#,##0.00 'ج.م'", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1770) },
                    { 11, "عدد الخانات العشرية للعملة", "DecimalPlaces", "2", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1772) },
                    { 12, "تنسيق التاريخ", "DateFormat", "dd/MM/yyyy", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1774) },
                    { 13, "لغة النظام", "Language", "ar-EG", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1776) },
                    { 14, "مسار النسخ الاحتياطية", "BackupPath", "C:\\Backups\\Accounting", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1777) },
                    { 15, "تفعيل النسخ الاحتياطية التلقائية", "AutoBackup", "true", new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1779) }
                });

            migrationBuilder.InsertData(
                table: "Units",
                columns: new[] { "UnitId", "IsActive", "UnitName", "UnitSymbol" },
                values: new object[,]
                {
                    { 1, true, "قطعة", "قطعة" },
                    { 2, true, "كيلو جرام", "كجم" },
                    { 3, true, "لتر", "لتر" },
                    { 4, true, "متر", "م" },
                    { 5, true, "علبة", "علبة" },
                    { 6, true, "كرتونة", "كرتونة" },
                    { 7, true, "جرام", "جم" },
                    { 8, true, "طن", "طن" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedDate", "Email", "FullName", "IsActive", "LastLoginDate", "PasswordHash", "Phone", "UserName" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "admin@system.com", "مدير النظام", true, null, "AQAAAAEAACcQAAAAEHk8+XhGFz6gw5u8b1k1l1k1l1k1l1k1l1k1l1k1l1k1l1k1l1k1==", "01000000000", "admin" },
                    { 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), "accountant@system.com", "المحاسب الرئيسي", true, null, "AQAAAAEAACcQAAAAEHk8+XhGFz6gw5u8b1k1l1k1l1k1l1k1l1k1l1k1l1k1l1k1l1k1==", "01100000000", "accountant" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "CategoryId", "CreatedDate", "CurrentStock", "Description", "IsActive", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PurchasePrice", "SalePrice" },
                values: new object[,]
                {
                    { 1, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 500m, "أرز أبيض من النوع الممتاز - منتج محلي", true, 2, 50m, "PR000001", "أرز أبيض مصري", 28.50m, 35.00m },
                    { 2, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 200m, "زيت طعام سولنزا - 1 لتر", true, 3, 20m, "PR000002", "زيت طعام سولنزا", 48.00m, 58.00m },
                    { 3, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 300m, "سكر أبيض ناعم - منتج محلي", true, 2, 30m, "PR000003", "سكر أبيض مصري", 22.00m, 28.00m },
                    { 4, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 150m, "شاي أحمد إيرل جراي - 100 كيس", true, 5, 15m, "PR000004", "شاي أحمد", 42.00m, 52.00m },
                    { 5, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 100m, "مسحوق غسيل أريال - 3 كيلو", true, 5, 10m, "PR000005", "صابون أريال", 75.00m, 90.00m },
                    { 6, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 80m, "سائل تنظيف أطباق فيري - 1 لتر", true, 5, 8m, "PR000006", "منظف فيري", 32.00m, 42.00m },
                    { 7, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 25m, "مقلاة تيفال غير لاصقة - 26 سم", true, 1, 5m, "PR000007", "مقلاة تيفال", 180.00m, 230.00m },
                    { 8, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 15m, "خلاط موليكس 3 سرعات - 400 واط", true, 1, 3m, "PR000008", "خلاط موليكس", 420.00m, 520.00m },
                    { 9, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 10m, "تليفزيون سامسونج LED 43 بوصة", true, 1, 2m, "PR000009", "تليفزيون سامسونج", 8500.00m, 10200.00m },
                    { 10, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 20m, "سامسونج جالاكسي A54 - 128 جيجا", true, 1, 5m, "PR000010", "موبايل سامسونج", 12000.00m, 14500.00m }
                });

            migrationBuilder.InsertData(
                table: "ProductUnits",
                columns: new[] { "ProductUnitId", "ConversionFactor", "IsActive", "ProductId", "UnitId" },
                values: new object[,]
                {
                    { 1, 25.0m, true, 1, 5 },
                    { 2, 1000.0m, true, 1, 6 },
                    { 3, 50.0m, true, 3, 5 },
                    { 4, 0.001m, true, 3, 7 },
                    { 5, 12.0m, true, 5, 6 },
                    { 6, 3.0m, true, 5, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashTransaction_Number",
                table: "CashTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CashBoxId",
                table: "CashTransactions",
                column: "CashBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_Category_Name",
                table: "Categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_Name",
                table: "Customers",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransaction_Number",
                table: "CustomerTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerTransactions_CustomerId",
                table: "CustomerTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequence_Type",
                table: "NumberSequences",
                column: "SequenceType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_Code",
                table: "Products",
                column: "ProductCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_MainUnitId",
                table: "Products",
                column: "MainUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_ProductId",
                table: "ProductUnits",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnits_UnitId",
                table: "ProductUnits",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceDetails_ProductId",
                table: "PurchaseInvoiceDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceDetails_PurchaseInvoiceId",
                table: "PurchaseInvoiceDetails",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceDetails_UnitId",
                table: "PurchaseInvoiceDetails",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoice_Number",
                table: "PurchaseInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_SupplierId",
                table: "PurchaseInvoices",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceDetails_ProductId",
                table: "SalesInvoiceDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceDetails_SalesInvoiceId",
                table: "SalesInvoiceDetails",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceDetails_UnitId",
                table: "SalesInvoiceDetails",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoice_Number",
                table: "SalesInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CustomerId",
                table: "SalesInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_UnitId",
                table: "StockMovements",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Name",
                table: "Suppliers",
                column: "SupplierName");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTransaction_Number",
                table: "SupplierTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierTransactions_SupplierId",
                table: "SupplierTransactions",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "SettingKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unit_Name",
                table: "Units",
                column: "UnitName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashTransactions");

            migrationBuilder.DropTable(
                name: "CustomerTransactions");

            migrationBuilder.DropTable(
                name: "NumberSequences");

            migrationBuilder.DropTable(
                name: "ProductUnits");

            migrationBuilder.DropTable(
                name: "PurchaseInvoiceDetails");

            migrationBuilder.DropTable(
                name: "SalesInvoiceDetails");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "SupplierTransactions");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "CashBoxes");

            migrationBuilder.DropTable(
                name: "PurchaseInvoices");

            migrationBuilder.DropTable(
                name: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Units");
        }
    }
}
