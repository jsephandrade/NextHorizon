using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceQaReviewFixedCategoryColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QaReviewCategoryScores",
                columns: table => new
                {
                    QaReviewCategoryScoreId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QaReviewId = table.Column<int>(type: "int", nullable: false),
                    QaEvaluationCategoryId = table.Column<int>(type: "int", nullable: true),
                    CategoryNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    WeightPercentSnapshot = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AverageScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WeightedPoints = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaReviewCategoryScores", x => x.QaReviewCategoryScoreId);
                    table.ForeignKey(
                        name: "FK_QaReviewCategoryScores_QaEvaluationCategories_QaEvaluationCategoryId",
                        column: x => x.QaEvaluationCategoryId,
                        principalTable: "QaEvaluationCategories",
                        principalColumn: "QaEvaluationCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QaReviewCategoryScores_QaReviews_QaReviewId",
                        column: x => x.QaReviewId,
                        principalTable: "QaReviews",
                        principalColumn: "QaReviewId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaReviewCategoryScores_QaEvaluationCategoryId",
                table: "QaReviewCategoryScores",
                column: "QaEvaluationCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_QaReviewCategoryScores_QaReviewId_DisplayOrder",
                table: "QaReviewCategoryScores",
                columns: new[] { "QaReviewId", "DisplayOrder" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO dbo.QaReviewCategoryScores
                (
                    QaReviewId,
                    QaEvaluationCategoryId,
                    CategoryNameSnapshot,
                    WeightPercentSnapshot,
                    AverageScore,
                    WeightedPoints,
                    DisplayOrder
                )
                SELECT
                    score.QaReviewId,
                    category.QaEvaluationCategoryId,
                    category.Name,
                    CAST(category.WeightPercent AS decimal(5,2)),
                    CAST(ROUND(AVG(CAST(score.Score AS decimal(10,4))), 2) AS decimal(5,2)),
                    CAST(ROUND((AVG(CAST(score.Score AS decimal(10,4))) / 5.0) * CAST(category.WeightPercent AS decimal(10,4)), 2) AS decimal(5,2)),
                    category.DisplayOrder
                FROM dbo.QaReviewQuestionScores AS score
                INNER JOIN dbo.QaEvaluationQuestions AS question
                    ON question.QaEvaluationQuestionId = score.QaEvaluationQuestionId
                INNER JOIN dbo.QaEvaluationCategories AS category
                    ON category.QaEvaluationCategoryId = question.QaEvaluationCategoryId
                GROUP BY
                    score.QaReviewId,
                    category.QaEvaluationCategoryId,
                    category.Name,
                    category.WeightPercent,
                    category.DisplayOrder;
                """);

            migrationBuilder.Sql(
                """
                ;WITH MissingReviews AS
                (
                    SELECT
                        review.QaReviewId,
                        review.QaEvaluationTemplateId,
                        review.AccuracyAverage,
                        review.ToneAverage,
                        review.ResolutionAverage
                    FROM dbo.QaReviews AS review
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM dbo.QaReviewCategoryScores AS categoryScore
                        WHERE categoryScore.QaReviewId = review.QaReviewId
                    )
                )
                INSERT INTO dbo.QaReviewCategoryScores
                (
                    QaReviewId,
                    QaEvaluationCategoryId,
                    CategoryNameSnapshot,
                    WeightPercentSnapshot,
                    AverageScore,
                    WeightedPoints,
                    DisplayOrder
                )
                SELECT
                    missing.QaReviewId,
                    category.QaEvaluationCategoryId,
                    category.Name,
                    CAST(category.WeightPercent AS decimal(5,2)),
                    CAST(
                        CASE category.DisplayOrder
                            WHEN 1 THEN missing.AccuracyAverage
                            WHEN 2 THEN missing.ToneAverage
                            WHEN 3 THEN missing.ResolutionAverage
                            ELSE 0
                        END
                        AS decimal(5,2)),
                    CAST(ROUND((
                        CAST(
                            CASE category.DisplayOrder
                                WHEN 1 THEN missing.AccuracyAverage
                                WHEN 2 THEN missing.ToneAverage
                                WHEN 3 THEN missing.ResolutionAverage
                                ELSE 0
                            END
                            AS decimal(10,4)) / 5.0) * CAST(category.WeightPercent AS decimal(10,4)), 2) AS decimal(5,2)),
                    category.DisplayOrder
                FROM MissingReviews AS missing
                INNER JOIN dbo.QaEvaluationCategories AS category
                    ON category.QaEvaluationTemplateId = missing.QaEvaluationTemplateId;
                """);

            migrationBuilder.DropColumn(
                name: "AccuracyAverage",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "ResolutionAverage",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "ToneAverage",
                table: "QaReviews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AccuracyAverage",
                table: "QaReviews",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ResolutionAverage",
                table: "QaReviews",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ToneAverage",
                table: "QaReviews",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE review
                SET
                    AccuracyAverage = ISNULL(categoryOne.AverageScore, 0),
                    ToneAverage = ISNULL(categoryTwo.AverageScore, 0),
                    ResolutionAverage = ISNULL(categoryThree.AverageScore, 0)
                FROM dbo.QaReviews AS review
                OUTER APPLY
                (
                    SELECT TOP (1) AverageScore
                    FROM dbo.QaReviewCategoryScores
                    WHERE QaReviewId = review.QaReviewId
                      AND DisplayOrder = 1
                    ORDER BY QaReviewCategoryScoreId
                ) AS categoryOne
                OUTER APPLY
                (
                    SELECT TOP (1) AverageScore
                    FROM dbo.QaReviewCategoryScores
                    WHERE QaReviewId = review.QaReviewId
                      AND DisplayOrder = 2
                    ORDER BY QaReviewCategoryScoreId
                ) AS categoryTwo
                OUTER APPLY
                (
                    SELECT TOP (1) AverageScore
                    FROM dbo.QaReviewCategoryScores
                    WHERE QaReviewId = review.QaReviewId
                      AND DisplayOrder = 3
                    ORDER BY QaReviewCategoryScoreId
                ) AS categoryThree;
                """);

            migrationBuilder.DropTable(
                name: "QaReviewCategoryScores");
        }
    }
}
