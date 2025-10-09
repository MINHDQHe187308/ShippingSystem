using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;  // THÊM: Import SignalR
using ASP.Hubs;  // THÊM: Import Hub
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class OrderRepository : OrderRepositoryInterface
    {
        private readonly ASPDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;  //  SignalR HubContext
        private readonly ILogger<OrderRepository> _logger;  //  Logger

        public OrderRepository(ASPDbContext context, IHubContext<OrderHub> hubContext, ILogger<OrderRepository> logger)  // Params cho inject
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
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

            Order orderToUpdate;
            bool isNewOrder = false;

            if (trackedOrder != null)
            {
                // Cập nhật thực thể đã được theo dõi
                orderToUpdate = trackedOrder.Entity;
            }
            else
            {
                // Truy vấn cơ sở dữ liệu với AsNoTracking
                var existing = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.UId == orderDto.OrderId);

                if (existing != null)
                {
                    // Gắn và cập nhật thực thể hiện có
                    orderToUpdate = new Order
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
                    orderToUpdate = new Order
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
                    await _context.Orders.AddAsync(orderToUpdate);
                    isNewOrder = true;
                }
            }

            // THÊM MỚI: Tính toán Plan times dựa trên LeadtimeMaster (theo thứ tự nối tiếp)
            var leadtimeMaster = await _context.LeadtimeMasters
                .FirstOrDefaultAsync(l => l.CustomerCode == orderDto.CustomerCode && l.TransCd == orderDto.TransCode);

            if (leadtimeMaster != null)
            {
                // Giả sử thời gian trong LeadtimeMaster là đơn vị phút (có thể điều chỉnh nếu là giờ hoặc giây)
                double collectTimeTotal = leadtimeMaster.CollectTimePerPallet * orderDto.TotalPallet;
                double prepareTimeTotal = leadtimeMaster.PrepareTimePerPallet * orderDto.TotalPallet;
                double loadingTimeTotal = leadtimeMaster.LoadingTimePerColumn * orderDto.Quantity;

                // Tính nối tiếp quy trình : 
                // 1. PlanDocumentsTime: Bắt đầu từ CreateDate + thời gian thu thập chứng từ (scan pallet)
                orderToUpdate.PlanDocumentsTime = orderDto.CreateDate.AddMinutes(collectTimeTotal);

                // 2. PlanAsyTime: Sau PlanDocumentsTime + thời gian chuẩn bị (lái xe phót lít tìm và lất pallet ra khu tập kết và quét ba điểm)
                orderToUpdate.PlanAsyTime = orderToUpdate.PlanDocumentsTime.AddMinutes(prepareTimeTotal);

                // 3. PlanDeliveryTime: Sau PlanAsyTime + thời gian loading lên container và xuất hàng
                orderToUpdate.PlanDeliveryTime = orderToUpdate.PlanAsyTime.AddMinutes(loadingTimeTotal);

                _logger.LogInformation("Calculated sequential plan times for order {OrderId}: PlanDocumentsTime={PlanDocumentsTime}, PlanAsyTime={PlanAsyTime}, PlanDeliveryTime={PlanDeliveryTime}",
                    orderDto.OrderId, orderToUpdate.PlanDocumentsTime, orderToUpdate.PlanAsyTime, orderToUpdate.PlanDeliveryTime);
            }
            else
            {
                _logger.LogWarning("No LeadtimeMaster found for CustomerCode={CustomerCode} and TransCd={TransCd}, skipping plan time calculation for order {OrderId}",
                    orderDto.CustomerCode, orderDto.TransCode, orderDto.OrderId);
            }

            // Nếu là order mới, cần set thêm các fields cơ bản nếu chưa có
            if (isNewOrder)
            {
                orderToUpdate.OrderStatus = 0; // Default status
            }
        }


        public async Task<List<Order>> GetOrdersByDate(DateTime date)
        {
            return await _context.Orders
                .Where(o =>
                    (o.PlanAsyTime >= date.Date && o.PlanAsyTime < date.Date.AddDays(1)) ||
                    (o.PlanDocumentsTime >= date.Date && o.PlanDocumentsTime < date.Date.AddDays(1)) ||
                    (o.PlanDeliveryTime >= date.Date && o.PlanDeliveryTime < date.Date.AddDays(1))
                )
                .Include(o => o.OrderDetails)
                .ToListAsync();
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


        public async Task<bool> UpdateOrderStatusIfNeeded(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointChecks)  //Include ThreePointChecks để tính AcAsyTime
                .FirstOrDefaultAsync(o => o.UId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for status update", orderId);
                return false;
            }

            int oldStatus = order.OrderStatus;

            // Tính collected pallets từ ShoppingLists (giữ nguyên)
            var allShoppingLists = order.OrderDetails.SelectMany(od => od.ShoppingLists ?? new List<ShoppingList>()).ToList();
            var collectedPallets = allShoppingLists
                .Where(sl => sl.CollectionStatus == 1 || sl.CollectedDate.HasValue)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();

            var totalOrderPallet = order.TotalPallet;
            bool isCompleted = totalOrderPallet > 0 && collectedPallets >= totalOrderPallet;
            bool isShipped = isCompleted && order.OrderDetails.Any(od => od.BookContStatus == 1);

            // Cập nhật status (giữ nguyên)
            if (isShipped)
                order.OrderStatus = 3;  // Shipped
            else if (isCompleted)
                order.OrderStatus = 2;  // Completed
            else if (collectedPallets > 0 && collectedPallets < totalOrderPallet)
                order.OrderStatus = 1;  // Pending
                                        // Giữ 0 nếu chưa collect

            // Tính Actual Times dựa trên dữ liệu thực tế (nối tiếp theo quy trình)
            bool actualTimesChanged = false;

            // 1. AcDocumentsTime: Max CollectedDate từ tất cả ShoppingLists (hoàn thành scan pallet)
            var maxCollectedDate = allShoppingLists
                .Where(sl => sl.CollectedDate.HasValue)
                .Max(sl => sl.CollectedDate.Value);
            if (maxCollectedDate.HasValue && (!order.AcDocumentsTime.HasValue || order.AcDocumentsTime.Value != maxCollectedDate.Value))
            {
                order.AcDocumentsTime = maxCollectedDate.Value;
                actualTimesChanged = true;
                _logger.LogInformation("Updated AcDocumentsTime for order {OrderId} to {AcDocumentsTime}", orderId, order.AcDocumentsTime);
            }

            // 2. AcAsyTime: Max IssuedDate từ tất cả ThreePointChecks (sau quét ba điểm), chỉ nếu AcDocumentsTime đã có
            if (order.AcDocumentsTime.HasValue)
            {
                var allThreePointChecks = allShoppingLists.SelectMany(sl => sl.ThreePointChecks ?? new List<ThreePointCheck>()).ToList();
                var maxIssuedDate = allThreePointChecks
                    .Where(tpc => tpc.IssuedDate > order.AcDocumentsTime.Value)  // Đảm bảo sau thời gian scan
                    .Max(tpc => tpc.IssuedDate);

                if (allThreePointChecks.Any() && (!order.AcAsyTime.HasValue || order.AcAsyTime.Value != maxIssuedDate))
                {
                    order.AcAsyTime = maxIssuedDate;
                    actualTimesChanged = true;
                    _logger.LogInformation("Updated AcAsyTime for order {OrderId} to {AcAsyTime}", orderId, order.AcAsyTime);
                }
            }

            // 3. AcDeliveryTime: Ngày hiện tại khi shipped (BookContStatus == 1), chỉ nếu AcAsyTime đã có
            if (order.AcAsyTime.HasValue && isShipped)
            {
                var now = DateTime.Now;  // Hoặc dùng DateTime.UtcNow nếu cần UTC
                if (!order.AcDeliveryTime.HasValue || order.AcDeliveryTime.Value != now)
                {
                    order.AcDeliveryTime = now;
                    actualTimesChanged = true;
                    _logger.LogInformation("Updated AcDeliveryTime for order {OrderId} to {AcDeliveryTime}", orderId, order.AcDeliveryTime);
                }
            }

            // Cập nhật status nếu thay đổi (giữ nguyên)
            bool statusChanged = order.OrderStatus != oldStatus;
            if (statusChanged || actualTimesChanged)
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} updated: Status from {Old} to {New}, ActualTimes changed: {Changed}",
                    orderId, oldStatus, order.OrderStatus, actualTimesChanged);

                // Notify SignalR (giữ nguyên)
                await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", order.UId.ToString(), order.OrderStatus);
            }

            return statusChanged || actualTimesChanged;
        }
    }
}