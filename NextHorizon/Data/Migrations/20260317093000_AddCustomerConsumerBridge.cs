using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NextHorizon.Data;

#nullable disable

namespace NextHorizon.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260317093000_AddCustomerConsumerBridge")]
    public partial class AddCustomerConsumerBridge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Customers]', N'U') IS NULL
                    THROW 51130, 'Migration blocked: [dbo].[Customers] is required before adding the consumer bridge.', 1;

                IF OBJECT_ID(N'[dbo].[Consumers]', N'U') IS NULL
                    THROW 51131, 'Migration blocked: [dbo].[Consumers] is required before adding the consumer bridge.', 1;

                IF COL_LENGTH(N'dbo.Customers', N'ConsumerId') IS NULL
                BEGIN
                    ALTER TABLE dbo.Customers
                    ADD ConsumerId INT NULL;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[Customers]', N'U')
                      AND name = N'IX_Customers_ConsumerId'
                )
                BEGIN
                    CREATE UNIQUE INDEX IX_Customers_ConsumerId
                        ON dbo.Customers (ConsumerId)
                        WHERE ConsumerId IS NOT NULL;
                END;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = N'FK_Customers_Consumers_ConsumerId'
                )
                BEGIN
                    ALTER TABLE dbo.Customers WITH CHECK
                        ADD CONSTRAINT FK_Customers_Consumers_ConsumerId
                        FOREIGN KEY (ConsumerId) REFERENCES dbo.Consumers(consumer_id) ON DELETE NO ACTION;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = N'FK_Customers_Consumers_ConsumerId'
                )
                BEGIN
                    ALTER TABLE dbo.Customers
                    DROP CONSTRAINT FK_Customers_Consumers_ConsumerId;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[Customers]', N'U')
                      AND name = N'IX_Customers_ConsumerId'
                )
                BEGIN
                    DROP INDEX IX_Customers_ConsumerId ON dbo.Customers;
                END;

                IF COL_LENGTH(N'dbo.Customers', N'ConsumerId') IS NOT NULL
                BEGIN
                    ALTER TABLE dbo.Customers
                    DROP COLUMN ConsumerId;
                END;
                """);
        }
    }
}
