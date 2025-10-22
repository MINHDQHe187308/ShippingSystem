// File: ASP.Service.Implentations/OrderService.cs (Updated to sync ALL orders by default, with optional force flag)
using ASP.DTO.DensoDTO;
using ASP.Models.Front;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

namespace ASP.Service.Implentations
{
    public class OrderService : OrderServiceInterface
    {
        private readonly ExternalApiServiceInterface _externalApiService;
        private readonly OrderRepositoryInterface _orderRepository;
        private readonly ILogger<OrderService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ShippingScheduleRepositoryInterface _shippingRepo;
        private readonly LeadtimeMasterRepositoryInterface _leadtimeRepo;

        public OrderService(ExternalApiServiceInterface externalApiService,
                            OrderRepositoryInterface orderRepository,
                            ILogger<OrderService> logger,
                            IMemoryCache memoryCache,
                            IHostApplicationLifetime lifetime,
                            ShippingScheduleRepositoryInterface shippingRepo,
                            LeadtimeMasterRepositoryInterface leadtimeRepo)
        {
            _externalApiService = externalApiService;
            _orderRepository = orderRepository;
            _logger = logger;
            _memoryCache = memoryCache;
            _lifetime = lifetime;
            _shippingRepo = shippingRepo;
            _leadtimeRepo = leadtimeRepo;
        }

        public async Task SyncOrdersAsync(bool forceSyncAll = true)  // Default to true: sync all orders + full hierarchy
        {
            try
            {
                // Sync leadtimes (giữ nguyên)
                var leadtimes = await _externalApiService.GetLeadtimesFromApiAsync();
                if (leadtimes != null && leadtimes.Any())
                {
                    foreach (var leadtimeDto in leadtimes)
                    {
                        await _leadtimeRepo.UpsertLeadtimeAsync(leadtimeDto);
                    }
                    await _leadtimeRepo.SaveChangesAsync();
                    _logger.LogInformation("Synced {Count} leadtimes to database", leadtimes.Count());
                }
                else
                {
                    _logger.LogWarning("No leadtimes retrieved from API");
                }

                // Sync schedules (giữ nguyên)
                var schedules = await _externalApiService.GetShippingSchedulesFromApiAsync();
                if (schedules != null && schedules.Any())
                {
                    foreach (var scheduleDto in schedules)
                    {
                        await _shippingRepo.UpsertShippingScheduleAsync(scheduleDto);
                    }
                    await _shippingRepo.SaveChangesAsync();
                    _logger.LogInformation("Synced {Count} shipping schedules to database", schedules.Count());
                }
                else
                {
                    _logger.LogWarning("No shipping schedules retrieved from API");
                }

                // Sync orders từ API mới
                var orders = await _externalApiService.GetOrdersFromApiAsync();
                if (orders == null || !orders.Any())
                {
                    _logger.LogWarning("No orders retrieved from API – checking for stop condition");
                    CheckAndStopIfNoNewData(0);
                    return;
                }

                // Log sample data để debug SL
                var totalSL = orders.Sum(o => o.OrderDetails.Sum(d => d.ShoppingLists?.Count ?? 0));
                _logger.LogInformation("Retrieved {Total} orders from API. Total ShoppingLists: {TotalSL}. Sample: First Order SL={FirstSL}",
                    orders.Count(), totalSL, orders.FirstOrDefault()?.OrderDetails?.FirstOrDefault()?.ShoppingLists?.Count ?? 0);

                DateTime lastSync = DateTime.MinValue;  // Default xa để sync all nếu force
                if (!forceSyncAll)
                {
                    if (!_memoryCache.TryGetValue("LastSyncTimestamp", out lastSync))
                    {
                        lastSync = DateTime.Now.AddDays(-1);
                    }
                }
                else
                {
                    _logger.LogInformation("Force sync ALL orders + full hierarchy (OrderDetails + ShoppingLists for each order)");
                }

                // Sync ALL orders (or filter if not force) - lấy full hierarchy cho mỗi order
                var syncOrders = forceSyncAll ? orders.ToList() : orders.Where(o => o.OrderCreatedDate > lastSync).ToList();
                int syncCount = syncOrders.Count;

                // Log SL in sync orders
                var syncSLCount = syncOrders.Sum(o => o.OrderDetails.Sum(d => d.ShoppingLists?.Count ?? 0));
                _logger.LogInformation("Syncing {Sync} orders (mode: {Mode}). Total SL in sync orders: {SyncSL}",
                    syncCount, forceSyncAll ? "ALL (full hierarchy)" : "new only", syncSLCount);

                if (syncCount == 0)
                {
                    CheckAndStopIfNoNewData(syncCount);
                    return;
                }

                // Sync với validation và upsert full hierarchy (Order + Details + SL for each)
                foreach (var orderDto in syncOrders)
                {
                    if (orderDto.PcOrderId == Guid.Empty || string.IsNullOrEmpty(orderDto.CustomerCode) ||
                        string.IsNullOrEmpty(orderDto.TranCd) || !orderDto.OrderDetails.Any())
                    {
                        _logger.LogWarning("Invalid order data: {PcOrderId}", orderDto.PcOrderId);
                        continue;
                    }

                    // Log hierarchy per order để trace
                    var orderDetailsCount = orderDto.OrderDetails.Count;
                    var orderSLCount = orderDto.OrderDetails.Sum(d => d.ShoppingLists?.Count ?? 0);
                    _logger.LogInformation("Upserting Order {Id}: {Details} details, {SL} shopping lists (full hierarchy sync)",
                        orderDto.PcOrderId, orderDetailsCount, orderSLCount);

                    await _orderRepository.UpsertOrderAsync(orderDto);
                    await ((OrderRepository)_orderRepository).UpdateOrderStatusIfNeeded(orderDto.PcOrderId);
                }

                await _orderRepository.SaveChangesAsync();
                _logger.LogInformation("Synced {SyncCount} orders to database. Inserted/Updated SL: {SyncSL}", syncCount, syncSLCount);

                // Update cache lastSync only if not force all
                if (!forceSyncAll)
                {
                    _memoryCache.Set("LastSyncTimestamp", DateTime.Now, TimeSpan.FromHours(24));
                    _memoryCache.Set("NoNewDataCount", 0, TimeSpan.FromHours(24));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing orders or schedules");
            }
        }

        private void CheckAndStopIfNoNewData(int syncCount)
        {
            if (!_memoryCache.TryGetValue("NoNewDataCount", out int noNewCount))
            {
                noNewCount = 0;
            }

            noNewCount++;
            _logger.LogWarning("No new data in sync #{Count} (total no-new: {NoNew})", noNewCount, noNewCount);

            _memoryCache.Set("NoNewDataCount", noNewCount, TimeSpan.FromHours(24));

            const int threshold = 5;
            if (noNewCount >= threshold)
            {
                _logger.LogInformation("No new data for {Threshold} syncs – Stopping WorkerService to avoid redundant syncs", threshold);
                _lifetime.StopApplication();
            }
        }
    }
}