using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models.QA;

public sealed record QaDashboardResponse(
    string Label,
    string AgentMetric,
    int UnclosedResolved,
    string TicketsRated,
    string TicketsRatedSub,
    int PendingRatings,
    string AverageQaScore,
    int RatingsUpdated,
    string ThroughputNote,
    int ThroughputPercent,
    string QualityNote,
    IReadOnlyList<int> ResolvedTrend,
    IReadOnlyList<int> RatedTrend,
    IReadOnlyList<QaDashboardAgentItem> TopAgents,
    IReadOnlyList<QaDashboardAwaitingTicketItem> AwaitingTickets);

public sealed record QaDashboardAgentItem(
    string Name,
    string ResolvedText,
    string Score);

public sealed record QaDashboardAwaitingTicketItem(
    int Id,
    string Agent,
    string Customer,
    string ResolvedAt);

public sealed record QaResolvedTicketsResponse(
    int RatedCount,
    int ResolvedCount,
    int PendingCount,
    int Page,
    int PageSize,
    int TotalPendingCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext,
    IReadOnlyList<QaResolvedTicketItem> Items);

public sealed record QaResolvedTicketItem(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ResolvedAtLabel);

public sealed record QaRatingQueueResponse(
    int QueueCount,
    int? QueuePosition,
    bool CurrentTicketInQueue,
    int? PreviousSupportFaqId,
    int? NextSupportFaqId,
    int? MatchedSupportFaqId,
    IReadOnlyList<QaRatingQueueItem> Items,
    int? PreviousFilteredSupportFaqId = null,
    int? NextFilteredSupportFaqId = null);

public sealed record QaRatingQueueItem(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ResolvedAtLabel,
    bool IsRated);

public sealed record QaAgentTicketsResponse(
    int AgentUserId,
    string AgentName,
    int AwaitingCount,
    int RatedCount,
    int AwaitingPage,
    int RatedPage,
    int PageSize,
    int AwaitingTotalPages,
    int RatedTotalPages,
    bool HasAwaitingPrevious,
    bool HasAwaitingNext,
    bool HasRatedPrevious,
    bool HasRatedNext,
    IReadOnlyList<QaAgentTicketItem> AwaitingItems,
    IReadOnlyList<QaAgentTicketItem> RatedItems);

public sealed record QaAgentTicketItem(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ResolvedAtLabel,
    string ResolvedAtDate,
    bool IsRated,
    double? OverallPercent);

public sealed record QaRatedHistoryResponse(
    int Count,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasPrevious,
    bool HasNext,
    IReadOnlyList<QaRatedHistoryItem> Items);

public sealed record QaRatedHistoryItem(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string RatedAtLabel,
    string RatedAtDate,
    double AccuracyPoints,
    double TonePoints,
    double ResolutionPoints,
    double OverallPercent);

public sealed class QaReviewUpsertRequest
{
    [Required]
    public IReadOnlyList<int> AccuracyScores { get; set; } = Array.Empty<int>();

    [Required]
    public IReadOnlyList<int> ToneScores { get; set; } = Array.Empty<int>();

    [Required]
    public IReadOnlyList<int> ResolutionScores { get; set; } = Array.Empty<int>();

    [StringLength(4000)]
    public string Notes { get; set; } = string.Empty;

    public bool SubmitToAgentNow { get; set; }

    public Dictionary<string, List<QaInlineCommentEntry>> InlineCommentThreads { get; set; } = new(StringComparer.Ordinal);
}

public sealed class QaInlineCommentEntry
{
    [StringLength(200)]
    public string By { get; set; } = string.Empty;

    [StringLength(80)]
    public string On { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Comment { get; set; } = string.Empty;

    public List<string> Images { get; set; } = new();
}

public sealed class QaInlineCommentsUpsertRequest
{
    public Dictionary<string, List<QaInlineCommentEntry>> InlineCommentThreads { get; set; } = new(StringComparer.Ordinal);
}

public sealed record QaReviewMutationResponse(
    bool Success,
    string Message,
    bool SubmittedToAgent = false,
    string SubmittedToAgentAtUtc = "",
    double? OverallPercent = null);

public sealed record QaConversationMessageViewModel(
    string MessageId,
    string SenderCssClass,
    string SenderDisplayName,
    string TimestampLabel,
    string MessageText,
    bool IsSelectable);

public sealed class QaRatingPageData
{
    public int SupportFaqId { get; init; }

    public bool HasActiveTicket { get; init; }

    public int? PreviousSupportFaqId { get; init; }

    public int? NextSupportFaqId { get; init; }

    public string AgentName { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string ConversationDateLabel { get; init; } = string.Empty;

    public IReadOnlyList<QaConversationMessageViewModel> Messages { get; init; } = Array.Empty<QaConversationMessageViewModel>();

    public string ReviewerDisplayName { get; init; } = string.Empty;

    public string InlineCommentsJson { get; init; } = "{}";

    public QaReview? Review { get; init; }

    public int QueueCount { get; init; }

    public int? QueuePosition { get; init; }

    public bool CurrentTicketInQueue { get; init; }

    public string QueueRange { get; init; } = "custom";

    public DateOnly? QueueFrom { get; init; }

    public DateOnly? QueueTo { get; init; }

    public string QueueSearch { get; init; } = string.Empty;
}
