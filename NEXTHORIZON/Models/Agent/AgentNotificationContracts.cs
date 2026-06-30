namespace NextHorizon.Models.AgentDashboard;

public sealed record AgentNotificationItem(
    int NotificationId,
    string Message,
    bool IsRead,
    string Category,
    string CreatedAtLabel,
    int? SupportFaqId,
    string TargetUrl,
    int? QaEvaluationTemplateId,
    bool OpensEvaluationPopup);

public sealed record AgentNotificationEvaluationDetail(
    int NotificationId,
    string Message,
    int? QaEvaluationTemplateId,
    IReadOnlyList<AgentNotificationEvaluationCategory> Categories);

public sealed record AgentNotificationEvaluationCategory(
    int QaEvaluationCategoryId,
    string Name,
    double WeightPercent,
    int DisplayOrder,
    IReadOnlyList<AgentNotificationEvaluationQuestion> Questions);

public sealed record AgentNotificationEvaluationQuestion(
    int QaEvaluationQuestionId,
    string QuestionKey,
    string Prompt,
    int DisplayOrder);

public sealed class AgentNotificationRecord
{
    public int NotificationId { get; set; }

    public string RecipientType { get; set; } = string.Empty;

    public int RecipientId { get; set; }

    public int? OrderId { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Category { get; set; } = string.Empty;
}
