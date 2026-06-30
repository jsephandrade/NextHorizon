using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextHorizon.Data;

#nullable disable

namespace NextHorizon.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260327000100_AddHelpCenterModule")]
    public partial class AddHelpCenterModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelpCategories",
                columns: table => new
                {
                    HelpCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpCategories", x => x.HelpCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "SupportContactChannels",
                columns: table => new
                {
                    SupportContactChannelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChannelType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    ActionHref = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportContactChannels", x => x.SupportContactChannelId);
                });

            migrationBuilder.CreateTable(
                name: "HelpFaqs",
                columns: table => new
                {
                    HelpFaqId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HelpCategoryId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SearchKeywords = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsFeaturedOnHome = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    SupportTicketId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ConsumerId = table.Column<int>(type: "int", nullable: true),
                    HelpCategoryId = table.Column<int>(type: "int", nullable: true),
                    FaqCategory = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.SupportTicketId);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Consumers_ConsumerId",
                        column: x => x.ConsumerId,
                        principalSchema: "dbo",
                        principalTable: "Consumers",
                        principalColumn: "consumer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTickets_HelpCategories_HelpCategoryId",
                        column: x => x.HelpCategoryId,
                        principalTable: "HelpCategories",
                        principalColumn: "HelpCategoryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_SupportContactChannels_IsActive_DisplayOrder",
                table: "SupportContactChannels",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_ConsumerId",
                table: "SupportTickets",
                column: "ConsumerId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_HelpCategoryId",
                table: "SupportTickets",
                column: "HelpCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_ReferenceCode",
                table: "SupportTickets",
                column: "ReferenceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_Status_CreatedAt",
                table: "SupportTickets",
                columns: new[] { "Status", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_UserId_CreatedAt",
                table: "SupportTickets",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            foreach (var channel in HelpCenterSeed.ContactChannels)
            {
                migrationBuilder.InsertData(
                    table: "SupportContactChannels",
                    columns: new[] { "SupportContactChannelId", "ChannelType", "Label", "Value", "DisplayText", "ActionHref", "DisplayOrder", "IsActive" },
                    columnTypes: new[] { "int", "nvarchar(40)", "nvarchar(80)", "nvarchar(320)", "nvarchar(320)", "nvarchar(400)", "int", "bit" },
                    values: new object[] { channel.SupportContactChannelId, channel.ChannelType, channel.Label, channel.Value, channel.DisplayText, channel.ActionHref, channel.DisplayOrder, channel.IsActive });
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpFaqs");

            migrationBuilder.DropTable(
                name: "SupportContactChannels");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "HelpCategories");
        }
    }
}

