using ASP.DTO.DensoDTO;  
using ASP.Models.ASPModel;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class OrderDetailRepository : OrderDetailRepositoryInterface
    {
        private readonly ASPDbContext _context;

        public OrderDetailRepository(ASPDbContext context)
        {
            _context = context;
        }

        // GIỮ NGUYÊN: GetOrderDetailsByOrderId
        public async Task<List<OrderDetail>> GetOrderDetailsByOrderId(Guid orderId)
        {
            return await _context.OrderDetails
                .Where(od => od.OId == orderId)
                .Include(od => od.ShoppingLists)
                    .ThenInclude(sl => sl.ThreePointCheck)
                .ToListAsync();
        }

    }
}