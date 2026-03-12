using NextHorizon.Models;

namespace NextHorizon.Services;

public interface ISellerPerformanceService
{
    Task<SellerPerformanceMetrics> GetSellerPerformanceAsync(int sellerId, DateTime today, CancellationToken cancellationToken = default);
    Task<Dictionary<int, List<decimal>>> GetMonthlyRevenueByYearAsync(int sellerId, int[] years, CancellationToken cancellationToken = default);
}
