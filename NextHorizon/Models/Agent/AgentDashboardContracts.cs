namespace NextHorizon.Models.Agent;

public sealed record AgentDashboardResponse(
    string AgentName,
    string MonthLabel,
    double QaScorePercent,
    string QaScoreLabel,
    int? QaRankPosition,
    int RankedAgentCount,
    string QaRankLabel,
    int AverageHandlingSeconds,
    string AverageHandlingLabel,
    int? AhtRankPosition,
    int AhtRankedAgentCount,
    string AhtRankLabel,
    string LastUpdatedLabel,
    string AcwLabel,
    bool AcwAvailable,
    IReadOnlyList<AgentDashboardTicketItem> RatedTickets,
    IReadOnlyList<AgentDashboardAcwItem> AcwRecords);

public sealed record AgentDashboardTicketItem(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ReviewerName,
    string RatedAtLabel,
    string RatedAtDate,
    double AccuracyPoints,
    double TonePoints,
    double ResolutionPoints,
    double OverallPercent,
    int HandlingSeconds,
    string HandlingLabel,
    string StartTimeLabel,
    string EndTimeLabel,
    string SearchText);

public sealed record AgentDashboardTicketDetail(
    int SupportFaqId,
    string AgentName,
    string CustomerName,
    string ReviewerName,
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
    string QuestionKey,
    int Score);

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
