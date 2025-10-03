using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ICCMS_Web.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ApiClient> _logger;
        private readonly string _baseUrl;

        public ApiClient(HttpClient httpClient, IConfiguration config, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            _baseUrl = _config["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
            _logger.LogInformation("ApiClient initialized with base URL: {BaseUrl}", _baseUrl);
        }

        public async Task<T?> GetAsync<T>(string endpoint, ClaimsPrincipal user)
        {
            _logger.LogInformation("Starting GET request to {Endpoint}", endpoint);

            try
            {
                // ===== Auth Token =====
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("GET {Endpoint}: No FirebaseToken found in claims.", endpoint);
                    return default;
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                // ===== Build URL =====
                var url = $"{_baseUrl}{endpoint}";
                _logger.LogInformation("GET {Endpoint}: Full URL {Url}", endpoint, url);

                // ===== Execute Request =====
                var res = await _httpClient.GetAsync(url);
                _logger.LogInformation("GET {Endpoint}: Response status {StatusCode}", endpoint, res.StatusCode);

                if (!res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    _logger.LogError("GET {Endpoint}: Failed with {StatusCode} {Reason}. Body: {Body}",
                        endpoint, res.StatusCode, res.ReasonPhrase, body);
                    return default;
                }

                // ===== Success: log raw JSON =====
                var json = await res.Content.ReadAsStringAsync();

                try
                {
                    var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result == null)
                    {
                        _logger.LogWarning("GET {Endpoint}: Deserialized result is null. JSON body was:\n{Json}", endpoint, json);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GET {Endpoint}: Failed to deserialize JSON. Raw body:\n{Json}", endpoint, json);
                    return default;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GET {Endpoint}: Exception thrown", endpoint);
                return default;
            }
        }

            public async Task<T?> PostAsync<T>(string endpoint, object data, ClaimsPrincipal user)
            {
                
                var token = user.FindFirst("FirebaseToken")?.Value ?? "";
                var payload = JsonSerializer.Serialize(data);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{endpoint}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("POST {Endpoint}: Failed with {Status} {Reason}. Body: {Body}",
                        endpoint, response.StatusCode, response.ReasonPhrase, text);
                    return default;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("POST {Endpoint}: Empty response", endpoint);
                    return default;
                }
                

                // ✅ Handle plain string ID (Firestore style)
                if (!text.TrimStart().StartsWith("{") && !text.TrimStart().StartsWith("["))
                {
                    _logger.LogInformation("POST {Endpoint}: API returned string ID {Id}", endpoint, text);
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)text; // cast the string as T
                    }
                    return default;
                }

                // ✅ Handle JSON as usual
                return JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            public async Task<T?> PutAsync<T>(string endpoint, object data, ClaimsPrincipal user)
            {
                var token = user.FindFirst("FirebaseToken")?.Value ?? "";
                var payload = JsonSerializer.Serialize(data);

                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}{endpoint}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PUT {Endpoint}: Failed with {Status} {Reason}. Body: {Body}",
                        endpoint, response.StatusCode, response.ReasonPhrase, text);
                    return default;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("PUT {Endpoint}: Empty response");
                    return default;
                }

                return JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }




    }
}
