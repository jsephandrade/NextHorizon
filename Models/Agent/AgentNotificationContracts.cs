namespace NextHorizon.Models.Agent;

public sealed record AgentNotificationItem(
    int NotificationId,
    string Message,
    bool IsRead,
    string Category,
    string CreatedAtLabel,
    int? SupportFaqId,
    string TargetUrl);

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
