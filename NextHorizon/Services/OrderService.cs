using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models;


namespace NextHorizon.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        public OrderService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<Logistics>> GetCouriersAsync()
        
{
    return await _context.Logistics.ToListAsync();
}
public async Task UpdateOrderAsync(Order order)
{
    _context.Orders.Update(order);
    await _context.SaveChangesAsync();
}
       public async Task<List<Order>> GetOrdersBySellerAsync(int sellerId, DateTime? startDate = null, DateTime? endDate = null)
{
    var query = _context.Orders
        .Include(o => o.OrderItems)
            .ThenInclude(item => item.Product)
        .Where(o => o.seller_id == sellerId);

    if (startDate.HasValue)
    {
        var normalizedStartDate = startDate.Value.Date;
        query = query.Where(o => o.OrderDate >= normalizedStartDate);
    }

    if (endDate.HasValue)
    {
        var exclusiveEndDate = endDate.Value.Date.AddDays(1);
        query = query.Where(o => o.OrderDate < exclusiveEndDate);
    }

    return await query
        .OrderByDescending(o => o.OrderDate)
        .ToListAsync();
}
   public async Task<AcceptOrderResult> AcceptOrderAsync(int orderId, int sellerId, int courierId)
{
    await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

    var order = await _context.Orders
        .Include(o => o.OrderItems)
        .FirstOrDefaultAsync(o => o.OrderID == orderId && o.seller_id == sellerId);

    if (order == null)
    {
        return new AcceptOrderResult { Success = false, Message = "Order not found or unauthorized." };
    }

    if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
    {
        return new AcceptOrderResult { Success = false, Message = $"Only pending orders can be accepted. Current status: {order.Status ?? "Unknown"}." };
    }

    if (order.OrderItems == null || order.OrderItems.Count == 0)
    {
        return new AcceptOrderResult { Success = false, Message = "This order has no line items to reserve from inventory." };
    }

    foreach (var item in order.OrderItems)
    {
        var variant = await ResolveVariantForOrderItemAsync(item);
        if (variant == null)
        {
            await transaction.RollbackAsync();
            return new AcceptOrderResult
            {
                Success = false,
                Message = $"Inventory record not found for product {item.ProductID} ({item.Color ?? "Default"} / {item.Size ?? "Default"})."
            };
        }

        var affectedRows = await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE dbo.ProductVariants
SET Quantity = Quantity - {item.Quantity},
    Availability = CASE WHEN Quantity - {item.Quantity} > 0 THEN N'In Stock' ELSE N'Out of Stock' END
WHERE VariantId = {variant.Id} AND Quantity >= {item.Quantity}");

        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();

            var latestQuantity = await _context.ProductVariants
                .Where(v => v.Id == variant.Id)
                .Select(v => (int?)v.Quantity)
                .FirstOrDefaultAsync();

            var availableQuantity = latestQuantity ?? 0;
            var variantLabel = string.Join(" / ", new[] { item.Color, item.Size }.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (string.IsNullOrWhiteSpace(variantLabel))
            {
                variantLabel = "selected variant";
            }

            return new AcceptOrderResult
            {
                Success = false,
                Message = $"Insufficient stock for {variantLabel}. Requested {item.Quantity}, available {availableQuantity}."
            };
        }
    }

    order.Status = "To Ship";
    order.FulfillmentStatus = "To Ship";
    order.logistics_id = courierId;
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();

    return new AcceptOrderResult
    {
        Success = true,
        Message = "Order accepted and inventory updated successfully."
    };
}

private async Task<DbProductVariant?> ResolveVariantForOrderItemAsync(OrderItem item)
{
    if (item.VariantId.HasValue)
    {
        var directVariant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == item.VariantId.Value);
        if (directVariant != null)
        {
            return directVariant;
        }
    }

    return await _context.ProductVariants.FirstOrDefaultAsync(v =>
        v.ProductId == item.ProductID
        && (item.Size == null || v.Size == item.Size)
        && (item.Color == null || v.Style == item.Color));
}
public async Task<bool> DeclineOrderAsync(int orderId, int sellerId, string reason)
{
    // 1. Find the order (with security check for the seller)
    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.OrderID == orderId && o.seller_id == sellerId);

    if (order == null) return false; 

    // 2. Change the status and save the exact reason
    order.Status = "Cancelled";
    order.CancellationReason = reason;

    // 3. Save to database
    await _context.SaveChangesAsync();
    return true;
}
public async Task<Order?> GetOrderByIdAsync(int orderId, int sellerId)
{
    var order = await _context.Orders
        .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(o => o.OrderID == orderId && o.seller_id == sellerId);

    if (order != null)
    {
        foreach (var item in order.OrderItems)
        {
            // We search the ProductVariants table for a match on Product, Size, and Color
            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.ProductId == item.ProductID && 
                                          v.Size == item.Size && 
                                          v.Style == item.Color); // Note: SQL calls it 'Style', OrderItems calls it 'Color'

            if (variant != null)
            {
                item.Sku = variant.SKU;
            }
        }
    }

    return order;
}
    }
}





