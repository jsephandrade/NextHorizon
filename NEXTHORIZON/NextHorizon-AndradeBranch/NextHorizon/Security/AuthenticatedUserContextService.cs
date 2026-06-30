using System.Security.Claims;
using NextHorizon.Data;
using Microsoft.EntityFrameworkCore;

namespace NextHorizon.Security;

public sealed class AuthenticatedUserContextService : IAuthenticatedUserContextService
{
    private const int DevelopmentFallbackSellerId = 1;

    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AuthenticatedUserContextService(ApplicationDbContext dbContext, IWebHostEnvironment webHostEnvironment)
    {
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
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

        var consumerRecord = await _dbContext.Set<ConsumerRef>()
            .AsNoTracking()
            .Where(consumer => consumer.UserId == userId)
            .Select(consumer => new
            {
                ConsumerId = (int?)consumer.ConsumerId,
                ConsumerAccountUserId = (int?)consumer.UserId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var sellerRecord = await _dbContext.Set<SellerRef>()
            .AsNoTracking()
            .Where(seller => seller.UserId == userId)
            .Select(seller => new
            {
                SellerId = (int?)seller.SellerId,
                SellerAccountUserId = (int?)seller.UserId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var consumerId = consumerRecord?.ConsumerId;
        var consumerAccountUserId = consumerRecord?.ConsumerAccountUserId;
        var sellerId = sellerRecord?.SellerId;
        var sellerAccountUserId = sellerRecord?.SellerAccountUserId;

        if (!sellerId.HasValue && _webHostEnvironment.IsDevelopment())
        {
            sellerId = DevelopmentFallbackSellerId;
            sellerAccountUserId = await _dbContext.Set<SellerRef>()
                .AsNoTracking()
                .Where(seller => seller.SellerId == sellerId.Value)
                .Select(seller => (int?)seller.UserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new AuthenticatedUserContext(userId, consumerId, consumerAccountUserId, sellerId, sellerAccountUserId);
    }
}

