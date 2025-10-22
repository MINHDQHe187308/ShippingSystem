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

        // Cập nhật: Upsert full hierarchy từ OrderDTO mới (Order + OrderDetails + ShoppingLists)
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
                        OrderStatus = (short)orderDto.OrderStatus,  // Map từ API mới
                        StartTime = orderDto.OrderCreatedDate,  // Default
                        EndTime = orderDto.OrderCreatedDate.AddHours(8),
                        AcStartTime = null,
                        AcEndTime = null,
                        TransMethod = 0,  // Default
                        ContSize = 0,  // Có thể map từ OrderDetails nếu cần
                        TotalColumn = orderDto.OrderDetails.Sum(od => od.Quantity),  // Tổng từ details
                        PartList = string.Join(",", orderDto.OrderDetails.Select(od => od.PartNo)),  // Concat parts
                    };
                    await _context.Orders.AddAsync(orderToUpdate);
                    isNewOrder = true;
                }
            }

            // Tính StartTime/EndTime dựa trên ShippingSchedule và Leadtime (giữ nguyên logic, dùng TranCd và CustomerCode)
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
                    double totalProcessTime = (leadtimeMaster.CollectTimePerPallet * orderDto.TotalPallet) +
                                              (leadtimeMaster.PrepareTimePerPallet * orderDto.TotalPallet) +
                                              (leadtimeMaster.LoadingTimePerColumn * orderDto.OrderDetails.Sum(od => od.Quantity));

                    var cutOffDateTime = orderDto.ShippingDate.Date.Add(shippingSchedule.CutOffTime.ToTimeSpan());
                    orderToUpdate.EndTime = cutOffDateTime;
                    orderToUpdate.StartTime = cutOffDateTime.AddMinutes(-totalProcessTime);

                    _logger.LogInformation("Calculated plan times for order {OrderId}: StartTime={StartTime}, EndTime={EndTime}", orderDto.PcOrderId, orderToUpdate.StartTime, orderToUpdate.EndTime);
                }
                else
                {
                    var cutOffDateTime = orderDto.ShippingDate.Date.Add(shippingSchedule.CutOffTime.ToTimeSpan());
                    orderToUpdate.EndTime = cutOffDateTime;
                    orderToUpdate.StartTime = cutOffDateTime.AddHours(-8);
                }
            }
            else
            {
                // Fallback
                orderToUpdate.StartTime = orderDto.OrderCreatedDate;
                orderToUpdate.EndTime = orderDto.OrderCreatedDate.AddHours(8);
            }

            if (isNewOrder)
            {
                orderToUpdate.OrderStatus = (int)OrderStatusEnumDTO.Available;  // Default từ enum mới
            }

            // Upsert OrderDetails (nested)
            foreach (var detailDto in orderDto.OrderDetails)
            {
                var existingDetail = await _context.OrderDetails
                    .FirstOrDefaultAsync(od => od.BookContDetailId == detailDto.BookContDetailId);

                OrderDetail detailToUpdate;
                if (existingDetail != null)
                {
                    detailToUpdate = existingDetail;
                    // Update fields for existing detail
                    detailToUpdate.ShippingId = detailDto.ShippingId;
                    detailToUpdate.ContNo = detailDto.ContNo;
                    detailToUpdate.PartNo = detailDto.PartNo;
                    detailToUpdate.PalletSize = detailDto.PalletSize;
                    detailToUpdate.Quantity = detailDto.Quantity;
                    detailToUpdate.TotalPallet = detailDto.TotalPallet;
                    // Skip Status if not in model
                    detailToUpdate.Warehouse = detailDto.Warehouse;
                    detailToUpdate.BookContStatus = (short)detailDto.BookContStatus;  // Explicit cast int to short
                    _context.OrderDetails.Update(detailToUpdate);
                }
                else
                {
                    detailToUpdate = new OrderDetail
                    {
                        UId = Guid.NewGuid(),  // Tạo mới nếu cần
                        OId = orderToUpdate.UId,  // Fixed: Use OId (FK for Order)
                        BookContDetailId = detailDto.BookContDetailId,
                        ShippingId = detailDto.ShippingId,
                        ContNo = detailDto.ContNo,
                        PartNo = detailDto.PartNo,
                        PalletSize = detailDto.PalletSize,
                        Quantity = detailDto.Quantity,
                        TotalPallet = detailDto.TotalPallet,
                        // Skip Status if not in model
                        Warehouse = detailDto.Warehouse,
                        BookContStatus = (short)detailDto.BookContStatus,  // Explicit cast int to short
                    };
                    await _context.OrderDetails.AddAsync(detailToUpdate);
                }

                // Log SL cho detail này (new addition)
                var slCount = detailDto.ShoppingLists?.Count ?? 0;
                _logger.LogDebug("Detail {BookContId}: {SL} shopping lists to upsert", detailDto.BookContDetailId, slCount);

                // Upsert ShoppingLists (nested sâu hơn)
                foreach (var slDto in detailDto.ShoppingLists ?? new List<ShoppingListDTO>())
                {
                    var existingSL = await _context.ShoppingLists
                        .FirstOrDefaultAsync(sl => sl.CollectionId == slDto.CollectionId);

                    ShoppingList slToUpdate;
                    if (existingSL != null)
                    {
                        slToUpdate = existingSL;
                        // Update fields for existing SL
                        slToUpdate.PalletId = slDto.PalletId;
                        slToUpdate.PalletNo = slDto.PalletNo;
                        slToUpdate.PLStatus = (short)slDto.PalletStatus;  // Explicit cast int to short, Map PalletStatus -> PLStatus
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
                            PLStatus = (short)slDto.PalletStatus,  // Explicit cast int to short, Map PalletStatus -> PLStatus
                            CollectedDate = slDto.CollectedDate,
                            // ThreePointCheck sẽ được tạo riêng nếu IsThreePointCheck true (xử lý ở UpdateOrderStatusIfNeeded nếu cần)
                        };
                        await _context.ShoppingLists.AddAsync(slToUpdate);
                        _logger.LogDebug("Inserted new SL CollectionId={Id}, PalletNo={No}, Status={Status}",
                            slDto.CollectionId, slDto.PalletNo, slDto.PalletStatus);
                    }

                    // Nếu IsThreePointCheck, tạo/update ThreePointCheck (1-1)
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
                            slToUpdate.ThreePointCheck = tpc;  // Link back
                            _logger.LogDebug("Inserted new ThreePointCheck for SL {CollectionId}", slDto.CollectionId);
                        }
                        else
                        {
                            // Update existing TPC
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
        }

        public async Task<List<Order>> GetOrdersByDate(DateTime date)
        {
            return await _context.Orders
                .Where(o =>
                    (o.StartTime >= date.Date && o.StartTime < date.Date.AddDays(1)) ||
                    (o.EndTime >= date.Date && o.EndTime < date.Date.AddDays(1))
                )
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersForWeek(DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(7);
            return await _context.Orders
                .Where(o =>
                    o.StartTime < weekEnd && o.EndTime > weekStart
                )
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .OrderBy(o => o.StartTime)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        // Cập nhật: Sử dụng PalletStatus (map qua PLStatus), BookContStatus cho status logic
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

            int oldStatus = order.OrderStatus;

            var allShoppingLists = order.OrderDetails.SelectMany(od => od.ShoppingLists ?? new List<ShoppingList>()).ToList();
            var collectedPallets = allShoppingLists
                .Where(sl => sl.PLStatus == (short)CollectionStatusEnumDTO.Collected)  // Map enum mới
                .Select(sl => sl.PalletNo)
                .Distinct()
                .Count();

            var totalOrderPallet = order.TotalPallet;
            bool isCompleted = totalOrderPallet > 0 && collectedPallets >= totalOrderPallet;
            bool isShipped = isCompleted && order.OrderDetails.Any(od => od.BookContStatus == (short)BookingStatusEnumDTO.Exported);  // Sử dụng BookContStatus từ API

            if (isShipped)
                order.OrderStatus = 3;
            else if (isCompleted)
                order.OrderStatus = 2;
            else if (collectedPallets > 0 && collectedPallets < totalOrderPallet)
                order.OrderStatus = 1;

            // Actual times: Sử dụng CollectedDate và ThreePointCheckTime (map từ API)
            bool actualTimesChanged = false;

            var collectedDates = allShoppingLists
                .Where(sl => sl.CollectedDate.HasValue)
                .Select(sl => sl.CollectedDate.Value)
                .ToList();
            if (collectedDates.Any())
            {
                var maxCollectedDate = collectedDates.Max();
                if (!order.AcStartTime.HasValue || order.AcStartTime.Value != maxCollectedDate)
                {
                    order.AcStartTime = maxCollectedDate;
                    actualTimesChanged = true;
                }
            }

            if (order.AcStartTime.HasValue)
            {
                var allThreePointChecks = allShoppingLists
                    .Where(sl => sl.ThreePointCheck != null)
                    .Select(sl => sl.ThreePointCheck)
                    .Where(tpc => tpc.IssuedDate > order.AcStartTime.Value)
                    .Select(tpc => tpc.IssuedDate)
                    .ToList();

                if (allThreePointChecks.Any())
                {
                    var maxIssuedDate = allThreePointChecks.Max();
                    if (!order.AcEndTime.HasValue || order.AcEndTime.Value != maxIssuedDate)
                    {
                        order.AcEndTime = maxIssuedDate;
                        actualTimesChanged = true;
                    }
                }
            }

            if (!order.AcEndTime.HasValue && isShipped)
            {
                order.AcEndTime = DateTime.Now;
                actualTimesChanged = true;
            }

            bool statusChanged = order.OrderStatus != oldStatus;
            if (statusChanged || actualTimesChanged)
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} updated: Status from {Old} to {New}, ActualTimes changed: {Changed}",
                    orderId, oldStatus, order.OrderStatus, actualTimesChanged);

                await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", order.UId.ToString(), order.OrderStatus);
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

            int oldStatus = order.OrderStatus;
            order.OrderStatus = 4;
            order.DelayStartTime = delayStartTime;
            order.DelayTime = delayTime;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to Delay (4) from {OldStatus}, DelayStart={DelayStart}, DelayTime={DelayTime}h",
                orderId, oldStatus, delayStartTime, delayTime);

            await _hubContext.Clients.All.SendAsync("OrderStatusUpdated", order.UId.ToString(), order.OrderStatus);
        }
    }
}