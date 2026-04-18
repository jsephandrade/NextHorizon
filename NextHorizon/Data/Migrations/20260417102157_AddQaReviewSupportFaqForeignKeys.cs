using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQaReviewSupportFaqForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_QaReviewInlineCommentDrafts_SupportFAQs_SupportFaqId",
                table: "QaReviewInlineCommentDrafts",
                column: "SupportFaqId",
                principalSchema: "dbo",
                principalTable: "SupportFAQs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QaReviews_SupportFAQs_SupportFaqId",
                table: "QaReviews",
                column: "SupportFaqId",
                principalSchema: "dbo",
                principalTable: "SupportFAQs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QaReviewInlineCommentDrafts_SupportFAQs_SupportFaqId",
                table: "QaReviewInlineCommentDrafts");

            migrationBuilder.DropForeignKey(
                name: "FK_QaReviews_SupportFAQs_SupportFaqId",
                table: "QaReviews");
        }
    }
}
