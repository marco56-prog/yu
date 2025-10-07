using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserPasswords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 118, DateTimeKind.Local).AddTicks(5245));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3842));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3864));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3868));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3871));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3874));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3876));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3879));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3882));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3884));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3886));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3888));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3891));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3893));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 20, 11, 49, 12, 120, DateTimeKind.Local).AddTicks(3895));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "4RIvpWW9kYMqUP0bTZ7pnsH2OSK2x6snM2QevGY5oyY=", "admin" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "4RIvpWW9kYMqUP0bTZ7pnsH2OSK2x6snM2QevGY5oyY=", "accountant" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "AQAAAAEAACcQAAAAEHk8+XhGFz6gw5u8b1k1l1k1l1k1l1k1l1k1l1k1l1k1l1k1l1k1==", "viewer" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                columns: new[] { "PasswordHash", "Role" },
                values: new object[] { "AQAAAAEAACcQAAAAEHk8+XhGFz6gw5u8b1k1l1k1l1k1l1k1l1k1l1k1l1k1l1k1l1k1==", "viewer" });
        }
    }
}
