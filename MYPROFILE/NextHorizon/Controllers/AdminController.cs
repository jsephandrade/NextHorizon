using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models.Admin_Models;
using NextHorizon.Services.AdminServices;
using NextHorizon.ViewModels;
using System.Collections.Generic;
using System.Linq;
namespace NextHorizon.Controllers
{
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService = new DashboardService();

        // URL: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");    

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }

            var model = _dashboardService.GetHeroStats();
            return View(model);
        }

        public IActionResult Analytics()
        {
            var userId = HttpContext.Session.GetInt32("UserId");    

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }


            var viewModel = new AnalyticsViewModel
            {
                // Growth Metrics (Moved from Dashboard)
                TotalConsumers = 12450,
                TotalSellers = 84,

                // Summary Metrics
                ChallengeToSaleConversionRate = 32.8,
                AvgKmPerPurchase = 12.4,
                TotalSocialInteractions = 4200000,

                // Ensure this is double or decimal based on your ViewModel definition
                AverageOrderValue = 4850.00,

                // Funnel Data
                UsersStartedChallenge = 5000,
                UsersCompletedGoal = 3200,
                UsersClaimedReward = 2100,
                UsersPurchased = 1640,

                // Performance Trends (Chart 1 - Line Chart)
                PerformanceTrends = new List<AnalyticsChartData>
        {
            new AnalyticsChartData { DateLabel = "Mar 04", TotalKm = 1500, TotalRevenue = 160000m },
            new AnalyticsChartData { DateLabel = "Mar 05", TotalKm = 1800, TotalRevenue = 210000m },
            new AnalyticsChartData { DateLabel = "Mar 06", TotalKm = 1650, TotalRevenue = 195000m },
            new AnalyticsChartData { DateLabel = "Mar 07", TotalKm = 2100, TotalRevenue = 240000m }
        },

                // Top Performing Sellers (Table)
                TopSellers = new List<SellerMetric>
        {
            new SellerMetric { SellerName = "Elite Strides Ph", ShopName = "Elite Strides Ph", OrdersFulfilled = 452, RevenueGenerated = 1254000.50m },
            new SellerMetric { SellerName = "Mountain Peak Gear", ShopName = "Mountain Peak Gear", OrdersFulfilled = 310, RevenueGenerated = 890600.00m },
            new SellerMetric { SellerName = "Urban Runner Co.", ShopName = "Urban Runner Co.", OrdersFulfilled = 285, RevenueGenerated = 412000.75m },
            new SellerMetric { SellerName = "Velocity Sports", ShopName = "Velocity Sports", OrdersFulfilled = 198, RevenueGenerated = 356000.00m },
            new SellerMetric { SellerName = "HydroFlow Official", ShopName = "HydroFlow Official", OrdersFulfilled = 156, RevenueGenerated = 98000.25m }
        },

                // Reward Metrics (Right Sidebar)
                TopChallengeRewards = new List<RewardMetric>
        {
            new RewardMetric { ProductName = "Ultra Boost Z", LinkedChallengeName = "Ironman Monthly", UnitsSold = 842 },
            new RewardMetric { ProductName = "Pro GPS Watch", LinkedChallengeName = "Summit Climbers", UnitsSold = 520 },
            new RewardMetric { ProductName = "Hydration Vest", LinkedChallengeName = "Trail Blazers", UnitsSold = 315 }
        },

                // Segment Engagement (Progress Bars)
                SegmentScores = new List<SegmentMetric>
        {
            new SegmentMetric { CategoryName = "Elite Runners", ProgressPercentage = 94 },
            new SegmentMetric { CategoryName = "Urban Commuters", ProgressPercentage = 78 },
            new SegmentMetric { CategoryName = "Trail Explorers", ProgressPercentage = 56 },
            new SegmentMetric { CategoryName = "Casual Walkers", ProgressPercentage = 42 }
        }
            };

            return View(viewModel);
        }

        public IActionResult Users()
        {   
            var userId = HttpContext.Session.GetInt32("UserId");    

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }

            return View();
        }

        public IActionResult Sellers()
        {
            var userId = HttpContext.Session.GetInt32("UserId");    

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }

            return View();
        }

        public IActionResult Logistics()
        {
            var userId = HttpContext.Session.GetInt32("UserId");    

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }

            return View();
        }
        public IActionResult Tasks()
        {
            return View();
        }

        public IActionResult ChallengeDetails()
        {
            return View();
        }

        public IActionResult Moderation()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }


        public IActionResult Logout()
        {

            //modify later to log out the user and redirect to login page
            return View();
        }
    }
}