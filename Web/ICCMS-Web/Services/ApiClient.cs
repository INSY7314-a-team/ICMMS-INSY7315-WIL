using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;

namespace ICCMS_Web.Services
{
    /// <summary>
    /// Centralized HTTP client wrapper for communicating with the ICCMS API.
    /// Includes short-lived (5 min) in-memory caching for GETs to reduce Firebase reads.
    /// </summary>
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ApiClient> _logger;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITempDataDictionaryFactory _tempDataFactory;
        private readonly IMemoryCache _cache;

        public ApiClient(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<ApiClient> logger,
            IHttpContextAccessor httpContextAccessor,
            ITempDataDictionaryFactory tempDataFactory)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _tempDataFactory = tempDataFactory;
            _cache = new MemoryCache(new MemoryCacheOptions());

            _baseUrl = _config["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
            _logger.LogInformation("ApiClient initialized with base URL: {BaseUrl}", _baseUrl);
        }

        // ===========================================================
        // 🔒 Universal Unauthorized Handler
        // ===========================================================
        private void HandleUnauthorized(string endpoint)
        {
            _logger.LogWarning("🔒 Unauthorized access detected on {Endpoint}. Redirecting user to login.", endpoint);

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                _logger.LogError("❌ HttpContext is null — cannot redirect to login.");
                return;
            }

            var tempData = _tempDataFactory.GetTempData(context);
            tempData["AuthErrorMessage"] = "Your session has expired. Please sign in again.";

            context.Response.Redirect("/Auth/Login");
        }

        // ===========================================================
        // 🔹 GET ASYNC (with caching)
        // ===========================================================
        public async Task<T?> GetAsync<T>(string endpoint, ClaimsPrincipal user, bool forceRefresh = false)
        {
            _logger.LogInformation("=== [ApiClient] GET {Endpoint} (forceRefresh={Force}) ===", endpoint, forceRefresh);

            if (!forceRefresh && _cache.TryGetValue(endpoint, out T cachedValue))
            {
                _logger.LogDebug("💾 Returning cached response for {Endpoint}", endpoint);
                return cachedValue;
            }

            try
            {
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("⚠️ No FirebaseToken found in claims. Likely logged out.");
                    HandleUnauthorized(endpoint);
                    return default;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var url = $"{_baseUrl}{endpoint}";
                _logger.LogDebug("🌍 GET URL → {Url}", url);

                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("📬 Response: {Code}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ GET {Endpoint} failed with {Status} {Reason}\nBody:\n{Body}",
                        endpoint, response.StatusCode, response.ReasonPhrase, body);
                    return default;
                }

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    _cache.Set(endpoint, result, TimeSpan.FromMinutes(5));
                    _logger.LogDebug("🧠 Cached response for {Endpoint} (TTL: 5 minutes)", endpoint);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Exception during GET {Endpoint}", endpoint);
                return default;
            }
        }

        // ===========================================================
        // 🔹 POST ASYNC
        // ===========================================================
        public async Task<T?> PostAsync<T>(string endpoint, object data, ClaimsPrincipal user)
        {
            _logger.LogInformation("=== [ApiClient] POST {Endpoint} ===", endpoint);

            try
            {
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("⚠️ No FirebaseToken found. Redirecting to login.");
                    HandleUnauthorized(endpoint);
                    return default;
                }

                var payload = JsonSerializer.Serialize(data);
                _logger.LogDebug("🧾 Payload:\n{Payload}", payload);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{endpoint}")
                {
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("📬 Response {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ POST {Endpoint} failed ({Code}) {Reason}\n{Body}", endpoint, response.StatusCode, response.ReasonPhrase, body);
                    return default;
                }

                // Invalidate cache for related GETs (keep simple)
                _cache.Remove(endpoint);

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("⚠️ POST {Endpoint}: Empty response.", endpoint);
                    return default;
                }

                // ✅ Handle plain string IDs
                if (!body.TrimStart().StartsWith("{") && typeof(T) == typeof(string))
                {
                    _logger.LogInformation("📎 API returned string ID {Id}", body);
                    return (T)(object)body;
                }

                return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Exception during POST {Endpoint}", endpoint);
                return default;
            }
        }

        // ===========================================================
        // 🔹 PUT ASYNC
        // ===========================================================
        public async Task<T?> PutAsync<T>(string endpoint, object data, ClaimsPrincipal user)
        {
            _logger.LogInformation("=== [ApiClient] PUT {Endpoint} ===", endpoint);

            try
            {
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                var payload = JsonSerializer.Serialize(data);
                _logger.LogDebug("🧾 PUT Payload:\n{Payload}", payload);

                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}{endpoint}")
                {
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                var text = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ PUT {Endpoint} failed ({Code}) {Reason}\n{Body}", endpoint, response.StatusCode, response.ReasonPhrase, text);
                    return default;
                }

                // Invalidate any cached GETs touching this resource
                _cache.Remove(endpoint);

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("⚠️ PUT {Endpoint}: Empty response.", endpoint);
                    return default;
                }

                return JsonSerializer.Deserialize<T>(text, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Exception during PUT {Endpoint}", endpoint);
                return default;
            }
        }

        // ===========================================================
        // 🔹 DELETE ASYNC
        // ===========================================================
        public async Task<bool> DeleteAsync(string endpoint, ClaimsPrincipal user)
        {
            _logger.LogInformation("=== [ApiClient] DELETE {Endpoint} ===", endpoint);

            try
            {
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    HandleUnauthorized(endpoint);
                    return false;
                }

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}{endpoint}")
                {
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) }
                };

                var response = await _httpClient.SendAsync(request);
                _logger.LogInformation("📬 Response {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(endpoint);
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ DELETE {Endpoint} failed ({Code}) {Reason}\n{Body}",
                        endpoint, response.StatusCode, response.ReasonPhrase, body);
                    return false;
                }

                // ✅ Success
                _cache.Remove(endpoint);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Exception during DELETE {Endpoint}", endpoint);
                return false;
            }
        }

    }
}
