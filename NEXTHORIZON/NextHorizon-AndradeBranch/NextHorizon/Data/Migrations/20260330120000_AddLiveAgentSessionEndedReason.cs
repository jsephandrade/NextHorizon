using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveAgentSessionEndedReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "EndedReason",
                table: "LiveAgentSessions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.Sql(
                """
                UPDATE [LiveAgentSessions]
                SET [EndedReason] = 1
                WHERE [Status] = 3 AND [EndedReason] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndedReason",
                table: "LiveAgentSessions");
        }
    }
}
