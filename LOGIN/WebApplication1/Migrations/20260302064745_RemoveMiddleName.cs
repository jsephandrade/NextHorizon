using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMiddleName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MiddleName",
                table: "Users",
                newName: "Nickname");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 47, 44, 429, DateTimeKind.Local).AddTicks(9105), new DateTime(2026, 3, 2, 14, 47, 44, 429, DateTimeKind.Local).AddTicks(9106) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 47, 44, 429, DateTimeKind.Local).AddTicks(9113), new DateTime(2026, 3, 2, 14, 47, 44, 429, DateTimeKind.Local).AddTicks(9114) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 47, 44, 429, DateTimeKind.Local).AddTicks(9119), new DateTime(2026, 3, 2, 14, 47, 44, 429, DateTimeKind.Local).AddTicks(9120) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Nickname",
                table: "Users",
                newName: "MiddleName");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 36, 27, 25, DateTimeKind.Local).AddTicks(1190), new DateTime(2026, 3, 2, 14, 36, 27, 25, DateTimeKind.Local).AddTicks(1190) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 36, 27, 25, DateTimeKind.Local).AddTicks(1197), new DateTime(2026, 3, 2, 14, 36, 27, 25, DateTimeKind.Local).AddTicks(1197) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 36, 27, 25, DateTimeKind.Local).AddTicks(1202), new DateTime(2026, 3, 2, 14, 36, 27, 25, DateTimeKind.Local).AddTicks(1202) });
        }
    }
}
