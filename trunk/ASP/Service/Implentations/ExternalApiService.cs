using ASP.DTO.DensoDTO;
using System.Text.Json;

namespace ASP.Service.Implentations
{
    public interface ExternalApiServiceInterface
    {
        public Task<IEnumerable<OrderDTO>> GetOrdersFromApiAsync();
        public Task<IEnumerable<ShippingScheduleDTO>> GetShippingSchedulesFromApiAsync();
        public  Task<IEnumerable<LeadtimeMasterDTO>> GetLeadtimesFromApiAsync();
    }
    public class ExternalApiService : ExternalApiServiceInterface
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalApiService> _logger;
        private readonly string _baseApiUrl;
        private readonly string _shippingApiUrl;
        private readonly string _leadtimemasterApiUrl;
        public ExternalApiService(HttpClient httpClient, ILogger<ExternalApiService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseApiUrl = configuration["ApiSettings:BaseUrl"] ?? "http://10.73.131.20/vzstockforbookcont/api/delivery/pcorder";
            _shippingApiUrl = configuration["ApiSettings:ShippingScheduleUrl"] ?? "http://10.73.131.20/vzstockforbookcont/api/shipping/schedule";
            _leadtimemasterApiUrl = configuration["ApiSettings:LeadtimeUrl"] ?? "http://10.73.131.20/vzstockforbookcont/api/leadtime/master";
        }
        //Lấy Data Orrder từ API
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
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<OrderDTO>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (apiResponse?.data != null)
                {
                    _logger.LogInformation("Retrieved {Count} orders from external API", apiResponse.data.Length);
                    return apiResponse.data;
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
        //Lấy Data ShippingSchedule từ API
        public async Task<IEnumerable<ShippingScheduleDTO>> GetShippingSchedulesFromApiAsync()
        {
            try
            {
                _logger.LogInformation("Calling ShippingSchedule API: {Url}", _shippingApiUrl);
                var response = await _httpClient.PostAsync(_shippingApiUrl, null);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ShippingSchedule API call failed with status code: {StatusCode}", response.StatusCode);
                    return new List<ShippingScheduleDTO>();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("ShippingSchedule API Response content length: {Length}", content?.Length ?? 0);
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<ShippingScheduleDTO>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (apiResponse?.data != null)
                {
                    _logger.LogInformation("Retrieved {Count} shipping schedules from external API", apiResponse.data.Length);
                    return apiResponse.data;
                }

                _logger.LogWarning("No shipping schedule data returned from external API");
                return new List<ShippingScheduleDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external API for shipping schedules");
                return new List<ShippingScheduleDTO>();
            }
        }
       
        //Lấy Data LeadtimeMaster từ API
        public async Task<IEnumerable<LeadtimeMasterDTO>> GetLeadtimesFromApiAsync()
        {
            try
            {
                _logger.LogInformation("Calling Leadtime API: {Url}", _leadtimemasterApiUrl);
                var response = await _httpClient.PostAsync(_leadtimemasterApiUrl, null);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Leadtime API call failed with status code: {StatusCode}", response.StatusCode);
                    return new List<LeadtimeMasterDTO>();
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Leadtime API Response content length: {Length}", content?.Length ?? 0);
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<LeadtimeMasterDTO>>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (apiResponse?.data != null)
                {
                    _logger.LogInformation("Retrieved {Count} leadtimes from external API", apiResponse.data.Length);
                    return apiResponse.data;
                }

                _logger.LogWarning("No leadtime data returned from external API");
                return new List<LeadtimeMasterDTO>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling external API for leadtimes");
                return new List<LeadtimeMasterDTO>();
            }
        }
    }
}
    




