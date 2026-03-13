namespace NextHorizon.Security;

public sealed record AuthenticatedUserContext(
    int UserId,
    int? ConsumerId,
    int? SellerId)
{
    public bool HasConsumerRole => ConsumerId.HasValue;

    public bool HasSellerRole => SellerId.HasValue;

    public bool HasMessagingRole => HasConsumerRole || HasSellerRole;
}
