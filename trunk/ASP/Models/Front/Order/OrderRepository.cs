using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class OrderRepository : OrderRepositoryInterface
    {
        private readonly ASPDbContext _context;

        public OrderRepository(ASPDbContext context)
        {
            _context = context;
        }

        public async Task<Order?> GetOrderByPCOrderIdAsync(string pcOrderId)
        {
            if (string.IsNullOrEmpty(pcOrderId))
                throw new ArgumentException("PCOrderId cannot be null or empty");

            return await _context.Orders
                .FirstOrDefaultAsync(o => o.PCOrderId == pcOrderId);
        }


        public async Task UpsertOrderAsync(OrderDTO orderDto)
        {
            if (orderDto == null || orderDto.OrderId == Guid.Empty)
                throw new ArgumentException("OrderDTO or OrderId cannot be null");

            // Kiểm tra xem thực thể đã được theo dõi chưa
            var trackedOrder = _context.ChangeTracker.Entries<Order>()
                .FirstOrDefault(e => e.Entity.UId == orderDto.OrderId);

            if (trackedOrder != null)
            {
                // Cập nhật thực thể đã được theo dõi
                trackedOrder.Entity.ShipDate = orderDto.ShippingDate;
                trackedOrder.Entity.CustomerCode = orderDto.CustomerCode;
                trackedOrder.Entity.TransCd = orderDto.TransCode;
                trackedOrder.Entity.TransMethod = 0;
                trackedOrder.Entity.ContSize = (short)orderDto.PalletSize;
                trackedOrder.Entity.TotalColumn = orderDto.Quantity;
                trackedOrder.Entity.PartList = orderDto.PartNo;
                trackedOrder.Entity.TotalPallet = orderDto.TotalPallet;    
                trackedOrder.Entity.OrderCreateDate = orderDto.CreateDate;
                trackedOrder.Entity.AcAsyTime = null;
                trackedOrder.Entity.AcDocumentsTime = null;
                trackedOrder.Entity.AcDeliveryTime = null;
                return;
            }

            // Truy vấn cơ sở dữ liệu với AsNoTracking
            var existing = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UId == orderDto.OrderId);

            if (existing != null)
            {
                // Gắn và cập nhật thực thể hiện có
                var orderToUpdate = new Order
                {
                    UId = orderDto.OrderId,
                    ShipDate = orderDto.ShippingDate,
                    CustomerCode = orderDto.CustomerCode,
                    TransCd = orderDto.TransCode,
                    TransMethod = 0,
                    ContSize = (short)orderDto.PalletSize,
                    TotalColumn = orderDto.Quantity,
                    PartList = orderDto.PartNo,
                    TotalPallet = orderDto.TotalPallet,                
                    OrderCreateDate = orderDto.CreateDate,
                    AcAsyTime = null,
                    AcDocumentsTime = null,
                    AcDeliveryTime = null
                };
                _context.Orders.Update(orderToUpdate);
            }
            else
            {
                // Thêm mới đơn hàng
                var newOrder = new Order
                {
                    UId = orderDto.OrderId,
                    PCOrderId = orderDto.OrderId.ToString(),
                    ShipDate = orderDto.ShippingDate,
                    CustomerCode = orderDto.CustomerCode,
                    TransCd = orderDto.TransCode,
                    TransMethod = 0,
                    ContSize = (short)orderDto.PalletSize,
                    TotalColumn = orderDto.Quantity,
                    PartList = orderDto.PartNo,
                    TotalPallet = orderDto.TotalPallet,               
                    OrderCreateDate = orderDto.CreateDate,
                    AcAsyTime = null,
                    AcDocumentsTime = null,
                    AcDeliveryTime = null
                };
                await _context.Orders.AddAsync(newOrder);
            }
        }
        public async Task<List<Order>> GetOrdersByDate(DateTime date)
        {
            return await _context.Orders
                .Where(o => o.ShipDate.Date == date.Date) // Filter theo ngày
                .Include(o => o.OrderDetails) // Include nếu cần details
                .ToListAsync();
        }

        // Nếu muốn filter theo PlanAsyTime thay vì ShipDate, thay bằng:
        // .Where(o => o.PlanAsyTime.Date == date.Date)
    

public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync(); 
        }
    }
}