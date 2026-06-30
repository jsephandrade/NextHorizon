using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQaReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QaReviews",
                columns: table => new
                {
                    QaReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupportFaqId = table.Column<int>(type: "int", nullable: false),
                    ReviewerStaffId = table.Column<int>(type: "int", nullable: false),
                    ReviewerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AccuracyAverage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ToneAverage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ResolutionAverage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OverallPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SubmittedToAgent = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedToAgentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InlineCommentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaReviews", x => x.QaReviewId);
                });

            migrationBuilder.CreateTable(
                name: "QaReviewQuestionScores",
                columns: table => new
                {
                    QaReviewQuestionScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QaReviewId = table.Column<int>(type: "int", nullable: false),
                    QuestionKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaReviewQuestionScores", x => x.QaReviewQuestionScoreId);
                    table.ForeignKey(
                        name: "FK_QaReviewQuestionScores_QaReviews_QaReviewId",
                        column: x => x.QaReviewId,
                        principalTable: "QaReviews",
                        principalColumn: "QaReviewId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaReviewQuestionScores_QaReviewId_QuestionKey",
                table: "QaReviewQuestionScores",
                columns: new[] { "QaReviewId", "QuestionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QaReviews_SupportFaqId",
                table: "QaReviews",
                column: "SupportFaqId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QaReviewQuestionScores");

            migrationBuilder.DropTable(
                name: "QaReviews");
        }
    }
}
