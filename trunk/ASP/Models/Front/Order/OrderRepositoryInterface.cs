using ASP.DTO.DensoDTO;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace ASP.Models.Front
{
    public interface OrderRepositoryInterface
    {
        public Task<Order?> GetOrderByPCOrderIdAsync(string pcOrderId);
        public Task UpsertOrderAsync(OrderDTO orderDto);
        public Task SaveChangesAsync();
        Task<List<Order>> GetOrdersByDate(DateTime date);
        Task<List<Order>> GetOrdersForWeek(DateTime weekStart);
        Task UpdateOrderStatusToDelay(Guid orderId, DateTime now, double delayTime);
        Task<Order?> GetOrderById(Guid orderId);

        Task<List<Order>> GetOrdersWithDelayByDate(DateTime date);
    }
}