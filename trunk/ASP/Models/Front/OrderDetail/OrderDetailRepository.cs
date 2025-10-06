using ASP.Models.ASPModel;
using Microsoft.EntityFrameworkCore;

namespace ASP.Models.Front
{
    public class OrderDetailRepository : OrderDetailRepositoryInterface
    {
        private readonly ASPDbContext _context;

        public OrderDetailRepository(ASPDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDetail>> GetOrderDetailsByOrderId(Guid orderId)
        {
            return await _context.OrderDetails
                .Where(od => od.OId == orderId)
                .ToListAsync();
        }
    }
}
