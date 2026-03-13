using NextHorizon.Models;
using NextHorizon.Messaging.Models;

namespace NextHorizon.Data.Messaging;

public interface IMessagingRepository
{
    Task<MessageConversationSummary> CreateOrGetGeneralAsync(int buyerConsumerId, int sellerId, CancellationToken cancellationToken);

    Task<MessageConversationSummary> CreateOrGetOrderAsync(int orderId, int buyerConsumerId, int sellerId, CancellationToken cancellationToken);

    Task<PagedResult<MessageConversationSummary>> ListByActorAsync(MessageActorContext actor, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<MessageConversationSummary?> GetConversationAsync(int conversationId, MessageActorContext actor, CancellationToken cancellationToken);

    Task<MessageItem?> SendMessageAsync(int conversationId, MessageActorContext actor, string body, string? attachmentUrl, CancellationToken cancellationToken);

    Task<IReadOnlyList<MessageItem>?> ListMessagesAsync(int conversationId, MessageActorContext actor, DateTime? before, int pageSize, CancellationToken cancellationToken);

    Task<bool> MarkReadAsync(int conversationId, MessageActorContext actor, CancellationToken cancellationToken);

    Task<bool> SoftDeleteMessageAsync(long messageId, int userId, CancellationToken cancellationToken);
}

public sealed record MessageActorContext(
    int UserId,
    int? ConsumerId,
    int? SellerId)
{
    public bool HasConversationRole => ConsumerId.HasValue || SellerId.HasValue;
}

public sealed record MessageConversationSummary(
    int ConversationId,
    int BuyerUserId,
    int SellerUserId,
    ConversationContextType ContextType,
    int? OrderId,
    DateTime? LastMessageAt,
    DateTime? BuyerLastReadAt,
    DateTime? SellerLastReadAt,
    string? LastMessagePreview,
    int UnreadCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record MessageItem(
    long MessageId,
    int ConversationId,
    int SenderUserId,
    string? Body,
    string? AttachmentUrl,
    DateTime SentAt,
    bool IsDeleted);

