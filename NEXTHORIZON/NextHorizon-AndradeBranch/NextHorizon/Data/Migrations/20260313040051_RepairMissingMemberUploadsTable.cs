using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Migrations
{
    /// <inheritdoc />
    public partial class RepairMissingMemberUploadsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Consumers]', N'U') IS NULL
                    THROW 51120, 'Repair blocked: [dbo].[Consumers] is required before restoring [dbo].[MemberUploads].', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[Consumers]', N'U')
                      AND c.name = N'consumer_id'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                )
                    THROW 51121, 'Repair blocked: [dbo].[Consumers].[consumer_id] must be NOT NULL INT.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes i
                    INNER JOIN sys.index_columns ic
                        ON ic.object_id = i.object_id
                       AND ic.index_id = i.index_id
                    INNER JOIN sys.columns c
                        ON c.object_id = ic.object_id
                       AND c.column_id = ic.column_id
                    WHERE i.object_id = OBJECT_ID(N'[dbo].[Consumers]', N'U')
                      AND i.is_hypothetical = 0
                      AND (i.is_primary_key = 1 OR i.is_unique = 1 OR i.is_unique_constraint = 1)
                    GROUP BY i.index_id
                    HAVING COUNT(*) = 1
                       AND MAX(CASE WHEN c.name = N'consumer_id' THEN 1 ELSE 0 END) = 1
                )
                    THROW 51122, 'Repair blocked: [dbo].[Consumers].[consumer_id] must have a single-column UNIQUE or PRIMARY KEY index.', 1;

                IF OBJECT_ID(N'[dbo].[MemberUploads]', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.MemberUploads
                    (
                        UploadId INT NOT NULL IDENTITY(1,1),
                        UserId INT NOT NULL,
                        Title NVARCHAR(100) NOT NULL,
                        ActivityName NVARCHAR(80) NOT NULL,
                        ActivityDate DATE NOT NULL,
                        ProofUrl NVARCHAR(400) NOT NULL,
                        DistanceKm DECIMAL(6,2) NOT NULL,
                        MovingTimeSec INT NOT NULL,
                        Steps INT NULL,
                        AvgPaceSecPerKm INT NULL,
                        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MemberUploads_CreatedAt DEFAULT SYSUTCDATETIME(),
                        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_MemberUploads_UpdatedAt DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT PK_MemberUploads PRIMARY KEY (UploadId)
                    );
                END;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND c.name = N'UploadId'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                      AND c.is_identity = 1
                )
                    THROW 51123, 'Repair blocked: [dbo].[MemberUploads].[UploadId] must be NOT NULL INT IDENTITY.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND c.name = N'UserId'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                )
                    THROW 51124, 'Repair blocked: [dbo].[MemberUploads].[UserId] must be NOT NULL INT.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND c.name = N'Title'
                      AND t.name = N'nvarchar'
                      AND c.max_length = 200
                      AND c.is_nullable = 0
                )
                    THROW 51125, 'Repair blocked: [dbo].[MemberUploads].[Title] must be NOT NULL NVARCHAR(100).', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND c.name = N'ActivityName'
                      AND t.name = N'nvarchar'
                      AND c.max_length = 160
                      AND c.is_nullable = 0
                )
                    THROW 51126, 'Repair blocked: [dbo].[MemberUploads].[ActivityName] must be NOT NULL NVARCHAR(80).', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND c.name = N'ActivityDate'
                      AND t.name = N'date'
                      AND c.is_nullable = 0
                )
                    THROW 51127, 'Repair blocked: [dbo].[MemberUploads].[ActivityDate] must be NOT NULL DATE.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND c.name = N'ProofUrl'
                      AND t.name = N'nvarchar'
                      AND c.max_length = 800
                      AND c.is_nullable = 0
                )
                    THROW 51128, 'Repair blocked: [dbo].[MemberUploads].[ProofUrl] must be NOT NULL NVARCHAR(400).', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND c.name = N'DistanceKm'
                      AND t.name = N'decimal'
                      AND c.precision = 6
                      AND c.scale = 2
                      AND c.is_nullable = 0
                )
                    THROW 51129, 'Repair blocked: [dbo].[MemberUploads].[DistanceKm] must be NOT NULL DECIMAL(6,2).', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND c.name = N'MovingTimeSec'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                )
                    THROW 51130, 'Repair blocked: [dbo].[MemberUploads].[MovingTimeSec] must be NOT NULL INT.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.key_constraints kc
                    INNER JOIN sys.index_columns ic
                        ON ic.object_id = kc.parent_object_id
                       AND ic.index_id = kc.unique_index_id
                    INNER JOIN sys.columns c
                        ON c.object_id = ic.object_id
                       AND c.column_id = ic.column_id
                    WHERE kc.parent_object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U')
                      AND kc.[type] = N'PK'
                    GROUP BY kc.name
                    HAVING COUNT(*) = 1
                       AND MAX(CASE WHEN c.name = N'UploadId' THEN 1 ELSE 0 END) = 1
                )
                    THROW 51131, 'Repair blocked: [dbo].[MemberUploads] must have a single-column primary key on [UploadId].', 1;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_User_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_User_UserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_Users_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_Users_UserId;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_Consumers_UserId')
                    ALTER TABLE dbo.MemberUploads WITH CHECK
                        ADD CONSTRAINT FK_MemberUploads_Consumers_UserId
                        FOREIGN KEY (UserId) REFERENCES dbo.Consumers(consumer_id) ON DELETE NO ACTION;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U') AND name = N'IX_MemberUploads_UserId_CreatedAt')
                    CREATE INDEX IX_MemberUploads_UserId_CreatedAt ON dbo.MemberUploads (UserId ASC, CreatedAt DESC);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MemberUploads]', N'U') AND name = N'IX_MemberUploads_ActivityDate')
                    CREATE INDEX IX_MemberUploads_ActivityDate ON dbo.MemberUploads (ActivityDate DESC);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally non-destructive: this repair migration restores a missing production table.
        }
    }
}
