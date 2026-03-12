namespace MemberTracker.Data.Messaging;

public interface IOrderConversationResolver
{
    Task<OrderConversationContext?> ResolveAsync(int orderId, int requestUserId, CancellationToken cancellationToken);
}

public sealed record OrderConversationContext(
    int OrderId,
    int BuyerUserId,
    int SellerUserId,
    bool CanRequestUserAccess);
