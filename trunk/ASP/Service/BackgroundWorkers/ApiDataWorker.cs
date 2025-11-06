using ASP.Service;
using ASP.Service.Implentations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ASP.Service.BackgroundWorkers
{ 
    public class ApiDataWorker : BackgroundService
    {
        private readonly ILogger<ApiDataWorker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly int _syncInterval;

        public ApiDataWorker(ILogger<ApiDataWorker> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _syncInterval = configuration.GetValue<int>("WorkerSettings:SyncIntervalSeconds", 30) * 1000; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker đang chạy lúc: {time}", DateTimeOffset.Now);
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var orderService = scope.ServiceProvider.GetRequiredService<OrderServiceInterface>();
                        // Sync ALL orders + full hierarchy (giữ nguyên logic từ Worker cũ)
                        await orderService.SyncOrdersAsync(forceSyncAll: true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong quá trình sync");
                }
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }
    }
}