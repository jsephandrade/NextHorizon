using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.AgentDashboard;

namespace NextHorizon.Services;

public sealed class AgentRankingService : IAgentRankingService
{
    private const string QueueWelcomeMessage = "Thank you for reaching out regarding your concern. An agent will be assigned to you shortly. There are currently 3 people ahead of you in the queue.";
    private const string WaitingReplyMessage = "An agent will assist you shortly.";
    private static readonly TimeSpan MaxSaneAht = TimeSpan.FromHours(8);

    private readonly ApplicationDbContext _dbContext;

    public AgentRankingService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecomputeMonthAsync(AgentRankingMetricType metricType, DateTime periodStartUtc, CancellationToken cancellationToken)
    {
        var normalizedMonthStartUtc = NormalizeMonthStartUtc(periodStartUtc);
        switch (metricType)
        {
            case AgentRankingMetricType.QaScore:
                await RecomputeQaMonthsCoreAsync([normalizedMonthStartUtc], cancellationToken);
                return;

            case AgentRankingMetricType.AverageHandlingTime:
                await RecomputeAhtMonthCoreAsync(normalizedMonthStartUtc, cancellationToken);
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(metricType), metricType, "Unsupported ranking metric type.");
        }
    }

    public async Task RecomputeAllQaMonthsAsync(CancellationToken cancellationToken)
    {
        var reviewCreatedAtUtcValues = await (
                from review in _dbContext.QaReviews.AsNoTracking()
                join supportFaq in _dbContext.SupportFaqRecords.AsNoTracking()
                    on review.SupportFaqId equals supportFaq.Id
                where supportFaq.AgentId.HasValue
                    && supportFaq.Status == "Resolved"
                    && supportFaq.EndTime != null
                select review.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var monthStarts = reviewCreatedAtUtcValues
            .Select(NormalizeMonthStartUtc)
            .Distinct()
            .OrderBy(item => item)
            .ToList();

        if (monthStarts.Count == 0)
        {
            return;
        }

        await RecomputeQaMonthsCoreAsync(monthStarts, cancellationToken);
    }

    private async Task RecomputeQaMonthsCoreAsync(IReadOnlyCollection<DateTime> monthStartsUtc, CancellationToken cancellationToken)
    {
        if (monthStartsUtc.Count == 0)
        {
            return;
        }

        var monthSet = monthStartsUtc
            .Select(NormalizeMonthStartUtc)
            .ToHashSet();
        var earliestMonthStartUtc = monthSet.Min();
        var latestMonthStartUtc = monthSet.Max();
        var upperBoundUtc = latestMonthStartUtc.AddMonths(1);

        var scoreSeeds = await (
                from review in _dbContext.QaReviews.AsNoTracking()
                join supportFaq in _dbContext.SupportFaqRecords.AsNoTracking()
                    on review.SupportFaqId equals supportFaq.Id
                where supportFaq.AgentId.HasValue
                    && supportFaq.Status == "Resolved"
                    && supportFaq.EndTime != null
                    && review.CreatedAtUtc >= earliestMonthStartUtc
                    && review.CreatedAtUtc < upperBoundUtc
                select new
                {
                    review.CreatedAtUtc,
                    AgentUserId = supportFaq.AgentId!.Value,
                    review.OverallPercent
                })
            .ToListAsync(cancellationToken);

        var summarizedScores = scoreSeeds
            .Select(item => new
            {
                MonthStartUtc = NormalizeMonthStartUtc(item.CreatedAtUtc),
                item.AgentUserId,
                MetricValue = item.OverallPercent
            })
            .Where(item => monthSet.Contains(item.MonthStartUtc))
            .GroupBy(item => new { item.MonthStartUtc, item.AgentUserId })
            .Select(group => new
            {
                group.Key.MonthStartUtc,
                group.Key.AgentUserId,
                MetricValue = decimal.Round(group.Average(item => item.MetricValue), 2, MidpointRounding.AwayFromZero),
                ReviewCount = group.Count()
            })
            .ToList();

        var replacementRows = BuildRankedRows(
            AgentRankingMetricType.QaScore,
            monthSet,
            summarizedScores.Select(item => new AgentMetricSummary(item.MonthStartUtc, item.AgentUserId, item.MetricValue, item.ReviewCount)).ToList(),
            rankLowestFirst: false);

        await ReplaceRankingsAsync(AgentRankingMetricType.QaScore, monthSet, replacementRows, cancellationToken);
    }

    private async Task RecomputeAhtMonthCoreAsync(DateTime monthStartUtc, CancellationToken cancellationToken)
    {
        var nextMonthStartUtc = monthStartUtc.AddMonths(1);
        var reviewFaqRows = await (
                from review in _dbContext.QaReviews.AsNoTracking()
                join supportFaq in _dbContext.SupportFaqRecords.AsNoTracking()
                    on review.SupportFaqId equals supportFaq.Id
                where supportFaq.AgentId.HasValue
                    && supportFaq.Status == "Resolved"
                    && supportFaq.EndTime != null
                    && review.CreatedAtUtc >= monthStartUtc
                    && review.CreatedAtUtc < nextMonthStartUtc
                select new
                {
                    SupportFaqId = review.SupportFaqId,
                    AgentUserId = supportFaq.AgentId!.Value,
                    supportFaq.StartTime,
                    supportFaq.EndTime,
                    supportFaq.DurationMinutes
                })
            .ToListAsync(cancellationToken);

        var supportFaqIds = reviewFaqRows
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var supportMessages = supportFaqIds.Length == 0
            ? new List<SupportMessageSeed>()
            : await _dbContext.SupportMessages
                .AsNoTracking()
                .Where(item => supportFaqIds.Contains(item.ConversationId))
                .OrderBy(item => item.CreatedAt)
                .ThenBy(item => item.Id)
                .Select(item => new SupportMessageSeed(item.ConversationId, item.MessageText, item.CreatedAt))
                .ToListAsync(cancellationToken);

        var messagesBySupportFaqId = supportMessages
            .GroupBy(item => item.ConversationId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<SupportMessageSeed>)group.ToList());

        var handlingRows = reviewFaqRows
            .Select(row =>
            {
                messagesBySupportFaqId.TryGetValue(row.SupportFaqId, out var messages);
                var handlingSeconds = ResolveHandlingSeconds(row.StartTime, row.EndTime, row.DurationMinutes, messages);
                return new
                {
                    row.AgentUserId,
                    HandlingSeconds = handlingSeconds
                };
            })
            .Where(item => item.HandlingSeconds > 0)
            .ToList();

        var agentSummaries = handlingRows
            .GroupBy(item => item.AgentUserId)
            .Select(group =>
            {
                var averageHandlingSeconds = (int)Math.Round(group.Average(item => item.HandlingSeconds), MidpointRounding.AwayFromZero);
                return new AgentMetricSummary(monthStartUtc, group.Key, averageHandlingSeconds, group.Count());
            })
            .ToList();

        var monthSet = new HashSet<DateTime> { monthStartUtc };
        var replacementRows = BuildRankedRows(
            AgentRankingMetricType.AverageHandlingTime,
            monthSet,
            agentSummaries,
            rankLowestFirst: true);

        await ReplaceRankingsAsync(AgentRankingMetricType.AverageHandlingTime, monthSet, replacementRows, cancellationToken);
    }

    private static List<AgentRanking> BuildRankedRows(
        AgentRankingMetricType metricType,
        IReadOnlyCollection<DateTime> monthSet,
        IReadOnlyList<AgentMetricSummary> summarizedScores,
        bool rankLowestFirst)
    {
        var replacementRows = new List<AgentRanking>(summarizedScores.Count);
        var nowUtc = DateTime.UtcNow;
        foreach (var monthStart in monthSet.OrderBy(item => item))
        {
            var monthScores = summarizedScores
                .Where(item => item.MonthStartUtc == monthStart)
                .OrderBy(item => rankLowestFirst ? item.MetricValue : -item.MetricValue)
                .ThenBy(item => item.AgentUserId)
                .ToList();

            var rankedAgentCount = monthScores.Count;
            var currentRank = 0;
            decimal? previousMetricValue = null;
            for (var index = 0; index < monthScores.Count; index++)
            {
                var score = monthScores[index];
                if (!previousMetricValue.HasValue || score.MetricValue != previousMetricValue.Value)
                {
                    currentRank = index + 1;
                    previousMetricValue = score.MetricValue;
                }

                replacementRows.Add(new AgentRanking
                {
                    AgentUserId = score.AgentUserId,
                    PeriodStartUtc = monthStart,
                    MetricType = metricType,
                    MetricValue = decimal.Round(score.MetricValue, 2, MidpointRounding.AwayFromZero),
                    ReviewCount = score.ReviewCount,
                    RankPosition = currentRank,
                    RankedAgentCount = rankedAgentCount,
                    CalculatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                });
            }
        }

        return replacementRows;
    }

    private async Task ReplaceRankingsAsync(
        AgentRankingMetricType metricType,
        IReadOnlyCollection<DateTime> monthSet,
        IReadOnlyCollection<AgentRanking> replacementRows,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingRows = await _dbContext.AgentRankings
            .Where(item => monthSet.Contains(item.PeriodStartUtc) && item.MetricType == metricType)
            .ToListAsync(cancellationToken);
        if (existingRows.Count > 0)
        {
            _dbContext.AgentRankings.RemoveRange(existingRows);
        }

        if (replacementRows.Count > 0)
        {
            _dbContext.AgentRankings.AddRange(replacementRows);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static int ResolveHandlingSeconds(
        DateTime? startTimeUtc,
        DateTime? endTimeUtc,
        int durationMinutes,
        IReadOnlyList<SupportMessageSeed>? messages)
    {
        if (startTimeUtc.HasValue && endTimeUtc.HasValue && endTimeUtc > startTimeUtc)
        {
            var timestampSpanSeconds = (int)Math.Round((endTimeUtc.Value - startTimeUtc.Value).TotalSeconds, MidpointRounding.AwayFromZero);
            if (timestampSpanSeconds > 0 && timestampSpanSeconds <= (int)MaxSaneAht.TotalSeconds)
            {
                return timestampSpanSeconds;
            }
        }

        if (durationMinutes > 0 && durationMinutes <= (int)MaxSaneAht.TotalMinutes)
        {
            return durationMinutes * 60;
        }

        if (messages is { Count: > 1 })
        {
            var meaningfulMessages = messages
                .Where(item => !IsSystemQueueMessage(item.MessageText))
                .ToList();

            if (meaningfulMessages.Count > 1)
            {
                var firstMessageAt = meaningfulMessages[0].CreatedAt;
                var lastMessageAt = meaningfulMessages[^1].CreatedAt;
                if (lastMessageAt > firstMessageAt)
                {
                    var messageSpanSeconds = (int)Math.Round((lastMessageAt - firstMessageAt).TotalSeconds, MidpointRounding.AwayFromZero);
                    if (messageSpanSeconds > 0 && messageSpanSeconds <= (int)MaxSaneAht.TotalSeconds)
                    {
                        return messageSpanSeconds;
                    }
                }
            }
        }

        return 0;
    }

    private static bool IsSystemQueueMessage(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return false;
        }

        return string.Equals(messageText, QueueWelcomeMessage, StringComparison.Ordinal)
            || string.Equals(messageText, WaitingReplyMessage, StringComparison.Ordinal)
            || messageText.StartsWith("âš  Final reminder:", StringComparison.Ordinal)
            || messageText.StartsWith("â± Conversation ended automatically", StringComparison.Ordinal);
    }

    private static DateTime NormalizeMonthStartUtc(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return new DateTime(utcValue.Year, utcValue.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed record AgentMetricSummary(
        DateTime MonthStartUtc,
        int AgentUserId,
        decimal MetricValue,
        int ReviewCount);

    private sealed record SupportMessageSeed(
        int ConversationId,
        string MessageText,
        DateTime CreatedAt);
}
