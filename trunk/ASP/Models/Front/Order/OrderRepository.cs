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
                        StartTime = orderDto.CreateDate,  // Default StartTime từ CreateDate
                        EndTime = orderDto.CreateDate.AddHours(8),  // Default EndTime (có thể tính sau)
                        AcStartTime = null,
                        AcEndTime = null,
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
                        StartTime = orderDto.CreateDate,  // Default StartTime từ CreateDate
                        EndTime = orderDto.CreateDate.AddHours(8),  // Default EndTime (có thể tính sau)
                        AcStartTime = null,
                        AcEndTime = null,
                    };
                    await _context.Orders.AddAsync(orderToUpdate);
                    isNewOrder = true;
                }
            }

            // Tính StartTime/EndTime dựa trên ShippingSchedule (trừ ngược từ CutOffTime)
            var shippingSchedule = await _context.ShippingSchedules
                .FirstOrDefaultAsync(s => s.CustomerCode == orderDto.CustomerCode &&
                                         s.TransCd == orderDto.TransCode &&
                                         s.Weekday == orderDto.ShippingDate.DayOfWeek);

            if (shippingSchedule != null)
            {
                var leadtimeMaster = await _context.LeadtimeMasters
                    .FirstOrDefaultAsync(l => l.CustomerCode == orderDto.CustomerCode && l.TransCd == orderDto.TransCode);

                if (leadtimeMaster != null)
                {
                    // Tính tổng thời gian quy trình (phút)
                    double totalProcessTime = (leadtimeMaster.CollectTimePerPallet * orderDto.TotalPallet) +
                                              (leadtimeMaster.PrepareTimePerPallet * orderDto.TotalPallet) +
                                              (leadtimeMaster.LoadingTimePerColumn * orderDto.Quantity);

                    // Tạo DateTime cho CutOffTime dựa trên ShipDate
                    var cutOffDateTime = orderDto.ShippingDate.Date.Add(shippingSchedule.CutOffTime.ToTimeSpan());

                    // Trừ ngược: EndTime = CutOffTime (thời gian xuất hàng)
                    orderToUpdate.EndTime = cutOffDateTime;

                    // StartTime = EndTime - totalProcessTime (bắt đầu quy trình)
                    orderToUpdate.StartTime = cutOffDateTime.AddMinutes(-totalProcessTime);

                    _logger.LogInformation("Calculated plan times from CutOffTime for order {OrderId}: StartTime={StartTime}, EndTime={EndTime} (CutOff: {CutOff}, TotalProcess: {TotalMin} min)",
                        orderDto.OrderId, orderToUpdate.StartTime, orderToUpdate.EndTime, cutOffDateTime, totalProcessTime);
                }
                else
                {
                    // Fallback nếu không có LeadtimeMaster
                    var cutOffDateTime = orderDto.ShippingDate.Date.Add(shippingSchedule.CutOffTime.ToTimeSpan());
                    orderToUpdate.EndTime = cutOffDateTime;
                    orderToUpdate.StartTime = cutOffDateTime.AddHours(-8);  // Default 8 giờ trước
                    _logger.LogWarning("No LeadtimeMaster, used default 8h before CutOff for order {OrderId}", orderDto.OrderId);
                }
            }
            else
            {
                // Fallback cũ nếu không có ShippingSchedule (tính cộng từ CreateDate)
                _logger.LogWarning("No ShippingSchedule found for {CustomerCode}-{TransCd}-{Weekday}, falling back to CreateDate calculation",
                    orderDto.CustomerCode, orderDto.TransCode, orderDto.ShippingDate.DayOfWeek);

                var leadtimeMaster = await _context.LeadtimeMasters
                    .FirstOrDefaultAsync(l => l.CustomerCode == orderDto.CustomerCode && l.TransCd == orderDto.TransCode);

                if (leadtimeMaster != null)
                {
                    double collectTimeTotal = leadtimeMaster.CollectTimePerPallet * orderDto.TotalPallet;
                    double prepareTimeTotal = leadtimeMaster.PrepareTimePerPallet * orderDto.TotalPallet;
                    double loadingTimeTotal = leadtimeMaster.LoadingTimePerColumn * orderDto.Quantity;
                    orderToUpdate.StartTime = orderDto.CreateDate.AddMinutes(collectTimeTotal);
                    orderToUpdate.EndTime = orderToUpdate.StartTime.AddMinutes(prepareTimeTotal + loadingTimeTotal);
                }
                else
                {
                    orderToUpdate.StartTime = orderDto.CreateDate;
                    orderToUpdate.EndTime = orderDto.CreateDate.AddHours(8);  // Default
                }
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
                    (o.StartTime >= date.Date && o.StartTime < date.Date.AddDays(1)) ||
                    (o.EndTime >= date.Date && o.EndTime < date.Date.AddDays(1))
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
                .ThenInclude(sl => sl.ThreePointCheck)  // SỬA: 1-1 relationship, dùng ThreePointCheck thay vì ThreePointChecks
                .FirstOrDefaultAsync(o => o.UId == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for status update", orderId);
                return false;
            }

            int oldStatus = order.OrderStatus;

            // Tính collected pallets từ ShoppingLists (giữ nguyên, sửa CollectionStatus thành PLStatus)
            var allShoppingLists = order.OrderDetails.SelectMany(od => od.ShoppingLists ?? new List<ShoppingList>()).ToList();
            var collectedPallets = allShoppingLists
                .Where(sl => sl.PLStatus == 1 || sl.CollectedDate.HasValue)  // SỬA: PLStatus thay vì CollectionStatus
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();

            var totalOrderPallet = order.TotalPallet;
            bool isCompleted = totalOrderPallet > 0 && collectedPallets >= totalOrderPallet;
            bool isShipped = isCompleted && order.OrderDetails.Any(od => od.BookContStatus == 1);

            if (isShipped)
                order.OrderStatus = 3;  // Shipped
            else if (isCompleted)
                order.OrderStatus = 2;  // Completed
            else if (collectedPallets > 0 && collectedPallets < totalOrderPallet)
                order.OrderStatus = 1;  // Pending
                                        // Giữ 0 nếu chưa collect

            // THÊM MỚI: Tính Actual Times dựa trên dữ liệu thực tế (nối tiếp theo quy trình, với null-safe)
            bool actualTimesChanged = false;

            // 1. AcStartTime: Max CollectedDate từ tất cả ShoppingLists (hoàn thành scan pallet)

            var collectedDates = allShoppingLists
                .Where(sl => sl.CollectedDate.HasValue)
                .Select(sl => sl.CollectedDate.Value)
                .ToList();
            if (collectedDates.Any())
            {
                var maxCollectedDate = collectedDates.Max();  // Max trên List<DateTime> non-nullable
                if (!order.AcStartTime.HasValue || order.AcStartTime.Value != maxCollectedDate)
                {
                    order.AcStartTime = maxCollectedDate;
                    actualTimesChanged = true;
                    _logger.LogInformation("Updated AcStartTime for order {OrderId} to {AcStartTime}", orderId, order.AcStartTime);
                }
            }

            // 2. AcEndTime: Max IssuedDate từ tất cả ThreePointCheck (sau quét ba điểm), chỉ nếu AcStartTime đã có
            if (order.AcStartTime.HasValue)
            {
                var allThreePointChecks = allShoppingLists.SelectMany(sl => sl.ThreePointCheck != null ? new List<ThreePointCheck> { sl.ThreePointCheck } : new List<ThreePointCheck>()).ToList();  // SỬA: Single ThreePointCheck, dùng SelectMany cho collection giả
                var validIssuedDates = allThreePointChecks
                    .Where(tpc => tpc.IssuedDate > order.AcStartTime.Value)
                    .Select(tpc => tpc.IssuedDate)
                    .ToList();

                if (validIssuedDates.Any())
                {
                    var maxIssuedDate = validIssuedDates.Max();  // Max trên List<DateTime>
                    if (!order.AcEndTime.HasValue || order.AcEndTime.Value != maxIssuedDate)
                    {
                        order.AcEndTime = maxIssuedDate;
                        actualTimesChanged = true;
                        _logger.LogInformation("Updated AcEndTime for order {OrderId} to {AcEndTime}", orderId, order.AcEndTime);
                    }
                }
            }

            // 3. AcEndTime (nâng cao): Ngày hiện tại khi shipped (BookContStatus == 1), chỉ nếu AcEndTime chưa có và isShipped
            if (order.AcEndTime.HasValue == false && isShipped)
            {
                var now = DateTime.Now;  // DateTime non-nullable, gán vào DateTime? fine
                order.AcEndTime = now;
                actualTimesChanged = true;
                _logger.LogInformation("Updated AcEndTime for order {OrderId} to {AcEndTime}", orderId, order.AcEndTime);
            }

            // Cập nhật nếu thay đổi (giữ nguyên)
            bool statusChanged = order.OrderStatus != oldStatus;
            if (statusChanged || actualTimesChanged)
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} updated: Status from {Old} to {New}, ActualTimes changed: {Changed}",
                    orderId, oldStatus, order.OrderStatus, actualTimesChanged);

                // Notify SignalR
                await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", order.UId.ToString(), order.OrderStatus);
            }

            return statusChanged || actualTimesChanged;
        }
    }
}