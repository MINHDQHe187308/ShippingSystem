using ASP.Models.Front;
using ASP.Services; 
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ASP.Service.Implentations
{
    public class EmailApiService : EmailServiceInterface
    {
        private readonly EmailApiSettings _settings;
        private readonly HttpClient _httpClient;
        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly ILogger<EmailApiService> _logger; 

        public EmailApiService(IOptions<EmailApiSettings> settings, HttpClient httpClient, ILogger<EmailApiService> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

  
        private async Task<string?> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;

            try
            {
                _logger.LogInformation("Requesting new token from {TokenEndpoint}", _settings.TokenEndpoint);
                var authRequest = new { UserName = _settings.Username, Password = _settings.Password };  // Match old: UserName (capital U)
                var json = JsonSerializer.Serialize(authRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.TokenEndpoint, content);
                response.EnsureSuccessStatusCode();

                var tokenResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Token Response: {TokenResponse}", tokenResponse);  // Match old log

                using var doc = JsonDocument.Parse(tokenResponse);
                var root = doc.RootElement;

                // Match old: Thử nhiều trường token
                string? token = null;
                if (root.TryGetProperty("token", out var tokenProp))
                {
                    token = tokenProp.GetString();
                    _logger.LogInformation("Found: token");
                }
                else if (root.TryGetProperty("accessToken", out var accessTokenProp))
                {
                    token = accessTokenProp.GetString();
                    _logger.LogInformation("Found: accessToken");
                }
                else if (root.TryGetProperty("access_token", out var accessToken2Prop))
                {
                    token = accessToken2Prop.GetString();
                    _logger.LogInformation("Found: access_token");
                }
                else
                {
                    // Fallback old: Log fields nếu không tìm thấy
                    var fields = string.Join(", ", root.EnumerateObject().Select(p => p.Name));
                    _logger.LogWarning("Không tìm thấy trường token chuẩn, các trường có: {Fields}", fields);
                    // Raw fallback nếu cần (như old)
                    token = tokenResponse.Trim('"').Trim();
                }

                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception($"Token not found! Response: {tokenResponse}");
                }

                _accessToken = token;

                // Expires (match old, default 1h nếu không có)
                if (root.TryGetProperty("expires_in", out var expiresProp))
                {
                    var expiresIn = expiresProp.GetInt32();
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);
                }
                else
                {
                    _tokenExpiry = DateTime.UtcNow.AddHours(1);
                }

                _logger.LogInformation("Token OK! Expires: {_tokenExpiry:HH:mm:ss}, Length: {Length}", _tokenExpiry, token.Length);
                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TOKEN ERROR");
                throw; // Re-throw như old
            }
        }

       
        public async Task SendEmailAsync(string to, string cc, string subject, string body, bool isHtml = true)
        {
            // Convert single to/cc sang List để match old (delay dùng list từ config)
            var sendToList = string.IsNullOrEmpty(to) ? new List<string>() : to.Split(',').Select(t => t.Trim()).ToList();
            var ccList = string.IsNullOrEmpty(cc) ? new List<string>() : cc.Split(',').Select(c => c.Trim()).ToList();

            await SendEmailWithListsAsync(sendToList, ccList, subject, body, isHtml);
        }

        // THÊM: Internal method để gửi với List (match old delay flow)
        private async Task SendEmailWithListsAsync(List<string> sendTo, List<string> cc, string subject, string body, bool isHtml = true)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Failed to obtain token, cannot send email to {Recipients}", string.Join(", ", sendTo));
                    return;
                }

                _logger.LogInformation("Sending email to: {SendTo}, cc: {Cc}, subject: {Subject}",
                    string.Join(", ", sendTo), string.Join(", ", cc), subject);

                // Match old payload: SystemName, Subject, EmailContent, SendTo/List, Cc/List
                var emailRequest = new
                {
                    SystemName = _settings.SenderName,
                    Subject = subject,
                    EmailContent = body,  // HTML string như old CreateDelayEmailContent
                    SendTo = sendTo,  // List<string> match old
                    Cc = cc ?? new List<string>()  // List<string> match old
                };

                var json = JsonSerializer.Serialize(emailRequest);
                _logger.LogInformation("JSON Payload: {Json} (Cc array: {CcArray})", json, JsonSerializer.Serialize(emailRequest.Cc));
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync(_settings.SendEndpoint, content);

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation(" Email API Response Status: {StatusCode}, Body: {ResponseBody}", response.StatusCode, responseBody);
                _logger.LogInformation(" Full Response Headers: {Headers}", string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(";", h.Value)}")));

                // THÊM: Parse response nếu có messageId (cho track delivery)
                if (!string.IsNullOrEmpty(responseBody))
                {
                    try
                    {
                        var emailResp = JsonSerializer.Deserialize<JsonElement>(responseBody);
                        if (emailResp.TryGetProperty("messageId", out var msgId))
                        {
                            _logger.LogInformation("Message ID for tracking: {MessageId}", msgId.GetString());
                        }
                        if (emailResp.TryGetProperty("success", out var successProp) && !successProp.GetBoolean())
                        {
                            _logger.LogWarning("API returned success=false: {Details}", responseBody);
                        }
                    }
                    catch { /* Ignore parse error */ }
                }

                response.EnsureSuccessStatusCode();  // Throw nếu không 2xx

                _logger.LogInformation("EMAIL SENT SUCCESS! Check inbox/spam/quarantine (delay possible 5-30min). Recipients: {Recipients}", string.Join(", ", sendTo));
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP Error sending email");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EMAIL FAILED");
                // THÊM: Retry 1 lần nếu transient (e.g., 503)
                if (ex is HttpRequestException && ex.Message.Contains("503") || ex.Message.Contains("timeout"))
                {
                    _logger.LogInformation("Retrying email send once...");
                    await Task.Delay(5000);  // Wait 5s
                    await SendEmailWithListsAsync(sendTo, cc, subject, body, isHtml);  // Recursive retry (max 1)
                }
                throw;
            }
        }
    }
}