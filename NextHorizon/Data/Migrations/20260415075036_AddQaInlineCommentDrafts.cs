using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQaInlineCommentDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QaReviewInlineCommentDrafts",
                columns: table => new
                {
                    QaReviewInlineCommentDraftId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupportFaqId = table.Column<int>(type: "int", nullable: false),
                    UpdatedByStaffId = table.Column<int>(type: "int", nullable: false),
                    UpdatedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InlineCommentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaReviewInlineCommentDrafts", x => x.QaReviewInlineCommentDraftId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaReviewInlineCommentDrafts_SupportFaqId",
                table: "QaReviewInlineCommentDrafts",
                column: "SupportFaqId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QaReviewInlineCommentDrafts");
        }
    }
}
