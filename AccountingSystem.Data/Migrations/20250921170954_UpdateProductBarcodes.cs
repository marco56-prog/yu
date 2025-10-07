using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // تحديث بيانات الباركود للمنتجات الموجودة
            migrationBuilder.Sql("UPDATE Products SET Barcode = '6221151000017' WHERE ProductId = 1");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '6221053000024' WHERE ProductId = 2");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '6221058000031' WHERE ProductId = 3");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '5000169014967' WHERE ProductId = 4");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '8001090407800' WHERE ProductId = 5");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '8001090323958' WHERE ProductId = 6");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '3168430199804' WHERE ProductId = 7");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '3045388801027' WHERE ProductId = 8");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '8801643958896' WHERE ProductId = 9");
            migrationBuilder.Sql("UPDATE Products SET Barcode = '8801643958902' WHERE ProductId = 10");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8198));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8252));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8254));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8256));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8258));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8259));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8261));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8263));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8264));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8266));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8267));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8272));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8273));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8275));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 9, 53, 522, DateTimeKind.Local).AddTicks(8277));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4791));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4846));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4849));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4851));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4853));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4854));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4856));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4858));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4860));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4861));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4863));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4864));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4866));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4868));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 20, 7, 13, 490, DateTimeKind.Local).AddTicks(4869));
        }
    }
}
