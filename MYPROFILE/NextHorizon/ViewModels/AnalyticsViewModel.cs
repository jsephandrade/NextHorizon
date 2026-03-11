using System.Collections.Generic;
using System.Linq;

namespace NextHorizon.ViewModels
{
    public class AnalyticsViewModel
    {
        // --- Growth Stats ---
        public int TotalConsumers { get; set; }
        public int TotalSellers { get; set; }
        public double AverageOrderValue { get; set; } // Added missing definition
        public double AvgKmPerPurchase { get; set; }
        public double ChallengeToSaleConversionRate { get; set; }
        public int TotalSocialInteractions { get; set; }

        // --- Funnel Stats ---
        public int UsersStartedChallenge { get; set; }
        public int UsersCompletedGoal { get; set; }
        public int UsersClaimedReward { get; set; }
        public int UsersPurchased { get; set; }

        // --- Lists (Renamed to match Controller metrics) ---
        public List<SegmentMetric> SegmentScores { get; set; } = new();
        public List<AnalyticsChartData> PerformanceTrends { get; set; } = new();
        public List<RewardMetric> TopChallengeRewards { get; set; } = new();
        public List<SellerMetric> TopSellers { get; set; } = new();
    }

    // --- Supporting Metric Classes ---

    public class AnalyticsChartData
    {
        public string DateLabel { get; set; } = string.Empty;
        public double TotalKm { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RewardMetric
    {
        public string ProductName { get; set; } = string.Empty;
        public string LinkedChallengeName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public string? ProductImageUrl { get; set; }
    }

    public class SegmentMetric
    {
        public string CategoryName { get; set; } = string.Empty;
        public double ProgressPercentage { get; set; }
    }

    public class SellerMetric
    {
        public string SellerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty; // Fixed: Added ShopName
        public int OrdersFulfilled { get; set; }
        public decimal RevenueGenerated { get; set; }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; } // Added Rank for Dashboard usage
        public string UserName { get; set; } = string.Empty;
        public double StravaKM { get; set; }
        public string Pace { get; set; } = "0:00"; // Added Pace
    }
}