using NextHorizon.Models;

namespace NextHorizon.Services;

public interface ISellerContextService
{
    Task<SellerContextInfo> ResolveSellerAsync(string? userIdentity, CancellationToken cancellationToken = default);
}
