using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.Agent;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class AgentDashboardService : IAgentDashboardService
{
    private const string QueueWelcomeMessage = "Thank you for reaching out regarding your concern. An agent will be assigned to you shortly. There are currently 3 people ahead of you in the queue.";
    private const string WaitingReplyMessage = "An agent will assist you shortly.";
    private static readonly TimeSpan MaxSaneAht = TimeSpan.FromHours(8);
    private static readonly string[] QuestionScoreOrder =
    [
        "accuracy_q1",
        "accuracy_q2",
        "accuracy_q3",
        "tone_q1",
        "tone_q2",
        "tone_q3",
        "resolution_q1",
        "resolution_q2",
        "resolution_q3"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _dbContext;

    public AgentDashboardService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AgentDashboardResponse> GetDashboardAsync(int agentUserId, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStartUtc = monthStartUtc.AddMonths(1);
        var nowLocal = DateTime.Now;
        var acwMonthStartLocal = new DateTime(nowLocal.Year, nowLocal.Month, 1, 0, 0, 0, DateTimeKind.Local);
        var nextAcwMonthStartLocal = acwMonthStartLocal.AddMonths(1);

        var agentName = await LoadAgentNameAsync(agentUserId, cancellationToken) ?? $"Agent {agentUserId}";
        var averageAcwSeconds = await ResolveAverageAcwSecondsAsync(agentUserId, acwMonthStartLocal, nextAcwMonthStartLocal, cancellationToken);
        var acwAvailable = true;
        var acwLabel = averageAcwSeconds.HasValue
            ? FormatDuration(averageAcwSeconds.Value)
            : "0mins 0secs";
        var currentMonthQaRanking = await _dbContext.AgentRankings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.AgentUserId == agentUserId
                    && item.PeriodStartUtc == monthStartUtc
                    && item.MetricType == AgentRankingMetricType.QaScore,
                cancellationToken);
        var qaRankPosition = currentMonthQaRanking?.RankPosition;
        var rankedAgentCount = currentMonthQaRanking?.RankedAgentCount ?? 0;
        var qaRankLabel = BuildRankLabel(qaRankPosition, rankedAgentCount);
        var currentMonthAhtRanking = await _dbContext.AgentRankings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.AgentUserId == agentUserId
                    && item.PeriodStartUtc == monthStartUtc
                    && item.MetricType == AgentRankingMetricType.AverageHandlingTime,
                cancellationToken);
        var ahtRankPosition = currentMonthAhtRanking?.RankPosition;
        var ahtRankedAgentCount = currentMonthAhtRanking?.RankedAgentCount ?? 0;
        var ahtRankLabel = BuildRankLabel(ahtRankPosition, ahtRankedAgentCount);

        var reviews = await _dbContext.QaReviews
            .AsNoTracking()
            .OrderByDescending(item => item.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
        {
            return EmptyResponse(
                agentName,
                monthStartUtc,
                acwLabel,
                acwAvailable,
                qaRankPosition,
                rankedAgentCount,
                qaRankLabel,
                ahtRankPosition,
                ahtRankedAgentCount,
                ahtRankLabel);
        }

        var reviewFaqIds = reviews
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var faqById = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => reviewFaqIds.Contains(item.Id) && item.AgentId == agentUserId)
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var agentReviews = reviews
            .Where(item => faqById.ContainsKey(item.SupportFaqId))
            .ToList();

        if (agentReviews.Count == 0)
        {
            return EmptyResponse(
                agentName,
                monthStartUtc,
                acwLabel,
                acwAvailable,
                qaRankPosition,
                rankedAgentCount,
                qaRankLabel,
                ahtRankPosition,
                ahtRankedAgentCount,
                ahtRankLabel);
        }

        var supportFaqIds = agentReviews
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var latestSessionsBySupportFaqId = await LoadLatestSessionsBySupportFaqIdAsync(supportFaqIds, cancellationToken);
        var consumers = await LoadConsumersAsync(latestSessionsBySupportFaqId, cancellationToken);
        var meaningfulMessagesBySupportFaqId = await LoadMeaningfulMessagesBySupportFaqIdAsync(supportFaqIds, cancellationToken);

        var tickets = agentReviews
            .Select(review =>
            {
                var faq = faqById[review.SupportFaqId];
                latestSessionsBySupportFaqId.TryGetValue(review.SupportFaqId, out var session);
                meaningfulMessagesBySupportFaqId.TryGetValue(review.SupportFaqId, out var ticketMessages);

                var customerName = ResolveCustomerName(session, consumers);
                var handlingSeconds = ResolveHandlingSeconds(faq, ticketMessages);
                var ratedAtUtc = review.CreatedAtUtc;
                var reviewerName = ResolveReviewerName(review.ReviewerName);

                return new AgentDashboardTicketSeed(
                    review.SupportFaqId,
                    agentName,
                    customerName,
                    reviewerName,
                    ratedAtUtc,
                    Math.Round((double)((review.AccuracyAverage / 5m) * 35m), 1),
                    Math.Round((double)((review.ToneAverage / 5m) * 35m), 1),
                    Math.Round((double)((review.ResolutionAverage / 5m) * 30m), 1),
                    Math.Round((double)review.OverallPercent, 1),
                    handlingSeconds,
                    faq.StartTime,
                    faq.EndTime);
            })
            .OrderByDescending(item => item.RatedAtUtc)
            .ThenByDescending(item => item.SupportFaqId)
            .ToList();
        var reviewBySupportFaqId = agentReviews
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.UpdatedAtUtc)
                    .ThenByDescending(item => item.QaReviewId)
                    .First());
        var ticketBySupportFaqId = tickets
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(group => group.Key, group => group.First());
        var acwRecords = await ResolveAcwRecordsAsync(
            agentUserId,
            acwMonthStartLocal,
            nextAcwMonthStartLocal,
            reviewBySupportFaqId,
            ticketBySupportFaqId,
            cancellationToken);

        var currentMonthTickets = tickets
            .Where(item => item.RatedAtUtc >= monthStartUtc && item.RatedAtUtc < nextMonthStartUtc)
            .ToList();

        var qaScorePercent = currentMonthTickets.Count == 0
            ? 0
            : Math.Round(currentMonthTickets.Average(item => item.OverallPercent), 1);

        var monthHandlingDurations = currentMonthTickets
            .Where(item => item.HandlingSeconds > 0)
            .Select(item => item.HandlingSeconds)
            .ToList();

        var averageHandlingSeconds = monthHandlingDurations.Count == 0
            ? 0
            : (int)Math.Round(monthHandlingDurations.Average(), MidpointRounding.AwayFromZero);

        var lastUpdatedUtc = agentReviews
            .Select(item => item.UpdatedAtUtc)
            .DefaultIfEmpty(monthStartUtc)
            .Max();

        var responseItems = tickets
            .Select(item => new AgentDashboardTicketItem(
                item.SupportFaqId,
                item.AgentName,
                item.CustomerName,
                item.ReviewerName,
                item.RatedAtUtc.ToString("MMM dd, yyyy"),
                item.RatedAtUtc.ToString("yyyy-MM-dd"),
                item.AccuracyPoints,
                item.TonePoints,
                item.ResolutionPoints,
                item.OverallPercent,
                item.HandlingSeconds,
                FormatDuration(item.HandlingSeconds),
                item.StartTimeUtc?.ToLocalTime().ToString("hh:mm tt") ?? "N/A",
                item.EndTimeUtc?.ToLocalTime().ToString("hh:mm tt") ?? "N/A",
                BuildSearchText(item)))
            .ToList();

        return new AgentDashboardResponse(
            agentName,
            monthStartUtc.ToString("MMMM yyyy"),
            qaScorePercent,
            $"{qaScorePercent:0.0}%",
            qaRankPosition,
            rankedAgentCount,
            qaRankLabel,
            averageHandlingSeconds,
            FormatDuration(averageHandlingSeconds),
            ahtRankPosition,
            ahtRankedAgentCount,
            ahtRankLabel,
            lastUpdatedUtc == monthStartUtc && agentReviews.Count == 0
                ? "No updates yet"
                : lastUpdatedUtc.ToLocalTime().ToString("MMMM dd, yyyy 'at' hh:mm tt"),
            acwLabel,
            acwAvailable,
            responseItems,
            acwRecords);
    }

    public async Task<AgentDashboardTicketDetail?> GetTicketDetailAsync(int agentUserId, int supportFaqId, CancellationToken cancellationToken)
    {
        var review = await _dbContext.QaReviews
            .AsNoTracking()
            .Include(item => item.QuestionScores)
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);

        if (review is null)
        {
            return null;
        }

        var faq = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == supportFaqId && item.AgentId == agentUserId, cancellationToken);

        if (faq is null)
        {
            return null;
        }

        var agentName = await LoadAgentNameAsync(agentUserId, cancellationToken) ?? $"Agent {agentUserId}";
        var latestSession = await _dbContext.LiveAgentSessions
            .AsNoTracking()
            .Where(item => item.SupportFaqId == supportFaqId)
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.LiveAgentSessionId)
            .FirstOrDefaultAsync(cancellationToken);

        ConsumerRef? consumer = null;
        if (latestSession?.ConsumerId is int consumerId)
        {
            consumer = await _dbContext.Set<ConsumerRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ConsumerId == consumerId, cancellationToken);
        }

        var allMessages = await _dbContext.SupportMessages
            .AsNoTracking()
            .Where(item => item.ConversationId == supportFaqId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var customerName = ResolveCustomerName(consumer);

        var meaningfulMessages = allMessages
            .Where(item => !IsSystemQueueMessage(item.MessageText))
            .ToList();

        var handlingSeconds = ResolveHandlingSeconds(faq, meaningfulMessages);
        var inlineComments = ParseInlineComments(review.InlineCommentsJson);
        var feedback = await _dbContext.AgentReviewFeedbackEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId && item.AgentUserId == agentUserId, cancellationToken);

        var messageItems = allMessages
            .Select(item => new AgentDashboardConversationMessage(
                item.Id.ToString(),
                ResolveSenderCssClass(item.SenderRole),
                ResolveSenderDisplayName(item, customerName, agentName),
                item.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt"),
                item.MessageText,
                inlineComments.TryGetValue(item.Id.ToString(), out var comments)
                    ? comments
                    : Array.Empty<AgentDashboardInlineComment>()))
            .ToList();

        if (messageItems.Count == 0 && !string.IsNullOrWhiteSpace(faq.Question))
        {
            messageItems.Add(new AgentDashboardConversationMessage(
                "question",
                "user",
                customerName,
                faq.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt"),
                faq.Question,
                Array.Empty<AgentDashboardInlineComment>()));
        }

        var questionScores = review.QuestionScores
            .OrderBy(item => Array.IndexOf(QuestionScoreOrder, item.QuestionKey))
            .ThenBy(item => item.QaReviewQuestionScoreId)
            .Select(item => new AgentDashboardQuestionScore(item.QuestionKey, item.Score))
            .ToList();

        return new AgentDashboardTicketDetail(
            supportFaqId,
            agentName,
            customerName,
            ResolveReviewerName(review.ReviewerName),
            (faq.EndTime ?? faq.CreatedAt).ToLocalTime().ToString("MMM dd, yyyy"),
            review.CreatedAtUtc.ToLocalTime().ToString("MMM dd, yyyy"),
            review.UpdatedAtUtc.ToLocalTime().ToString("MMMM dd, yyyy 'at' hh:mm tt"),
            string.IsNullOrWhiteSpace(review.Notes) ? "No QA notes were recorded for this review." : review.Notes.Trim(),
            Math.Round((double)((review.AccuracyAverage / 5m) * 35m), 1),
            Math.Round((double)((review.ToneAverage / 5m) * 35m), 1),
            Math.Round((double)((review.ResolutionAverage / 5m) * 30m), 1),
            Math.Round((double)review.OverallPercent, 1),
            handlingSeconds,
            FormatDuration(handlingSeconds),
            faq.StartTime?.ToLocalTime().ToString("hh:mm tt") ?? "N/A",
            faq.EndTime?.ToLocalTime().ToString("hh:mm tt") ?? "N/A",
            feedback?.Notes ?? string.Empty,
            feedback?.Acknowledged ?? false,
            FormatAcknowledgedLabel(feedback?.AcknowledgedAtUtc, feedback?.Acknowledged ?? false),
            questionScores,
            messageItems);
    }

    public async Task<AgentDashboardMutationResponse> SaveNotesAsync(int agentUserId, int supportFaqId, string? notes, CancellationToken cancellationToken)
    {
        var normalizedNotes = (notes ?? string.Empty).Trim();
        if (normalizedNotes.Length > 4000)
        {
            return MutationFailure("Agent notes exceed the maximum length.");
        }

        var feedback = await GetOrCreateFeedbackAsync(agentUserId, supportFaqId, cancellationToken);
        if (feedback is null)
        {
            return MutationFailure("Reviewed ticket not found for this agent.");
        }

        feedback.Notes = normalizedNotes;
        feedback.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MutationSuccess("Agent notes saved.", feedback);
    }

    public async Task<AgentDashboardMutationResponse> AcknowledgeAsync(int agentUserId, int supportFaqId, CancellationToken cancellationToken)
    {
        var feedback = await GetOrCreateFeedbackAsync(agentUserId, supportFaqId, cancellationToken);
        if (feedback is null)
        {
            return MutationFailure("Reviewed ticket not found for this agent.");
        }

        if (!feedback.Acknowledged)
        {
            feedback.Acknowledged = true;
            feedback.AcknowledgedAtUtc = DateTime.UtcNow;
            feedback.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MutationSuccess("QA review acknowledged.", feedback);
    }

    private async Task<Dictionary<int, LiveAgentSession>> LoadLatestSessionsBySupportFaqIdAsync(int[] supportFaqIds, CancellationToken cancellationToken)
    {
        var latestSessions = await _dbContext.LiveAgentSessions
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.SupportFaqId))
            .ToListAsync(cancellationToken);

        return latestSessions
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.UpdatedAt)
                    .ThenByDescending(item => item.LiveAgentSessionId)
                    .First());
    }

    private async Task<Dictionary<int, ConsumerRef>> LoadConsumersAsync(
        IReadOnlyDictionary<int, LiveAgentSession> latestSessionsBySupportFaqId,
        CancellationToken cancellationToken)
    {
        var consumerIds = latestSessionsBySupportFaqId.Values
            .Where(item => item.ConsumerId.HasValue)
            .Select(item => item.ConsumerId!.Value)
            .Distinct()
            .ToArray();

        return consumerIds.Length == 0
            ? new Dictionary<int, ConsumerRef>()
            : await _dbContext.Set<ConsumerRef>()
                .AsNoTracking()
                .Where(item => consumerIds.Contains(item.ConsumerId))
                .ToDictionaryAsync(item => item.ConsumerId, cancellationToken);
    }

    private async Task<Dictionary<int, List<SupportMessage>>> LoadMeaningfulMessagesBySupportFaqIdAsync(int[] supportFaqIds, CancellationToken cancellationToken)
    {
        var messagesBySupportFaqId = await _dbContext.SupportMessages
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.ConversationId))
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return messagesBySupportFaqId
            .Where(item => !IsSystemQueueMessage(item.MessageText))
            .GroupBy(item => item.ConversationId)
            .ToDictionary(group => group.Key, group => group.ToList());
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
            var rows = new List<AgentNameSeed>();
            var userIdOrdinal = reader.GetOrdinal("UserID");
            var agentNameOrdinal = reader.GetOrdinal("AgentName");
            var agentStatusOrdinal = reader.GetOrdinal("AgentStatus");

            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new AgentNameSeed(
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

    private async Task<int?> ResolveAverageAcwSecondsAsync(
        int agentUserId,
        DateTime monthStartLocal,
        DateTime nextMonthStartLocal,
        CancellationToken cancellationToken)
    {
        var acwRows = await _dbContext.SupportAgents
            .AsNoTracking()
            .Where(item => item.UserId == agentUserId)
            .Select(item => new { item.ACWStartTime, item.ACWEndTime })
            .ToListAsync(cancellationToken);

        var validDurations = acwRows
            .Where(item =>
                item.ACWStartTime.HasValue
                && item.ACWEndTime.HasValue
                && item.ACWEndTime.Value > item.ACWStartTime.Value
                && item.ACWEndTime.Value >= monthStartLocal
                && item.ACWEndTime.Value < nextMonthStartLocal)
            .Select(item => (int)Math.Round((item.ACWEndTime!.Value - item.ACWStartTime!.Value).TotalSeconds, MidpointRounding.AwayFromZero))
            .Where(totalSeconds => totalSeconds > 0)
            .ToList();

        if (validDurations.Count == 0)
        {
            return null;
        }

        return (int)Math.Round(validDurations.Average(), MidpointRounding.AwayFromZero);
    }

    private async Task<IReadOnlyList<AgentDashboardAcwItem>> ResolveAcwRecordsAsync(
        int agentUserId,
        DateTime monthStartLocal,
        DateTime nextMonthStartLocal,
        IReadOnlyDictionary<int, QaReview> reviewBySupportFaqId,
        IReadOnlyDictionary<int, AgentDashboardTicketSeed> ticketBySupportFaqId,
        CancellationToken cancellationToken)
    {
        var agentRows = await _dbContext.SupportAgents
            .AsNoTracking()
            .Where(item =>
                item.UserId == agentUserId
                && item.ConversationId.HasValue
                && item.ACWStartTime.HasValue
                && item.ACWEndTime.HasValue)
            .OrderByDescending(item => item.ACWEndTime)
            .ThenByDescending(item => item.ChatId)
            .ToListAsync(cancellationToken);

        var records = new List<AgentDashboardAcwItem>();

        foreach (var row in agentRows)
        {
            if (!row.ConversationId.HasValue || !row.ACWStartTime.HasValue || !row.ACWEndTime.HasValue)
            {
                continue;
            }

            var supportFaqId = row.ConversationId.Value;
            var isRated = reviewBySupportFaqId.TryGetValue(supportFaqId, out var review);
            var canViewDetails = isRated;
            ticketBySupportFaqId.TryGetValue(supportFaqId, out var ticket);

            var acwStartLocal = row.ACWStartTime.Value;
            var acwEndLocal = row.ACWEndTime.Value;
            if (acwEndLocal <= acwStartLocal)
            {
                continue;
            }

            if (acwEndLocal < monthStartLocal || acwEndLocal >= nextMonthStartLocal)
            {
                continue;
            }

            var acwSeconds = (int)Math.Round((acwEndLocal - acwStartLocal).TotalSeconds, MidpointRounding.AwayFromZero);
            if (acwSeconds <= 0)
            {
                continue;
            }

            var resolvedAgentName = !string.IsNullOrWhiteSpace(row.AgentName)
                ? row.AgentName.Trim()
                : ticket?.AgentName ?? $"Agent {agentUserId}";
            var resolvedCustomerName = !string.IsNullOrWhiteSpace(row.ClientName)
                ? row.ClientName.Trim()
                : ticket?.CustomerName ?? "Unknown Customer";
            var resolvedCategory = string.IsNullOrWhiteSpace(row.Category) ? "N/A" : row.Category.Trim();
            var resolvedChatStatus = string.IsNullOrWhiteSpace(row.ChatStatus) ? "N/A" : row.ChatStatus.Trim();
            var resolvedPreviewQuestion = string.IsNullOrWhiteSpace(row.PreviewQuestion) ? "N/A" : row.PreviewQuestion.Trim();
            var reviewerName = isRated && review is not null
                ? ResolveReviewerName(review.ReviewerName)
                : "Not rated yet";
            var ratingLabel = isRated ? "Rated" : "Not rated yet";
            var acwLabel = FormatDuration(acwSeconds);
            var acwStartLabel = acwStartLocal.ToString("hh:mm tt");
            var acwEndLabel = acwEndLocal.ToString("hh:mm tt");
            var acwEndDate = acwEndLocal.ToString("yyyy-MM-dd");
            var overallPercent = isRated && review is not null
                ? Math.Round((double)review.OverallPercent, 1)
                : 0;
            var searchText = BuildAcwSearchText(
                supportFaqId,
                resolvedAgentName,
                resolvedCustomerName,
                reviewerName,
                ratingLabel,
                resolvedCategory,
                resolvedChatStatus,
                resolvedPreviewQuestion,
                acwStartLabel,
                acwEndLabel,
                acwLabel,
                acwEndLocal);

            records.Add(new AgentDashboardAcwItem(
                supportFaqId,
                resolvedAgentName,
                resolvedCustomerName,
                reviewerName,
                isRated,
                canViewDetails,
                ratingLabel,
                resolvedCategory,
                resolvedChatStatus,
                resolvedPreviewQuestion,
                acwStartLabel,
                acwEndLabel,
                acwEndDate,
                acwSeconds,
                acwLabel,
                overallPercent,
                searchText));
        }

        return records;
    }

    private static AgentDashboardResponse EmptyResponse(
        string agentName,
        DateTime monthStartUtc,
        string acwLabel,
        bool acwAvailable,
        int? qaRankPosition,
        int rankedAgentCount,
        string qaRankLabel,
        int? ahtRankPosition,
        int ahtRankedAgentCount,
        string ahtRankLabel)
    {
        return new AgentDashboardResponse(
            agentName,
            monthStartUtc.ToString("MMMM yyyy"),
            0,
            "0.0%",
            qaRankPosition,
            rankedAgentCount,
            qaRankLabel,
            0,
            "0mins 0secs",
            ahtRankPosition,
            ahtRankedAgentCount,
            ahtRankLabel,
            "No updates yet",
            acwLabel,
            acwAvailable,
            Array.Empty<AgentDashboardTicketItem>(),
            Array.Empty<AgentDashboardAcwItem>());
    }

    private static int ResolveHandlingSeconds(SupportFaqRecord faq, IReadOnlyList<SupportMessage>? messages)
    {
        if (faq.StartTime.HasValue && faq.EndTime.HasValue && faq.EndTime > faq.StartTime)
        {
            var timestampSpanSeconds = (int)Math.Round((faq.EndTime.Value - faq.StartTime.Value).TotalSeconds, MidpointRounding.AwayFromZero);
            if (timestampSpanSeconds > 0 && timestampSpanSeconds <= (int)MaxSaneAht.TotalSeconds)
            {
                return timestampSpanSeconds;
            }
        }

        if (faq.DurationMinutes > 0 && faq.DurationMinutes <= (int)MaxSaneAht.TotalMinutes)
        {
            return faq.DurationMinutes * 60;
        }

        if (messages is { Count: > 1 })
        {
            var firstMessageAt = messages[0].CreatedAt;
            var lastMessageAt = messages[^1].CreatedAt;
            if (lastMessageAt > firstMessageAt)
            {
                var messageSpanSeconds = (int)Math.Round((lastMessageAt - firstMessageAt).TotalSeconds, MidpointRounding.AwayFromZero);
                if (messageSpanSeconds > 0 && messageSpanSeconds <= (int)MaxSaneAht.TotalSeconds)
                {
                    return messageSpanSeconds;
                }
            }
        }

        return 0;
    }

    private static string FormatDuration(int totalSeconds)
    {
        if (totalSeconds <= 0)
        {
            return "0mins 0secs";
        }

        var duration = TimeSpan.FromSeconds(totalSeconds);
        var hours = (int)duration.TotalHours;
        var minutes = duration.Minutes;
        var seconds = duration.Seconds;

        var minuteLabel = minutes == 1 ? "min" : "mins";
        var secondLabel = seconds == 1 ? "sec" : "secs";

        if (hours <= 0)
        {
            return $"{minutes}{minuteLabel} {seconds}{secondLabel}";
        }

        var hourLabel = hours == 1 ? "hr" : "hrs";
        return $"{hours}{hourLabel} {minutes}{minuteLabel} {seconds}{secondLabel}";
    }

    private static string ResolveCustomerName(LiveAgentSession? session, IReadOnlyDictionary<int, ConsumerRef> consumers)
    {
        if (session?.ConsumerId is not int consumerId || !consumers.TryGetValue(consumerId, out var consumer))
        {
            return "Unknown Customer";
        }

        return ResolveCustomerName(consumer);
    }

    private static string ResolveCustomerName(ConsumerRef? consumer)
    {
        if (consumer is null)
        {
            return "Unknown Customer";
        }

        var parts = new[] { consumer.FirstName, consumer.MiddleName, consumer.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();

        if (parts.Length > 0)
        {
            return string.Join(' ', parts);
        }

        return string.IsNullOrWhiteSpace(consumer.Username) ? "Unknown Customer" : consumer.Username.Trim();
    }

    private static string ResolveSenderDisplayName(SupportMessage message, string customerName, string agentName)
    {
        if (string.Equals(message.SenderRole, "Consumer", StringComparison.OrdinalIgnoreCase))
        {
            return customerName;
        }

        if (string.Equals(message.SenderRole, "Seller", StringComparison.OrdinalIgnoreCase))
        {
            return "Seller";
        }

        if (string.Equals(message.SenderRole, "Support Agent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(message.SenderRole, "Agent", StringComparison.OrdinalIgnoreCase))
        {
            return message.SenderId == 0 ? "Support" : $"Agent {agentName}";
        }

        return message.SenderId == 0 ? "Support" : $"Agent {agentName}";
    }

    private static string ResolveSenderCssClass(string? senderRole)
    {
        if (string.Equals(senderRole, "Consumer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(senderRole, "Seller", StringComparison.OrdinalIgnoreCase))
        {
            return "user";
        }

        return "agent";
    }

    private static string ResolveReviewerName(string? reviewerName)
    {
        return string.IsNullOrWhiteSpace(reviewerName) ? "QA Reviewer" : reviewerName.Trim();
    }

    private async Task<AgentReviewFeedback?> GetOrCreateFeedbackAsync(int agentUserId, int supportFaqId, CancellationToken cancellationToken)
    {
        var reviewExists = await _dbContext.QaReviews
            .AsNoTracking()
            .AnyAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);
        if (!reviewExists)
        {
            return null;
        }

        var faq = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == supportFaqId && item.AgentId == agentUserId, cancellationToken);
        if (faq is null)
        {
            return null;
        }

        var feedback = await _dbContext.AgentReviewFeedbackEntries
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId && item.AgentUserId == agentUserId, cancellationToken);

        if (feedback is not null)
        {
            return feedback;
        }

        feedback = new AgentReviewFeedback
        {
            SupportFaqId = supportFaqId,
            AgentUserId = agentUserId,
            Notes = string.Empty,
            Acknowledged = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AgentReviewFeedbackEntries.Add(feedback);
        return feedback;
    }

    private static AgentDashboardMutationResponse MutationFailure(string message)
    {
        return new AgentDashboardMutationResponse(false, message, string.Empty, false, "Not acknowledged");
    }

    private static AgentDashboardMutationResponse MutationSuccess(string message, AgentReviewFeedback feedback)
    {
        return new AgentDashboardMutationResponse(
            true,
            message,
            feedback.Notes,
            feedback.Acknowledged,
            FormatAcknowledgedLabel(feedback.AcknowledgedAtUtc, feedback.Acknowledged));
    }

    private static string FormatAcknowledgedLabel(DateTime? acknowledgedAtUtc, bool acknowledged)
    {
        if (!acknowledged)
        {
            return "Not acknowledged";
        }

        return acknowledgedAtUtc.HasValue
            ? acknowledgedAtUtc.Value.ToLocalTime().ToString("Acknowledged on MMMM dd, yyyy 'at' hh:mm tt")
            : "Acknowledged";
    }

    private static string BuildRankLabel(int? rankPosition, int rankedAgentCount)
    {
        if (!rankPosition.HasValue || rankPosition.Value <= 0 || rankedAgentCount <= 0)
        {
            return "Rank: Not ranked yet";
        }

        return $"Rank: #{rankPosition.Value} of {rankedAgentCount}";
    }

    private static string BuildSearchText(AgentDashboardTicketSeed item)
    {
        return string.Join(
            ' ',
            item.SupportFaqId,
            item.AgentName,
            item.CustomerName,
            item.ReviewerName,
            item.RatedAtUtc.ToString("MMM dd, yyyy"),
            item.OverallPercent.ToString("0.0"),
            item.AccuracyPoints.ToString("0.0"),
            item.TonePoints.ToString("0.0"),
            item.ResolutionPoints.ToString("0.0"))
            .ToLowerInvariant();
    }

    private static string BuildAcwSearchText(
        int supportFaqId,
        string agentName,
        string customerName,
        string reviewerName,
        string ratingLabel,
        string category,
        string chatStatus,
        string previewQuestion,
        string acwStartLabel,
        string acwEndLabel,
        string acwLabel,
        DateTime acwEndLocal)
    {
        return string.Join(
                ' ',
                supportFaqId,
                agentName,
                customerName,
                reviewerName,
                ratingLabel,
                category,
                chatStatus,
                previewQuestion,
                acwStartLabel,
                acwEndLabel,
                acwLabel,
                acwEndLocal.ToString("MMM dd, yyyy"))
            .ToLowerInvariant();
    }

    private static Dictionary<string, IReadOnlyList<AgentDashboardInlineComment>> ParseInlineComments(string? inlineCommentsJson)
    {
        if (string.IsNullOrWhiteSpace(inlineCommentsJson) || string.Equals(inlineCommentsJson.Trim(), "{}", StringComparison.Ordinal))
        {
            return new Dictionary<string, IReadOnlyList<AgentDashboardInlineComment>>(StringComparer.Ordinal);
        }

        try
        {
            var commentMap = JsonSerializer.Deserialize<Dictionary<string, List<QaInlineCommentEntry>>>(inlineCommentsJson, JsonOptions);
            if (commentMap is null || commentMap.Count == 0)
            {
                return new Dictionary<string, IReadOnlyList<AgentDashboardInlineComment>>(StringComparer.Ordinal);
            }

            return commentMap
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyList<AgentDashboardInlineComment>)(pair.Value ?? [])
                        .Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.Comment))
                        .Select(entry => new AgentDashboardInlineComment(
                            string.IsNullOrWhiteSpace(entry!.By) ? "QA Reviewer" : entry.By.Trim(),
                            (entry.On ?? string.Empty).Trim(),
                            (entry.Comment ?? string.Empty).Trim(),
                            (entry.Images ?? [])
                                .Where(image => !string.IsNullOrWhiteSpace(image))
                                .Select(image => image.Trim())
                                .ToArray()))
                        .ToList(),
                    StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, IReadOnlyList<AgentDashboardInlineComment>>(StringComparer.Ordinal);
        }
    }

    private static bool IsSystemQueueMessage(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return false;
        }

        return string.Equals(messageText, QueueWelcomeMessage, StringComparison.Ordinal)
            || string.Equals(messageText, WaitingReplyMessage, StringComparison.Ordinal)
            || messageText.StartsWith("⚠ Final reminder:", StringComparison.Ordinal)
            || messageText.StartsWith("⏱ Conversation ended automatically", StringComparison.Ordinal);
    }

    private sealed record AgentDashboardTicketSeed(
        int SupportFaqId,
        string AgentName,
        string CustomerName,
        string ReviewerName,
        DateTime RatedAtUtc,
        double AccuracyPoints,
        double TonePoints,
        double ResolutionPoints,
        double OverallPercent,
        int HandlingSeconds,
        DateTime? StartTimeUtc,
        DateTime? EndTimeUtc);

    private sealed record AgentNameSeed(
        int UserId,
        string? AgentName,
        string? AgentStatus);
}
