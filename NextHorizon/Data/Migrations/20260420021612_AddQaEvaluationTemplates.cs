using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQaEvaluationTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QaEvaluationTemplateId",
                table: "QaReviews",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "QuestionKey",
                table: "QaReviewQuestionScores",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "CategoryNameSnapshot",
                table: "QaReviewQuestionScores",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "QaEvaluationQuestionId",
                table: "QaReviewQuestionScores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionTextSnapshot",
                table: "QaReviewQuestionScores",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "QaEvaluationTemplates",
                columns: table => new
                {
                    QaEvaluationTemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaEvaluationTemplates", x => x.QaEvaluationTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "QaEvaluationCategories",
                columns: table => new
                {
                    QaEvaluationCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QaEvaluationTemplateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaEvaluationCategories", x => x.QaEvaluationCategoryId);
                    table.ForeignKey(
                        name: "FK_QaEvaluationCategories_QaEvaluationTemplates_QaEvaluationTemplateId",
                        column: x => x.QaEvaluationTemplateId,
                        principalTable: "QaEvaluationTemplates",
                        principalColumn: "QaEvaluationTemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QaEvaluationQuestions",
                columns: table => new
                {
                    QaEvaluationQuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QaEvaluationCategoryId = table.Column<int>(type: "int", nullable: false),
                    QuestionKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaEvaluationQuestions", x => x.QaEvaluationQuestionId);
                    table.ForeignKey(
                        name: "FK_QaEvaluationQuestions_QaEvaluationCategories_QaEvaluationCategoryId",
                        column: x => x.QaEvaluationCategoryId,
                        principalTable: "QaEvaluationCategories",
                        principalColumn: "QaEvaluationCategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT dbo.QaEvaluationTemplates ON;

                INSERT INTO dbo.QaEvaluationTemplates
                    (QaEvaluationTemplateId, VersionNumber, IsActive, CreatedAtUtc, UpdatedAtUtc, ActivatedAtUtc)
                VALUES
                    (1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME());

                SET IDENTITY_INSERT dbo.QaEvaluationTemplates OFF;

                SET IDENTITY_INSERT dbo.QaEvaluationCategories ON;

                INSERT INTO dbo.QaEvaluationCategories
                    (QaEvaluationCategoryId, QaEvaluationTemplateId, Name, WeightPercent, DisplayOrder)
                VALUES
                    (1, 1, N'Accuracy', 35.00, 1),
                    (2, 1, N'Tone and empathy', 35.00, 2),
                    (3, 1, N'Resolution quality', 30.00, 3);

                SET IDENTITY_INSERT dbo.QaEvaluationCategories OFF;

                SET IDENTITY_INSERT dbo.QaEvaluationQuestions ON;

                INSERT INTO dbo.QaEvaluationQuestions
                    (QaEvaluationQuestionId, QaEvaluationCategoryId, QuestionKey, Prompt, DisplayOrder)
                VALUES
                    (1, 1, N'accuracy_q1', N'Did the agent provide correct policy or product details?', 1),
                    (2, 1, N'accuracy_q2', N'Did the response avoid factual errors or contradictions?', 2),
                    (3, 1, N'accuracy_q3', N'Were the next steps accurate and actionable?', 3),
                    (4, 2, N'tone_q1', N'Did the agent acknowledge the customer concern with empathy?', 1),
                    (5, 2, N'tone_q2', N'Was the tone respectful and professional throughout?', 2),
                    (6, 2, N'tone_q3', N'Did the language stay clear, calm, and customer-friendly?', 3),
                    (7, 3, N'resolution_q1', N'Was the core issue fully resolved?', 1),
                    (8, 3, N'resolution_q2', N'Did the agent provide a complete and practical solution?', 2),
                    (9, 3, N'resolution_q3', N'Did the agent set clear follow-up expectations when needed?', 3);

                SET IDENTITY_INSERT dbo.QaEvaluationQuestions OFF;

                UPDATE dbo.QaReviews
                SET QaEvaluationTemplateId = 1
                WHERE QaEvaluationTemplateId IS NULL;

                UPDATE score
                SET
                    score.QaEvaluationQuestionId = question.QaEvaluationQuestionId,
                    score.CategoryNameSnapshot = category.Name,
                    score.QuestionTextSnapshot = question.Prompt
                FROM dbo.QaReviewQuestionScores AS score
                INNER JOIN dbo.QaEvaluationQuestions AS question
                    ON question.QuestionKey = score.QuestionKey
                INNER JOIN dbo.QaEvaluationCategories AS category
                    ON category.QaEvaluationCategoryId = question.QaEvaluationCategoryId;

                UPDATE dbo.QaReviewQuestionScores
                SET
                    CategoryNameSnapshot = CASE
                        WHEN QuestionKey LIKE N'accuracy_%' THEN N'Accuracy'
                        WHEN QuestionKey LIKE N'tone_%' THEN N'Tone and empathy'
                        WHEN QuestionKey LIKE N'resolution_%' THEN N'Resolution quality'
                        ELSE N'Legacy'
                    END,
                    QuestionTextSnapshot = CASE
                        WHEN NULLIF(LTRIM(RTRIM(QuestionTextSnapshot)), N'') IS NULL THEN QuestionKey
                        ELSE QuestionTextSnapshot
                    END
                WHERE NULLIF(LTRIM(RTRIM(CategoryNameSnapshot)), N'') IS NULL
                    OR NULLIF(LTRIM(RTRIM(QuestionTextSnapshot)), N'') IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "QaEvaluationTemplateId",
                table: "QaReviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QaReviews_QaEvaluationTemplateId",
                table: "QaReviews",
                column: "QaEvaluationTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_QaReviewQuestionScores_QaEvaluationQuestionId",
                table: "QaReviewQuestionScores",
                column: "QaEvaluationQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QaEvaluationCategories_QaEvaluationTemplateId_DisplayOrder",
                table: "QaEvaluationCategories",
                columns: new[] { "QaEvaluationTemplateId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QaEvaluationQuestions_QaEvaluationCategoryId_DisplayOrder",
                table: "QaEvaluationQuestions",
                columns: new[] { "QaEvaluationCategoryId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QaEvaluationQuestions_QaEvaluationCategoryId_QuestionKey",
                table: "QaEvaluationQuestions",
                columns: new[] { "QaEvaluationCategoryId", "QuestionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QaEvaluationTemplates_IsActive",
                table: "QaEvaluationTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_QaEvaluationTemplates_VersionNumber",
                table: "QaEvaluationTemplates",
                column: "VersionNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QaReviewQuestionScores_QaEvaluationQuestions_QaEvaluationQuestionId",
                table: "QaReviewQuestionScores",
                column: "QaEvaluationQuestionId",
                principalTable: "QaEvaluationQuestions",
                principalColumn: "QaEvaluationQuestionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QaReviews_QaEvaluationTemplates_QaEvaluationTemplateId",
                table: "QaReviews",
                column: "QaEvaluationTemplateId",
                principalTable: "QaEvaluationTemplates",
                principalColumn: "QaEvaluationTemplateId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QaReviewQuestionScores_QaEvaluationQuestions_QaEvaluationQuestionId",
                table: "QaReviewQuestionScores");

            migrationBuilder.DropForeignKey(
                name: "FK_QaReviews_QaEvaluationTemplates_QaEvaluationTemplateId",
                table: "QaReviews");

            migrationBuilder.DropTable(
                name: "QaEvaluationQuestions");

            migrationBuilder.DropTable(
                name: "QaEvaluationCategories");

            migrationBuilder.DropTable(
                name: "QaEvaluationTemplates");

            migrationBuilder.DropIndex(
                name: "IX_QaReviews_QaEvaluationTemplateId",
                table: "QaReviews");

            migrationBuilder.DropIndex(
                name: "IX_QaReviewQuestionScores_QaEvaluationQuestionId",
                table: "QaReviewQuestionScores");

            migrationBuilder.DropColumn(
                name: "QaEvaluationTemplateId",
                table: "QaReviews");

            migrationBuilder.DropColumn(
                name: "CategoryNameSnapshot",
                table: "QaReviewQuestionScores");

            migrationBuilder.DropColumn(
                name: "QaEvaluationQuestionId",
                table: "QaReviewQuestionScores");

            migrationBuilder.DropColumn(
                name: "QuestionTextSnapshot",
                table: "QaReviewQuestionScores");

            migrationBuilder.AlterColumn<string>(
                name: "QuestionKey",
                table: "QaReviewQuestionScores",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);
        }
    }
}
