SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.MemberUploads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MemberUploads
    (
        UploadId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MemberUploads PRIMARY KEY,
        UserId INT NOT NULL,
        Title NVARCHAR(100) NOT NULL,
        ActivityName NVARCHAR(80) NOT NULL,
        ActivityDate DATE NOT NULL,
        ProofUrl NVARCHAR(400) NOT NULL,
        DistanceKm DECIMAL(6,2) NOT NULL,
        MovingTimeSec INT NOT NULL,
        Steps INT NULL,
        AvgPaceSecPerKm INT NULL,
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_MemberUploads_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_MemberUploads_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MemberUploads_UserId_CreatedAt' AND object_id = OBJECT_ID(N'dbo.MemberUploads'))
BEGIN
    CREATE INDEX IX_MemberUploads_UserId_CreatedAt
        ON dbo.MemberUploads (UserId ASC, CreatedAt DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MemberUploads_ActivityDate' AND object_id = OBJECT_ID(N'dbo.MemberUploads'))
BEGIN
    CREATE INDEX IX_MemberUploads_ActivityDate
        ON dbo.MemberUploads (ActivityDate DESC);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_Create
    @UserID INT,
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
                THEN CONVERT(INT, ROUND(@MovingTimeSec / CONVERT(DECIMAL(18,6), @DistanceKm), 0))
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
        AvgPaceSecPerKm
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
        @AvgPaceSecPerKm
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
    WHERE UploadId = CONVERT(INT, SCOPE_IDENTITY());
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_GetMy
    @UserID INT,
    @Page INT = 1,
    @PageSize INT = 20,
    @Sort NVARCHAR(50) = N'createdAt_desc'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page < 1 SET @Page = 1;
    IF @PageSize < 1 SET @PageSize = 20;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    DECLARE @NormalizedSort NVARCHAR(50) = LOWER(LTRIM(RTRIM(ISNULL(@Sort, N'createdat_desc'))));

    IF @NormalizedSort NOT IN (N'createdat_desc', N'activitydate_desc', N'longestdistance', N'bestpace')
    BEGIN
        SET @NormalizedSort = N'createdat_desc';
    END;

    DECLARE @OrderBy NVARCHAR(100) =
        CASE @NormalizedSort
            WHEN N'activitydate_desc' THEN N'ActivityDate DESC, UploadId DESC'
            WHEN N'longestdistance' THEN N'DistanceKm DESC, UploadId DESC'
            WHEN N'bestpace' THEN N'AvgPaceSecPerKm ASC, UploadId DESC'
            ELSE N'CreatedAt DESC, UploadId DESC'
        END;

    DECLARE @Sql NVARCHAR(MAX) = N'
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
ORDER BY ' + @OrderBy + N'
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) AS TotalCount
FROM dbo.MemberUploads
WHERE UserId = @UserID;';

    EXEC sp_executesql
        @Sql,
        N'@UserID INT, @Offset INT, @PageSize INT',
        @UserID = @UserID,
        @Offset = @Offset,
        @PageSize = @PageSize;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_GetAll
    @Page INT = 1,
    @PageSize INT = 20,
    @Sort NVARCHAR(50) = N'createdAt_desc'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page < 1 SET @Page = 1;
    IF @PageSize < 1 SET @PageSize = 20;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    DECLARE @NormalizedSort NVARCHAR(50) = LOWER(LTRIM(RTRIM(ISNULL(@Sort, N'createdat_desc'))));

    IF @NormalizedSort NOT IN (N'createdat_desc', N'activitydate_desc', N'longestdistance', N'bestpace')
    BEGIN
        SET @NormalizedSort = N'createdat_desc';
    END;

    DECLARE @OrderBy NVARCHAR(100) =
        CASE @NormalizedSort
            WHEN N'activitydate_desc' THEN N'ActivityDate DESC, UploadId DESC'
            WHEN N'longestdistance' THEN N'DistanceKm DESC, UploadId DESC'
            WHEN N'bestpace' THEN N'AvgPaceSecPerKm ASC, UploadId DESC'
            ELSE N'CreatedAt DESC, UploadId DESC'
        END;

    DECLARE @Sql NVARCHAR(MAX) = N'
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
ORDER BY ' + @OrderBy + N'
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) AS TotalCount
FROM dbo.MemberUploads;';

    EXEC sp_executesql
        @Sql,
        N'@Offset INT, @PageSize INT',
        @Offset = @Offset,
        @PageSize = @PageSize;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_Update
    @UploadID INT,
    @UserID INT,
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
                THEN CONVERT(INT, ROUND(@MovingTimeSec / CONVERT(DECIMAL(18,6), @DistanceKm), 0))
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
    END;

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
GO

CREATE OR ALTER PROCEDURE dbo.sp_MemberUpload_Delete
    @UploadID INT,
    @UserID INT,
    @IsAdmin BIT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.MemberUploads
    WHERE UploadId = @UploadID
      AND (@IsAdmin = 1 OR UserId = @UserID);

    SELECT CONVERT(BIT, CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END) AS Deleted;
END;
GO
