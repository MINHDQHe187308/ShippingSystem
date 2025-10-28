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



        public OrderService(ExternalApiServiceInterface externalApiService,
                            OrderRepositoryInterface orderRepository,
                            ILogger<OrderService> logger,
                            IMemoryCache memoryCache,
                            IHostApplicationLifetime lifetime
                          )
        {
            _externalApiService = externalApiService;
            _orderRepository = orderRepository;
            _logger = logger;
            _memoryCache = memoryCache;
            _lifetime = lifetime;

        }

        public async Task SyncOrdersAsync(bool forceSyncAll = true)
        {
            try
            {

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
                _logger.LogError(ex, "Error syncing orders");
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