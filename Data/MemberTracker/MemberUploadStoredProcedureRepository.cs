using System.Data;
using System.Data.Common;
using NextHorizon.Data;
using NextHorizon.Models;
using NextHorizon.Modules.MemberTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace NextHorizon.Modules.MemberTracker.Data;

public interface IMemberUploadRepository
{
    Task<MemberUpload> CreateAsync(CreateMemberUploadDbRequest request, CancellationToken cancellationToken);

    Task<MemberUpload?> GetByIdAsync(int uploadId, CancellationToken cancellationToken);

    Task<PagedResult<MemberUpload>> GetMyUploadsAsync(int userId, UploadQueryOptions queryOptions, CancellationToken cancellationToken);

    Task<PagedResult<MemberUpload>> GetAllUploadsAsync(UploadQueryOptions queryOptions, CancellationToken cancellationToken);

    Task<MemberUpload?> UpdateAsync(UpdateMemberUploadDbRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(DeleteMemberUploadDbRequest request, CancellationToken cancellationToken);
}

public sealed record UploadQueryOptions(int PageNumber, int PageSize, string Sort);

public sealed record CreateMemberUploadDbRequest(
    int UserId,
    string Title,
    string ActivityName,
    DateTime ActivityDate,
    string ProofUrl,
    decimal DistanceKm,
    int MovingTimeSec,
    int? Steps);

public sealed record UpdateMemberUploadDbRequest(
    int UploadId,
    int UserId,
    bool IsAdmin,
    string Title,
    string ActivityName,
    DateTime ActivityDate,
    string? ProofUrl,
    decimal DistanceKm,
    int MovingTimeSec,
    int? Steps);

public sealed record DeleteMemberUploadDbRequest(int UploadId, int UserId, bool IsAdmin);

public class MemberUploadStoredProcedureRepository : IMemberUploadRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MemberUploadStoredProcedureRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MemberUpload> CreateAsync(CreateMemberUploadDbRequest request, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MemberUpload_Create";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@UserID", request.UserId, DbType.Int32);
            AddParameter(command, "@Title", request.Title, DbType.String);
            AddParameter(command, "@ActivityName", request.ActivityName, DbType.String);
            AddParameter(command, "@ActivityDate", request.ActivityDate, DbType.Date);
            AddParameter(command, "@ProofUrl", request.ProofUrl, DbType.String);
            AddParameter(command, "@DistanceKm", request.DistanceKm, DbType.Decimal);
            AddParameter(command, "@MovingTimeSec", request.MovingTimeSec, DbType.Int32);
            AddParameter(command, "@Steps", request.Steps, DbType.Int32);

            var upload = await ReadSingleUploadAsync(command, cancellationToken);
            return upload ?? throw new InvalidOperationException("Stored procedure sp_MemberUpload_Create did not return a row.");
        }, cancellationToken);

    public Task<MemberUpload?> GetByIdAsync(int uploadId, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                                  SELECT UploadId, UserId, Title, ActivityName, ActivityDate, ProofUrl, DistanceKm, MovingTimeSec, Steps, AvgPaceSecPerKm, CreatedAt, UpdatedAt
                                  FROM dbo.MemberUploads
                                  WHERE UploadId = @UploadID;
                                  """;
            command.CommandType = CommandType.Text;
            AddParameter(command, "@UploadID", uploadId, DbType.Int32);

            return await ReadSingleUploadAsync(command, cancellationToken);
        }, cancellationToken);

    public Task<PagedResult<MemberUpload>> GetMyUploadsAsync(int userId, UploadQueryOptions queryOptions, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MemberUpload_GetMy";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@UserID", userId, DbType.Int32);
            AddParameter(command, "@Page", queryOptions.PageNumber, DbType.Int32);
            AddParameter(command, "@PageSize", queryOptions.PageSize, DbType.Int32);
            AddParameter(command, "@Sort", queryOptions.Sort, DbType.String);

            return await ReadPagedResultAsync(command, queryOptions.PageNumber, queryOptions.PageSize, cancellationToken);
        }, cancellationToken);

    public Task<PagedResult<MemberUpload>> GetAllUploadsAsync(UploadQueryOptions queryOptions, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MemberUpload_GetAll";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@Page", queryOptions.PageNumber, DbType.Int32);
            AddParameter(command, "@PageSize", queryOptions.PageSize, DbType.Int32);
            AddParameter(command, "@Sort", queryOptions.Sort, DbType.String);

            return await ReadPagedResultAsync(command, queryOptions.PageNumber, queryOptions.PageSize, cancellationToken);
        }, cancellationToken);

    public Task<MemberUpload?> UpdateAsync(UpdateMemberUploadDbRequest request, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MemberUpload_Update";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@UploadID", request.UploadId, DbType.Int32);
            AddParameter(command, "@UserID", request.UserId, DbType.Int32);
            AddParameter(command, "@IsAdmin", request.IsAdmin, DbType.Boolean);
            AddParameter(command, "@Title", request.Title, DbType.String);
            AddParameter(command, "@ActivityName", request.ActivityName, DbType.String);
            AddParameter(command, "@ActivityDate", request.ActivityDate, DbType.Date);
            AddParameter(command, "@ProofUrl", request.ProofUrl, DbType.String);
            AddParameter(command, "@DistanceKm", request.DistanceKm, DbType.Decimal);
            AddParameter(command, "@MovingTimeSec", request.MovingTimeSec, DbType.Int32);
            AddParameter(command, "@Steps", request.Steps, DbType.Int32);

            return await ReadSingleUploadAsync(command, cancellationToken);
        }, cancellationToken);

    public Task<bool> DeleteAsync(DeleteMemberUploadDbRequest request, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MemberUpload_Delete";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@UploadID", request.UploadId, DbType.Int32);
            AddParameter(command, "@UserID", request.UserId, DbType.Int32);
            AddParameter(command, "@IsAdmin", request.IsAdmin, DbType.Boolean);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return false;
            }

            return reader.GetBoolean(reader.GetOrdinal("Deleted"));
        }, cancellationToken);

    private async Task<PagedResult<MemberUpload>> ReadPagedResultAsync(
        DbCommand command,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var items = new List<MemberUpload>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapUpload(reader));
        }

        var totalCount = 0;
        if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
        {
            totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
        }

        return new PagedResult<MemberUpload>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items,
        };
    }

    private async Task<MemberUpload?> ReadSingleUploadAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapUpload(reader);
    }

    private async Task<T> WithOpenConnectionAsync<T>(
        Func<DbConnection, Task<T>> action,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            return await action(connection);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static MemberUpload MapUpload(DbDataReader reader)
    {
        return new MemberUpload
        {
            UploadId = reader.GetInt32(reader.GetOrdinal("UploadId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            ActivityName = reader.GetString(reader.GetOrdinal("ActivityName")),
            ActivityDate = reader.GetDateTime(reader.GetOrdinal("ActivityDate")),
            ProofUrl = reader.GetString(reader.GetOrdinal("ProofUrl")),
            DistanceKm = reader.GetDecimal(reader.GetOrdinal("DistanceKm")),
            MovingTimeSec = reader.GetInt32(reader.GetOrdinal("MovingTimeSec")),
            Steps = GetNullableInt(reader, "Steps"),
            AvgPaceSecPerKm = GetNullableInt(reader, "AvgPaceSecPerKm"),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
        };
    }

    private static int? GetNullableInt(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static void AddParameter(DbCommand command, string name, object? value, DbType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}

