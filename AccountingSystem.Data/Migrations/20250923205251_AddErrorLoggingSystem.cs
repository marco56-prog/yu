using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorLoggingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Promotions_PromotionId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_DiscountRules_DiscountRuleDiscountId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Promotions_PromotionId",
                table: "Products");

            // migrationBuilder.DropTable(
            //     name: "CustomReports");  // تم تعطيل هذا لأن الجدول غير موجود

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Users_UserName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_DiscountRuleDiscountId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_PromotionId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Customers_PromotionId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DiscountRuleDiscountId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "IPAddress",
                table: "AuditLogs",
                newName: "IpAddress");

            migrationBuilder.AlterColumn<string>(
                name: "OldValues",
                table: "AuditLogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewValues",
                table: "AuditLogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "AuditLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AuditLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "AuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Cashiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashierCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastLoginTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanOpenCashDrawer = table.Column<bool>(type: "bit", nullable: false),
                    CanProcessReturns = table.Column<bool>(type: "bit", nullable: false),
                    CanApplyDiscounts = table.Column<bool>(type: "bit", nullable: false),
                    CanVoidTransactions = table.Column<bool>(type: "bit", nullable: false),
                    CanViewReports = table.Column<bool>(type: "bit", nullable: false),
                    CanManageInventory = table.Column<bool>(type: "bit", nullable: false),
                    MaxDiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cashiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPromotion",
                columns: table => new
                {
                    ApplicableCustomersCustomerId = table.Column<int>(type: "int", nullable: false),
                    PromotionsPromotionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPromotion", x => new { x.ApplicableCustomersCustomerId, x.PromotionsPromotionId });
                    table.ForeignKey(
                        name: "FK_CustomerPromotion_Customers_ApplicableCustomersCustomerId",
                        column: x => x.ApplicableCustomersCustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerPromotion_Promotions_PromotionsPromotionId",
                        column: x => x.PromotionsPromotionId,
                        principalTable: "Promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscountRuleProduct",
                columns: table => new
                {
                    ApplicableProductsProductId = table.Column<int>(type: "int", nullable: false),
                    DiscountRulesDiscountId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountRuleProduct", x => new { x.ApplicableProductsProductId, x.DiscountRulesDiscountId });
                    table.ForeignKey(
                        name: "FK_DiscountRuleProduct_DiscountRules_DiscountRulesDiscountId",
                        column: x => x.DiscountRulesDiscountId,
                        principalTable: "DiscountRules",
                        principalColumn: "DiscountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountRuleProduct_Products_ApplicableProductsProductId",
                        column: x => x.ApplicableProductsProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiscountType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaximumDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ApplicableOn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    UsageLimit = table.Column<int>(type: "int", nullable: false),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discounts_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId");
                    table.ForeignKey(
                        name: "FK_Discounts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId");
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErrorId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InnerException = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LineNumber = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Activity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<int>(type: "int", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    LastOccurrence = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    IsStarred = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorLogs_Users_ResolvedBy",
                        column: x => x.ResolvedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ErrorLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPrograms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AmountPerPoint = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PointValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumRedemption = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductPromotion",
                columns: table => new
                {
                    ApplicableProductsProductId = table.Column<int>(type: "int", nullable: false),
                    PromotionsPromotionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPromotion", x => new { x.ApplicableProductsProductId, x.PromotionsPromotionId });
                    table.ForeignKey(
                        name: "FK_ProductPromotion_Products_ApplicableProductsProductId",
                        column: x => x.ApplicableProductsProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductPromotion_Promotions_PromotionsPromotionId",
                        column: x => x.PromotionsPromotionId,
                        principalTable: "Promotions",
                        principalColumn: "PromotionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashierSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashierId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpectedClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CashSalesTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CardSalesTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalReturns = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDiscounts = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionsCount = table.Column<int>(type: "int", nullable: false),
                    ReturnsCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashierSessions_Cashiers_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Cashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErrorLogId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorLogComments_ErrorLogs_ErrorLogId",
                        column: x => x.ErrorLogId,
                        principalTable: "ErrorLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ErrorLogComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomerLoyaltyPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    LoyaltyProgramId = table.Column<int>(type: "int", nullable: false),
                    PointsEarned = table.Column<int>(type: "int", nullable: false),
                    PointsRedeemed = table.Column<int>(type: "int", nullable: false),
                    PointsBalance = table.Column<int>(type: "int", nullable: false),
                    LastEarnedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRedeemedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLoyaltyPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerLoyaltyPoints_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerLoyaltyPoints_LoyaltyPrograms_LoyaltyProgramId",
                        column: x => x.LoyaltyProgramId,
                        principalTable: "LoyaltyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashDrawerOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CashierId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    OperationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashDrawerOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashDrawerOperations_CashierSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "CashierSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashDrawerOperations_Cashiers_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Cashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "POSTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CashierId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPrinted = table.Column<bool>(type: "bit", nullable: false),
                    IsVoided = table.Column<bool>(type: "bit", nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_POSTransactions_CashierSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "CashierSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_POSTransactions_Cashiers_CashierId",
                        column: x => x.CashierId,
                        principalTable: "Cashiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_POSTransactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId");
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerLoyaltyPointId = table.Column<int>(type: "int", nullable: false),
                    POSTransactionId = table.Column<int>(type: "int", nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    MonetaryValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_CustomerLoyaltyPoints_CustomerLoyaltyPointId",
                        column: x => x.CustomerLoyaltyPointId,
                        principalTable: "CustomerLoyaltyPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_POSTransactions_POSTransactionId",
                        column: x => x.POSTransactionId,
                        principalTable: "POSTransactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "POSPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_POSPayments_POSTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "POSTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "POSTransactionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSTransactionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_POSTransactionItems_POSTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "POSTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_POSTransactionItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "NotificationId", "AcknowledgedDate", "ActionUrl", "Color", "CreatedBy", "CreatedDate", "ExpiryDate", "Icon", "IsAutoGenerated", "Message", "Priority", "ReadDate", "ReferenceId", "ReferenceType", "RequiresAction", "Status", "TargetUserId", "Title", "Type" },
                values: new object[,]
                {
                    { 1, null, null, null, "System", new DateTime(2023, 12, 31, 22, 0, 0, 0, DateTimeKind.Local), null, null, true, "مخزون المنتج 'قهوة محمصة' أصبح أقل من الحد الأدنى (5 من أصل 20)", 3, null, 18, "Product", false, 1, null, "تحذير: نفاد مخزون", 1 },
                    { 2, null, null, null, "System", new DateTime(2023, 12, 31, 19, 0, 0, 0, DateTimeKind.Local), null, null, true, "مخزون المنتج 'جبنة رومي' أصبح أقل من الحد الأدنى (8 من أصل 15)", 2, null, 19, "Product", false, 1, null, "تحذير: نفاد مخزون", 1 },
                    { 3, null, null, null, "System", new DateTime(2023, 12, 31, 0, 0, 0, 0, DateTimeKind.Local), null, null, true, "تم إنشاء فاتورة مبيعات جديدة S-2024-008 بقيمة 20,775 جنيه", 2, new DateTime(2023, 12, 31, 23, 0, 0, 0, DateTimeKind.Local), 8, "SalesInvoice", false, 2, null, "فاتورة جديدة", 5 },
                    { 4, null, null, null, "System", new DateTime(2023, 12, 25, 0, 0, 0, 0, DateTimeKind.Local), null, null, true, "يوجد مبلغ مستحق للمورد 'شركة الأهرام للمواد الغذائية' قدره 15,000 جنيه", 3, null, 1, "Supplier", false, 3, null, "دفعة مستحقة", 7 },
                    { 5, null, null, null, "System", new DateTime(2023, 12, 29, 0, 0, 0, 0, DateTimeKind.Local), null, null, true, "تم تحقيق 85% من هدف مبيعات الشهر الحالي. المطلوب: 50,000 ج.م، المُحقق: 42,500 ج.م", 1, new DateTime(2023, 12, 30, 0, 0, 0, 0, DateTimeKind.Local), null, null, false, 2, null, "هدف المبيعات", 5 },
                    { 6, null, null, null, "System", new DateTime(2023, 12, 31, 0, 0, 0, 0, DateTimeKind.Local), null, null, true, "المستخدم 'admin' قام بتسجيل الدخول من عنوان IP جديد", 1, null, null, null, false, 4, null, "نشاط المستخدم", 9 }
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                column: "ProductCode",
                value: "FD001");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                column: "ProductCode",
                value: "FD002");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                column: "ProductCode",
                value: "FD003");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 4,
                column: "ProductCode",
                value: "FD004");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 5,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "MinimumStock", "ProductCode", "ProductName", "PurchasePrice", "SalePrice" },
                values: new object[] { 2, 400m, "مكرونة الوادي بنة - 500 جرام", 40m, "FD005", "مكرونة الوادي", 8.50m, 12.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 6,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PurchasePrice" },
                values: new object[] { 2, 180m, "عدس أحمر مستورد - درجة أولى", 2, 25m, "FD006", "عدس أحمر", 35.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 7,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PurchasePrice", "SalePrice" },
                values: new object[] { 2, 250m, "ملح طعام مكرر - 1 كيلو", 5, 30m, "FD007", "ملح طعام جوهرة", 5.00m, 7.50m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 8,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PurchasePrice", "SalePrice" },
                values: new object[] { 2, 80m, "عسل نحل طبيعي من المناحل المصرية - 1 كيلو", 5, 10m, "FD008", "عسل نحل طبيعي", 180.00m, 225.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 9,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PurchasePrice", "SalePrice" },
                values: new object[] { 3, 100m, "مسحوق غسيل أريال - 3 كيلو", 5, 10m, "CL001", "صابون أريال", 75.00m, 90.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 10,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PurchasePrice", "SalePrice" },
                values: new object[] { 3, 80m, "سائل تنظيف أطباق فيري - 1 لتر", 5, 8m, "CL002", "منظف فيري", 32.00m, 42.00m });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "ProductId", "Barcode", "CategoryId", "CreatedDate", "CurrentStock", "Description", "ExpiryDate", "HasExpiryDate", "ImagePath", "IsActive", "IsSerialNumberRequired", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PurchasePrice", "SalePrice", "WarehouseId", "Weight" },
                values: new object[,]
                {
                    { 11, null, 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 120m, "معقم ديتول متعدد الاستخدامات - 750 مل", null, false, null, true, false, 5, 15m, "CL003", "معقم ديتول", 28.00m, 38.00m, null, 0m },
                    { 12, null, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 25m, "مقلاة تيفال غير لاصقة - 26 سم", null, false, null, true, false, 1, 5m, "HW001", "مقلاة تيفال", 180.00m, 230.00m, null, 0m },
                    { 13, null, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 15m, "خلاط موليكس 3 سرعات - 400 واط", null, false, null, true, false, 1, 3m, "HW002", "خلاط موليكس", 420.00m, 520.00m, null, 0m },
                    { 14, null, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 35m, "طقم 6 أكواب زجاجية شفافة", null, false, null, true, false, 1, 8m, "HW003", "طقم أكواب زجاجية", 85.00m, 120.00m, null, 0m },
                    { 15, null, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 10m, "تليفزيون سامسونج LED 43 بوصة", null, false, null, true, false, 1, 2m, "EL001", "تليفزيون سامسونج", 8500.00m, 10200.00m, null, 0m },
                    { 16, null, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 20m, "سامسونج جالاكسي A54 - 128 جيجا", null, false, null, true, false, 1, 5m, "EL002", "موبايل سامسونج", 12000.00m, 14500.00m, null, 0m },
                    { 17, null, 5, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 8m, "لاب توب ديل Inspiron 15 - Intel i5", null, false, null, true, false, 1, 2m, "EL003", "لاب توب ديل", 18000.00m, 22000.00m, null, 0m },
                    { 18, null, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 5m, "قهوة عربية محمصة - 250 جرام", null, false, null, true, false, 5, 20m, "FD009", "قهوة محمصة", 45.00m, 60.00m, null, 0m },
                    { 19, null, 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 8m, "جبنة رومي مصرية - 1 كيلو", null, false, null, true, false, 2, 15m, "FD010", "جبنة رومي", 120.00m, 155.00m, null, 0m },
                    { 20, null, 4, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Local), 12m, "ماكينة قهوة إسبريسو كهربائية", null, false, null, true, false, 1, 3m, "HW004", "ماكينة قهوة", 850.00m, 1100.00m, null, 0m }
                });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 2,
                columns: new[] { "DiscountAmount", "LineTotal", "ProductId", "UnitPrice" },
                values: new object[] { 0.00m, 84.00m, 3, 28.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 3,
                columns: new[] { "DiscountAmount", "LineTotal", "ProductId", "Quantity", "UnitPrice" },
                values: new object[] { 4.00m, 100.00m, 4, 2.0m, 52.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 4,
                columns: new[] { "LineTotal", "ProductId", "Quantity", "SalesInvoiceId", "UnitPrice" },
                values: new object[] { 150.00m, 7, 20.0m, 1, 7.50m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 5,
                columns: new[] { "LineTotal", "ProductId", "Quantity", "UnitPrice" },
                values: new object[] { 464.00m, 2, 8.0m, 58.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 6,
                columns: new[] { "LineTotal", "ProductId", "Quantity", "UnitPrice" },
                values: new object[] { 120.00m, 5, 10.0m, 12.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 7,
                columns: new[] { "DiscountAmount", "LineTotal", "ProductId", "Quantity", "SalesInvoiceId", "UnitPrice" },
                values: new object[] { 0.00m, 360.00m, 9, 4.0m, 2, 90.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 8,
                columns: new[] { "DiscountAmount", "LineTotal", "SalesInvoiceId", "UnitPrice" },
                values: new object[] { 0.00m, 225.00m, 2, 225.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 9,
                columns: new[] { "DiscountAmount", "LineTotal", "ProductId", "Quantity", "UnitPrice" },
                values: new object[] { 10.00m, 200.00m, 10, 5.0m, 42.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "InvoiceDate" },
                values: new object[] { new DateTime(2023, 12, 7, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2023, 12, 7, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "InvoiceDate", "SubTotal", "TaxAmount", "TotalAmount" },
                values: new object[] { new DateTime(2023, 12, 9, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2023, 12, 9, 0, 0, 0, 0, DateTimeKind.Local), 1200.00m, 180.00m, 1380.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 3,
                columns: new[] { "CreatedDate", "InvoiceDate", "Status", "SubTotal", "TaxAmount", "TotalAmount" },
                values: new object[] { new DateTime(2023, 12, 14, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2023, 12, 14, 0, 0, 0, 0, DateTimeKind.Local), 2, 750.00m, 112.50m, 812.50m });

            migrationBuilder.InsertData(
                table: "SalesInvoices",
                columns: new[] { "SalesInvoiceId", "CreatedBy", "CreatedDate", "CustomerId", "DiscountAmount", "InvoiceDate", "InvoiceNumber", "IsPosted", "NetTotal", "Notes", "PaidAmount", "RemainingAmount", "Status", "SubTotal", "TaxAmount", "TotalAmount" },
                values: new object[,]
                {
                    { 4, "", new DateTime(2023, 12, 17, 0, 0, 0, 0, DateTimeKind.Local), 4, 100.00m, new DateTime(2023, 12, 17, 0, 0, 0, 0, DateTimeKind.Local), "S-2024-004", false, 0m, "", 0m, 0m, 2, 2200.00m, 330.00m, 2430.00m },
                    { 5, "", new DateTime(2023, 12, 20, 0, 0, 0, 0, DateTimeKind.Local), 5, 30.00m, new DateTime(2023, 12, 20, 0, 0, 0, 0, DateTimeKind.Local), "S-2024-005", false, 0m, "", 0m, 0m, 2, 850.00m, 127.50m, 947.50m },
                    { 6, "", new DateTime(2023, 12, 22, 0, 0, 0, 0, DateTimeKind.Local), 6, 200.00m, new DateTime(2023, 12, 22, 0, 0, 0, 0, DateTimeKind.Local), "S-2024-006", false, 0m, "", 0m, 0m, 2, 3500.00m, 525.00m, 3825.00m },
                    { 7, "", new DateTime(2023, 12, 29, 0, 0, 0, 0, DateTimeKind.Local), 1, 0.00m, new DateTime(2023, 12, 29, 0, 0, 0, 0, DateTimeKind.Local), "S-2024-007", false, 0m, "", 0m, 0m, 2, 420.00m, 63.00m, 483.00m },
                    { 8, "", new DateTime(2023, 12, 31, 0, 0, 0, 0, DateTimeKind.Local), 2, 500.00m, new DateTime(2023, 12, 31, 0, 0, 0, 0, DateTimeKind.Local), "S-2024-008", false, 0m, "", 0m, 0m, 1, 18500.00m, 2775.00m, 20775.00m }
                });

            migrationBuilder.InsertData(
                table: "StockMovements",
                columns: new[] { "StockMovementId", "CreatedBy", "MovementDate", "MovementType", "Notes", "ProductId", "Quantity", "QuantityInMainUnit", "ReferenceId", "ReferenceNumber", "ReferenceType", "UnitCost", "UnitId", "WarehouseId" },
                values: new object[,]
                {
                    { 1, "", new DateTime(2023, 12, 7, 0, 0, 0, 0, DateTimeKind.Local), 2, "بيع فاتورة S-2024-001", 1, -5.0m, 0m, 1, "S-2024-001", "SalesInvoice", 28.50m, null, null },
                    { 2, "", new DateTime(2023, 12, 9, 0, 0, 0, 0, DateTimeKind.Local), 2, "بيع فاتورة S-2024-002", 2, -8.0m, 0m, 2, "S-2024-002", "SalesInvoice", 48.00m, null, null }
                });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3850));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3905));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3908));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3910));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3912));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3913));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3915));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3917));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3919));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3921));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3922));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3924));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3926));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3927));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 23, 23, 52, 50, 857, DateTimeKind.Local).AddTicks(3929));

            migrationBuilder.InsertData(
                table: "SalesInvoiceItems",
                columns: new[] { "SalesInvoiceItemId", "DiscountAmount", "DiscountPercentage", "LineTotal", "NetAmount", "Notes", "ProductId", "Quantity", "SalesInvoiceId", "TotalPrice", "UnitId", "UnitPrice" },
                values: new object[,]
                {
                    { 10, 0.00m, 0m, 304.00m, 0m, "", 11, 8.0m, 3, 0m, null, 38.00m },
                    { 11, 0.00m, 0m, 240.00m, 0m, "", 14, 2.0m, 3, 0m, null, 120.00m },
                    { 12, 30.00m, 0m, 660.00m, 0m, "", 12, 3.0m, 4, 0m, null, 230.00m },
                    { 13, 20.00m, 0m, 1020.00m, 0m, "", 13, 2.0m, 4, 0m, null, 520.00m },
                    { 14, 50.00m, 0m, 1050.00m, 0m, "", 20, 1.0m, 4, 0m, null, 1100.00m },
                    { 15, 8.00m, 0m, 160.00m, 0m, "", 6, 4.0m, 5, 0m, null, 42.00m },
                    { 16, 0.00m, 0m, 300.00m, 0m, "", 18, 5.0m, 5, 0m, null, 60.00m },
                    { 17, 10.00m, 0m, 300.00m, 0m, "", 19, 2.0m, 5, 0m, null, 155.00m },
                    { 18, 5.00m, 0m, 100.00m, 0m, "", 1, 3.0m, 5, 0m, null, 35.00m },
                    { 19, 200.00m, 0m, 10000.00m, 0m, "", 15, 1.0m, 6, 0m, null, 10200.00m },
                    { 20, 0.00m, 0m, 29000.00m, 0m, "", 16, 2.0m, 6, 0m, null, 14500.00m },
                    { 21, 0.00m, 0m, 70.00m, 0m, "", 1, 2.0m, 7, 0m, null, 35.00m },
                    { 22, 0.00m, 0m, 140.00m, 0m, "", 3, 5.0m, 7, 0m, null, 28.00m },
                    { 23, 0.00m, 0m, 208.00m, 0m, "", 4, 4.0m, 7, 0m, null, 52.00m },
                    { 24, 500.00m, 0m, 21500.00m, 0m, "", 17, 1.0m, 8, 0m, null, 22000.00m }
                });

            migrationBuilder.InsertData(
                table: "StockMovements",
                columns: new[] { "StockMovementId", "CreatedBy", "MovementDate", "MovementType", "Notes", "ProductId", "Quantity", "QuantityInMainUnit", "ReferenceId", "ReferenceNumber", "ReferenceType", "UnitCost", "UnitId", "WarehouseId" },
                values: new object[,]
                {
                    { 3, "", new DateTime(2023, 12, 22, 0, 0, 0, 0, DateTimeKind.Local), 2, "بيع تليفزيون سامسونج", 15, -1.0m, 0m, 6, "S-2024-006", "SalesInvoice", 8500.00m, null, null },
                    { 4, "", new DateTime(2023, 12, 2, 0, 0, 0, 0, DateTimeKind.Local), 1, "شراء قهوة محمصة من المورد", 18, 50.0m, 0m, null, "P-2024-001", "Purchase", 45.00m, null, null },
                    { 5, "", new DateTime(2023, 12, 22, 0, 0, 0, 0, DateTimeKind.Local), 2, "مبيعات متعددة خلال الشهر", 18, -45.0m, 0m, null, "Multiple Sales", "Sales", 45.00m, null, null },
                    { 6, "", new DateTime(2023, 12, 27, 0, 0, 0, 0, DateTimeKind.Local), 3, "تسوية: جبنة منتهية الصلاحية", 19, -2.0m, 0m, null, "ADJ-001", "Adjustment", 120.00m, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerOperations_CashierId",
                table: "CashDrawerOperations",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDrawerOperations_SessionId",
                table: "CashDrawerOperations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cashier_Code",
                table: "Cashiers",
                column: "CashierCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cashier_Username",
                table: "Cashiers",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashierSession_CashierDate",
                table: "CashierSessions",
                columns: new[] { "CashierId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLoyaltyPoints_CustomerId",
                table: "CustomerLoyaltyPoints",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLoyaltyPoints_LoyaltyProgramId",
                table: "CustomerLoyaltyPoints",
                column: "LoyaltyProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPromotion_PromotionsPromotionId",
                table: "CustomerPromotion",
                column: "PromotionsPromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRuleProduct_DiscountRulesDiscountId",
                table: "DiscountRuleProduct",
                column: "DiscountRulesDiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_Discount_Code",
                table: "Discounts",
                column: "DiscountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_CategoryId",
                table: "Discounts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_ProductId",
                table: "Discounts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogComments_CreatedAt",
                table: "ErrorLogComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogComments_ErrorLogId",
                table: "ErrorLogComments",
                column: "ErrorLogId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogComments_UserId",
                table: "ErrorLogComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_CreatedAt",
                table: "ErrorLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_CreatedAt_ErrorType",
                table: "ErrorLogs",
                columns: new[] { "CreatedAt", "ErrorType" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_ErrorId",
                table: "ErrorLogs",
                column: "ErrorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_ErrorType",
                table: "ErrorLogs",
                column: "ErrorType");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_ResolvedBy",
                table: "ErrorLogs",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Severity",
                table: "ErrorLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Status",
                table: "ErrorLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Status_Severity",
                table: "ErrorLogs",
                columns: new[] { "Status", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_UserId",
                table: "ErrorLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_CustomerLoyaltyPointId",
                table: "LoyaltyTransactions",
                column: "CustomerLoyaltyPointId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_POSTransactionId",
                table: "LoyaltyTransactions",
                column: "POSTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_POSPayments_TransactionId",
                table: "POSPayments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_POSTransactionItems_ProductId",
                table: "POSTransactionItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_POSTransactionItems_TransactionId",
                table: "POSTransactionItems",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_POSTransaction_Date",
                table: "POSTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_POSTransaction_Number",
                table: "POSTransactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_POSTransactions_CashierId",
                table: "POSTransactions",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_POSTransactions_CustomerId",
                table: "POSTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_POSTransactions_SessionId",
                table: "POSTransactions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPromotion_PromotionsPromotionId",
                table: "ProductPromotion",
                column: "PromotionsPromotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashDrawerOperations");

            migrationBuilder.DropTable(
                name: "CustomerPromotion");

            migrationBuilder.DropTable(
                name: "DiscountRuleProduct");

            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.DropTable(
                name: "ErrorLogComments");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "POSPayments");

            migrationBuilder.DropTable(
                name: "POSTransactionItems");

            migrationBuilder.DropTable(
                name: "ProductPromotion");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "CustomerLoyaltyPoints");

            migrationBuilder.DropTable(
                name: "POSTransactions");

            migrationBuilder.DropTable(
                name: "LoyaltyPrograms");

            migrationBuilder.DropTable(
                name: "CashierSessions");

            migrationBuilder.DropTable(
                name: "Cashiers");

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "NotificationId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "NotificationId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "NotificationId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "NotificationId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "NotificationId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Notifications",
                keyColumn: "NotificationId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "StockMovementId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "StockMovementId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "StockMovementId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "StockMovementId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "StockMovementId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "StockMovements",
                keyColumn: "StockMovementId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "AuditLogs",
                newName: "IPAddress");

            migrationBuilder.AddColumn<int>(
                name: "DiscountRuleDiscountId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OldValues",
                table: "AuditLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewValues",
                table: "AuditLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Users_UserName",
                table: "Users",
                column: "UserName");

            migrationBuilder.CreateTable(
                name: "CustomReports",
                columns: table => new
                {
                    ReportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    ChartDefinitions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ColumnDefinitions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    LastRun = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QueryDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_CustomReports_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 1,
                column: "PromotionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 2,
                column: "PromotionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 3,
                column: "PromotionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 4,
                column: "PromotionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 5,
                column: "PromotionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 6,
                column: "PromotionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "DiscountRuleDiscountId", "ProductCode", "PromotionId" },
                values: new object[] { null, "PR000001", null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "DiscountRuleDiscountId", "ProductCode", "PromotionId" },
                values: new object[] { null, "PR000002", null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                columns: new[] { "DiscountRuleDiscountId", "ProductCode", "PromotionId" },
                values: new object[] { null, "PR000003", null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 4,
                columns: new[] { "DiscountRuleDiscountId", "ProductCode", "PromotionId" },
                values: new object[] { null, "PR000004", null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 5,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "DiscountRuleDiscountId", "MinimumStock", "ProductCode", "ProductName", "PromotionId", "PurchasePrice", "SalePrice" },
                values: new object[] { 3, 100m, "مسحوق غسيل أريال - 3 كيلو", null, 10m, "PR000005", "صابون أريال", null, 75.00m, 90.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 6,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "DiscountRuleDiscountId", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PromotionId", "PurchasePrice" },
                values: new object[] { 3, 80m, "سائل تنظيف أطباق فيري - 1 لتر", null, 5, 8m, "PR000006", "منظف فيري", null, 32.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 7,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "DiscountRuleDiscountId", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PromotionId", "PurchasePrice", "SalePrice" },
                values: new object[] { 4, 25m, "مقلاة تيفال غير لاصقة - 26 سم", null, 1, 5m, "PR000007", "مقلاة تيفال", null, 180.00m, 230.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 8,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "DiscountRuleDiscountId", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PromotionId", "PurchasePrice", "SalePrice" },
                values: new object[] { 4, 15m, "خلاط موليكس 3 سرعات - 400 واط", null, 1, 3m, "PR000008", "خلاط موليكس", null, 420.00m, 520.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 9,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "DiscountRuleDiscountId", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PromotionId", "PurchasePrice", "SalePrice" },
                values: new object[] { 5, 10m, "تليفزيون سامسونج LED 43 بوصة", null, 1, 2m, "PR000009", "تليفزيون سامسونج", null, 8500.00m, 10200.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 10,
                columns: new[] { "CategoryId", "CurrentStock", "Description", "DiscountRuleDiscountId", "MainUnitId", "MinimumStock", "ProductCode", "ProductName", "PromotionId", "PurchasePrice", "SalePrice" },
                values: new object[] { 5, 20m, "سامسونج جالاكسي A54 - 128 جيجا", null, 1, 5m, "PR000010", "موبايل سامسونج", null, 12000.00m, 14500.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 2,
                columns: new[] { "DiscountAmount", "LineTotal", "ProductId", "UnitPrice" },
                values: new object[] { 5.00m, 169.00m, 2, 58.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 3,
                columns: new[] { "DiscountAmount", "LineTotal", "ProductId", "Quantity", "UnitPrice" },
                values: new object[] { 10.00m, 102.00m, 3, 4.0m, 28.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 4,
                columns: new[] { "LineTotal", "ProductId", "Quantity", "SalesInvoiceId", "UnitPrice" },
                values: new object[] { 520.00m, 4, 10.0m, 2, 52.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 5,
                columns: new[] { "LineTotal", "ProductId", "Quantity", "UnitPrice" },
                values: new object[] { 180.00m, 5, 2.0m, 90.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 6,
                columns: new[] { "LineTotal", "ProductId", "Quantity", "UnitPrice" },
                values: new object[] { 42.00m, 6, 1.0m, 42.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 7,
                columns: new[] { "DiscountAmount", "LineTotal", "ProductId", "Quantity", "SalesInvoiceId", "UnitPrice" },
                values: new object[] { 20.00m, 440.00m, 7, 2.0m, 3, 230.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 8,
                columns: new[] { "DiscountAmount", "LineTotal", "SalesInvoiceId", "UnitPrice" },
                values: new object[] { 30.00m, 490.00m, 3, 520.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoiceItems",
                keyColumn: "SalesInvoiceItemId",
                keyValue: 9,
                columns: new[] { "DiscountAmount", "LineTotal", "ProductId", "Quantity", "UnitPrice" },
                values: new object[] { 0.00m, 350.00m, 1, 10.0m, 35.00m });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "InvoiceDate" },
                values: new object[] { new DateTime(2024, 1, 11, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 1, 11, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "InvoiceDate", "SubTotal", "TaxAmount", "TotalAmount" },
                values: new object[] { new DateTime(2024, 1, 16, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 1, 16, 0, 0, 0, 0, DateTimeKind.Local), 750.00m, 112.50m, 862.50m });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 3,
                columns: new[] { "CreatedDate", "InvoiceDate", "Status", "SubTotal", "TaxAmount", "TotalAmount" },
                values: new object[] { new DateTime(2024, 1, 21, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2024, 1, 21, 0, 0, 0, 0, DateTimeKind.Local), 1, 1200.00m, 180.00m, 1330.00m });

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8799));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8844));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8848));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8850));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8852));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8854));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8856));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8858));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8860));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8862));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8864));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8866));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8868));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8870));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 22, 19, 26, 22, 412, DateTimeKind.Local).AddTicks(8872));

            migrationBuilder.CreateIndex(
                name: "IX_Products_DiscountRuleDiscountId",
                table: "Products",
                column: "DiscountRuleDiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_PromotionId",
                table: "Products",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PromotionId",
                table: "Customers",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomReports_CreatedBy",
                table: "CustomReports",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Promotions_PromotionId",
                table: "Customers",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_DiscountRules_DiscountRuleDiscountId",
                table: "Products",
                column: "DiscountRuleDiscountId",
                principalTable: "DiscountRules",
                principalColumn: "DiscountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Promotions_PromotionId",
                table: "Products",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "PromotionId");
        }
    }
}
