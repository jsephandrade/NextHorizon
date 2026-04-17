using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextHorizon.Data;

#nullable disable

namespace NextHorizon.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260328120000_RemoveRedundantHelpFaqSchema")]
    public partial class RemoveRedundantHelpFaqSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportTickets_HelpCategories_HelpCategoryId",
                table: "SupportTickets");

            migrationBuilder.DropTable(
                name: "HelpFaqs");

            migrationBuilder.DropIndex(
                name: "IX_SupportTickets_HelpCategoryId",
                table: "SupportTickets");

            migrationBuilder.DropTable(
                name: "HelpCategories");

            migrationBuilder.DropColumn(
                name: "HelpCategoryId",
                table: "SupportTickets");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HelpCategoryId",
                table: "SupportTickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HelpCategories",
                columns: table => new
                {
                    HelpCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpCategories", x => x.HelpCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "HelpFaqs",
                columns: table => new
                {
                    HelpFaqId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Answer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    HelpCategoryId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsFeaturedOnHome = table.Column<bool>(type: "bit", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SearchKeywords = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpFaqs", x => x.HelpFaqId);
                    table.ForeignKey(
                        name: "FK_HelpFaqs_HelpCategories_HelpCategoryId",
                        column: x => x.HelpCategoryId,
                        principalTable: "HelpCategories",
                        principalColumn: "HelpCategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HelpCategories_IsActive_DisplayOrder",
                table: "HelpCategories",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HelpCategories_Slug",
                table: "HelpCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HelpFaqs_HelpCategoryId_IsActive_DisplayOrder",
                table: "HelpFaqs",
                columns: new[] { "HelpCategoryId", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HelpFaqs_IsFeaturedOnHome",
                table: "HelpFaqs",
                column: "IsFeaturedOnHome");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_HelpCategoryId",
                table: "SupportTickets",
                column: "HelpCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupportTickets_HelpCategories_HelpCategoryId",
                table: "SupportTickets",
                column: "HelpCategoryId",
                principalTable: "HelpCategories",
                principalColumn: "HelpCategoryId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
