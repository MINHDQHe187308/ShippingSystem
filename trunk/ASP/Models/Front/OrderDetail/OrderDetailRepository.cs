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

        // Thêm vào OrderDetailRepository.cs
        public async Task<List<OrderDetail>> GetOrderDetailsByOrderId(Guid orderId)
        {
            return await _context.OrderDetails
                .Where(od => od.OId == orderId)
                .Include(od => od.ShoppingLists) 
                    .ThenInclude(sl => sl.ThreePointChecks) 
                .ToListAsync();
        }
    }
}
