using System.Security.Claims;
using NextHorizon.Data;
using Microsoft.EntityFrameworkCore;

namespace NextHorizon.Security;

public sealed class AuthenticatedUserContextService : IAuthenticatedUserContextService
{
    private readonly ApplicationDbContext _dbContext;

    public AuthenticatedUserContextService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AuthenticatedUserContext?> GetCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            return Task.FromResult<AuthenticatedUserContext?>(null);
        }

        return GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<AuthenticatedUserContext?> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return null;
        }

        var userExists = await _dbContext.Set<PlatformUser>()
            .AsNoTracking()
            .AnyAsync(user => user.UserId == userId && user.IsActive, cancellationToken);

        if (!userExists)
        {
            return null;
        }

        var consumerId = await _dbContext.Set<ConsumerRef>()
            .AsNoTracking()
            .Where(consumer => consumer.UserId == userId)
            .Select(consumer => (int?)consumer.ConsumerId)
            .FirstOrDefaultAsync(cancellationToken);

        var sellerId = await _dbContext.Set<SellerRef>()
            .AsNoTracking()
            .Where(seller => seller.UserId == userId)
            .Select(seller => (int?)seller.SellerId)
            .FirstOrDefaultAsync(cancellationToken);

        return new AuthenticatedUserContext(userId, consumerId, sellerId);
    }
}

