using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQaEvaluationAuditIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "QaEvaluationTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "QaEvaluationTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "QaEvaluationQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "QaEvaluationQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "QaEvaluationCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedById",
                table: "QaEvaluationCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "QaEvaluationTemplates");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "QaEvaluationTemplates");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "QaEvaluationQuestions");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "QaEvaluationQuestions");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "QaEvaluationCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedById",
                table: "QaEvaluationCategories");
        }
    }
}
