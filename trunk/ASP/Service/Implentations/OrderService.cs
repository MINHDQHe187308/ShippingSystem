using ASP.DTO.DensoDTO;
using ASP.Models.Front;

namespace ASP.Service.Implentations
{
    public class OrderService : OrderServiceInterface
    {

        private readonly ExternalApiServiceInterface _externalApiService;
        private readonly OrderRepositoryInterface _orderRepository;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ExternalApiServiceInterface externalApiService, OrderRepositoryInterface orderRepository, ILogger<OrderService> logger)
        {
            _externalApiService = externalApiService;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task SyncOrdersAsync()
        {
            try
            {
                var orders = await _externalApiService.GetOrdersFromApiAsync();
                if (orders == null || !orders.Any())
                {
                    _logger.LogWarning("No orders retrieved from API");
                    return;
                }

                foreach (var orderDto in orders)
                {
                    if (orderDto.OrderId == Guid.Empty || string.IsNullOrEmpty(orderDto.CustomerCode) ||
                        string.IsNullOrEmpty(orderDto.TransCode) || string.IsNullOrEmpty(orderDto.PartNo))
                    {
                        _logger.LogWarning("Invalid order data: {OrderId}", orderDto.OrderId);
                        continue;
                    }

                    await _orderRepository.UpsertOrderAsync(orderDto);
                }

                await _orderRepository.SaveChangesAsync();
                _logger.LogInformation("Synced {Count} orders to database", orders.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing orders");
            }
        }
    }
}