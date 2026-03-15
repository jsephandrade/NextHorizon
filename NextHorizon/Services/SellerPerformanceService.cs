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

    public async Task<List<TopSellingProduct>> GetTopPerformingProductsAsync(
        int sellerId,
        int topCount = 5,
        DateTime? from = null,
        CancellationToken cancellationToken = default)
    {
        var result = new List<TopSellingProduct>();
        if (sellerId <= 0 || string.IsNullOrWhiteSpace(_connectionString))
        {
            return result;
        }

        if (topCount <= 0)
        {
            topCount = 5;
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            result = await GetTopProductsFromOrderItemsAsync(connection, sellerId, topCount, from, cancellationToken);
            if (result.Count == 0)
            {
                result = await GetTopProductsFromOrdersAsync(connection, sellerId, topCount, from, cancellationToken);
            }

            for (var i = 0; i < result.Count; i++)
            {
                result[i].Rank = i + 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch top products for seller {SellerId}. Returning empty results.", sellerId);
            return new List<TopSellingProduct>();
        }

        return result;
    }

    private static async Task<List<TopSellingProduct>> GetTopProductsFromOrderItemsAsync(
        SqlConnection connection,
        int sellerId,
        int topCount,
        DateTime? from,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT TOP (@TopCount)
                COALESCE(NULLIF(LTRIM(RTRIM(p.ProductName)), ''), CONCAT('Product #', oi.ProductID)) AS ProductName,
                CONCAT('PID-', oi.ProductID) AS Sku,
                COALESCE(NULLIF(LTRIM(RTRIM(p.Category)), ''), 'General') AS Category,
                'https://via.placeholder.com/80?text=Product' AS ImageUrl,
                SUM(ISNULL(oi.Quantity, 1)) AS UnitsSold,
                SUM(ISNULL(oi.Quantity, 1) * ISNULL(oi.UnitPrice, 0)) AS RevenueGenerated
            FROM dbo.Orders o
            INNER JOIN dbo.OrderItems oi ON oi.OrderID = o.OrderID
            LEFT JOIN dbo.Products p ON p.ProductId = oi.ProductID
            WHERE o.seller_id = @SellerId
                AND ISNULL(o.Status, '') <> 'Cancelled'
                AND (@From IS NULL OR o.OrderDate >= @From)
            GROUP BY oi.ProductID, p.ProductName, p.Category
            ORDER BY UnitsSold DESC, RevenueGenerated DESC, ProductName ASC;
        ";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@SellerId", sellerId);
        command.Parameters.AddWithValue("@TopCount", topCount);
        command.Parameters.Add("@From", System.Data.SqlDbType.DateTime).Value = (object?)from ?? DBNull.Value;

        var products = new List<TopSellingProduct>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new TopSellingProduct
            {
                ProductName = Convert.ToString(reader["ProductName"]) ?? "(Unnamed Product)",
                Sku = Convert.ToString(reader["Sku"]) ?? "-",
                Category = Convert.ToString(reader["Category"]) ?? "General",
                ImageUrl = NormalizeImageUrl(Convert.ToString(reader["ImageUrl"])),
                UnitsSold = reader["UnitsSold"] is DBNull ? 0 : Convert.ToInt32(reader["UnitsSold"]),
                RevenueGenerated = reader["RevenueGenerated"] is DBNull ? 0m : Convert.ToDecimal(reader["RevenueGenerated"])
            });
        }

        return products;
    }

    private static async Task<List<TopSellingProduct>> GetTopProductsFromOrdersAsync(
        SqlConnection connection,
        int sellerId,
        int topCount,
        DateTime? from,
        CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT TOP (@TopCount)
                o.ProductName AS ProductName,
                '-' AS Sku,
                'General' AS Category,
                'https://via.placeholder.com/80?text=Product' AS ImageUrl,
                SUM(ISNULL(o.Quantity, 1)) AS UnitsSold,
                SUM(ISNULL(o.TotalAmount, 0)) AS RevenueGenerated
            FROM dbo.Orders o
            WHERE o.seller_id = @SellerId
                AND ISNULL(o.Status, '') <> 'Cancelled'
                AND o.ProductName IS NOT NULL
                AND LTRIM(RTRIM(o.ProductName)) <> ''
                AND (@From IS NULL OR o.OrderDate >= @From)
            GROUP BY o.ProductName
            ORDER BY UnitsSold DESC, RevenueGenerated DESC, ProductName ASC;
        ";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@SellerId", sellerId);
        command.Parameters.AddWithValue("@TopCount", topCount);
        command.Parameters.Add("@From", System.Data.SqlDbType.DateTime).Value = (object?)from ?? DBNull.Value;

        var products = new List<TopSellingProduct>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new TopSellingProduct
            {
                ProductName = Convert.ToString(reader["ProductName"]) ?? "(Unnamed Product)",
                Sku = Convert.ToString(reader["Sku"]) ?? "-",
                Category = Convert.ToString(reader["Category"]) ?? "General",
                ImageUrl = NormalizeImageUrl(Convert.ToString(reader["ImageUrl"])),
                UnitsSold = reader["UnitsSold"] is DBNull ? 0 : Convert.ToInt32(reader["UnitsSold"]),
                RevenueGenerated = reader["RevenueGenerated"] is DBNull ? 0m : Convert.ToDecimal(reader["RevenueGenerated"])
            });
        }

        return products;
    }

    private static string NormalizeImageUrl(string? raw)
    {
        const string fallback = "https://via.placeholder.com/80?text=Product";
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        var normalized = raw.Trim().Replace('\\', '/');
        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        if (normalized.StartsWith("~/", StringComparison.Ordinal))
        {
            return normalized[1..];
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            return normalized;
        }

        return "/" + normalized;
    }
}
