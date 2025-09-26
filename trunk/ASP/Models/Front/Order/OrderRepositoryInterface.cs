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
    }
}