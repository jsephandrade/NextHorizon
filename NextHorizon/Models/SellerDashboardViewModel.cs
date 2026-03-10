using System;
using System.Collections.Generic;

namespace NextHorizon.Models
{
    public class SellerDashboardViewModel
    {
        public string SellerName { get; set; } = string.Empty;
        public DateTime CurrentDate { get; set; }

        // Critical Actions
        public int OrdersToShip { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockAlerts { get; set; }
        public decimal WithdrawAmount { get; set; }
        public string WithdrawStatus { get; set; } = string.Empty;

        // Performance Metrics
        public decimal TodaySales { get; set; }
        public decimal SalesGrowth { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalVisits { get; set; }

        // Recent Orders
        public List<Order> RecentOrders { get; set; } = new();

        // Order Management Table
        public List<Order> Orders { get; set; } = new();

        // Top Selling Products
        public List<TopSellingProduct> TopProducts { get; set; } = new();

         // Analytics: Year -> monthly revenue totals (Jan..Dec)
        public Dictionary<int, List<decimal>> MonthlyRevenueByYear { get; set; } = new();
    }
}
