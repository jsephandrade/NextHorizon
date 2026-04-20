using System.Data;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaRatedHistoryService : IQaRatedHistoryService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _dbContext;

    public QaRatedHistoryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QaRatedHistoryResponse> GetRatedHistoryAsync(
        int page,
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        string? score,
        CancellationToken cancellationToken)
    {
        var (startUtc, endExclusiveUtc) = ResolveDateRange(range, from, to);

        IQueryable<QaReview> reviewQuery = _dbContext.QaReviews
            .AsNoTracking()
            .Include(item => item.CategoryScores);

        if (startUtc.HasValue)
        {
            reviewQuery = reviewQuery.Where(item => item.CreatedAtUtc >= startUtc.Value);
        }

        if (endExclusiveUtc.HasValue)
        {
            reviewQuery = reviewQuery.Where(item => item.CreatedAtUtc < endExclusiveUtc.Value);
        }

        var reviews = await reviewQuery
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
        {
            return new QaRatedHistoryResponse(0, 1, DefaultPageSize, 1, false, false, Array.Empty<QaRatedHistoryItem>());
        }

        var supportFaqIds = reviews
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var supportFaqs = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var agentUserIds = reviews
            .Select(item => item.AgentUserId)
            .Distinct()
            .ToArray();

        var agentNames = await LoadAgentNamesAsync(agentUserIds, cancellationToken);
        var normalizedSearch = (search ?? string.Empty).Trim().ToLowerInvariant();

        var items = reviews
            .Where(item => MatchesScoreBand((double)item.OverallPercent, score))
            .Select(item =>
            {
                var agentName = agentNames.TryGetValue(item.AgentUserId, out var resolvedAgentName)
                    ? resolvedAgentName
                    : "Unassigned";
                var concernFrom = supportFaqs.TryGetValue(item.SupportFaqId, out var supportFaq)
                    ? QaConcernFormatting.NormalizeConcernFrom(supportFaq.UserType)
                    : "Unknown";
                var ratedAtLabel = item.CreatedAtUtc.ToString("MMM dd, yyyy");

                return new QaRatedHistoryItem(
                    item.SupportFaqId,
                    agentName,
                    concernFrom,
                    QaConcernFormatting.NormalizeReviewerName(item.ReviewerName),
                    ratedAtLabel,
                    item.CreatedAtUtc.ToString("yyyy-MM-dd"),
                    BuildCategoryScores(item),
                    BuildCategorySummary(item),
                    Math.Round((double)item.OverallPercent, 1));
            })
            .Where(item => string.IsNullOrWhiteSpace(normalizedSearch)
                || BuildSearchText(item).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var totalCount = items.Count;
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)DefaultPageSize);
        var currentPage = Math.Clamp(page, 1, totalPages);
        var pagedItems = items
            .Skip((currentPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToList();

        return new QaRatedHistoryResponse(
            totalCount,
            currentPage,
            DefaultPageSize,
            totalPages,
            currentPage > 1,
            currentPage < totalPages,
            pagedItems);
    }

    private async Task<Dictionary<int, string>> LoadAgentNamesAsync(int[] agentUserIds, CancellationToken cancellationToken)
    {
        if (agentUserIds.Length == 0)
        {
            return new Dictionary<int, string>();
        }

        var rows = new List<QaAgentRecord>();
        var agentUserIdSet = agentUserIds.ToHashSet();
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
                WHERE UserID IS NOT NULL
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var userIdOrdinal = reader.GetOrdinal("UserID");
            var agentNameOrdinal = reader.GetOrdinal("AgentName");
            var agentStatusOrdinal = reader.GetOrdinal("AgentStatus");

            while (await reader.ReadAsync(cancellationToken))
            {
                var userId = reader.GetInt32(userIdOrdinal);
                if (!agentUserIdSet.Contains(userId))
                {
                    continue;
                }

                rows.Add(new QaAgentRecord(
                    userId,
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

        return rows
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var availableName = group
                        .Where(item => string.Equals(item.AgentStatus, "available", StringComparison.OrdinalIgnoreCase))
                        .Select(item => item.AgentName)
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

                    if (!string.IsNullOrWhiteSpace(availableName))
                    {
                        return availableName.Trim();
                    }

                    var anyName = group
                        .Select(item => item.AgentName)
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

                    return string.IsNullOrWhiteSpace(anyName) ? $"Agent {group.Key}" : anyName.Trim();
                });
    }

    private static string BuildSearchText(QaRatedHistoryItem item)
    {
        return string.Join(' ', item.SupportFaqId, item.AgentName, item.ConcernFrom, item.ReviewerName, item.RatedAtLabel).ToLowerInvariant();
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
