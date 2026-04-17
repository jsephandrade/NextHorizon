using System;
using System.Collections.Generic;

namespace NextHorizon.Models
{
    public class SellerDashboardViewModel
    {
        public string SellerName { get; set; } = string.Empty;
        public DateTime CurrentDate { get; set; }

        // Critical Actions
        public int ReturnRequests { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockAlerts { get; set; }
        public decimal WithdrawAmount { get; set; }
        public string WithdrawStatus { get; set; } = string.Empty;

        // Performance Metrics
        public decimal TodayOrderValue { get; set; }
        public decimal YesterdayOrderValue { get; set; }
        public decimal TodaySales { get; set; }
        public int TodayOrderCount { get; set; }
        public int TodayUnitsSold { get; set; }
        public int ShippedTodayCount { get; set; }
        public int InFulfillmentCount { get; set; }
        public int OpenOrderCount { get; set; }
        public int TotalUnitsSold { get; set; }
        public int TotalOrders { get; set; }
        public decimal SalesGrowth { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RecognizedRevenueToday { get; set; }
        public decimal YesterdayRecognizedRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal RefundedRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal PipelineRevenue { get; set; }
        public int CancelledOrders { get; set; }
        public int RefundedOrders { get; set; }
        public int ActiveReturnRequests { get; set; }
        public int TotalVisits { get; set; }

        // Recent Orders
        public List<Order> RecentOrders { get; set; } = new();

        // Order Management Table
        public List<Order> Orders { get; set; } = new();

        // Top Selling Products
        public List<TopSellingProduct> TopProducts { get; set; } = new();

        // Analytics: range-keyed top products (keys: "1H", "1D", "7D", "1M")
        public Dictionary<string, List<TopSellingProduct>> TopProductsByRange { get; set; } = new();

        // Analytics: Year -> monthly revenue totals (Jan..Dec)
        public Dictionary<int, List<decimal>> MonthlyRevenueByYear { get; set; } = new();
        public Dictionary<int, List<int>> MonthlyOrdersByYear { get; set; } = new();
        public Dictionary<int, List<int>> MonthlyUnitsByYear { get; set; } = new();
        public List<CategoryPerformanceViewModel> TopCategories { get; set; } = new();
    }
}
