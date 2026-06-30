using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveAgentSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiveAgentSessions",
                columns: table => new
                {
                    LiveAgentSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupportFaqId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ConsumerId = table.Column<int>(type: "int", nullable: true),
                    CategorySlug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CategoryTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FirstQuestion = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveAgentSessions", x => x.LiveAgentSessionId);
                    table.ForeignKey(
                        name: "FK_LiveAgentSessions_Consumers_ConsumerId",
                        column: x => x.ConsumerId,
                        principalSchema: "dbo",
                        principalTable: "Consumers",
                        principalColumn: "consumer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiveAgentSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiveAgentSessions_ConsumerId",
                table: "LiveAgentSessions",
                column: "ConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveAgentSessions_SupportFaqId",
                table: "LiveAgentSessions",
                column: "SupportFaqId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiveAgentSessions_UserId_Status_CreatedAt",
                table: "LiveAgentSessions",
                columns: new[] { "UserId", "Status", "CreatedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveAgentSessions");
        }
    }
}
