using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberUploadSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Steps",
                table: "MemberUploads",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_Create
                    @UserID NVARCHAR(450),
                    @Title NVARCHAR(100),
                    @ActivityName NVARCHAR(80),
                    @ActivityDate DATE,
                    @ProofUrl NVARCHAR(400),
                    @DistanceKm DECIMAL(6,2),
                    @MovingTimeSec INT,
                    @Steps INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AvgPaceSecPerKm INT =
                        CASE
                            WHEN @DistanceKm > 0
                                THEN CAST(ROUND(@MovingTimeSec / NULLIF(CAST(@DistanceKm AS FLOAT), 0), 0) AS INT)
                            ELSE NULL
                        END;

                    INSERT INTO dbo.MemberUploads
                    (
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        Steps,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    )
                    VALUES
                    (
                        @UserID,
                        @Title,
                        @ActivityName,
                        @ActivityDate,
                        @ProofUrl,
                        @DistanceKm,
                        @MovingTimeSec,
                        @Steps,
                        @AvgPaceSecPerKm,
                        SYSUTCDATETIME(),
                        SYSUTCDATETIME()
                    );

                    SELECT
                        UploadId,
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        Steps,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    FROM dbo.MemberUploads
                    WHERE UploadId = CAST(SCOPE_IDENTITY() AS INT);
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_GetMy
                    @UserID NVARCHAR(450),
                    @Page INT,
                    @PageSize INT,
                    @Sort NVARCHAR(50)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Page < 1 SET @Page = 1;
                    IF @PageSize < 1 SET @PageSize = 20;
                    IF @PageSize > 100 SET @PageSize = 100;
                    IF @Sort IS NULL OR LTRIM(RTRIM(@Sort)) = N'' SET @Sort = N'createdAt_desc';

                    SELECT
                        UploadId,
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        Steps,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    FROM dbo.MemberUploads
                    WHERE UserId = @UserID
                    ORDER BY
                        CASE WHEN @Sort = N'longestDistance' THEN DistanceKm END DESC,
                        CASE WHEN @Sort = N'bestPace' THEN ISNULL(AvgPaceSecPerKm, 2147483647) END ASC,
                        CASE WHEN @Sort = N'activityDate_desc' THEN ActivityDate END DESC,
                        CreatedAt DESC,
                        UploadId DESC
                    OFFSET (@Page - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(1) AS TotalCount
                    FROM dbo.MemberUploads
                    WHERE UserId = @UserID;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_GetAll
                    @Page INT,
                    @PageSize INT,
                    @Sort NVARCHAR(50)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Page < 1 SET @Page = 1;
                    IF @PageSize < 1 SET @PageSize = 20;
                    IF @PageSize > 100 SET @PageSize = 100;
                    IF @Sort IS NULL OR LTRIM(RTRIM(@Sort)) = N'' SET @Sort = N'createdAt_desc';

                    SELECT
                        UploadId,
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        Steps,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    FROM dbo.MemberUploads
                    ORDER BY
                        CASE WHEN @Sort = N'longestDistance' THEN DistanceKm END DESC,
                        CASE WHEN @Sort = N'bestPace' THEN ISNULL(AvgPaceSecPerKm, 2147483647) END ASC,
                        CASE WHEN @Sort = N'activityDate_desc' THEN ActivityDate END DESC,
                        CreatedAt DESC,
                        UploadId DESC
                    OFFSET (@Page - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(1) AS TotalCount
                    FROM dbo.MemberUploads;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_Update
                    @UploadID INT,
                    @UserID NVARCHAR(450),
                    @IsAdmin BIT,
                    @Title NVARCHAR(100),
                    @ActivityName NVARCHAR(80),
                    @ActivityDate DATE,
                    @ProofUrl NVARCHAR(400) = NULL,
                    @DistanceKm DECIMAL(6,2),
                    @MovingTimeSec INT,
                    @Steps INT = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AvgPaceSecPerKm INT =
                        CASE
                            WHEN @DistanceKm > 0
                                THEN CAST(ROUND(@MovingTimeSec / NULLIF(CAST(@DistanceKm AS FLOAT), 0), 0) AS INT)
                            ELSE NULL
                        END;

                    UPDATE dbo.MemberUploads
                    SET
                        Title = @Title,
                        ActivityName = @ActivityName,
                        ActivityDate = @ActivityDate,
                        ProofUrl = COALESCE(@ProofUrl, ProofUrl),
                        DistanceKm = @DistanceKm,
                        MovingTimeSec = @MovingTimeSec,
                        Steps = @Steps,
                        AvgPaceSecPerKm = @AvgPaceSecPerKm,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE UploadId = @UploadID
                        AND (@IsAdmin = 1 OR UserId = @UserID);

                    IF @@ROWCOUNT = 0
                    BEGIN
                        RETURN;
                    END

                    SELECT
                        UploadId,
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        Steps,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    FROM dbo.MemberUploads
                    WHERE UploadId = @UploadID;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_Create
                    @UserID NVARCHAR(450),
                    @Title NVARCHAR(100),
                    @ActivityName NVARCHAR(80),
                    @ActivityDate DATE,
                    @ProofUrl NVARCHAR(400),
                    @DistanceKm DECIMAL(6,2),
                    @MovingTimeSec INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AvgPaceSecPerKm INT =
                        CASE
                            WHEN @DistanceKm > 0
                                THEN CAST(ROUND(@MovingTimeSec / NULLIF(CAST(@DistanceKm AS FLOAT), 0), 0) AS INT)
                            ELSE NULL
                        END;

                    INSERT INTO dbo.MemberUploads
                    (
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    )
                    VALUES
                    (
                        @UserID,
                        @Title,
                        @ActivityName,
                        @ActivityDate,
                        @ProofUrl,
                        @DistanceKm,
                        @MovingTimeSec,
                        @AvgPaceSecPerKm,
                        SYSUTCDATETIME(),
                        SYSUTCDATETIME()
                    );

                    SELECT
                        UploadId,
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    FROM dbo.MemberUploads
                    WHERE UploadId = CAST(SCOPE_IDENTITY() AS INT);
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_GetMy
                    @UserID NVARCHAR(450),
                    @Page INT,
                    @PageSize INT,
                    @Sort NVARCHAR(50)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Page < 1 SET @Page = 1;
                    IF @PageSize < 1 SET @PageSize = 20;
                    IF @PageSize > 100 SET @PageSize = 100;
                    IF @Sort IS NULL OR LTRIM(RTRIM(@Sort)) = N'' SET @Sort = N'createdAt_desc';

                    SELECT
                        UploadId,
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    FROM dbo.MemberUploads
                    WHERE UserId = @UserID
                    ORDER BY
                        CASE WHEN @Sort = N'longestDistance' THEN DistanceKm END DESC,
                        CASE WHEN @Sort = N'bestPace' THEN ISNULL(AvgPaceSecPerKm, 2147483647) END ASC,
                        CASE WHEN @Sort = N'activityDate_desc' THEN ActivityDate END DESC,
                        CreatedAt DESC,
                        UploadId DESC
                    OFFSET (@Page - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(1) AS TotalCount
                    FROM dbo.MemberUploads
                    WHERE UserId = @UserID;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_GetAll
                    @Page INT,
                    @PageSize INT,
                    @Sort NVARCHAR(50)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Page < 1 SET @Page = 1;
                    IF @PageSize < 1 SET @PageSize = 20;
                    IF @PageSize > 100 SET @PageSize = 100;
                    IF @Sort IS NULL OR LTRIM(RTRIM(@Sort)) = N'' SET @Sort = N'createdAt_desc';

                    SELECT
                        UploadId,
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    FROM dbo.MemberUploads
                    ORDER BY
                        CASE WHEN @Sort = N'longestDistance' THEN DistanceKm END DESC,
                        CASE WHEN @Sort = N'bestPace' THEN ISNULL(AvgPaceSecPerKm, 2147483647) END ASC,
                        CASE WHEN @Sort = N'activityDate_desc' THEN ActivityDate END DESC,
                        CreatedAt DESC,
                        UploadId DESC
                    OFFSET (@Page - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(1) AS TotalCount
                    FROM dbo.MemberUploads;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_Update
                    @UploadID INT,
                    @UserID NVARCHAR(450),
                    @IsAdmin BIT,
                    @Title NVARCHAR(100),
                    @ActivityName NVARCHAR(80),
                    @ActivityDate DATE,
                    @ProofUrl NVARCHAR(400) = NULL,
                    @DistanceKm DECIMAL(6,2),
                    @MovingTimeSec INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @AvgPaceSecPerKm INT =
                        CASE
                            WHEN @DistanceKm > 0
                                THEN CAST(ROUND(@MovingTimeSec / NULLIF(CAST(@DistanceKm AS FLOAT), 0), 0) AS INT)
                            ELSE NULL
                        END;

                    UPDATE dbo.MemberUploads
                    SET
                        Title = @Title,
                        ActivityName = @ActivityName,
                        ActivityDate = @ActivityDate,
                        ProofUrl = COALESCE(@ProofUrl, ProofUrl),
                        DistanceKm = @DistanceKm,
                        MovingTimeSec = @MovingTimeSec,
                        AvgPaceSecPerKm = @AvgPaceSecPerKm,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE UploadId = @UploadID
                        AND (@IsAdmin = 1 OR UserId = @UserID);

                    IF @@ROWCOUNT = 0
                    BEGIN
                        RETURN;
                    END

                    SELECT
                        UploadId,
                        UserId,
                        Title,
                        ActivityName,
                        ActivityDate,
                        ProofUrl,
                        DistanceKm,
                        MovingTimeSec,
                        AvgPaceSecPerKm,
                        CreatedAt,
                        UpdatedAt
                    FROM dbo.MemberUploads
                    WHERE UploadId = @UploadID;
                END;
                """);

            migrationBuilder.DropColumn(
                name: "Steps",
                table: "MemberUploads");
        }
    }
}
