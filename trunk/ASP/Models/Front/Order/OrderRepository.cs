using ASP.DTO.DensoDTO;
using ASP.Models.ASPModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using ASP.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASP.Models.Front
{
    public class OrderRepository : OrderRepositoryInterface
    {
        private readonly ASPDbContext _context;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(ASPDbContext context, IHubContext<OrderHub> hubContext, ILogger<OrderRepository> logger)
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
            if (orderDto == null || orderDto.PcOrderId == Guid.Empty)
                throw new ArgumentException("OrderDTO or PcOrderId cannot be null");

            var trackedOrder = _context.ChangeTracker.Entries<Order>()
                .FirstOrDefault(e => e.Entity.UId == orderDto.PcOrderId);
            Order orderToUpdate;
            bool isNewOrder = false;
            if (trackedOrder != null)
            {
                orderToUpdate = trackedOrder.Entity;
            }
            else
            {
                var existing = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.UId == orderDto.PcOrderId);
                if (existing != null)
                {
                    orderToUpdate = existing;
                    _context.Orders.Update(orderToUpdate);
                }
                else
                {
                    orderToUpdate = new Order
                    {
                        UId = orderDto.PcOrderId,
                        PCOrderId = orderDto.PcOrderId.ToString(),
                        ShipDate = orderDto.ShippingDate,
                        CustomerCode = orderDto.CustomerCode,
                        TransCd = orderDto.TranCd,
                        TotalPallet = orderDto.TotalPallet,
                        OrderCreateDate = orderDto.OrderCreatedDate,
                        ApiOrderStatus = (short)orderDto.OrderStatus,                 
                        OrderStatus = 0,  
                        StartTime = orderDto.OrderCreatedDate,
                        EndTime = orderDto.OrderCreatedDate.AddHours(3),
                        AcStartTime = null,
                        AcEndTime = null,
                        TransMethod = 0,
                        ContSize = 0,
                        //// Bỏ TotalColumn vì không sử dụng
                        //PartList = string.Join(",", orderDto.OrderDetails.Select(od => od.PartNo)),
                    };
                    await _context.Orders.AddAsync(orderToUpdate);
                    isNewOrder = true;
                }
            }

            // Map ApiOrderStatus cho cả update case (nếu existing)
            orderToUpdate.ApiOrderStatus = (short)orderDto.OrderStatus;
            
            // Update ShipDate nếu thay đổi từ API
            if (orderToUpdate.ShipDate != orderDto.ShippingDate)
            {
                _logger.LogInformation("Order {OrderId}: ShipDate changed from {OldDate} to {NewDate}", 
                    orderDto.PcOrderId, orderToUpdate.ShipDate, orderDto.ShippingDate);
                orderToUpdate.ShipDate = orderDto.ShippingDate;
            }

            var shippingSchedule = await _context.ShippingSchedules
                .FirstOrDefaultAsync(s => s.CustomerCode == orderDto.CustomerCode &&
                                         s.TransCd == orderDto.TranCd &&
                                         s.Weekday == orderDto.ShippingDate.DayOfWeek);
            if (shippingSchedule != null)
            {
                var leadtimeMaster = await _context.LeadtimeMasters
                    .FirstOrDefaultAsync(l => l.CustomerCode == orderDto.CustomerCode && l.TransCd == orderDto.TranCd);
                if (leadtimeMaster != null)
                {
                    // Tính thời gian tải hàng từ ThreePointCheckTime
                    double loadingTime = 0;
                    var deliveredPallets = orderDto.OrderDetails
                        .SelectMany(od => od.ShoppingLists)
                        .Where(sl => sl.PalletStatus == (int)CollectionStatusEnumDTO.Delivered && sl.ThreePointCheckTime.HasValue)
                        .ToList();
                    if (deliveredPallets.Any())
                    {
                        loadingTime = deliveredPallets
                            .Sum(sl => (sl.ThreePointCheckTime.Value - (sl.CollectedDate ?? orderDto.OrderCreatedDate)).TotalMinutes);
                        _logger.LogInformation("Order {OrderId}: Calculated loadingTime={LoadingTime} minutes from {DeliveredCount} delivered pallets",
                            orderDto.PcOrderId, loadingTime, deliveredPallets.Count);
                    }
                    else
                    {
                        // Dự phòng: Sử dụng LoadingTimePerPallet
                        double loadingTimePerPallet = 0.5; // Giả định 0.5 phút nếu không có
                        loadingTime = loadingTimePerPallet * orderDto.TotalPallet;
                        _logger.LogWarning("Order {OrderId}: No delivered pallets or ThreePointCheckTime available, using LoadingTimePerPallet={LoadingTimePerPallet} minutes/pallet, TotalPallet={TotalPallet}, loadingTime={LoadingTime} minutes",
                            orderDto.PcOrderId, loadingTimePerPallet, orderDto.TotalPallet, loadingTime);
                    }
                    // Tính totalProcessTime
                    double totalProcessTime = (leadtimeMaster.CollectTimePerPallet * orderDto.TotalPallet) +
                                              (leadtimeMaster.PrepareTimePerPallet * orderDto.TotalPallet) +
                                              loadingTime;
                    // Kiểm tra ngưỡng
                    if (totalProcessTime > 1440) // 1 ngày
                    {
                        _logger.LogWarning("totalProcessTime quá lớn: {Time} phút cho order {OrderId}. Sử dụng giá trị mặc định 180 phút.",
                            totalProcessTime, orderDto.PcOrderId);
                        totalProcessTime = 180; // 8 giờ
                    }
                    // Tính StartTime và EndTime
                    var cutOffDateTime = orderDto.ShippingDate.Date.Add(shippingSchedule.CutOffTime.ToTimeSpan());
                    orderToUpdate.EndTime = cutOffDateTime;
                    orderToUpdate.StartTime = cutOffDateTime.AddMinutes(-totalProcessTime);
                    _logger.LogInformation("Calculated plan times for order {OrderId}: StartTime={StartTime}, EndTime={EndTime}, totalProcessTime={TotalProcessTime} minutes, loadingTime={LoadingTime} minutes",
                        orderDto.PcOrderId, orderToUpdate.StartTime, orderToUpdate.EndTime, totalProcessTime, loadingTime);
                }
                else
                {
                    // Không có LeadtimeMaster
                    var cutOffDateTime = orderDto.ShippingDate.Date.Add(shippingSchedule.CutOffTime.ToTimeSpan());
                    orderToUpdate.EndTime = cutOffDateTime;
                    orderToUpdate.StartTime = cutOffDateTime.AddHours(-3);
                    _logger.LogInformation("No LeadtimeMaster found for order {OrderId}. Using default 8-hour processing time.", orderDto.PcOrderId);
                }
            }
            else
            {
                // Fallback
                orderToUpdate.StartTime = orderDto.OrderCreatedDate;
                orderToUpdate.EndTime = orderDto.OrderCreatedDate.AddHours(3);
                _logger.LogInformation("No ShippingSchedule found for order {OrderId}. Using OrderCreatedDate-based times.", orderDto.PcOrderId);
            }
            if (isNewOrder)
            {
                orderToUpdate.OrderStatus = (short)OrderStatusEnumDTO.Available;  // Default cho progress local (có thể là 0 nếu Planned)
            }

            // Upsert OrderDetails (giữ nguyên phần này)
            foreach (var detailDto in orderDto.OrderDetails)
            {
                var existingDetail = await _context.OrderDetails
                    .FirstOrDefaultAsync(od => od.BookContDetailId == detailDto.BookContDetailId);
                OrderDetail detailToUpdate;
                if (existingDetail != null)
                {
                    detailToUpdate = existingDetail;
                    detailToUpdate.ShippingId = detailDto.ShippingId;
                    detailToUpdate.ContNo = detailDto.ContNo;
                    detailToUpdate.PartNo = detailDto.PartNo;
                    detailToUpdate.PalletSize = detailDto.PalletSize;
                    detailToUpdate.Quantity = detailDto.Quantity;
                    detailToUpdate.TotalPallet = detailDto.TotalPallet;
                    detailToUpdate.Warehouse = detailDto.Warehouse;
                    detailToUpdate.BookContStatus = (short)detailDto.BookContStatus;
                    _context.OrderDetails.Update(detailToUpdate);
                }
                else
                {
                    detailToUpdate = new OrderDetail
                    {
                        UId = Guid.NewGuid(),
                        OId = orderToUpdate.UId,
                        BookContDetailId = detailDto.BookContDetailId,
                        ShippingId = detailDto.ShippingId,
                        ContNo = detailDto.ContNo,
                        PartNo = detailDto.PartNo,
                        PalletSize = detailDto.PalletSize,
                        Quantity = detailDto.Quantity,
                        TotalPallet = detailDto.TotalPallet,
                        Warehouse = detailDto.Warehouse,
                        BookContStatus = (short)detailDto.BookContStatus,
                    };
                    await _context.OrderDetails.AddAsync(detailToUpdate);
                }
                var slCount = detailDto.ShoppingLists?.Count ?? 0;
                _logger.LogDebug("Detail {BookContId}: {SL} shopping lists to upsert", detailDto.BookContDetailId, slCount);
                // Upsert ShoppingLists (giữ nguyên)
                foreach (var slDto in detailDto.ShoppingLists ?? new List<ShoppingListDTO>())
                {
                    var existingSL = await _context.ShoppingLists
                        .FirstOrDefaultAsync(sl => sl.CollectionId == slDto.CollectionId);
                    ShoppingList slToUpdate;
                    if (existingSL != null)
                    {
                        slToUpdate = existingSL;
                        slToUpdate.PalletId = slDto.PalletId;
                        slToUpdate.PalletNo = slDto.PalletNo;
                        slToUpdate.PLStatus = (short)slDto.PalletStatus;
                        slToUpdate.CollectedDate = slDto.CollectedDate;
                        _context.ShoppingLists.Update(slToUpdate);
                        _logger.LogDebug("Updated SL CollectionId={Id}, PalletNo={No}, Status={Status}",
                            slDto.CollectionId, slDto.PalletNo, slDto.PalletStatus);
                    }
                    else
                    {
                        slToUpdate = new ShoppingList
                        {
                            UId = Guid.NewGuid(),
                            ODId = detailToUpdate.UId,
                            CollectionId = slDto.CollectionId,
                            PalletId = slDto.PalletId,
                            PalletNo = slDto.PalletNo,
                            PLStatus = (short)slDto.PalletStatus,
                            CollectedDate = slDto.CollectedDate,
                        };
                        await _context.ShoppingLists.AddAsync(slToUpdate);
                        _logger.LogDebug("Inserted new SL CollectionId={Id}, PalletNo={No}, Status={Status}",
                            slDto.CollectionId, slDto.PalletNo, slDto.PalletStatus);
                    }
                    if (slDto.IsThreePointCheck)
                    {
                        var existingTPC = await _context.ThreePointChecks
                            .FirstOrDefaultAsync(tpc => tpc.SPId == slToUpdate.UId);
                        if (existingTPC == null)
                        {
                            var tpc = new ThreePointCheck
                            {
                                UId = Guid.NewGuid(),
                                SPId = slToUpdate.UId,
                                PalletMarkQrContent = slDto.PlMarkQr,
                                PalletNoQrContent = slDto.PlNoQr,
                                CasemarkQrContent = slDto.CasemarkQr,
                                IssuedDate = slDto.ThreePointCheckTime ?? DateTime.Now,
                            };
                            await _context.ThreePointChecks.AddAsync(tpc);
                            slToUpdate.ThreePointCheck = tpc;
                            _logger.LogDebug("Inserted new ThreePointCheck for SL {CollectionId}", slDto.CollectionId);
                        }
                        else
                        {
                            existingTPC.PalletMarkQrContent = slDto.PlMarkQr;
                            existingTPC.PalletNoQrContent = slDto.PlNoQr;
                            existingTPC.CasemarkQrContent = slDto.CasemarkQr;
                            existingTPC.IssuedDate = slDto.ThreePointCheckTime ?? DateTime.Now;
                            _context.ThreePointChecks.Update(existingTPC);
                            _logger.LogDebug("Updated ThreePointCheck for SL {CollectionId}", slDto.CollectionId);
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
            // Trigger status update sau khi upsert để recalculate cumulative counts và push SignalR
            await UpdateOrderStatusIfNeeded(orderToUpdate.UId);
        }

        public async Task<List<Order>> GetOrdersByDate(DateTime date)
        {
            return await _context.Orders
                .Where(o => o.ShipDate.Date == date.Date
                            && o.ApiOrderStatus != (short)OrderStatusEnumDTO.Cancel)  // Thêm filter: Bỏ qua Cancel từ API
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersForWeek(DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(7);
            return await _context.Orders
                .Where(o => o.ShipDate >= weekStart && o.ShipDate < weekEnd)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .OrderBy(o => o.ShipDate)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // Cập nhật: Sử dụng cumulative count với >= cho status logic
        public async Task<bool> UpdateOrderStatusIfNeeded(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .FirstOrDefaultAsync(o => o.UId == orderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for status update", orderId);
                return false;
            }
            short oldStatus = order.OrderStatus;  // Sử dụng short để nhất quán
            var allShoppingLists = order.OrderDetails.SelectMany(od => od.ShoppingLists ?? new List<ShoppingList>()).ToList();
            // Cumulative: Đã Collected (status >=1, loại trừ Canceled)
            var collectedPallets = allShoppingLists
                .Where(sl => sl.PLStatus >= (short)CollectionStatusEnumDTO.Collected &&
                             sl.PLStatus != (short)CollectionStatusEnumDTO.Canceled)
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();
            var totalOrderPallet = order.TotalPallet;
            bool isCompleted = totalOrderPallet > 0 && collectedPallets >= totalOrderPallet;
            // Shipped: Tất cả completed VÀ BookContStatus >= Exported (cumulative cho BookCont nếu cần)
            bool isShipped = isCompleted &&
                             order.OrderDetails.All(od => od.BookContStatus >= (short)BookingStatusEnumDTO.Exported); // Sử dụng All thay vì Any để đảm bảo full
            if (isShipped)
                order.OrderStatus = 3; // Shipped
            else if (isCompleted)
                order.OrderStatus = 2; // Completed
            else if (collectedPallets > 0 && collectedPallets < totalOrderPallet)
                order.OrderStatus = 1; // Pending
            else
                order.OrderStatus = 0;  // Planned (default nếu không match)

            bool actualTimesChanged = false;
            var today = DateTime.Now.Date;
            // Detect nếu order "sang ngày mới" (điều kiện reset: ShipDate hoặc StartTime trong today, ví dụ delay/reschedule)
            bool isNewDayOrder = order.ShipDate.Date == today || order.StartTime.Date == today;
            // Filter CollectedDate: Chỉ lấy trong today
            var validCollectedDates = allShoppingLists
                .Where(sl => sl.CollectedDate.HasValue && sl.CollectedDate.Value.Date == today)
                .Select(sl => sl.CollectedDate.Value)
                .ToList();
            if (validCollectedDates.Any())
            {
                var earliestCollectedDate = validCollectedDates.Min();
                if (!order.AcStartTime.HasValue || order.AcStartTime.Value.Date != earliestCollectedDate.Date ||
                    order.AcStartTime.Value.TimeOfDay != earliestCollectedDate.TimeOfDay) // Check full time nếu cần precise
                {
                    order.AcStartTime = earliestCollectedDate;
                    actualTimesChanged = true;
                    _logger.LogInformation("Order {OrderId}: AcStartTime updated to {AcStartTime} ({Count} dates in today {Today})",
                        orderId, earliestCollectedDate, validCollectedDates.Count, today);
                }
            }
            else
            {
                // Reset nếu không có data fresh VÀ là ngày mới (delay/reschedule case)
                if (isNewDayOrder && order.AcStartTime.HasValue)
                {
                    order.AcStartTime = null;
                    actualTimesChanged = true;
                    _logger.LogInformation("Order {OrderId}: Reset AcStartTime to null (no fresh data in today {Today}, new day order)",
                        orderId, today);
                }
                else
                {
                    _logger.LogDebug("Order {OrderId}: No CollectedDates in today {Today}, keeping existing AcStartTime",
                        orderId, today);
                }
            }
            if (order.AcStartTime.HasValue)
            {
                // Tương tự cho IssuedDate: Chỉ trong today và >= AcStartTime
                var validThreePointChecks = allShoppingLists
                    .Where(sl => sl.ThreePointCheck != null)
                    .Select(sl => sl.ThreePointCheck)
                    .Where(tpc => tpc.IssuedDate.Date == today && // Chỉ today
                                  tpc.IssuedDate >= order.AcStartTime.Value)
                    .Select(tpc => tpc.IssuedDate)
                    .ToList();
                if (validThreePointChecks.Any())
                {
                    var latestIssuedDate = validThreePointChecks.Max();
                    if (!order.AcEndTime.HasValue || order.AcEndTime.Value != latestIssuedDate)
                    {
                        order.AcEndTime = latestIssuedDate;
                        actualTimesChanged = true;
                        _logger.LogInformation("Order {OrderId}: AcEndTime updated to {AcEndTime} ({Count} dates in today {Today})",
                            orderId, latestIssuedDate, validThreePointChecks.Count, today);
                    }
                }
                else if (isShipped)
                {
                    var fallbackEnd = DateTime.Now; // Full now nếu shipped
                    if (!order.AcEndTime.HasValue || order.AcEndTime.Value != fallbackEnd)
                    {
                        order.AcEndTime = fallbackEnd;
                        actualTimesChanged = true;
                        _logger.LogInformation("Order {OrderId}: Shipped but no TPC in today, fallback AcEndTime to {FallbackEnd}",
                            orderId, fallbackEnd);
                    }
                }
                else
                {
                    // Reset AcEndTime nếu không có data fresh, AcStartTime có (và là ngày mới)
                    if (isNewDayOrder && order.AcEndTime.HasValue)
                    {
                        order.AcEndTime = null;
                        actualTimesChanged = true;
                        _logger.LogInformation("Order {OrderId}: Reset AcEndTime to null (no fresh data in today {Today}, new day order)",
                            orderId, today);
                    }
                }
            }
            else
            {
                // Nếu AcStartTime null (sau reset hoặc chưa có), cũng reset AcEndTime nếu cần
                if (isNewDayOrder && order.AcEndTime.HasValue)
                {
                    order.AcEndTime = null;
                    actualTimesChanged = true;
                    _logger.LogInformation("Order {OrderId}: Reset AcEndTime to null (AcStartTime null, new day order)",
                        orderId);
                }
            }
            bool statusChanged = order.OrderStatus != oldStatus;
            if (statusChanged || actualTimesChanged)
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} updated: ProgressStatus from {Old} to {New} (ApiOrderStatus unchanged: {ApiStatus}), ActualTimes changed: {Changed}",
                    orderId, oldStatus, order.OrderStatus, order.ApiOrderStatus, actualTimesChanged);
                await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", order.UId.ToString(), order.OrderStatus, order.ApiOrderStatus);
            }
            return statusChanged || actualTimesChanged;
        }

        public async Task<Order?> GetOrderById(Guid orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .Include(o => o.DelayHistories)
                .FirstOrDefaultAsync(o => o.UId == orderId);
        }

        public async Task<List<Order>> GetOrdersWithDelayByDate(DateTime date)
        {
            return await _context.Orders
                .Where(o =>
                    (o.StartTime >= date.Date && o.StartTime < date.Date.AddDays(1)) ||
                    (o.EndTime >= date.Date && o.EndTime < date.Date.AddDays(1))
                )
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .Include(o => o.DelayHistories)
                .ToListAsync();
        }

        public async Task UpdateOrderStatusToDelay(Guid orderId, DateTime delayStartTime, double delayTime)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.UId == orderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for delay status update", orderId);
                return;
            }
            short oldStatus = order.OrderStatus;
            order.OrderStatus = 4;
            order.DelayStartTime = delayStartTime;
            order.DelayTime = delayTime;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} status updated to Delay (4) from {OldStatus}, ApiOrderStatus unchanged: {ApiStatus}, DelayStart={DelayStart}, DelayTime={DelayTime}h",
                orderId, oldStatus, order.ApiOrderStatus, delayStartTime, delayTime);
            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", order.UId.ToString(), order.OrderStatus, order.ApiOrderStatus);
        }
    }
}