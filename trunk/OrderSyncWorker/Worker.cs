using ASP.Service;
using ASP.Service.Implentations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DensoWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory; // S? d?ng IServiceScopeFactory
        private readonly int _syncInterval;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory; // Tiêm IServiceScopeFactory
            _syncInterval = configuration.GetValue<int>("WorkerSettings:SyncIntervalSeconds", 30) * 1000;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker ?ang ch?y lúc: {time}", DateTimeOffset.Now);
                try
                {
                    // T?o m?t ph?m vi ?? gi?i quy?t các d?ch v? scoped
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        // Gi?i quy?t OrderServiceInterface trong ph?m vi
                        var orderService = scope.ServiceProvider.GetRequiredService<OrderServiceInterface>();
                        await orderService.SyncOrdersAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "L?i trong quá trình ??ng b? hóa ??n hàng");
                }
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }
    }
}