namespace NextHorizon.Models;

public sealed record NotificationListItem(
    int NotificationId,
    string Message,
    string Category,
    string CreatedAtLabel,
    bool IsRead,
    int? TargetId,
    bool IsClickable);

public sealed record NotificationListData(
    IReadOnlyList<NotificationListItem> Items,
    bool HasUnread);
