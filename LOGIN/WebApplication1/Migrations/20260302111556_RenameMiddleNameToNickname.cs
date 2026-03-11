using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class RenameMiddleNameToNickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 15, 55, 116, DateTimeKind.Local).AddTicks(5388), new DateTime(2026, 3, 2, 19, 15, 55, 116, DateTimeKind.Local).AddTicks(5389) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 15, 55, 116, DateTimeKind.Local).AddTicks(5397), new DateTime(2026, 3, 2, 19, 15, 55, 116, DateTimeKind.Local).AddTicks(5397) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 2, 19, 15, 55, 116, DateTimeKind.Local).AddTicks(5402), new DateTime(2026, 3, 2, 19, 15, 55, 116, DateTimeKind.Local).AddTicks(5403) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
