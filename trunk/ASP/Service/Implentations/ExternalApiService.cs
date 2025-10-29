using ASP.DTO.DensoDTO;
using System.Text.Json;

namespace ASP.Service.Implentations
{
    public interface ExternalApiServiceInterface
    {
        Task<IEnumerable<OrderDTO>> GetOrdersFromApiAsync();
        
    }

    public class ExternalApiService : ExternalApiServiceInterface
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalApiService> _logger;
        private readonly string _baseApiUrl;
      

        public ExternalApiService(HttpClient httpClient, ILogger<ExternalApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseApiUrl = configuration["ApiSettings:BaseUrl"] ?? "http://10.73.131.20/vzstockforbookcont/api/delivery/shippingprogress";
           
        }

        // Lấy Data Order từ API mới (shippingprogress)
        public async Task<IEnumerable<OrderDTO>> GetOrdersFromApiAsync()
        {
            try
            {
                _logger.LogInformation("Calling external API: {Url}", _baseApiUrl);
                var response = await _httpClient.PostAsync(_baseApiUrl, null);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call failed with status code: {StatusCode}", response.StatusCode);
                    return new List<OrderDTO>();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("API Response content length: {Length}", content?.Length ?? 0);
                var apiResponse = JsonSerializer.Deserialize<DeliveryResponse<OrderDTO>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (apiResponse?.Data != null && apiResponse.Data.Any())
                {
                    _logger.LogInformation("Retrieved {Count} orders from external API", apiResponse.Data.Count);
                    return apiResponse.Data;
                }

                _logger.LogWarning("No data returned from external API");
                return new List<OrderDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external API for orders");
                return new List<OrderDTO>();
            }
        }
    }
}