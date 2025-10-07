using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePasswordsToAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7584));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7636));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7639));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7641));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7643));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7644));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7646));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7648));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7649));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7651));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7653));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7654));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7656));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7658));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 5, 34, 49, 804, DateTimeKind.Local).AddTicks(7659));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "PasswordHash",
                value: "PBKDF2:120000:c6c26yJQmQpsRPYGDmB5nQ==:V5IsjDNiy2POmseczguUobxGvG3j0EJkMTU1W0zH5PM=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                column: "PasswordHash",
                value: "PBKDF2:120000:c6c26yJQmQpsRPYGDmB5nQ==:V5IsjDNiy2POmseczguUobxGvG3j0EJkMTU1W0zH5PM=");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 1,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5601));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 2,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5657));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 3,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5660));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 4,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5662));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 5,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5664));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 6,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5666));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 7,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5668));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 8,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5670));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 9,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5672));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 10,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5673));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 11,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5675));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 12,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5677));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 13,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5679));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 14,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5681));

            migrationBuilder.UpdateData(
                table: "SystemSettings",
                keyColumn: "SettingId",
                keyValue: 15,
                column: "UpdatedDate",
                value: new DateTime(2025, 9, 21, 2, 58, 27, 776, DateTimeKind.Local).AddTicks(5682));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 1,
                column: "PasswordHash",
                value: "PBKDF2:120000:PGNg+Cl/J4cLnGWW7O/jig==:0Bz30EyUSlL31bRGOwtVjl7HnPijc/LXjc/n61cfmxM=");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "UserId",
                keyValue: 2,
                column: "PasswordHash",
                value: "PBKDF2:120000:PLFFaq+TAv9O75a7lxr05A==:PXLgbcOXJsHy8LWXj57CH/+DvLgpY3Q3cjpOXRMciF4=");
        }
    }
}
