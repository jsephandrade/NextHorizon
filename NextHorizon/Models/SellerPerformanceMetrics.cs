namespace NextHorizon.Models;

public sealed class SellerPerformanceMetrics
{
    public decimal TodaySales { get; set; }
    public int TodayUnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal SalesGrowth { get; set; }
}
