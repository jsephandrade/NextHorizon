using MemberTracker.Models;
using MemberTracker.Models.Messaging;

namespace MemberTracker.Data.Messaging;

public interface IMessagingRepository
{
    Task<MessageConversationSummary> CreateOrGetGeneralAsync(int buyerUserId, int sellerUserId, CancellationToken cancellationToken);

    Task<MessageConversationSummary> CreateOrGetOrderAsync(int orderId, int buyerUserId, int sellerUserId, CancellationToken cancellationToken);

    Task<PagedResult<MessageConversationSummary>> ListByUserAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<MessageConversationSummary?> GetConversationAsync(int conversationId, int userId, CancellationToken cancellationToken);

    Task<MessageItem?> SendMessageAsync(int conversationId, int senderUserId, string body, string? attachmentUrl, CancellationToken cancellationToken);

    Task<IReadOnlyList<MessageItem>?> ListMessagesAsync(int conversationId, int userId, DateTime? before, int pageSize, CancellationToken cancellationToken);

    Task<bool> MarkReadAsync(int conversationId, int userId, CancellationToken cancellationToken);

    Task<bool> SoftDeleteMessageAsync(long messageId, int userId, CancellationToken cancellationToken);
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
