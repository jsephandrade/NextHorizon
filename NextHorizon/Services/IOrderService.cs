using NextHorizon.Models;

namespace NextHorizon.Services
{
    public interface IOrderService
    {
       Task UpdateOrderAsync(Order order);
        Task<List<Order>> GetOrdersBySellerAsync(int sellerId);
Task<bool> AcceptOrderAsync(int orderId, int sellerId, int logisticsId);
        Task<List<Logistics>> GetCouriersAsync();
        Task<bool> DeclineOrderAsync(int orderId, int sellerId, string reason);
        Task<Order> GetOrderByIdAsync(int orderId, int sellerId);
    }
}