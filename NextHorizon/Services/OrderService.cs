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
       public async Task<List<Order>> GetOrdersBySellerAsync(int sellerId)
{
    return await _context.Orders
       .Include(o => o.OrderItems)
      .Where(o => o.seller_id == sellerId) 
        .OrderByDescending(o => o.OrderDate)
        .ToListAsync();
}
   public async Task<bool> AcceptOrderAsync(int orderId, int sellerId, int courierId)
{
    var order = await _context.Orders
        .FirstOrDefaultAsync(o => o.OrderID == orderId && o.seller_id == sellerId);

    if (order == null) return false; 
    
    order.Status = "To Ship";
    order.logistics_id = courierId; 
    await _context.SaveChangesAsync();

    return true;
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
public async Task<Order> GetOrderByIdAsync(int orderId, int sellerId)
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
            var variant = await _context.Set<ProductVariant>()
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