namespace NextHorizon.Models.AgentDashboard;

public sealed record AgentDashboardResponse(
    string AgentName,
    string MonthLabel,
    double QaScorePercent,
    string QaScoreLabel,
    string QaReviewedResolvedLabel,
    int? QaRankPosition,
    int RankedAgentCount,
    string QaRankLabel,
    int AverageHandlingSeconds,
    string AverageHandlingLabel,
    int AverageAcwSeconds,
    int? AhtRankPosition,
    int AhtRankedAgentCount,
    string AhtRankLabel,
    string LastUpdatedLabel,
    string AcwLabel,
    bool AcwAvailable,
    IReadOnlyList<AgentDashboardMonthScorecard> MonthScorecards,
    IReadOnlyList<AgentDashboardTicketItem> RatedTickets,
    IReadOnlyList<AgentDashboardResolvedTicketItem> ResolvedTickets,
    IReadOnlyList<AgentDashboardAcwItem> AcwRecords);

public sealed record AgentDashboardMonthScorecard(
    string MonthKey,
    string MonthLabel,
    string QaScoreLabel,
    string QaRankLabel,
    int AhtSeconds,
    string AhtLabel,
    string AhtRankLabel,
    int AcwSeconds,
    string AcwLabel,
    string LastUpdatedLabel,
    bool HasQaData,
    bool HasAhtData,
    bool HasAcwData);

public sealed record AgentDashboardTicketItem(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ConcernSourceLabel,
    string ReviewerName,
    string RatedAtLabel,
    string RatedAtDate,
    string CategorySummaryLabel,
    double OverallPercent,
    int HandlingSeconds,
    string HandlingLabel,
    string StartTimeLabel,
    string EndTimeLabel,
    string SearchText);

public sealed record AgentDashboardResolvedTicketItem(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ConcernSourceLabel,
    string ReviewerName,
    string ReviewStateLabel,
    string ResolvedAtLabel,
    string ResolvedAtDate,
    bool IsReviewed,
    double OverallPercent,
    string SearchText);

public sealed record AgentDashboardTicketDetail(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ConcernSourceLabel,
    string ReviewerName,
    bool IsReviewed,
    string ReviewStateLabel,
    string ConversationDateLabel,
    string RatedAtLabel,
    string ReviewUpdatedLabel,
    string QaNotes,
    double AccuracyPoints,
    double TonePoints,
    double ResolutionPoints,
    double OverallPercent,
    int HandlingSeconds,
    string HandlingLabel,
    string StartTimeLabel,
    string EndTimeLabel,
    string AgentNotes,
    bool IsAcknowledged,
    string AcknowledgedLabel,
    IReadOnlyList<AgentDashboardCategoryCard> CategoryCards,
    IReadOnlyList<AgentDashboardQuestionScore> QuestionScores,
    IReadOnlyList<AgentDashboardConversationMessage> Messages);

public sealed class AgentDashboardNotesSaveRequest
{
    public string Notes { get; set; } = string.Empty;
}

public sealed record AgentDashboardMutationResponse(
    bool Success,
    string Message,
    string AgentNotes,
    bool IsAcknowledged,
    string AcknowledgedLabel);

public sealed record AgentDashboardQuestionScore(
    string Prompt,
    int Score,
    string CategoryKey,
    string CategoryTitle,
    double CategoryWeightPercent,
    int CategoryDisplayOrder,
    string QuestionText,
    int QuestionDisplayOrder);

public sealed record AgentDashboardCategoryCard(
    string CategoryTitle,
    double WeightPercent,
    double ScorePoints,
    int DisplayOrder);

public sealed record AgentDashboardConversationMessage(
    string MessageId,
    string SenderCssClass,
    string SenderDisplayName,
    string TimestampLabel,
    string MessageText,
    IReadOnlyList<AgentDashboardInlineComment> InlineComments);

public sealed record AgentDashboardInlineComment(
    string By,
    string On,
    string Comment,
    IReadOnlyList<string> Images);

public sealed record AgentDashboardAcwItem(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ConcernSourceLabel,
    string ReviewerName,
    bool IsRated,
    bool CanViewDetails,
    string RatingLabel,
    string Category,
    string ChatStatus,
    string PreviewQuestion,
    string AcwStartLabel,
    string AcwEndLabel,
    string AcwEndDate,
    int AcwSeconds,
    string AcwLabel,
    double OverallPercent,
    string SearchText);
