using System.Data;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaAgentTicketsService : IQaAgentTicketsService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _dbContext;

    public QaAgentTicketsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QaAgentTicketsResponse?> GetAgentTicketsAsync(
        int agentUserId,
        int awaitingPage,
        int ratedPage,
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        string? score,
        CancellationToken cancellationToken)
    {
        var (startUtc, endExclusiveUtc) = ResolveDateRange(range, from, to);

        var resolvedFaqQuery = _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => item.Status == "Resolved"
                && item.EndTime != null);

        if (startUtc.HasValue)
        {
            resolvedFaqQuery = resolvedFaqQuery.Where(item => item.EndTime >= startUtc.Value);
        }

        if (endExclusiveUtc.HasValue)
        {
            resolvedFaqQuery = resolvedFaqQuery.Where(item => item.EndTime < endExclusiveUtc.Value);
        }

        var resolvedFaqs = await resolvedFaqQuery
            .Select(item => new ResolvedConversationSeed(
                item.Id,
                item.AgentId,
                item.UserType,
                item.EndTime!.Value))
            .ToListAsync(cancellationToken);

        var agentName = await LoadAgentNameAsync(agentUserId, cancellationToken);

        if (resolvedFaqs.Count == 0)
        {
            return agentName is null
                ? null
                : new QaAgentTicketsResponse(
                    agentUserId,
                    agentName,
                    0,
                    0,
                    1,
                    1,
                    DefaultPageSize,
                    1,
                    1,
                    false,
                    false,
                    false,
                    false,
                    Array.Empty<QaAgentTicketItem>(),
                    Array.Empty<QaAgentTicketItem>());
        }

        var supportFaqIds = resolvedFaqs
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var reviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Include(item => item.CategoryScores)
            .Where(item => supportFaqIds.Contains(item.SupportFaqId) && item.AgentUserId == agentUserId)
            .ToListAsync(cancellationToken);

        var reviewBySupportFaqId = reviews
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.UpdatedAtUtc).First());

        var normalizedSearch = (search ?? string.Empty).Trim().ToLowerInvariant();

        var snapshots = resolvedFaqs
            .Select(item =>
            {
                reviewBySupportFaqId.TryGetValue(item.SupportFaqId, out var review);
                var concernFrom = QaConcernFormatting.NormalizeConcernFrom(item.UserType);
                var resolvedAtLabel = item.ResolvedAtUtc.ToString("MMM dd, yyyy hh:mm tt");

                return new QaAgentTicketSnapshot(
                    item.SupportFaqId,
                    agentName ?? $"Agent {agentUserId}",
                    concernFrom,
                    item.ResolvedAtUtc,
                    resolvedAtLabel,
                    item.AgentUserId == agentUserId,
                    review);
            })
            .Where(item => item.IsCurrentAgentAssignment || item.Review is not null)
            .Where(item => string.IsNullOrWhiteSpace(normalizedSearch)
                || BuildSearchText(item.SupportFaqId, item.AgentName, item.ConcernFrom, item.ResolvedAtLabel)
                    .Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.ResolvedAtUtc)
            .ThenByDescending(item => item.SupportFaqId)
            .ToList();

        var awaitingItems = snapshots
            .Where(item => item.Review is null)
            .Select(item => new QaAgentTicketItem(
                item.SupportFaqId,
                item.AgentName,
                item.ConcernFrom,
                item.ResolvedAtLabel,
                item.ResolvedAtUtc.ToString("yyyy-MM-dd"),
                false,
                null,
                string.Empty,
                string.Empty,
                Array.Empty<QaCategoryScoreItem>(),
                string.Empty))
            .ToList();

        var ratedItems = snapshots
            .Where(item => item.Review is not null)
            .Where(item => MatchesScoreBand((double)item.Review!.OverallPercent, score))
            .Select(item => new QaAgentTicketItem(
                item.SupportFaqId,
                item.AgentName,
                item.ConcernFrom,
                item.ResolvedAtLabel,
                item.ResolvedAtUtc.ToString("yyyy-MM-dd"),
                true,
                Math.Round((double)item.Review!.OverallPercent, 1),
                QaConcernFormatting.NormalizeReviewerName(item.Review!.ReviewerName),
                item.Review!.CreatedAtUtc.ToString("MMM dd, yyyy"),
                BuildCategoryScores(item.Review!),
                BuildCategorySummary(item.Review!)))
            .ToList();

        var awaitingTotalPages = awaitingItems.Count == 0
            ? 1
            : (int)Math.Ceiling(awaitingItems.Count / (double)DefaultPageSize);
        var ratedTotalPages = ratedItems.Count == 0
            ? 1
            : (int)Math.Ceiling(ratedItems.Count / (double)DefaultPageSize);
        var currentAwaitingPage = Math.Clamp(awaitingPage, 1, awaitingTotalPages);
        var currentRatedPage = Math.Clamp(ratedPage, 1, ratedTotalPages);
        var pagedAwaitingItems = awaitingItems
            .Skip((currentAwaitingPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToList();
        var pagedRatedItems = ratedItems
            .Skip((currentRatedPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToList();

        return new QaAgentTicketsResponse(
            agentUserId,
            agentName ?? $"Agent {agentUserId}",
            awaitingItems.Count,
            ratedItems.Count,
            currentAwaitingPage,
            currentRatedPage,
            DefaultPageSize,
            awaitingTotalPages,
            ratedTotalPages,
            currentAwaitingPage > 1,
            currentAwaitingPage < awaitingTotalPages,
            currentRatedPage > 1,
            currentRatedPage < ratedTotalPages,
            pagedAwaitingItems,
            pagedRatedItems);
    }

    private async Task<string?> LoadAgentNameAsync(int agentUserId, CancellationToken cancellationToken)
    {
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
                WHERE UserID = @UserID
                  AND UserID IS NOT NULL
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@UserID";
            parameter.Value = agentUserId;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<QaAgentRecord>();
            var userIdOrdinal = reader.GetOrdinal("UserID");
            var agentNameOrdinal = reader.GetOrdinal("AgentName");
            var agentStatusOrdinal = reader.GetOrdinal("AgentStatus");

            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new QaAgentRecord(
                    reader.GetInt32(userIdOrdinal),
                    reader.IsDBNull(agentNameOrdinal) ? null : reader.GetString(agentNameOrdinal),
                    reader.IsDBNull(agentStatusOrdinal) ? null : reader.GetString(agentStatusOrdinal)));
            }

            var availableName = rows
                .Where(item => string.Equals(item.AgentStatus, "available", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.AgentName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

            if (!string.IsNullOrWhiteSpace(availableName))
            {
                return availableName.Trim();
            }

            var anyName = rows
                .Select(item => item.AgentName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

            return string.IsNullOrWhiteSpace(anyName) ? null : anyName.Trim();
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string BuildSearchText(int supportFaqId, string agentName, string concernFrom, string resolvedAtLabel)
    {
        return string.Join(' ', supportFaqId, agentName, concernFrom, resolvedAtLabel).ToLowerInvariant();
    }

    private static bool MatchesScoreBand(double overallPercent, string? score)
    {
        var normalized = (score ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "high" => overallPercent >= 90d && overallPercent <= 100d,
            "good" => overallPercent >= 80d && overallPercent < 90d,
            "mid" => overallPercent >= 70d && overallPercent < 80d,
            "low" => overallPercent < 70d,
            _ => true
        };
    }

    private static (DateTime? StartUtc, DateTime? EndExclusiveUtc) ResolveDateRange(
        string? range,
        DateOnly? from,
        DateOnly? to)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedRange = (range ?? "custom").Trim().ToLowerInvariant();

        if (normalizedRange == "today")
        {
            var start = todayUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return (start, start.AddDays(1));
        }

        if (normalizedRange == "last7")
        {
            var startDay = todayUtc.AddDays(-6);
            var start = startDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return (start, todayUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1));
        }

        if (normalizedRange == "last30")
        {
            var startDay = todayUtc.AddDays(-29);
            var start = startDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return (start, todayUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1));
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            (from, to) = (to, from);
        }

        return (
            from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            to?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1));
    }

    private sealed record ResolvedConversationSeed(
        int SupportFaqId,
        int? AgentUserId,
        string UserType,
        DateTime ResolvedAtUtc);

    private sealed record QaAgentTicketSnapshot(
        int SupportFaqId,
        string AgentName,
        string ConcernFrom,
        DateTime ResolvedAtUtc,
        string ResolvedAtLabel,
        bool IsCurrentAgentAssignment,
        QaReview? Review);

    private sealed record QaAgentRecord(
        int UserId,
        string? AgentName,
        string? AgentStatus);

    private static IReadOnlyList<QaCategoryScoreItem> BuildCategoryScores(QaReview review)
    {
        return review.CategoryScores
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.QaReviewCategoryScoreId)
            .Select(item => new QaCategoryScoreItem(
                string.IsNullOrWhiteSpace(item.CategoryNameSnapshot) ? "QA Category" : item.CategoryNameSnapshot.Trim(),
                Math.Round((double)item.WeightedPoints, 1),
                Math.Round((double)item.AverageScore, 2),
                Math.Round((double)item.WeightPercentSnapshot, 2),
                item.DisplayOrder))
            .ToList();
    }

    private static string BuildCategorySummary(QaReview review)
    {
        var items = BuildCategoryScores(review);
        if (items.Count == 0)
        {
            return "No category breakdown recorded.";
        }

        return string.Join(", ", items.Select(item => $"{item.CategoryName} {item.WeightedPoints:0.0}/{item.WeightPercent:0.##}"));
    }
}
