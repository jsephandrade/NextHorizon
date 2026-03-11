using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class Nickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 48, 34, 314, DateTimeKind.Local).AddTicks(3413), new DateTime(2026, 3, 2, 14, 48, 34, 314, DateTimeKind.Local).AddTicks(3414) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 48, 34, 314, DateTimeKind.Local).AddTicks(3420), new DateTime(2026, 3, 2, 14, 48, 34, 314, DateTimeKind.Local).AddTicks(3421) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 14, 48, 34, 314, DateTimeKind.Local).AddTicks(3439), new DateTime(2026, 3, 2, 14, 48, 34, 314, DateTimeKind.Local).AddTicks(3440) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
