using Microsoft.Extensions.Configuration;

namespace MemberTracker.Data.Messaging;

public sealed class SimulatedOrderConversationResolver : IOrderConversationResolver
{
    private readonly IReadOnlyDictionary<int, SimulatedOrderEntry> _knownOrders;

    public SimulatedOrderConversationResolver(IConfiguration configuration)
    {
        var configuredEntries = configuration
            .GetSection("Messaging:SimulatedOrders")
            .Get<List<SimulatedOrderEntry>>() ?? [];

        _knownOrders = configuredEntries
            .Where(entry =>
                entry.OrderId > 0 &&
                int.TryParse(entry.BuyerUserId, out _) &&
                int.TryParse(entry.SellerUserId, out _))
            .GroupBy(entry => entry.OrderId)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    public Task<OrderConversationContext?> ResolveAsync(int orderId, int requestUserId, CancellationToken cancellationToken)
    {
        if (orderId <= 0 || requestUserId <= 0)
        {
            return Task.FromResult<OrderConversationContext?>(null);
        }

        if (_knownOrders.TryGetValue(orderId, out var configuredOrder))
        {
            var buyerUserId = int.Parse(configuredOrder.BuyerUserId);
            var sellerUserId = int.Parse(configuredOrder.SellerUserId);
            var canAccess = requestUserId == buyerUserId || requestUserId == sellerUserId;

            return Task.FromResult<OrderConversationContext?>(new OrderConversationContext(
                configuredOrder.OrderId,
                buyerUserId,
                sellerUserId,
                canAccess));
        }

        return Task.FromResult<OrderConversationContext?>(new OrderConversationContext(
            orderId,
            requestUserId,
            CreateDeterministicUserId($"simulated-seller-{orderId}"),
            true));
    }

    private static int CreateDeterministicUserId(string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return (int)(BitConverter.ToUInt32(bytes, 0) % int.MaxValue) + 1;
    }

    public sealed class SimulatedOrderEntry
    {
        public int OrderId { get; init; }

        public string BuyerUserId { get; init; } = string.Empty;

        public string SellerUserId { get; init; } = string.Empty;
    }
}
