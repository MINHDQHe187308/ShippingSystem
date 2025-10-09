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
        private readonly IServiceScopeFactory _serviceScopeFactory; // Su dung IServiceScopeFactory
        private readonly int _syncInterval;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory; // Tiem IServiceScopeFactory
            _syncInterval = configuration.GetValue<int>("WorkerSettings:SyncIntervalSeconds", 30) * 1000;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker dang chay luc: {time}", DateTimeOffset.Now);
                try
                {
                   
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        // Giai quyet OrderServiceInterface trong pham vi
                        var orderService = scope.ServiceProvider.GetRequiredService<OrderServiceInterface>();
                        await orderService.SyncOrdersAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Loi trong qua trinh");
                }
                await Task.Delay(_syncInterval, stoppingToken);
            }
        }
    }
}