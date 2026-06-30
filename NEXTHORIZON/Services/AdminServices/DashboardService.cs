using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using NextHorizon.Models.Admin_Models;

namespace NextHorizon.Services.AdminServices
{
    public class DashboardService
    {
        private readonly AppDbContext _db;

        public DashboardService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<SuperAdminDashboardViewModel> GetHeroStatsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;

            var model = new SuperAdminDashboardViewModel
            {
                Stats = new PlatformStats
                {
                    TotalConsumers = await _db.Consumers.AsNoTracking().CountAsync(cancellationToken),
                    TotalSellers = await _db.Sellers.AsNoTracking().CountAsync(cancellationToken),
                    ActiveChallenges = await _db.Challenges.AsNoTracking()
                        .CountAsync(c => c.StartDate <= now && c.EndDate >= now && c.Status != "Cancelled", cancellationToken),
                    TotalKudos = FormatCompactNumber(await _db.ChallengeParticipants.AsNoTracking()
                        .SumAsync(p => (decimal?)(p.TotalDistanceKm ?? 0m), cancellationToken) ?? 0m)
                },
                PendingSellers = await _db.Sellers.AsNoTracking()
                    .CountAsync(s => (s.SellerStatus ?? "").ToLower() == "pending"
                        || (s.SellerStatus ?? "").ToLower() == "unverified", cancellationToken),
                PendingTickets = await _db.SupportTickets.AsNoTracking()
                    .CountAsync(t => t.Status != MyAspNetApp.Models.HelpCenter.SupportTicketStatus.Resolved, cancellationToken)
            };

            model.PlatformRevenue = await _db.Products.AsNoTracking()
                .Join(_db.ProductVariants.AsNoTracking(),
                    product => product.ProductId,
                    variant => variant.ProductId,
                    (product, variant) => variant.Price ?? 0m)
                .SumAsync(cancellationToken);

            model.TopSellers = await _db.Sellers.AsNoTracking()
                .Select(seller => new TopSellerViewModel
                {
                    ShopName = seller.BusinessName ?? "Seller",
                    MostPurchasedCount = _db.Products.Count(product => product.SellerId == seller.SellerId),
                    TotalProductsSold = _db.Products.Count(product => product.SellerId == seller.SellerId),
                    GrowthStatus = (seller.SellerStatus ?? "Active")
                })
                .OrderByDescending(s => s.TotalProductsSold)
                .Take(5)
                .ToListAsync(cancellationToken);

            var leaderboardRows = await (
                from participant in _db.ChallengeParticipants.AsNoTracking()
                join consumer in _db.Consumers.AsNoTracking()
                    on participant.UserId equals consumer.UserId into consumers
                from consumer in consumers.DefaultIfEmpty()
                join user in _db.Users.AsNoTracking()
                    on participant.UserId equals user.UserId into users
                from user in users.DefaultIfEmpty()
                orderby (participant.TotalDistanceKm ?? 0m) descending
                select new
                {
                    Name = consumer != null
                        ? ((consumer.FirstName ?? "") + " " + (consumer.LastName ?? "")).Trim()
                        : user != null ? user.Email : "Athlete",
                    Distance = participant.TotalDistanceKm ?? 0m,
                    Seconds = participant.TotalTimeSeconds ?? 0,
                    IsCompleted = participant.IsCompleted ?? false
                })
                .Take(5)
                .ToListAsync(cancellationToken);

            model.ConsumerLeaderboard = leaderboardRows
                .Select((row, index) => new ConsumerLeaderboardViewModel
                {
                    Rank = index + 1,
                    UserName = string.IsNullOrWhiteSpace(row.Name) ? "Athlete" : row.Name,
                    StravaKM = row.Distance,
                    Pace = FormatPace(row.Distance, row.Seconds),
                    IsVerified = row.IsCompleted
                })
                .ToList();

            model.PendingTicketsList = await _db.SupportTickets.AsNoTracking()
                .Where(t => t.Status != MyAspNetApp.Models.HelpCenter.SupportTicketStatus.Resolved)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new PendingTicketViewModel
                {
                    Id = t.SupportTicketId,
                    Category = t.FaqCategory ?? "Support",
                    Question = t.Subject,
                    Status = t.Status.ToString(),
                    UserType = "Consumer",
                    SenderType = "Consumer",
                    SenderName = t.ReferenceCode,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync(cancellationToken);

            model.ApprovalHub.AddRange(await _db.Sellers.AsNoTracking()
                .Where(s => (s.SellerStatus ?? "").ToLower() == "pending"
                    || (s.SellerStatus ?? "").ToLower() == "unverified")
                .OrderByDescending(s => s.CreatedAt)
                .Take(3)
                .Select(s => new DashboardApprovalItem
                {
                    RequestType = "New Seller",
                    EntityName = s.BusinessName ?? "Seller",
                    Details = s.BusinessEmail ?? "",
                    Status = s.SellerStatus ?? "Pending",
                    ActionLabel = "Review",
                    RedirectUrl = "/Admin/Sellers"
                })
                .ToListAsync(cancellationToken));

            model.ApprovalHub.AddRange(await _db.Products.AsNoTracking()
                .Where(p => (p.Status ?? "").ToLower() == "pending")
                .OrderByDescending(p => p.ProductId)
                .Take(3)
                .Select(p => new DashboardApprovalItem
                {
                    RequestType = "Product",
                    EntityName = p.ProductName,
                    Details = p.Category ?? "",
                    Status = p.Status ?? "Pending",
                    ActionLabel = "Review",
                    RedirectUrl = "/Admin/FinanceRequest"
                })
                .ToListAsync(cancellationToken));

            return model;
        }

        private static string FormatCompactNumber(decimal value)
        {
            return value >= 1000m ? $"{value / 1000m:N1}K" : value.ToString("N0");
        }

        private static string FormatPace(decimal distanceKm, int seconds)
        {
            if (distanceKm <= 0 || seconds <= 0)
            {
                return "0:00/KM";
            }

            var pace = TimeSpan.FromSeconds((double)(seconds / distanceKm));
            return $"{(int)pace.TotalMinutes}:{pace.Seconds:00}/KM";
        }
    }
}
