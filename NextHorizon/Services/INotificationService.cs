using NextHorizon.Models;

namespace NextHorizon.Services;

public interface INotificationService
{
    Task<NotificationListData> GetNotificationsAsync(
        int userId,
        int take,
        CancellationToken cancellationToken);

    Task MarkAllReadAsync(
        int userId,
        CancellationToken cancellationToken);

    Task ClearAllAsync(
        int userId,
        CancellationToken cancellationToken);

    Task NotifyUserAsync(
        int userId,
        string message,
        string category,
        int? orderId,
        CancellationToken cancellationToken);

    Task NotifyUsersByUserTypeAsync(
        string userType,
        string message,
        string category,
        int? orderId,
        CancellationToken cancellationToken);
}
