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
        private readonly IMemoryCache _memoryCache;  //  Cache để lưu lastSync info
        private readonly IHostApplicationLifetime _lifetime;  // Để stop app
        private readonly ShippingScheduleRepositoryInterface _shippingRepo;
        private readonly LeadtimeMasterRepositoryInterface _leadtimeRepo;
        public OrderService(ExternalApiServiceInterface externalApiService,
                            OrderRepositoryInterface orderRepository,
                            ILogger<OrderService> logger,
                            IMemoryCache memoryCache,  // Inject cache
                            IHostApplicationLifetime lifetime,
                            ShippingScheduleRepositoryInterface shippingRepo,
                            LeadtimeMasterRepositoryInterface leadtimeRepo)  // Inject lifetime
        {
            _externalApiService = externalApiService;
            _orderRepository = orderRepository;
            _logger = logger;
            _memoryCache = memoryCache;
            _lifetime = lifetime;
            _shippingRepo = shippingRepo;
            _leadtimeRepo = leadtimeRepo;
        }
        public async Task SyncOrdersAsync()
        {
            try
            {
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
                var orders = await _externalApiService.GetOrdersFromApiAsync();
                if (orders == null || !orders.Any())
                {
                    _logger.LogWarning("No orders retrieved from API – checking for stop condition");
                    CheckAndStopIfNoNewData(0);  // Check với count=0
                    return;
                }

                // Lấy lastSyncTimestamp từ cache (default 1 ngày trước nếu chưa có)
                if (!_memoryCache.TryGetValue("LastSyncTimestamp", out DateTime lastSync))
                {
                    lastSync = DateTime.Now.AddDays(-1);
                }

                // Filter orders mới (tránh sync thừa)
                var newOrders = orders.Where(o => o.CreateDate > lastSync).ToList();
                int newCount = newOrders.Count;

                _logger.LogInformation("Retrieved {Total} orders from API, {New} new since last sync", orders.Count(), newCount);

                if (newCount == 0)
                {
                    CheckAndStopIfNoNewData(newCount);  // Check nếu không có mới
                    return;
                }

                // Sync chỉ newOrders (tránh thiếu nếu API full)
                foreach (var orderDto in newOrders)
                {
                    if (orderDto.OrderId == Guid.Empty || string.IsNullOrEmpty(orderDto.CustomerCode) ||
                        string.IsNullOrEmpty(orderDto.TransCode) || string.IsNullOrEmpty(orderDto.PartNo))
                    {
                        _logger.LogWarning("Invalid order data: {OrderId}", orderDto.OrderId);
                        continue;
                    }

                    await _orderRepository.UpsertOrderAsync(orderDto);
                    await ((OrderRepository)_orderRepository).UpdateOrderStatusIfNeeded(orderDto.OrderId);
                }

                await _orderRepository.SaveChangesAsync();
                _logger.LogInformation("Synced {NewCount} new orders to database", newCount);

                // THÊM: Update cache lastSync
                _memoryCache.Set("LastSyncTimestamp", DateTime.Now, TimeSpan.FromHours(24));  // Cache 24h
                _memoryCache.Set("NoNewDataCount", 0, TimeSpan.FromHours(24));  // Reset counter
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing orders or schedules");
                // Không stop nếu lỗi, để retry
            }
        }

        // THÊM MỚI: Phương thức check và stop nếu hết dữ liệu
        private void CheckAndStopIfNoNewData(int newCount)
        {
            if (!_memoryCache.TryGetValue("NoNewDataCount", out int noNewCount))
            {
                noNewCount = 0;
            }

            noNewCount++;
            _logger.LogWarning("No new data in sync #{Count} (total no-new: {NoNew})", noNewCount, noNewCount);

            _memoryCache.Set("NoNewDataCount", noNewCount, TimeSpan.FromHours(24));

            const int threshold = 5;  // Ngưỡng: 5 lần liên tiếp không có mới → Stop
            if (noNewCount >= threshold)
            {
                _logger.LogInformation("No new data for {Threshold} syncs – Stopping WorkerService to avoid redundant syncs", threshold);
                _lifetime.StopApplication();  // Dừng app graceful
            }
        }
    }
}