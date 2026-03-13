using System.Security.Claims;

namespace NextHorizon.Security;

public interface IAuthenticatedUserContextService
{
    Task<AuthenticatedUserContext?> GetCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);

    Task<AuthenticatedUserContext?> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
}
