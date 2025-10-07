using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRepresentativeAndWarehouseToSalesInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warehouses_WarehouseName",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "IsMainWarehouse",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CurrentStock",
                table: "ProductStocks");

            migrationBuilder.RenameColumn(
                name: "ReservedStock",
                table: "ProductStocks",
                newName: "ReservedQuantity");

            migrationBuilder.RenameColumn(
                name: "MaximumStock",
                table: "ProductStocks",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "LastMovementDate",
                table: "ProductStocks",
                newName: "LastUpdated");

            migrationBuilder.AlterColumn<string>(
                name: "WarehouseName",
                table: "Warehouses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "WarehouseCode",
                table: "Warehouses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Warehouses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Warehouses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCode",
                table: "Suppliers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RepresentativeId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "SalesInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "ErrorLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Signature",
                table: "ErrorLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "Customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessSettings",
                table: "Cashiers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Representatives",
                columns: table => new
                {
                    RepresentativeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepresentativeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RepresentativeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Representatives", x => x.RepresentativeId);
                });

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 1,
                column: "CustomerCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 2,
                column: "CustomerCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 3,
                column: "CustomerCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 4,
                column: "CustomerCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 5,
                column: "CustomerCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 6,
                column: "CustomerCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 1,
                columns: new[] { "RepresentativeId", "WarehouseId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 2,
                columns: new[] { "RepresentativeId", "WarehouseId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 3,
                columns: new[] { "RepresentativeId", "WarehouseId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 4,
                columns: new[] { "RepresentativeId", "WarehouseId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 5,
                columns: new[] { "RepresentativeId", "WarehouseId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 6,
                columns: new[] { "RepresentativeId", "WarehouseId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 7,
                columns: new[] { "RepresentativeId", "WarehouseId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "SalesInvoices",
                keyColumn: "SalesInvoiceId",
                keyValue: 8,
                columns: new[] { "RepresentativeId", "WarehouseId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "SupplierId",
                keyValue: 1,
                column: "SupplierCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "SupplierId",
                keyValue: 2,
                column: "SupplierCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "SupplierId",
                keyValue: 3,
                column: "SupplierCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "SupplierId",
                keyValue: 4,
                column: "SupplierCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "SupplierId",
                keyValue: 5,
                column: "SupplierCode",
                value: "");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(797));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(841));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(846));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(850));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(853));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(856));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(859));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(862));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(864));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(867));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(870));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(873));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(875));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(878));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 5, 21, 34, 10, 127, DateTimeKind.Local).AddTicks(881));

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_RepresentativeId",
                table: "SalesInvoices",
                column: "RepresentativeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_WarehouseId",
                table: "SalesInvoices",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Representatives_RepresentativeId",
                table: "SalesInvoices",
                column: "RepresentativeId",
                principalTable: "Representatives",
                principalColumn: "RepresentativeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Warehouses_WarehouseId",
                table: "SalesInvoices",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Representatives_RepresentativeId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Warehouses_WarehouseId",
                table: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "Representatives");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_RepresentativeId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_WarehouseId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "SupplierCode",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "RepresentativeId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CanAccessSettings",
                table: "Cashiers");

            migrationBuilder.RenameColumn(
                name: "ReservedQuantity",
                table: "ProductStocks",
                newName: "ReservedStock");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "ProductStocks",
                newName: "MaximumStock");

            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "ProductStocks",
                newName: "LastMovementDate");

            migrationBuilder.AlterColumn<string>(
                name: "WarehouseName",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "WarehouseCode",
                table: "Warehouses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Warehouses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Warehouses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMainWarehouse",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Warehouses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentStock",
                table: "ProductStocks",
                type: "decimal(18,4)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8024));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8073));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8075));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8077));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8078));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8080));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8081));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8083));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8084));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8086));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8087));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8089));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8090));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8091));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 10, 1, 22, 46, 9, 867, DateTimeKind.Local).AddTicks(8093));

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_WarehouseName",
                table: "Warehouses",
                column: "WarehouseName",
                unique: true);
        }
    }
}
