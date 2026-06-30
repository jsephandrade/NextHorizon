using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MyAspNetApp.Data;
using MyAspNetApp.Models;
using MyAspNetApp.Models.ViewModels;
using NextHorizon.Models.Admin_Models;
using NextHorizon.Services.AdminServices;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace MyAspNetApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly DashboardService _dashboardService;

        public AdminController(AppDbContext db, IMemoryCache cache, DashboardService dashboardService)
        {
            _db = db;
            _cache = cache;
            _dashboardService = dashboardService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        {
            var model = await _dashboardService.GetHeroStatsAsync(cancellationToken);
            return View(model);
        }

        [HttpGet]
        public IActionResult GetLeaderboard(string category = "All")
        {
            return Json(Array.Empty<object>());
        }

        public async Task<IActionResult> Analytics()
        {
            try
            {
                var topSellers = new List<NextHorizon.Models.Admin_Models.SellerMetric>();
                var topProducts = new List<NextHorizon.Models.Admin_Models.ProductMetric>();
                var performanceTrends = new List<NextHorizon.Models.Admin_Models.AnalyticsChartData>();
                var peakEngagement = new List<NextHorizon.Models.Admin_Models.HourlyEngagementMetric>();

                int totalConsumers = 0, totalSellers = 0, totalOrders = 0;
                decimal totalRevenue = 0; decimal avgOrderValue = 0;

                var connectionString = _db.Database.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("sp_GetAnalyticsSummary", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        await connection.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                totalConsumers = reader.GetInt32(reader.GetOrdinal("TotalConsumers"));
                                totalSellers = reader.GetInt32(reader.GetOrdinal("TotalSellers"));
                                totalOrders = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                                var avg = reader["AvgOrderValue"];
                                avgOrderValue = avg != DBNull.Value ? Convert.ToDecimal(avg) : 0;
                            }

                            await reader.NextResultAsync();
                            while (await reader.ReadAsync())
                                performanceTrends.Add(new NextHorizon.Models.Admin_Models.AnalyticsChartData
                                {
                                    DateLabel = reader["DateLabel"]?.ToString() ?? string.Empty,
                                    TotalRevenue = reader.IsDBNull(reader.GetOrdinal("TotalRevenue")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                                    ChallengeParticipants = reader.IsDBNull(reader.GetOrdinal("ChallengeParticipants")) ? 0 : reader.GetInt32(reader.GetOrdinal("ChallengeParticipants"))
                                });

                            await reader.NextResultAsync();
                            while (await reader.ReadAsync())
                                topSellers.Add(new NextHorizon.Models.Admin_Models.SellerMetric
                                {
                                    Rank = (int)reader.GetInt64(reader.GetOrdinal("Rank")),
                                    SellerName = reader["ShopName"]?.ToString() ?? string.Empty,
                                    ShopName = reader["ShopName"]?.ToString() ?? string.Empty,
                                    OrdersFulfilled = reader.GetInt32(reader.GetOrdinal("OrdersFulfilled")),
                                    RevenueGenerated = reader.GetDecimal(reader.GetOrdinal("RevenueGenerated"))
                                });

                            await reader.NextResultAsync();
                            while (await reader.ReadAsync())
                                topProducts.Add(new NextHorizon.Models.Admin_Models.ProductMetric
                                {
                                    ProductName = reader["ProductName"]?.ToString() ?? string.Empty,
                                    Category = reader["Category"]?.ToString() ?? string.Empty,
                                    UnitsSold = reader.GetInt32(reader.GetOrdinal("UnitsSold")),
                                    Revenue = reader.GetDecimal(reader.GetOrdinal("Revenue")),
                                    SellerName = reader["ShopName"]?.ToString() ?? string.Empty
                                });

                            await reader.NextResultAsync();
                            while (await reader.ReadAsync())
                                peakEngagement.Add(new NextHorizon.Models.Admin_Models.HourlyEngagementMetric
                                {
                                    Hour = reader.GetInt32(reader.GetOrdinal("Hour")),
                                    PurchaseCount = reader.GetInt32(reader.GetOrdinal("PurchaseCount")),
                                    ActivitySyncCount = reader.GetInt32(reader.GetOrdinal("ActivitySyncCount"))
                                });
                        }
                    }
                }

                if (!performanceTrends.Any())
                    performanceTrends.Add(new NextHorizon.Models.Admin_Models.AnalyticsChartData { DateLabel = "No Data", TotalRevenue = 0, ChallengeParticipants = 0 });

                var viewModel = new AnalyticsViewModel
                {
                    TotalConsumers = totalConsumers,
                    TotalSellers = totalSellers,
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = (double)avgOrderValue,
                    ChallengeToSaleConversionRate = totalOrders > 0 && totalConsumers > 0 ? Math.Round((double)totalOrders / totalConsumers * 100, 1) : 0,
                    PerformanceTrends = performanceTrends,
                    TopSellers = topSellers,
                    TopMovingProducts = topProducts,
                    PeakEngagementData = peakEngagement.Any() ? peakEngagement : new List<NextHorizon.Models.Admin_Models.HourlyEngagementMetric> { new() { Hour = 0, ActivitySyncCount = 0, PurchaseCount = 0 } }
                };

                return View(viewModel);
            }
            catch
            {
                return View(new AnalyticsViewModel());
            }
        }

        public IActionResult Tasks()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetChallengeStatistics()
        {
            try
            {
                var totalAthletes = await _db.ChallengeParticipants
                    .AsNoTracking()
                    .Select(p => p.UserId)
                    .Distinct()
                    .CountAsync();

                var now = DateTime.Now;
                var activeChallenges = await _db.Challenges
                    .AsNoTracking()
                    .CountAsync(c => c.StartDate <= now && c.EndDate >= now && c.Status != "Cancelled");

                var participantTotals = await _db.ChallengeParticipants
                    .AsNoTracking()
                    .Select(p => new { p.TotalDistanceKm, p.TotalTimeSeconds })
                    .ToListAsync();

                var avgDistance = participantTotals.Count == 0
                    ? 0m
                    : participantTotals.Average(p => p.TotalDistanceKm ?? 0m);
                var totalTimeHours = participantTotals.Sum(p => p.TotalTimeSeconds ?? 0) / 3600m;

                return Json(new
                {
                    totalAthletes,
                    activeChallenges,
                    avgDistance,
                    totalTimeHours
                });
            }
            catch
            {
                return Json(new { totalAthletes = 0, activeChallenges = 0, avgDistance = 0m, totalTimeHours = 0m });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChallenges(string? status = null)
        {
            try
            {
                var challenges = await _db.Challenges
                    .AsNoTracking()
                    .OrderByDescending(c => c.StartDate)
                    .ToListAsync();

                if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
                {
                    challenges = challenges
                        .Where(c => string.Equals(ResolveChallengeStatus(c), status, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                return Json(challenges.Select(ToChallengeJson).ToList());
            }
            catch
            {
                return Json(Array.Empty<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveChallenges()
        {
            try
            {
                var now = DateTime.Now;
                var challenges = await _db.Challenges
                    .AsNoTracking()
                    .Where(c => c.EndDate >= now && c.Status != "Cancelled")
                    .OrderBy(c => c.StartDate)
                    .Select(c => new
                    {
                        challengeId = c.ChallengeId,
                        title = c.Title,
                        activityType = c.ActivityType
                    })
                    .ToListAsync();

                return Json(challenges);
            }
            catch
            {
                return Json(Array.Empty<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGlobalLeaderboard(int? challengeId = null)
        {
            try
            {
                var rows = await (
                    from participant in _db.ChallengeParticipants.AsNoTracking()
                    join challenge in _db.Challenges.AsNoTracking()
                        on participant.ChallengeId equals challenge.ChallengeId
                    join consumer in _db.Consumers.AsNoTracking()
                        on participant.UserId equals consumer.UserId into consumers
                    from consumer in consumers.DefaultIfEmpty()
                    join user in _db.Users.AsNoTracking()
                        on participant.UserId equals user.UserId into users
                    from user in users.DefaultIfEmpty()
                    where !challengeId.HasValue || participant.ChallengeId == challengeId.Value
                    select new
                    {
                        participant,
                        challenge,
                        consumer,
                        user
                    })
                    .ToListAsync();

                var leaderboard = rows
                    .OrderByDescending(x => x.participant.TotalDistanceKm ?? 0m)
                    .ThenBy(x => x.participant.TotalTimeSeconds ?? int.MaxValue)
                    .Select((x, index) => new
                    {
                        globalRank = index + 1,
                        participantId = x.participant.ParticipantId,
                        userId = x.participant.UserId,
                        challengeId = x.challenge.ChallengeId,
                        athleteName = BuildAdminAthleteName(x.consumer, x.user),
                        avatarUrl = x.user?.ProfilePicture is { Length: > 0 }
                            ? $"/Leaderboard/ProfileImage/{x.participant.UserId}?v={(x.user.UpdatedAt ?? x.user.CreatedAt)?.Ticks ?? 0}"
                            : $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(BuildAdminAthleteName(x.consumer, x.user))}",
                        challengeTitle = x.challenge.Title,
                        activityType = x.challenge.ActivityType,
                        totalDistanceKm = x.participant.TotalDistanceKm ?? 0m,
                        challengeGoalKm = x.challenge.GoalKm,
                        totalActivities = x.participant.TotalActivities ?? 0,
                        totalTimeFormatted = FormatAdminDuration(x.participant.TotalTimeSeconds ?? 0),
                        progressPercent = x.challenge.GoalKm <= 0 ? 0m : Math.Min(100m, ((x.participant.TotalDistanceKm ?? 0m) / x.challenge.GoalKm) * 100m)
                    })
                    .ToList();

                return Json(leaderboard);
            }
            catch
            {
                return Json(Array.Empty<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllParticipants(int? challengeId = null)
        {
            try
            {
                var rows = await (
                    from participant in _db.ChallengeParticipants.AsNoTracking()
                    join challenge in _db.Challenges.AsNoTracking()
                        on participant.ChallengeId equals challenge.ChallengeId
                    join consumer in _db.Consumers.AsNoTracking()
                        on participant.UserId equals consumer.UserId into consumers
                    from consumer in consumers.DefaultIfEmpty()
                    join user in _db.Users.AsNoTracking()
                        on participant.UserId equals user.UserId into users
                    from user in users.DefaultIfEmpty()
                    where !challengeId.HasValue || participant.ChallengeId == challengeId.Value
                    orderby participant.JoinedAt descending
                    select new
                    {
                        participant,
                        challenge,
                        consumer,
                        user
                    })
                    .ToListAsync();

                var participants = rows.Select(x => new
                {
                    participantId = x.participant.ParticipantId,
                    challengeId = x.participant.ChallengeId,
                    userId = x.participant.UserId,
                    consumerId = x.participant.ConsumerId,
                    athleteName = BuildAdminAthleteName(x.consumer, x.user),
                    email = x.user?.Email ?? string.Empty,
                    username = x.consumer?.Username ?? string.Empty,
                    phoneNumber = x.consumer?.PhoneNumber ?? string.Empty,
                    challengeTitle = x.challenge.Title,
                    activityType = x.challenge.ActivityType,
                    joinedAt = x.participant.JoinedAt,
                    goalKm = x.challenge.GoalKm,
                    challengeGoalKm = x.challenge.GoalKm,
                    totalDistanceKm = x.participant.TotalDistanceKm ?? 0m,
                    totalActivities = x.participant.TotalActivities ?? 0,
                    totalTimeSeconds = x.participant.TotalTimeSeconds ?? 0,
                    totalTimeFormatted = FormatAdminDuration(x.participant.TotalTimeSeconds ?? 0),
                    status = x.participant.Status,
                    avatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(BuildAdminAthleteName(x.consumer, x.user))}",
                    progressPercent = x.challenge.GoalKm <= 0 ? 0m : Math.Min(100m, ((x.participant.TotalDistanceKm ?? 0m) / x.challenge.GoalKm) * 100m)
                }).ToList();

                return Json(new { success = true, participants });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, participants = Array.Empty<object>() });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateParticipantStatus([FromBody] UpdateParticipantStatusRequest request)
        {
            try
            {
                var participant = await _db.ChallengeParticipants
                    .FirstOrDefaultAsync(p => p.ParticipantId == request.ParticipantId);

                if (participant == null)
                {
                    return Json(new { success = false, message = "Participant not found." });
                }

                participant.Status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : request.Status.Trim();
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = $"Participant status updated to {participant.Status}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAllPrizes(string status = "all", string? searchCode = null)
        {
            return Json(new { success = true, prizes = Array.Empty<object>() });
        }

        [HttpPost]
        public IActionResult ClaimPrizeWithProof()
        {
            return Json(new { success = false, message = "Prize claiming is not configured for the current challenge database." });
        }

        [HttpPost]
        public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeRequest request)
        {
            try
            {
                var challenge = new DbChallenge
                {
                    Title = request.Title?.Trim() ?? string.Empty,
                    Description = request.Description?.Trim() ?? string.Empty,
                    Rules = request.Rules?.Trim() ?? string.Empty,
                    Prizes = request.Prizes?.Trim() ?? string.Empty,
                    GoalKm = request.GoalKm,
                    ActivityType = request.ActivityType?.Trim() ?? string.Empty,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Status = ResolveChallengeStatus(request.StartDate, request.EndDate),
                    BannerImage = DecodeDataUrl(request.BannerBase64),
                    BannerImageName = request.BannerImageName,
                    BannerImageContentType = request.BannerImageContentType,
                    CreatedAt = DateTime.Now,
                    CreatedBy = HttpContext.Session.GetInt32("StaffId") ?? 0,
                    TotalParticipants = 0,
                    TotalCompleted = 0
                };

                _db.Challenges.Add(challenge);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Challenge created successfully.", challengeId = challenge.ChallengeId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateChallenge([FromBody] UpdateChallengeRequest request)
        {
            try
            {
                var challenge = await _db.Challenges.FirstOrDefaultAsync(c => c.ChallengeId == request.ChallengeId);
                if (challenge == null)
                {
                    return Json(new { success = false, message = "Challenge not found." });
                }

                challenge.Title = request.Title?.Trim() ?? challenge.Title;
                challenge.Description = request.Description?.Trim() ?? challenge.Description;
                challenge.Rules = request.Rules?.Trim() ?? challenge.Rules;
                challenge.Prizes = request.Prizes?.Trim() ?? challenge.Prizes;
                challenge.GoalKm = request.GoalKm;
                challenge.ActivityType = request.ActivityType?.Trim() ?? challenge.ActivityType;
                challenge.StartDate = request.StartDate;
                challenge.EndDate = request.EndDate;
                challenge.Status = string.IsNullOrWhiteSpace(request.Status)
                    ? ResolveChallengeStatus(request.StartDate, request.EndDate)
                    : request.Status.Trim();
                var banner = DecodeDataUrl(request.BannerBase64);
                if (banner is { Length: > 0 })
                {
                    challenge.BannerImage = banner;
                    challenge.BannerImageName = request.BannerImageName;
                    challenge.BannerImageContentType = request.BannerImageContentType;
                }
                challenge.UpdatedAt = DateTime.Now;
                challenge.UpdatedBy = HttpContext.Session.GetInt32("StaffId") ?? 0;

                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Challenge updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAnalyticsData(int days = 30, string? startDate = null, string? endDate = null)
        {
            try
            {
                DateTime start, end;

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    if (!DateTime.TryParse(startDate, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out start)
                     || !DateTime.TryParse(endDate, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out end))
                    {
                        return Json(new { error = "Invalid date format." });
                    }
                    end = end.AddDays(1);
                }
                else
                {
                    end = DateTime.Now;
                    start = days switch
                    {
                        7 => DateTime.Now.AddDays(-7),
                        90 => DateTime.Now.AddDays(-90),
                        _ => DateTime.Now.AddDays(-30)
                    };
                }

                var trends = new List<object>();
                var peakData = new List<object>();
                var topProducts = new List<object>();
                int totalOrders = 0;
                decimal totalRevenue = 0, avgOrder = 0;

                var connectionString = _db.Database.GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    using (var cmd = new SqlCommand("sp_GetAnalyticsData", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Start", start);
                        cmd.Parameters.AddWithValue("@End", end);
                        await connection.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                                trends.Add(new
                                {
                                    dateLabel = reader["DateLabel"]?.ToString() ?? string.Empty,
                                    totalRevenue = reader.IsDBNull(reader.GetOrdinal("TotalRevenue")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalRevenue")),
                                    challengeParticipants = reader.IsDBNull(reader.GetOrdinal("ChallengeParticipants")) ? 0 : reader.GetInt32(reader.GetOrdinal("ChallengeParticipants"))
                                });

                            await reader.NextResultAsync();
                            if (await reader.ReadAsync())
                            {
                                totalOrders = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                                var rev = reader["TotalRevenue"];
                                totalRevenue = rev != DBNull.Value ? Convert.ToDecimal(rev) : 0;
                                var avg = reader["AvgOrder"];
                                avgOrder = avg != DBNull.Value ? Convert.ToDecimal(avg) : 0;
                            }

                            await reader.NextResultAsync();
                            while (await reader.ReadAsync())
                                peakData.Add(new
                                {
                                    hour = reader.GetInt32(reader.GetOrdinal("Hour")),
                                    purchaseCount = reader.GetInt32(reader.GetOrdinal("PurchaseCount")),
                                    activitySyncCount = reader.GetInt32(reader.GetOrdinal("ActivitySyncCount"))
                                });

                            await reader.NextResultAsync();
                            while (await reader.ReadAsync())
                                topProducts.Add(new
                                {
                                    productName = reader["ProductName"]?.ToString() ?? string.Empty,
                                    unitsSold = reader.GetInt32(reader.GetOrdinal("UnitsSold")),
                                    revenue = reader.GetDecimal(reader.GetOrdinal("Revenue")),
                                    sellerName = reader["ShopName"]?.ToString() ?? string.Empty
                                });
                        }
                    }
                }

                return Json(new { trends, peakData, topProducts, totalOrders, totalRevenue, avgOrderValue = avgOrder });
            }
            catch (Exception ex) { return Json(new { error = ex.Message }); }
        }

        public IActionResult Consumers()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetConsumers(string viewType = "active")
        {
            if (string.Equals(viewType, "archived", StringComparison.OrdinalIgnoreCase))
            {
                return Json(Array.Empty<object>());
            }

            try
            {
                var records = await _db.Consumers
                    .AsNoTracking()
                    .Include(c => c.User)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var consumers = records.Select(c => new
                    {
                        consumerId = c.ConsumerId,
                        fullName = string.Join(" ", new[] { c.FirstName, c.MiddleName, c.LastName }
                            .Where(x => !string.IsNullOrWhiteSpace(x))),
                        phoneNumber = c.PhoneNumber ?? string.Empty,
                        email = c.User != null ? c.User.Email ?? string.Empty : string.Empty,
                        address = c.Address ?? string.Empty,
                        dateJoined = c.CreatedAt.HasValue ? c.CreatedAt.Value.ToString("MMM dd, yyyy") : string.Empty
                    })
                    .ToList();

                return Json(consumers);
            }
            catch
            {
                return Json(Array.Empty<object>());
            }
        }

        [HttpPost]
        public IActionResult DeleteConsumer()
        {
            return Json(new { success = false, message = "Consumer archiving is not configured in the merged database." });
        }

        [HttpPost]
        public IActionResult RestoreConsumer()
        {
            return Json(new { success = false, message = "Consumer restore is not configured in the merged database." });
        }

        public IActionResult Sellers()
        {
            return View(new AnalyticsViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> GetSellers(string status = "Pending")
        {
            try
            {
                var records = await (
                    from seller in _db.Sellers.AsNoTracking()
                    join user in _db.Users.AsNoTracking() on seller.UserId equals user.UserId into users
                    from user in users.DefaultIfEmpty()
                    select new { seller, user })
                    .ToListAsync();

                var normalizedStatus = status?.Trim() ?? "Pending";
                var sellers = records
                    .Where(x => SellerMatchesStatus(x.seller.SellerStatus, normalizedStatus))
                    .OrderByDescending(x => x.seller.CreatedAt)
                    .Select(x => new
                    {
                        sellerId = x.seller.SellerId,
                        businessName = x.seller.BusinessName ?? "Unnamed business",
                        ownerName = x.user?.Email ?? x.seller.BusinessEmail ?? "Seller",
                        businessEmail = x.seller.BusinessEmail ?? x.user?.Email ?? string.Empty,
                        businessPhone = x.seller.BusinessPhone ?? string.Empty,
                        businessAddress = x.seller.BusinessAddress ?? string.Empty,
                        status = x.seller.SellerStatus ?? string.Empty,
                        createdAt = x.seller.CreatedAt,
                        hasDocument = !string.IsNullOrWhiteSpace(x.seller.DocumentPath),
                        totalProducts = 0,
                        totalSales = 0m
                    })
                    .ToList();

                return Json(sellers);
            }
            catch
            {
                return Json(Array.Empty<object>());
            }
        }

        [HttpGet]
        public IActionResult GetSellerDocumentInfo(int sellerId)
        {
            return Json(new { format = "none" });
        }

        [HttpGet]
        public IActionResult GetSellerDocument(int sellerId, string docType = "single")
        {
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetSellerLogo(int sellerId)
        {
            try
            {
                var seller = await _db.Sellers.AsNoTracking().FirstOrDefaultAsync(s => s.SellerId == sellerId);
                if (seller?.LogoData is { Length: > 0 })
                {
                    return File(seller.LogoData, seller.LogoContentType ?? seller.LogoMimeType ?? "image/png");
                }
            }
            catch
            {
            }

            return NotFound();
        }

        [HttpPost]
        public IActionResult UpdateSellerStatus()
        {
            return Json(new { success = false, message = "Seller status updates are not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult UpdateSellerInfo()
        {
            return Json(new { success = false, message = "Seller profile editing is not configured in the merged admin module." });
        }

        public async Task<IActionResult> FinanceRequest(CancellationToken cancellationToken)
        {
            return View(await BuildFinanceRequestsViewModelAsync(cancellationToken));
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingPayouts(CancellationToken cancellationToken)
        {
            var model = await BuildFinanceRequestsViewModelAsync(cancellationToken);
            return Json(model.PendingPayouts);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingDiscounts(CancellationToken cancellationToken)
        {
            var model = await BuildFinanceRequestsViewModelAsync(cancellationToken);
            return Json(model.PendingDiscounts);
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingProducts(CancellationToken cancellationToken)
        {
            var model = await BuildFinanceRequestsViewModelAsync(cancellationToken);
            return Json(model.PendingProducts);
        }

        [HttpGet]
        public async Task<IActionResult> GetGlobalPromotions(CancellationToken cancellationToken)
        {
            var model = await BuildFinanceRequestsViewModelAsync(cancellationToken);
            return Json(model.ActivePromotions);
        }

        [HttpPost]
        public IActionResult ProcessPayout()
        {
            return Json(new { success = false, message = "Payout processing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult ProcessDiscount()
        {
            return Json(new { success = false, message = "Discount processing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult UpdateProductStatus()
        {
            return Json(new { success = false, message = "Product approval processing is available from Product Approvals." });
        }

        [HttpPost]
        public IActionResult SaveGlobalPromotion()
        {
            return Json(new { success = false, message = "Global promotions are not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult DeleteGlobalPromotion()
        {
            return Json(new { success = false, message = "Global promotions are not configured in the merged admin module." });
        }

        public IActionResult Logistics()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLogisticsStats(CancellationToken cancellationToken)
        {
            var logistics = await LoadLogisticsPartnersAsync(null, null, cancellationToken);
            return Json(new
            {
                totalCount = logistics.Count,
                activeCount = logistics.Count(l => string.Equals(l.Status, "Active", StringComparison.OrdinalIgnoreCase)),
                inactiveCount = logistics.Count(l => string.Equals(l.Status, "Inactive", StringComparison.OrdinalIgnoreCase)),
                archivedCount = logistics.Count(l => string.Equals(l.Status, "Archived", StringComparison.OrdinalIgnoreCase))
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLogistics(string? statusFilter = null, string? searchTerm = null, CancellationToken cancellationToken = default)
        {
            return Json(await LoadLogisticsPartnersAsync(statusFilter, searchTerm, cancellationToken));
        }

        [HttpGet]
        public IActionResult GetLogisticsPerformance(int logisticsId)
        {
            return Json(new { success = true, basicMetrics = new { }, monthlyTrend = Array.Empty<object>() });
        }

        [HttpPost]
        public IActionResult SaveLogistics()
        {
            return Json(new { success = false, message = "Logistics editing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult UpdateLogisticsStatus()
        {
            return Json(new { success = false, message = "Logistics status updates are not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult DeleteLogistics()
        {
            return Json(new { success = false, message = "Logistics archiving is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult RestoreLogistics()
        {
            return Json(new { success = false, message = "Logistics restore is not configured in the merged admin module." });
        }

        public async Task<IActionResult> HelpCenter(CancellationToken cancellationToken)
        {
            return View(await BuildHelpCenterViewModelAsync(cancellationToken));
        }

        [HttpGet]
        public async Task<IActionResult> GetQueueSessions(CancellationToken cancellationToken)
        {
            var model = await BuildHelpCenterViewModelAsync(cancellationToken);
            return Json(model.Sessions.Select(s => new
            {
                id = s.Id,
                no = s.SessionNo,
                name = s.CustomerName,
                email = s.CustomerEmail,
                role = s.Role,
                av = s.Initials,
                bg = s.AvatarColor,
                waitSec = s.WaitSeconds,
                pos = s.QueuePosition,
                status = s.Status,
                cat = s.Category,
                agent = s.AssignedTo ?? "Unassigned"
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAgentStatus(CancellationToken cancellationToken)
        {
            var model = await BuildHelpCenterViewModelAsync(cancellationToken);
            return Json(model.Agents.Select(a => new
            {
                name = a.Name,
                initials = a.Initials,
                status = a.Status,
                sessions = a.ActiveSessions,
                max = a.MaxSessions,
                slots = a.Slots.Select(s => new { convId = s.ConversationId, client = s.ClientName, cat = s.Category, slotNum = s.SlotNumber })
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableAgents(CancellationToken cancellationToken)
        {
            var agents = await _db.SupportAgents.AsNoTracking()
                .Where(a => (a.AgentStatus ?? "").ToLower() == "online" || (a.AgentStatus ?? "").ToLower() == "available")
                .OrderBy(a => a.AgentName)
                .Select(a => new { id = a.ChatId, name = a.AgentName ?? $"Agent {a.ChatId}" })
                .ToListAsync(cancellationToken);
            return Json(agents);
        }

        [HttpPost]
        public IActionResult QueueAssign()
        {
            return Json(new { success = false, message = "No support agents are available." });
        }

        [HttpPost]
        public IActionResult FaqCategoryAdd()
        {
            return Json(new { success = false, message = "FAQ category editing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult FaqCategoryDelete()
        {
            return Json(new { success = false, message = "FAQ category editing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult FaqAdd()
        {
            return Json(new { success = false, message = "FAQ editing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult FaqUpdate()
        {
            return Json(new { success = false, message = "FAQ editing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult FaqDelete()
        {
            return Json(new { success = false, message = "FAQ editing is not configured in the merged admin module." });
        }

        private async Task<FinanceRequestsViewModel> BuildFinanceRequestsViewModelAsync(CancellationToken cancellationToken)
        {
            var model = new FinanceRequestsViewModel();

            var pendingProducts = await _db.Products.AsNoTracking()
                .Where(p => (p.Status ?? "").ToLower() == "pending")
                .OrderByDescending(p => p.ProductId)
                .Take(100)
                .ToListAsync(cancellationToken);

            var productIds = pendingProducts.Select(p => p.ProductId).ToList();
            var variantRows = await _db.ProductVariants.AsNoTracking()
                .Where(v => productIds.Contains(v.ProductId))
                .GroupBy(v => v.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Price = g.Select(v => v.Price).FirstOrDefault(p => p.HasValue) ?? 0m,
                    Image = g.Select(v => v.ImagePath).FirstOrDefault(p => p != null)
                })
                .ToListAsync(cancellationToken);

            var sellerIds = pendingProducts.Select(p => p.SellerId).Distinct().ToList();
            var sellers = await _db.Sellers.AsNoTracking()
                .Where(s => sellerIds.Contains(s.SellerId))
                .ToDictionaryAsync(s => s.SellerId, cancellationToken);
            var variants = variantRows.ToDictionary(v => v.ProductId);

            model.PendingProducts = pendingProducts.Select(product =>
            {
                sellers.TryGetValue(product.SellerId, out var seller);
                variants.TryGetValue(product.ProductId, out var variant);
                return new ProductApprovalRequest
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ShopName = seller?.BusinessName ?? "Seller",
                    SellerName = seller?.BusinessName ?? "Seller",
                    Category = product.Category ?? "",
                    Price = variant?.Price ?? 0m,
                    MainImage = variant?.Image ?? "",
                    SubmittedAt = DateTime.Now,
                    Status = product.Status ?? "Pending"
                };
            }).ToList();

            var pendingPromotions = await _db.Promotions.AsNoTracking()
                .Where(p => (p.Status ?? "").ToLower() == "pending")
                .OrderByDescending(p => p.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            model.PendingDiscounts = pendingPromotions.Select(promotion => new DiscountRequest
            {
                Id = promotion.Id,
                Name = promotion.Name,
                Type = promotion.Type,
                BannerSize = promotion.BannerSize ?? "",
                TotalDiscountPercent = promotion.TotalDiscountPercent ?? 0m,
                TotalDiscountFix = promotion.TotalDiscountFix ?? 0m,
                DiscountPercent = promotion.TotalDiscountPercent ?? 0m,
                Status = promotion.Status ?? "Pending",
                CreatedAt = promotion.CreatedAt ?? DateTime.Now,
                ShopName = "Seller promotion",
                ProductName = promotion.Name,
                OriginalPrice = 0m,
                DiscountedPrice = 0m,
                MinimumRequirementType = promotion.MinimumRequirementType,
                MinimumPurchaseAmount = promotion.MinimumPurchaseAmount,
                UsageLimit = promotion.UsageLimit,
                UntilPromotionLast = promotion.UntilPromotionLast,
                BuyQuantity = promotion.BuyQuantity,
                TakeQuantity = promotion.TakeQuantity,
                FreeItemRequirement = promotion.FreeItemRequirement,
                ReturnWindowDays = promotion.ReturnWindowDays ?? 0
            }).ToList();

            model.ActivePromotions = await _db.Promotions.AsNoTracking()
                .Where(p => (p.Status ?? "").ToLower() == "active" || (p.Status ?? "").ToLower() == "approved")
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .Select(p => new GlobalPromotion
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Type,
                    DiscountPercent = p.TotalDiscountPercent ?? 0m,
                    Status = p.Status ?? "Active",
                    CreatedAt = p.CreatedAt ?? DateTime.Now
                })
                .ToListAsync(cancellationToken);

            model.PendingPayouts = await LoadPendingPayoutsAsync(cancellationToken);
            model.TotalPendingPayoutsCount = model.PendingPayouts.Count;
            model.TotalPendingPayoutAmount = model.PendingPayouts.Sum(p => p.Amount);
            model.TotalPendingPayoutAmountManual = model.TotalPendingPayoutAmount;
            model.TotalPendingDiscountsCount = model.PendingDiscounts.Count;
            model.TotalPendingProductsCount = model.PendingProducts.Count;

            return model;
        }

        private async Task<List<PayoutRequest>> LoadPendingPayoutsAsync(CancellationToken cancellationToken)
        {
            var payouts = new List<PayoutRequest>();
            await using var connection = _db.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            try
            {
                if (shouldClose)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                var table = await FindExistingTableAsync(connection, new[] { "withdrawal_requests", "WithdrawalRequests", "SellerWithdrawals", "PayoutRequests" }, cancellationToken);
                if (table == null)
                {
                    return payouts;
                }

                var columns = await GetColumnsAsync(connection, table, cancellationToken);
                var idColumn = FindColumn(columns, "id", "Id", "WithdrawalId", "withdrawal_id");
                var sellerColumn = FindColumn(columns, "seller_id", "SellerId");
                var amountColumn = FindColumn(columns, "amount", "Amount");
                var statusColumn = FindColumn(columns, "status", "Status");
                var dateColumn = FindColumn(columns, "created_at", "CreatedAt", "requested_at", "RequestedAt");
                var bankColumn = FindColumn(columns, "bank_name", "BankName", "method", "Method");
                var noteColumn = FindColumn(columns, "seller_note", "SellerNote", "note", "Notes");
                if (idColumn == null || amountColumn == null)
                {
                    return payouts;
                }

                await using var command = connection.CreateCommand();
                command.CommandText =
                    $"SELECT TOP 100 {Quote(idColumn)} AS Id, " +
                    $"{(sellerColumn == null ? "0" : Quote(sellerColumn))} AS SellerId, " +
                    $"{Quote(amountColumn)} AS Amount, " +
                    $"{(statusColumn == null ? "'Pending'" : Quote(statusColumn))} AS Status, " +
                    $"{(dateColumn == null ? "GETDATE()" : Quote(dateColumn))} AS RequestedAt, " +
                    $"{(bankColumn == null ? "''" : Quote(bankColumn))} AS BankName, " +
                    $"{(noteColumn == null ? "''" : Quote(noteColumn))} AS SellerNote " +
                    $"FROM {Quote(table)} " +
                    $"{(statusColumn == null ? "" : $"WHERE UPPER(COALESCE({Quote(statusColumn)}, '')) = 'PENDING' ")}" +
                    $"ORDER BY {(dateColumn == null ? Quote(idColumn) : Quote(dateColumn))} DESC";

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var sellerId = GetInt(reader, "SellerId");
                    payouts.Add(new PayoutRequest
                    {
                        Id = GetLong(reader, "Id"),
                        WithdrawalId = GetLong(reader, "Id"),
                        SellerId = sellerId,
                        Amount = GetDecimal(reader, "Amount"),
                        Status = GetString(reader, "Status", "Pending"),
                        RequestedAt = GetDate(reader, "RequestedAt", DateTime.Now),
                        BankName = GetString(reader, "BankName"),
                        SellerNote = GetString(reader, "SellerNote"),
                        ShopName = "Seller",
                        SellerName = "Seller"
                    });
                }
            }
            catch
            {
                return payouts;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }

            if (payouts.Count > 0)
            {
                var sellerIds = payouts.Select(p => p.SellerId).Where(id => id > 0).Distinct().ToList();
                var sellers = await _db.Sellers.AsNoTracking()
                    .Where(s => sellerIds.Contains(s.SellerId))
                    .ToDictionaryAsync(s => s.SellerId, cancellationToken);

                foreach (var payout in payouts)
                {
                    if (sellers.TryGetValue(payout.SellerId, out var seller))
                    {
                        payout.ShopName = seller.BusinessName ?? "Seller";
                        payout.SellerName = seller.BusinessName ?? "Seller";
                        payout.SellerEmail = seller.BusinessEmail ?? "";
                    }
                }
            }

            return payouts;
        }

        private async Task<List<LogisticsPartner>> LoadLogisticsPartnersAsync(string? statusFilter, string? searchTerm, CancellationToken cancellationToken)
        {
            var partners = new List<LogisticsPartner>();
            await using var connection = _db.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;

            try
            {
                if (shouldClose)
                {
                    await connection.OpenAsync(cancellationToken);
                }

                var table = await FindExistingTableAsync(connection, new[] { "Logistics", "logistics" }, cancellationToken);
                if (table == null)
                {
                    return partners;
                }

                var columns = await GetColumnsAsync(connection, table, cancellationToken);
                var idColumn = FindColumn(columns, "logistics_id", "LogisticsId", "Id");
                var nameColumn = FindColumn(columns, "courier_name", "CourierName", "Name");
                if (idColumn == null || nameColumn == null)
                {
                    return partners;
                }

                var serviceColumn = FindColumn(columns, "service_type", "ServiceType", "Type");
                var statusColumn = FindColumn(columns, "status", "Status");
                var logoColumn = FindColumn(columns, "logo_base64", "LogoBase64", "LogoUrl", "logo_url");
                var contactColumn = FindColumn(columns, "contact_person", "ContactPerson");
                var emailColumn = FindColumn(columns, "contact_email", "ContactEmail");
                var phoneColumn = FindColumn(columns, "contact_phone", "ContactPhone");
                var trackingColumn = FindColumn(columns, "tracking_url_template", "TrackingUrlTemplate");
                var preferredColumn = FindColumn(columns, "is_preferred", "IsPreferred");
                var minColumn = FindColumn(columns, "min_delivery_days", "MinDeliveryDays");
                var maxColumn = FindColumn(columns, "max_delivery_days", "MaxDeliveryDays");
                var createdColumn = FindColumn(columns, "created_at", "CreatedAt");

                await using var command = connection.CreateCommand();
                var where = new List<string>();
                if (!string.IsNullOrWhiteSpace(statusFilter) && statusColumn != null)
                {
                    where.Add($"{Quote(statusColumn)} = @Status");
                    AddParameter(command, "@Status", statusFilter);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    where.Add($"{Quote(nameColumn)} LIKE @Search");
                    AddParameter(command, "@Search", $"%{searchTerm}%");
                }

                command.CommandText =
                    $"SELECT TOP 200 {Quote(idColumn)} AS LogisticsId, {Quote(nameColumn)} AS CourierName, " +
                    $"{SelectOrDefault(serviceColumn, "ServiceType", "''")}, {SelectOrDefault(statusColumn, "Status", "'Active'")}, " +
                    $"{SelectOrDefault(logoColumn, "LogoBase64", "''")}, {SelectOrDefault(contactColumn, "ContactPerson", "''")}, " +
                    $"{SelectOrDefault(emailColumn, "ContactEmail", "''")}, {SelectOrDefault(phoneColumn, "ContactPhone", "''")}, " +
                    $"{SelectOrDefault(trackingColumn, "TrackingUrlTemplate", "''")}, {SelectOrDefault(preferredColumn, "IsPreferred", "0")}, " +
                    $"{SelectOrDefault(minColumn, "MinDeliveryDays", "NULL")}, {SelectOrDefault(maxColumn, "MaxDeliveryDays", "NULL")}, " +
                    $"{SelectOrDefault(createdColumn, "CreatedAt", "GETDATE()")} " +
                    $"FROM {Quote(table)} {(where.Count == 0 ? "" : "WHERE " + string.Join(" AND ", where))} ORDER BY {Quote(nameColumn)}";

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    partners.Add(new LogisticsPartner
                    {
                        LogisticsId = GetInt(reader, "LogisticsId"),
                        CourierName = GetString(reader, "CourierName"),
                        ServiceType = GetString(reader, "ServiceType", "Standard Delivery"),
                        Status = GetString(reader, "Status", "Active"),
                        LogoBase64 = GetString(reader, "LogoBase64"),
                        ContactPerson = GetString(reader, "ContactPerson"),
                        ContactEmail = GetString(reader, "ContactEmail"),
                        ContactPhone = GetString(reader, "ContactPhone"),
                        TrackingUrlTemplate = GetString(reader, "TrackingUrlTemplate"),
                        IsPreferred = GetBool(reader, "IsPreferred"),
                        MinDeliveryDays = GetNullableInt(reader, "MinDeliveryDays"),
                        MaxDeliveryDays = GetNullableInt(reader, "MaxDeliveryDays"),
                        AvgDeliveryDays = 0m,
                        SuccessRate = 0m,
                        CreatedAt = GetDate(reader, "CreatedAt", DateTime.Now)
                    });
                }
            }
            catch
            {
                return partners;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }

            return partners;
        }

        private async Task<HelpCenterV2ViewModel> BuildHelpCenterViewModelAsync(CancellationToken cancellationToken)
        {
            var model = new HelpCenterV2ViewModel();
            var now = DateTime.Now;

            var sessions = await _db.LiveAgentSessions.AsNoTracking()
                .OrderByDescending(s => s.UpdatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
            var users = await _db.Users.AsNoTracking()
                .Where(u => sessions.Select(s => s.UserId).Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, cancellationToken);
            var supportFaqs = await _db.SupportFaqRecords.AsNoTracking()
                .Where(f => sessions.Select(s => s.SupportFaqId).Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

            model.Sessions = sessions.Select((session, index) =>
            {
                users.TryGetValue(session.UserId, out var user);
                supportFaqs.TryGetValue(session.SupportFaqId, out var faq);
                var displayName = user?.Email ?? $"User {session.UserId}";
                return new QueueSession
                {
                    Id = session.LiveAgentSessionId,
                    SessionNo = $"S-{session.LiveAgentSessionId:D5}",
                    CustomerName = displayName,
                    CustomerEmail = user?.Email ?? "",
                    Role = faq?.UserType ?? "Consumer",
                    Initials = BuildInitials(displayName),
                    AvatarColor = "#1a1a1a",
                    WaitSeconds = Math.Max(0, (int)(now - session.CreatedAt).TotalSeconds),
                    QueuePosition = index + 1,
                    Status = session.Status.ToString().ToLowerInvariant(),
                    Category = session.CategoryTitle,
                    AssignedTo = null
                };
            }).ToList();

            model.Agents = await _db.SupportAgents.AsNoTracking()
                .OrderBy(a => a.AgentName)
                .Select(a => new AgentStatusViewModel
                {
                    Name = a.AgentName ?? $"Agent {a.ChatId}",
                    Initials = BuildInitials(a.AgentName ?? $"Agent {a.ChatId}"),
                    Status = a.AgentStatus ?? "offline",
                    ActiveSessions = 0,
                    MaxSessions = 3
                })
                .ToListAsync(cancellationToken);

            model.Faqs = await _db.FaqRecords.AsNoTracking()
                .OrderBy(f => f.Category)
                .ThenBy(f => f.Question)
                .Select(f => new FaqItem
                {
                    Id = f.FaqId,
                    Question = f.Question,
                    Answer = f.Answer,
                    Category = f.Category,
                    UserType = f.UserType
                })
                .ToListAsync(cancellationToken);

            model.Categories = model.Faqs
                .Select(f => f.Category)
                .Concat(await _db.SupportFaqRecords.AsNoTracking().Select(f => f.Category).ToListAsync(cancellationToken))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            model.Stats.InQueue = model.Sessions.Count(s => s.Status == "waiting");
            model.Stats.ActiveSessions = model.Sessions.Count(s => s.Status == "active");
            model.Stats.ResolvedToday = sessions.Count(s => s.Status == MyAspNetApp.Models.HelpCenter.LiveAgentSessionStatus.Resolved && s.UpdatedAt.Date == now.Date);
            model.Stats.AgentsOnline = model.Agents.Count(a => string.Equals(a.Status, "online", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a.Status, "available", StringComparison.OrdinalIgnoreCase));

            return model;
        }

        private static string BuildInitials(string value)
        {
            var parts = value.Split(new[] { ' ', '.', '@', '_' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
        }

        private static string SelectOrDefault(string? column, string alias, string defaultSql)
            => column == null ? $"{defaultSql} AS {Quote(alias)}" : $"{Quote(column)} AS {Quote(alias)}";

        private static void AddParameter(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static async Task<string?> FindExistingTableAsync(DbConnection connection, IEnumerable<string> names, CancellationToken cancellationToken)
        {
            foreach (var name in names)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
                AddParameter(command, "@TableName", name);
                var value = await command.ExecuteScalarAsync(cancellationToken);
                if (Convert.ToInt32(value) > 0)
                {
                    return name;
                }
            }

            return null;
        }

        private static async Task<HashSet<string>> GetColumnsAsync(DbConnection connection, string tableName, CancellationToken cancellationToken)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
            AddParameter(command, "@TableName", tableName);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(reader.GetString(0));
            }

            return columns;
        }

        private static string? FindColumn(HashSet<string> columns, params string[] names)
            => names.FirstOrDefault(columns.Contains);

        private static string Quote(string identifier)
            => $"[{identifier.Replace("]", "]]")}]";

        private static string GetString(DbDataReader reader, string name, string fallback = "")
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? fallback : Convert.ToString(reader.GetValue(ordinal)) ?? fallback;
        }

        private static int GetInt(DbDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static long GetLong(DbDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0L : Convert.ToInt64(reader.GetValue(ordinal));
        }

        private static int? GetNullableInt(DbDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static decimal GetDecimal(DbDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static DateTime GetDate(DbDataReader reader, string name, DateTime fallback)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? fallback : Convert.ToDateTime(reader.GetValue(ordinal));
        }

        private static bool GetBool(DbDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal));
        }

        public IActionResult Settings()
        {
            ViewBag.CurrentAdminName = HttpContext.Session.GetString("FullName") ?? "Admin";
            ViewBag.CurrentAdminEmail = HttpContext.Session.GetString("Email") ?? string.Empty;
            return View();
        }

        [HttpGet]
        public IActionResult GetActiveAdmins()
        {
            return Json(Array.Empty<object>());
        }

        [HttpGet]
        public IActionResult GetRevokedAdmins()
        {
            return Json(Array.Empty<object>());
        }

        [HttpGet]
        public IActionResult GetAuditLogs(int page = 1, int pageSize = 50, string? search = null)
        {
            return Json(new { success = true, logs = Array.Empty<object>(), totalCount = 0 });
        }

        [HttpPost]
        public IActionResult AddAdmin()
        {
            return Json(new { success = false, message = "Admin user editing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult UpdateAdmin()
        {
            return Json(new { success = false, message = "Admin user editing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult RevokeAdmin()
        {
            return Json(new { success = false, message = "Admin revoking is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult ReinstateAdmin()
        {
            return Json(new { success = false, message = "Admin reinstating is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult UpdateProfile()
        {
            return Json(new { success = false, message = "Profile editing is not configured in the merged admin module." });
        }

        [HttpPost]
        public IActionResult ChangePassword()
        {
            return Json(new { success = false, message = "Password changes are not configured in the merged admin module." });
        }

        public async Task<IActionResult> ProductApprovals()
        {
            var products = await _db.Products
                .AsNoTracking()
                .OrderByDescending(p => p.ProductId)
                .ToListAsync();

            var productIds = products.Select(p => p.ProductId).ToList();
            var variantImages = await _db.ProductVariants
                .AsNoTracking()
                .Where(v => productIds.Contains(v.ProductId) && !string.IsNullOrEmpty(v.ImagePath))
                .GroupBy(v => v.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ImagePath = g.OrderBy(v => v.Id).Select(v => v.ImagePath).FirstOrDefault()
                })
                .ToListAsync();

            var imageMap = variantImages.ToDictionary(x => x.ProductId, x => x.ImagePath);
            foreach (var product in products)
            {
                if (string.IsNullOrWhiteSpace(product.ImagePath) &&
                    imageMap.TryGetValue(product.ProductId, out var imagePath) &&
                    !string.IsNullOrWhiteSpace(imagePath))
                {
                    product.ImagePath = imagePath;
                }
            }

            var model = new ProductApprovalDashboardViewModel
            {
                PendingProducts = products.Where(p => string.Equals(p.Status, "pending", StringComparison.OrdinalIgnoreCase)).ToList(),
                ApprovedProducts = products.Where(p => string.Equals(p.Status, "approved", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "active", StringComparison.OrdinalIgnoreCase)).ToList(),
                RejectedProducts = products.Where(p => string.Equals(p.Status, "rejected", StringComparison.OrdinalIgnoreCase)).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> PromotionApprovals()
        {
            var promotions = await _db.Promotions
                .AsNoTracking()
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            var model = new PromotionApprovalDashboardViewModel
            {
                PendingPromotions = promotions.Where(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase)).Select(ToViewModel).ToList(),
                ApprovedPromotions = promotions.Where(p => string.Equals(p.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "active", StringComparison.OrdinalIgnoreCase)).Select(ToViewModel).ToList(),
                RejectedPromotions = promotions.Where(p => string.Equals(p.Status, "Rejected", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "inactive", StringComparison.OrdinalIgnoreCase)).Select(ToViewModel).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                product.Status = "approved";
                await _db.SaveChangesAsync();
                InvalidateProductCaches();
                TempData["AdminSuccessMessage"] = "Product approved.";
            }

            return RedirectToAction(nameof(ProductApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                product.Status = "rejected";
                await _db.SaveChangesAsync();
                InvalidateProductCaches();
                TempData["AdminSuccessMessage"] = "Product rejected.";
            }

            return RedirectToAction(nameof(ProductApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePromotion(int id)
        {
            var promotion = await _db.Promotions.FindAsync(id);
            if (promotion != null)
            {
                promotion.Status = "active";
                promotion.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["AdminSuccessMessage"] = "Promotion approved.";
            }

            return RedirectToAction(nameof(PromotionApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPromotion(int id)
        {
            var promotion = await _db.Promotions.FindAsync(id);
            if (promotion != null)
            {
                promotion.Status = "inactive";
                promotion.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["AdminSuccessMessage"] = "Promotion rejected.";
            }

            return RedirectToAction(nameof(PromotionApprovals));
        }

        private static object ToChallengeJson(DbChallenge challenge)
        {
            var completionRate = (challenge.TotalParticipants ?? 0) <= 0
                ? 0m
                : Math.Round(((decimal)(challenge.TotalCompleted ?? 0) / (challenge.TotalParticipants ?? 1)) * 100m, 1);

            return new
            {
                challengeId = challenge.ChallengeId,
                title = challenge.Title,
                description = challenge.Description,
                rules = challenge.Rules,
                prizes = challenge.Prizes,
                goalKm = challenge.GoalKm,
                activityType = challenge.ActivityType,
                startDate = challenge.StartDate,
                endDate = challenge.EndDate,
                status = ResolveChallengeStatus(challenge),
                bannerBase64 = ToDataUrl(challenge.BannerImage, challenge.BannerImageContentType),
                bannerImageName = challenge.BannerImageName,
                bannerImageContentType = challenge.BannerImageContentType,
                totalParticipants = challenge.TotalParticipants ?? 0,
                totalCompleted = challenge.TotalCompleted ?? 0,
                completionRate,
                createdAt = challenge.CreatedAt,
                updatedAt = challenge.UpdatedAt
            };
        }

        private static string ResolveChallengeStatus(DbChallenge challenge)
        {
            if (string.Equals(challenge.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return "Cancelled";
            }

            return ResolveChallengeStatus(challenge.StartDate, challenge.EndDate);
        }

        private static string ResolveChallengeStatus(DateTime startDate, DateTime endDate)
        {
            var now = DateTime.Now;
            if (startDate > now)
            {
                return "Upcoming";
            }

            return endDate < now ? "Completed" : "Live";
        }

        private static string BuildAdminAthleteName(Consumer? consumer, User? user)
        {
            if (!string.IsNullOrWhiteSpace(consumer?.Username))
            {
                return consumer.Username.Trim();
            }

            var fullName = string.Join(" ", new[] { consumer?.FirstName, consumer?.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim()));

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            return string.IsNullOrWhiteSpace(user?.Email) ? "Challenge Athlete" : user.Email!;
        }

        private static string FormatAdminDuration(int seconds)
        {
            var duration = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return duration.TotalHours >= 1
                ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
                : $"{duration.Minutes}m";
        }

        private static string? ToDataUrl(byte[]? bytes, string? contentType)
        {
            return bytes is { Length: > 0 }
                ? $"data:{(string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType)};base64,{Convert.ToBase64String(bytes)}"
                : null;
        }

        private static byte[]? DecodeDataUrl(string? dataUrl)
        {
            if (string.IsNullOrWhiteSpace(dataUrl))
            {
                return null;
            }

            var commaIndex = dataUrl.IndexOf(',');
            var base64 = commaIndex >= 0 ? dataUrl[(commaIndex + 1)..] : dataUrl;
            try
            {
                return Convert.FromBase64String(base64);
            }
            catch
            {
                return null;
            }
        }

        private static bool SellerMatchesStatus(string? sellerStatus, string requestedStatus)
        {
            var current = sellerStatus?.Trim() ?? string.Empty;
            return requestedStatus.ToLowerInvariant() switch
            {
                "pending" => string.Equals(current, "Pending", StringComparison.OrdinalIgnoreCase),
                "active" => string.Equals(current, "Active", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(current, "Approved", StringComparison.OrdinalIgnoreCase),
                "suspended" or "archived" => string.Equals(current, "Suspended", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(current, "Archived", StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }

        private static PromotionViewModel ToViewModel(DbPromotion entity)
        {
            return new PromotionViewModel
            {
                Id = entity.Id,
                Name = entity.Name,
                Type = entity.Type,
                BannerSize = entity.BannerSize ?? "300x250",
                TotalDiscountPercent = entity.TotalDiscountPercent ?? 0m,
                TotalDiscountFix = entity.TotalDiscountFix ?? 0m,
                UsageLimit = entity.UsageLimit ?? 0,
                UntilPromotionLast = entity.UntilPromotionLast,
                BuyQuantity = entity.BuyQuantity ?? 0,
                TakeQuantity = entity.TakeQuantity ?? 0,
                FreeItemRequirement = entity.FreeItemRequirement ?? "ExactProduct",
                ReturnWindowDays = entity.ReturnWindowDays ?? 0,
                ReturnReasons = PromotionSerialization.Deserialize(entity.ReturnReasonsJson ?? "[]"),
                ReturnConditionRequirements = PromotionSerialization.Deserialize(entity.ReturnConditionRequirementsJson ?? "[]"),
                MinimumRequirementType = entity.MinimumRequirementType ?? "None",
                MinimumPurchaseAmount = entity.MinimumPurchaseAmount ?? 0m,
                Status = entity.Status,
                SelectedProductIds = PromotionSerialization.Deserialize(entity.SelectedProductIdsJson ?? "[]")
            };
        }

        private void InvalidateProductCaches()
        {
            _cache.Remove("products:all");
            _cache.Remove("products:men");
            _cache.Remove("products:women");
            _cache.Remove("seller:products:index");
        }
    }
}
