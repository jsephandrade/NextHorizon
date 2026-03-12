using Microsoft.Data.SqlClient;
using NextHorizon.Models;

namespace NextHorizon.Services;

public sealed class SellerPerformanceService : ISellerPerformanceService
{
    private readonly string _connectionString;
    private readonly ILogger<SellerPerformanceService> _logger;

    public SellerPerformanceService(IConfiguration configuration, ILogger<SellerPerformanceService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _logger = logger;
    }

    public async Task<SellerPerformanceMetrics> GetSellerPerformanceAsync(
        int sellerId,
        DateTime today,
        CancellationToken cancellationToken = default)
    {
        if (sellerId <= 0 || string.IsNullOrWhiteSpace(_connectionString))
        {
            return new SellerPerformanceMetrics();
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.sp_GetSellerPerformanceMetrics", connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SellerId", sellerId);
            command.Parameters.AddWithValue("@Today", today.Date);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return new SellerPerformanceMetrics();
            }

            return new SellerPerformanceMetrics
            {
                TodaySales = reader["TodaySales"] is DBNull ? 0m : Convert.ToDecimal(reader["TodaySales"]),
                TodayUnitsSold = reader["TodayUnitsSold"] is DBNull ? 0 : Convert.ToInt32(reader["TodayUnitsSold"]),
                TotalRevenue = reader["TotalRevenue"] is DBNull ? 0m : Convert.ToDecimal(reader["TotalRevenue"]),
                SalesGrowth = reader["SalesGrowth"] is DBNull ? 0m : Convert.ToDecimal(reader["SalesGrowth"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch seller performance metrics for seller {SellerId}. Returning defaults.", sellerId);
            return new SellerPerformanceMetrics();
        }
    }

    public async Task<Dictionary<int, List<decimal>>> GetMonthlyRevenueByYearAsync(
        int sellerId,
        int[] years,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<int, List<decimal>>();

        if (sellerId <= 0 || string.IsNullOrWhiteSpace(_connectionString) || years is null || years.Length == 0)
        {
            return result;
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            foreach (var year in years)
            {
                var monthlyRevenue = new List<decimal>(new decimal[12]);
                result[year] = monthlyRevenue;

                const string query = @"
                    SELECT 
                        ISNULL(MONTH(OrderDate), 0) AS [Month],
                        ISNULL(SUM(CAST(TotalAmount AS DECIMAL(18,2))), 0) AS TotalAmount
                    FROM dbo.Orders
                    WHERE seller_id = @SellerId
                        AND YEAR(OrderDate) = @Year
                        AND Status IN ('Paid', 'Completed')
                    GROUP BY MONTH(OrderDate)
                    ORDER BY MONTH(OrderDate)
                ";

                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SellerId", sellerId);
                command.Parameters.AddWithValue("@Year", year);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var month = reader["Month"] is DBNull ? 0 : Convert.ToInt32(reader["Month"]);
                    var amount = reader["TotalAmount"] is DBNull ? 0m : Convert.ToDecimal(reader["TotalAmount"]);

                    if (month >= 1 && month <= 12)
                    {
                        monthlyRevenue[month - 1] = amount;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch monthly revenue for seller {SellerId}. Returning empty results.", sellerId);
        }

        return result;
    }
}
