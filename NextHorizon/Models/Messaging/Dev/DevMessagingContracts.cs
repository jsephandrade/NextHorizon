namespace MemberTracker.Models.Messaging.Dev;

public sealed class DevGeneralConversationRequest
{
    public string ActorUserId { get; init; } = string.Empty;
    public string BuyerUserId { get; init; } = string.Empty;
    public string SellerUserId { get; init; } = string.Empty;
}

public sealed class DevOrderConversationRequest
{
    public string ActorUserId { get; init; } = string.Empty;
    public int OrderId { get; init; }
    public string? BuyerUserId { get; init; }
    public string? SellerUserId { get; init; }
}

public sealed class DevSendMessageRequest
{
    public string ActorUserId { get; init; } = string.Empty;
    public string? Body { get; init; }
    public IFormFile? Attachment { get; init; }
}

public sealed class DevActorRequest
{
    public string ActorUserId { get; init; } = string.Empty;
}
