using System.Data;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models;

namespace NextHorizon.Services;

public sealed class QaAgentsService : IQaAgentsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<QaAgentsService> _logger;

    public QaAgentsService(ApplicationDbContext dbContext, ILogger<QaAgentsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentSummaryViewModel>> GetAgentsAsync(CancellationToken cancellationToken)
    {
        var agentRecords = await LoadUsableAgentRecordsAsync(cancellationToken);

        if (agentRecords.Count == 0)
        {
            return Array.Empty<AgentSummaryViewModel>();
        }

        var resolvedCounts = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => item.Status == "Resolved"
                && item.AgentId.HasValue
                && item.EndTime != null)
            .GroupBy(item => item.AgentId!.Value)
            .Select(group => new
            {
                AgentUserId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.AgentUserId, item => item.Count, cancellationToken);

        var reviewSummaries = await (
            from review in _dbContext.QaReviews.AsNoTracking()
            join faq in _dbContext.SupportFaqRecords.AsNoTracking()
                on review.SupportFaqId equals faq.Id
            where faq.Status == "Resolved"
                && faq.EndTime != null
            group review by review.AgentUserId
            into grouped
            select new
            {
                AgentUserId = grouped.Key,
                RatedTickets = grouped.Count(),
                AverageOverallPercent = grouped.Average(item => item.OverallPercent)
            })
            .ToDictionaryAsync(
                item => item.AgentUserId,
                item => new
                {
                    item.RatedTickets,
                    Score = Math.Round((double)item.AverageOverallPercent / 20d, 1)
                },
                cancellationToken);

        var agents = agentRecords
            .GroupBy(item => item.UserId)
            .Select(group =>
            {
                var displayName = group
                    .Where(item => !string.IsNullOrWhiteSpace(item.AgentName))
                    .Select(item => item.AgentName!.Trim())
                    .FirstOrDefault();

                resolvedCounts.TryGetValue(group.Key, out var resolvedTickets);

                var ratedTickets = 0;
                var score = 0d;
                if (reviewSummaries.TryGetValue(group.Key, out var summary))
                {
                    ratedTickets = summary.RatedTickets;
                    score = summary.Score;
                }

                return new AgentSummaryViewModel
                {
                    AgentUserId = group.Key,
                    AgentName = string.IsNullOrWhiteSpace(displayName) ? $"Agent {group.Key}" : displayName,
                    ResolvedTickets = resolvedTickets,
                    RatedTickets = ratedTickets,
                    Score = score
                };
            })
            .OrderByDescending(item => item.ResolvedTickets)
            .ThenBy(item => item.AgentName)
            .ToList();

        return agents;
    }

    private async Task<List<QaAgentRecord>> LoadUsableAgentRecordsAsync(CancellationToken cancellationToken)
    {
        var skippedRowCount = 0;
        var records = new List<QaAgentRecord>();
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT UserID, AgentName, AgentStatus
                FROM dbo.Agents
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var userIdOrdinal = reader.GetOrdinal("UserID");
            var agentNameOrdinal = reader.GetOrdinal("AgentName");
            var agentStatusOrdinal = reader.GetOrdinal("AgentStatus");

            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(userIdOrdinal))
                {
                    skippedRowCount++;
                    continue;
                }

                records.Add(new QaAgentRecord(
                    reader.GetInt32(userIdOrdinal),
                    reader.IsDBNull(agentNameOrdinal) ? null : reader.GetString(agentNameOrdinal),
                    reader.IsDBNull(agentStatusOrdinal) ? null : reader.GetString(agentStatusOrdinal)));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        if (skippedRowCount > 0)
        {
            _logger.LogWarning(
                "Skipped {SkippedRowCount} malformed row(s) from dbo.Agents while loading the QA Agents page because UserID was null.",
                skippedRowCount);
        }

        return records;
    }

    private sealed record QaAgentRecord(
        int UserId,
        string? AgentName,
        string? AgentStatus);
}
