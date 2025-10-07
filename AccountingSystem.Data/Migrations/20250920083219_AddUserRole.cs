using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 639, DateTimeKind.Local).AddTicks(5922));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4146));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4178));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4182));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4184));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4185));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4188));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4190));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4193));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4196));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4197));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4199));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4201));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4202));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 32, 18, 643, DateTimeKind.Local).AddTicks(4204));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "Role",
                value: "viewer");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                column: "Role",
                value: "viewer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 274, DateTimeKind.Local).AddTicks(4801));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1732));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1752));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1756));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1758));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1761));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1763));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1765));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1767));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1770));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1772));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1774));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1776));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1777));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 2, 36, 56, 276, DateTimeKind.Local).AddTicks(1779));
        }
    }
}
