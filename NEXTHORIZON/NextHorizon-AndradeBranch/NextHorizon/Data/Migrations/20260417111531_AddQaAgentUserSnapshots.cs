using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQaAgentUserSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgentUserId",
                table: "QaReviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgentUserId",
                table: "QaReviewInlineCommentDrafts",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE review
                SET review.AgentUserId = supportFaq.AgentId
                FROM dbo.QaReviews AS review
                INNER JOIN dbo.SupportFAQs AS supportFaq
                    ON supportFaq.Id = review.SupportFaqId;
                """);

            migrationBuilder.Sql(
                """
                UPDATE draft
                SET draft.AgentUserId = supportFaq.AgentId
                FROM dbo.QaReviewInlineCommentDrafts AS draft
                INNER JOIN dbo.SupportFAQs AS supportFaq
                    ON supportFaq.Id = draft.SupportFaqId;
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM dbo.QaReviews
                    WHERE AgentUserId IS NULL
                )
                BEGIN
                    THROW 51200, 'Migration blocked: dbo.QaReviews contains rows without a resolvable AgentUserId snapshot.', 1;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM dbo.QaReviewInlineCommentDrafts
                    WHERE AgentUserId IS NULL
                )
                BEGIN
                    THROW 51201, 'Migration blocked: dbo.QaReviewInlineCommentDrafts contains rows without a resolvable AgentUserId snapshot.', 1;
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "AgentUserId",
                table: "QaReviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AgentUserId",
                table: "QaReviewInlineCommentDrafts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QaReviews_AgentUserId",
                table: "QaReviews",
                column: "AgentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QaReviewInlineCommentDrafts_AgentUserId",
                table: "QaReviewInlineCommentDrafts",
                column: "AgentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QaReviews_AgentUserId",
                table: "QaReviews");

            migrationBuilder.DropIndex(
                name: "IX_QaReviewInlineCommentDrafts_AgentUserId",
                table: "QaReviewInlineCommentDrafts");

            migrationBuilder.DropColumn(
                name: "AgentUserId",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "AgentUserId",
                table: "QaReviewInlineCommentDrafts");
        }
    }
}
