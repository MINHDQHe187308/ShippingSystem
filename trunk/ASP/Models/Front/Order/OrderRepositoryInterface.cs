// File: ASP.Models.Front/OrderRepositoryInterface.cs (Cập nhật signature UpsertOrderAsync dùng OrderDTO mới)
using ASP.DTO.DensoDTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public interface OrderRepositoryInterface
    {
        Task<Order?> GetOrderByPCOrderIdAsync(string pcOrderId);
        Task UpsertOrderAsync(OrderDTO orderDto);  // Giữ nguyên tên, nhưng DTO đã thay đổi
        Task SaveChangesAsync();
        Task<List<Order>> GetOrdersByDate(DateTime date);
        Task<List<Order>> GetOrdersForWeek(DateTime weekStart);
        Task UpdateOrderStatusToDelay(Guid orderId, DateTime now, double delayTime);
        Task<Order?> GetOrderById(Guid orderId);
        Task<List<Order>> GetOrdersWithDelayByDate(DateTime date);
    }
}