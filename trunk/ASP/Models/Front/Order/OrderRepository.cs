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
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Crypto.Generators;

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

            return await _context.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.PCOrderId == pcOrderId);
        }

        public async Task UpsertOrderAsync(OrderDTO orderDto)
        {
            if (orderDto == null || orderDto.PcOrderId == Guid.Empty)
                throw new ArgumentException("OrderDTO or PcOrderId cannot be null");

            // Wrap toàn bộ trong transaction để atomic (rollback nếu fail)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                            TotalColumn = 0,
                        };
                        await _context.Orders.AddAsync(orderToUpdate);
                        isNewOrder = true;
                    }
                }

                // Map ApiOrderStatus cho cả update case
                orderToUpdate.ApiOrderStatus = (short)orderDto.OrderStatus;

                // Update ShipDate nếu thay đổi từ API
                if (orderToUpdate.ShipDate != orderDto.ShippingDate)
                {
                    _logger.LogInformation("Order {OrderId}: ShipDate changed from {OldDate} to {NewDate}",
                        orderDto.PcOrderId, orderToUpdate.ShipDate, orderDto.ShippingDate);
                    orderToUpdate.ShipDate = orderDto.ShippingDate;
                }

                // Tính Start/EndTime (giữ nguyên toàn bộ phần shippingSchedule, leadtimeMaster, fallback...)
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
                        double loadingTimePerPallet = 0.5;
                        double loadingTime = loadingTimePerPallet * orderDto.TotalPallet;
                        _logger.LogInformation("Order {OrderId}: Estimated loadingTime={LoadingTime} minutes using {LoadingTimePerPallet} min/pallet for {TotalPallet} pallets",
                            orderDto.PcOrderId, loadingTime, loadingTimePerPallet, orderDto.TotalPallet);
                        double totalProcessTime = (leadtimeMaster.CollectTimePerPallet * orderDto.TotalPallet) +
                                                  (leadtimeMaster.PrepareTimePerPallet * orderDto.TotalPallet) +
                                                  loadingTime;
                        if (totalProcessTime > 1440)
                        {
                            _logger.LogWarning("totalProcessTime quá lớn: {Time} phút cho order {OrderId}. Sử dụng giá trị mặc định 180 phút.",
                                totalProcessTime, orderDto.PcOrderId);
                            totalProcessTime = 180;
                        }
                        var cutOffDateTime = orderDto.ShippingDate.Date.Add(shippingSchedule.CutOffTime.ToTimeSpan());
                        orderToUpdate.EndTime = cutOffDateTime;
                        orderToUpdate.StartTime = cutOffDateTime.AddMinutes(-totalProcessTime);
                        _logger.LogInformation("Calculated plan times for order {OrderId}: StartTime={StartTime}, EndTime={EndTime}, totalProcessTime={TotalProcessTime} minutes, loadingTime={LoadingTime} minutes",
                            orderDto.PcOrderId, orderToUpdate.StartTime, orderToUpdate.EndTime, totalProcessTime, loadingTime);
                    }
                    else
                    {
                        var cutOffDateTime = orderDto.ShippingDate.Date.Add(shippingSchedule.CutOffTime.ToTimeSpan());
                        orderToUpdate.EndTime = cutOffDateTime;
                        orderToUpdate.StartTime = cutOffDateTime.AddHours(-3);
                        _logger.LogInformation("No LeadtimeMaster found for order {OrderId}. Using default 8-hour processing time.", orderDto.PcOrderId);
                    }
                }
                else
                {
                    orderToUpdate.StartTime = orderDto.OrderCreatedDate;
                    orderToUpdate.EndTime = orderDto.OrderCreatedDate.AddHours(3);
                    _logger.LogInformation("No ShippingSchedule found for order {OrderId}. Using OrderCreatedDate-based times.", orderDto.PcOrderId);
                }

                if (isNewOrder)
                {
                    orderToUpdate.OrderStatus = (short)OrderStatusEnumDTO.Available;
                }

                // FIX: Nếu new Order, SAVE NGAY để insert UId trước khi add details (tránh FK violation)
                if (isNewOrder)
                {
                    _logger.LogDebug("New order {OrderId}: Saving parent Order first to ensure FK exists", orderDto.PcOrderId);
                    await _context.SaveChangesAsync(); // Insert Order.UId ngay
                }

                // Upsert OrderDetails
                foreach (var detailDto in orderDto.OrderDetails)
                {
                    // Safety check: Đảm bảo parent Order tồn tại trước khi add detail
                    var parentOrderExists = await _context.Orders.AsNoTracking().AnyAsync(o => o.UId == orderToUpdate.UId);
                    if (!parentOrderExists)
                    {
                        _logger.LogError("FK violation risk: Parent Order {OrderId} not found in DB before adding detail {DetailId}",
                            orderToUpdate.UId, detailDto.BookContDetailId);
                        throw new InvalidOperationException($"Parent Order {orderToUpdate.UId} missing - cannot add detail.");
                    }

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
                            OId = orderToUpdate.UId, // FK an toàn vì Order đã save nếu new
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

                    // Upsert ShoppingLists (giữ nguyên toàn bộ, bao gồm becameDelivered logic và ThreePointCheck)
                    foreach (var slDto in detailDto.ShoppingLists ?? new List<ShoppingListDTO>())
                    {
                        var existingSL = await _context.ShoppingLists
                            .FirstOrDefaultAsync(sl => sl.CollectionId == slDto.CollectionId);
                        ShoppingList slToUpdate;
                        short newStatus = (short)slDto.PalletStatus;
                        short? oldStatus = null;
                        if (existingSL != null)
                        {
                            slToUpdate = existingSL;
                            oldStatus = existingSL.PLStatus;
                            slToUpdate.PalletId = slDto.PalletId;
                            slToUpdate.PalletNo = slDto.PalletNo;
                            slToUpdate.PLStatus = newStatus;
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
                                PLStatus = newStatus,
                                CollectedDate = slDto.CollectedDate,
                            };
                            await _context.ShoppingLists.AddAsync(slToUpdate);
                            _logger.LogDebug("Inserted new SL CollectionId={Id}, PalletNo={No}, Status={Status}",
                                slDto.CollectionId, slDto.PalletNo, slDto.PalletStatus);
                        }

                        // If this shopping list's status changed to Delivered (3), update the parent Order's UpdatedDate and AcEndTime (giữ nguyên)
                        try
                        {
                            const short deliveredStatus = (short)CollectionStatusEnumDTO.Delivered;
                            bool becameDelivered = newStatus == deliveredStatus && (oldStatus == null || oldStatus != deliveredStatus);
                            if (becameDelivered)
                            {
                                var now = DateTime.Now;
                                orderToUpdate. AcEndTime = now;
                                orderToUpdate.UpdatedDate = now;
                                var entry = _context.Entry(orderToUpdate);
                                if (entry != null)
                                {
                                    var prop = entry.Property("UpdatedDate");
                                    if (prop != null)
                                    {
                                        prop.CurrentValue = now;
                                    }
                                }
                                _context.Orders.Update(orderToUpdate);
                                _logger.LogInformation("Order {OrderId}: ShoppingList {CollectionId} became Delivered - UpdatedDate and AcEndTime set to {Now}",
                                    orderToUpdate.UId, slDto.CollectionId, now);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error updating Order timestamps when SL {CollectionId} became Delivered", slDto.CollectionId);
                        }

                        // ThreePointCheck (giữ nguyên)
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

                // SAVE cuối: Details + SL + updates (Order đã save nếu new)
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Commit transaction

                // Trigger status update (giữ nguyên)
                await UpdateOrderStatusIfNeeded(orderToUpdate.UId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to upsert order {OrderId}: Rolling back transaction. Inner: {Message}",
                    orderDto.PcOrderId, ex.Message);
                throw; // Re-throw để service handle (ví dụ: retry)
            }
        }

        public async Task<List<Order>> GetOrdersByDate(DateTime date)
        {
            return await _context.Orders.AsNoTracking()
                .Where(o => o.ShipDate.Date == date.Date
                            && o.ApiOrderStatus != (short)OrderStatusEnumDTO.Cancel) // Thêm filter: Bỏ qua Cancel từ API
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersForWeek(DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(7);
            return await _context.Orders.AsNoTracking()
                .Where(o => o.ShipDate >= weekStart && o.ShipDate < weekEnd)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .OrderBy(o => o.ShipDate)
                .AsSplitQuery()
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

            short oldStatus = order.OrderStatus; // Sử dụng short để nhất quán
            var allShoppingLists = order.OrderDetails.SelectMany(od => od.ShoppingLists ?? new List<ShoppingList>()).ToList();

            // Cumulative: Đã Collected (status >=1, loại trừ Canceled)
            var collectedPallets = allShoppingLists
                .Where(sl => sl.PLStatus >= (short)CollectionStatusEnumDTO.Collected &&
                             sl.PLStatus != (short)CollectionStatusEnumDTO.Canceled)
                .Select(sl => sl.PalletId)
                .Distinct()
                .Count();

            // Tất cả pallets đã LOADED lên cont (status >= Delivered, tức là đã qua Collected + ThreePointCheck + Loaded)
            var loadedPallets = allShoppingLists
                .Where(sl => sl.PLStatus >= (short)CollectionStatusEnumDTO.Delivered &&
                             sl.PLStatus != (short)CollectionStatusEnumDTO.Canceled)
                .Select(sl => sl.PalletId)
                .Distinct()
                .Count();

            // ... (phần load order và tính collectedPallets, loadedPallets giữ nguyên)
            var totalOrderPallet = order.TotalPallet;
            bool isCompleted = totalOrderPallet > 0 && loadedPallets >= totalOrderPallet;

            // Shipped: Tất cả completed VÀ BookContStatus >= Exported
            bool isShipped = isCompleted &&
                             order.OrderDetails.All(od => od.BookContStatus >= (short)BookingStatusEnumDTO.Exported);

            if (isShipped)
                order.OrderStatus = 3; // Shipped
            else if (isCompleted)
                order.OrderStatus = 2; // Completed (Tất cả pallets đã load lên cont)
            else if (collectedPallets > 0 || loadedPallets > 0) // FIX: Bao quát cả collected và loaded partial
                order.OrderStatus = 1; // Pending (Đang thu thập hoặc loading, nhưng chưa đủ)
            else
                order.OrderStatus = 0; // Planned

            // ... (phần actualTimesChanged giữ nguyên)
            bool actualTimesChanged = false;

            // SỬA: Loại bỏ filter today, lấy tất cả collectedDates từ quá khứ
            var validCollectedDates = allShoppingLists
                .Where(sl => sl.CollectedDate.HasValue)
                .Select(sl => sl.CollectedDate!.Value)
                .ToList();
            if (validCollectedDates.Any())
            {
                var earliestCollectedDate = validCollectedDates.Min();
                if (!order.AcStartTime.HasValue || order.AcStartTime.Value != earliestCollectedDate)
                {
                    order.AcStartTime = earliestCollectedDate;
                    actualTimesChanged = true;
                    _logger.LogInformation("Order {OrderId}: AcStartTime updated to {AcStartTime} (from all historical dates, {Count} total)",
                        orderId, earliestCollectedDate, validCollectedDates.Count);
                }
            }

            // SỬA: Mở rộng fallback để normalize fake Now (offset >30min và same day)
            bool needsEndTimeFallback = (order.OrderStatus >= 2) &&  // Completed or Shipped
                (!order.AcEndTime.HasValue ||  // Original: null
                  (order.AcEndTime.Value.Date == DateTime.Today &&  // Same day as sync
                   order.AcEndTime.Value > order.EndTime.AddMinutes(30)));  // Offset >30min, coi như fake Now

            if (needsEndTimeFallback)
            {
                var fallbackTime = order.EndTime;
                if (order.AcEndTime.HasValue)  // Normalize existing fake
                {
                    _logger.LogWarning("Order {OrderId}: Normalizing fake AcEndTime {Fake} to PlanEndTime={Plan} (offset >30min, likely batch sync artifact)",
                        orderId, order.AcEndTime.Value, fallbackTime);
                }
                else
                {
                    _logger.LogWarning("Order {OrderId}: Backfilled AcEndTime with PlanEndTime={PlanEnd} (status={Status}, no actual captured - likely app started late)",
                        orderId, order.EndTime, order.OrderStatus);
                }
                order.AcEndTime = fallbackTime; // Fallback: Dùng PlanEndTime trực tiếp (no .Value)
                order.UpdatedDate = fallbackTime; // Giả sử UpdatedDate là DateTime (non-nullable)
                actualTimesChanged = true;
            }

            // Thêm log so sánh AcEndTime vs EndTime
            if (order.AcEndTime.HasValue)
            {
                var offsetMin = (order.AcEndTime.Value - order.EndTime).TotalMinutes;
                _logger.LogDebug("Order {OrderId}: AcEndTime={Ac} vs EndTime={End} (offset {Offset:F1} min)",
                    orderId, order.AcEndTime.Value, order.EndTime, offsetMin);
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
            return await _context.Orders.AsNoTracking()
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .ThenInclude(sl => sl.ThreePointCheck)
                .Include(o => o.DelayHistories)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.UId == orderId);
        }

        public async Task<List<Order>> GetOrdersWithDelayByDate(DateTime date)
        {
            return await _context.Orders.AsNoTracking()
                .Where(o =>
                    (o.StartTime >= date.Date && o.StartTime < date.Date.AddDays(1)) ||
                    (o.EndTime >= date.Date && o.EndTime < date.Date.AddDays(1))
                )
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ShoppingLists)
                .Include(o => o.DelayHistories)
                .AsSplitQuery()
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