using System.Collections.Generic;
using System.Linq;

namespace NextHorizon.Models.Admin_Models
{
    public class AnalyticsViewModel
    {
        public int TotalConsumers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageOrderValue { get; set; }
        public double ChallengeToSaleConversionRate { get; set; }
        public int UsersPurchased { get; set; }

        public List<AnalyticsChartData> PerformanceTrends { get; set; } = new();
        public List<SellerMetric> TopSellers { get; set; } = new();
        public List<ProductMetric> TopProducts { get; set; } = new();

        public List<ProductMetric> TopMovingProducts
        {
            get => TopProducts;
            set => TopProducts = value;
        }

        public List<HourlyEngagementMetric> PeakEngagementData { get; set; } = new();
    }

    public class AnalyticsChartData
    {
        public string DateLabel { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int ChallengeParticipants { get; set; }
    }

    public class ProductMetric
    {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Stock { get; set; }
        public int SalesCount { get; set; }
        public string SellerName { get; set; } = string.Empty;

        public int UnitsSold
        {
            get => SalesCount;
            set => SalesCount = value;
        }
    }

    public class SellerMetric
    {
        public int Rank { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public int OrdersFulfilled { get; set; }
        public decimal RevenueGenerated { get; set; }
        public double GrowthPercentage { get; set; }
    }

    public class HourlyEngagementMetric
    {
        public int Hour { get; set; }
        public int ActivitySyncCount { get; set; }
        public int PurchaseCount { get; set; }
    }

    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string UserName { get; set; } = string.Empty;
        public double StravaKM { get; set; }
        public string Pace { get; set; } = "0:00";
    }
}