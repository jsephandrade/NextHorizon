using NextHorizon.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260302113000_FixMemberUploadListProcedureCountScope")]
    public partial class FixMemberUploadListProcedureCountScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

                    ;WITH Base AS
                    (
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
                    )
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
                    FROM Base
                    ORDER BY
                        CASE WHEN @Sort = N'longestDistance' THEN DistanceKm END DESC,
                        CASE WHEN @Sort = N'bestPace' THEN ISNULL(AvgPaceSecPerKm, 2147483647) END ASC,
                        CASE WHEN @Sort = N'activityDate_desc' THEN ActivityDate END DESC,
                        CreatedAt DESC,
                        UploadId DESC
                    OFFSET (@Page - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(1) AS TotalCount
                    FROM Base;
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

                    ;WITH Base AS
                    (
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
                    )
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
                    FROM Base
                    ORDER BY
                        CASE WHEN @Sort = N'longestDistance' THEN DistanceKm END DESC,
                        CASE WHEN @Sort = N'bestPace' THEN ISNULL(AvgPaceSecPerKm, 2147483647) END ASC,
                        CASE WHEN @Sort = N'activityDate_desc' THEN ActivityDate END DESC,
                        CreatedAt DESC,
                        UploadId DESC
                    OFFSET (@Page - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(1) AS TotalCount
                    FROM Base;
                END;
                """);
        }
    }
}

