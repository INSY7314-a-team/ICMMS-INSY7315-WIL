using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ICCMS_Web.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ApiClient> _logger;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITempDataDictionaryFactory _tempDataFactory;

        // Circuit breaker to prevent infinite loops
        private static readonly Dictionary<string, int> _failureCounts = new();
        private static readonly Dictionary<string, DateTime> _lastFailureTimes = new();
        private const int MAX_FAILURES = 3;
        private const int CIRCUIT_BREAKER_TIMEOUT_MINUTES = 5;

        public ApiClient(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<ApiClient> logger,
            IHttpContextAccessor httpContextAccessor,
            ITempDataDictionaryFactory tempDataFactory
        )
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _tempDataFactory = tempDataFactory;

            _baseUrl = _config["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
            _logger.LogInformation("ApiClient initialized with base URL: {BaseUrl}", _baseUrl);
        }

        // ===========================================================
        // 🚀 UNIVERSAL UNAUTHORIZED HANDLER
        // ===========================================================
        private void HandleUnauthorized(string endpoint)
        {
            _logger.LogWarning(
                "🔒 Unauthorized access detected on {Endpoint}. API authentication failed.",
                endpoint
            );

            // Don't redirect automatically - let the controllers handle this gracefully
            // This prevents redirect loops when the API is down but user is still authenticated
        }

        // ===========================================================
        // 🔹 GET ASYNC
        // ===========================================================
        public async Task<T?> GetAsync<T>(string endpoint, ClaimsPrincipal user)
        {
            _logger.LogInformation("=== [ApiClient] GET {Endpoint} ===", endpoint);

            // Check circuit breaker
            if (IsCircuitOpen(endpoint))
            {
                _logger.LogWarning(
                    "Circuit breaker OPEN for {Endpoint}. Skipping request.",
                    endpoint
                );
                return default;
            }

            try
            {
                // 1️⃣ Extract Firebase token from user claims
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No FirebaseToken found in claims. Likely logged out.");
                    HandleUnauthorized(endpoint);
                    return default;
                }

                // 2️⃣ Build request
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    token
                );
                var url = $"{_baseUrl}{endpoint}";
                _logger.LogDebug("GET URL → {Url}", url);

                // 3️⃣ Execute request
                var response = await _httpClient.GetAsync(url);
                _logger.LogInformation("Response: {Code}", response.StatusCode);

                // 4️⃣ Handle 401 Unauthorized
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                // 5️⃣ Handle non-success responses
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "GET {Endpoint} failed with {Status} {Reason}\nBody:\n{Body}",
                        endpoint,
                        response.StatusCode,
                        response.ReasonPhrase,
                        body
                    );

                    // Record failure for circuit breaker
                    RecordFailure(endpoint);
                    return default;
                }

                // Reset failure count on success
                ResetFailureCount(endpoint);

                // 6️⃣ Deserialize success payload
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw JSON: {Json}", json);

                var result = JsonSerializer.Deserialize<T>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (result == null)
                    _logger.LogWarning(
                        "GET {Endpoint} returned null object after deserialization.",
                        endpoint
                    );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during GET {Endpoint}", endpoint);
                RecordFailure(endpoint);
                return default;
            }
        }

        // ===========================================================
        // 🔹 POST ASYNC
        // ===========================================================
        public async Task<T?> PostAsync<T>(string endpoint, object data, ClaimsPrincipal user)
        {
            _logger.LogInformation("POST {Endpoint} ===", endpoint);

            try
            {
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No FirebaseToken found. Redirecting to login.");
                    HandleUnauthorized(endpoint);
                    return default;
                }

                var payload = JsonSerializer.Serialize(data);
                _logger.LogDebug("Payload:\n{Payload}", payload);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{endpoint}")
                {
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                };

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "POST {Endpoint} failed ({Code}) {Reason}\n{Body}",
                        endpoint,
                        response.StatusCode,
                        response.ReasonPhrase,
                        body
                    );
                    return default;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    _logger.LogWarning("POST {Endpoint}: Empty response.", endpoint);
                    return default;
                }

                // ✅ Handle plain string IDs
                if (!body.TrimStart().StartsWith("{") && typeof(T) == typeof(string))
                {
                    _logger.LogInformation("API returned string ID {Id}", body);
                    return (T)(object)body;
                }

                return JsonSerializer.Deserialize<T>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during POST {Endpoint}", endpoint);
                return default;
            }
        }

        // ===========================================================
        // 🔹 PUT ASYNC
        // ===========================================================
        public async Task<T?> PutAsync<T>(string endpoint, object data, ClaimsPrincipal user)
        {
            _logger.LogInformation("PUT {Endpoint} ===", endpoint);

            try
            {
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                var payload = JsonSerializer.Serialize(data);
                _logger.LogDebug("PUT Payload:\n{Payload}", payload);

                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}{endpoint}")
                {
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                };

                var response = await _httpClient.SendAsync(request);
                _logger.LogInformation("PUT response: {Response}", response);
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "PUT {Endpoint} failed ({Code}) {Reason}\n{Body}",
                        endpoint,
                        response.StatusCode,
                        response.ReasonPhrase
                    );
                    return default;
                }

                // Handle 204 No Content responses
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogInformation(
                        "PUT {Endpoint}: No Content (204) - operation successful",
                        endpoint
                    );
                    return default; // Return default value for 204 responses
                }

                // Check if response has content before trying to deserialize
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength == 0)
                {
                    _logger.LogWarning("PUT {Endpoint}: Empty response body", endpoint);
                    return default;
                }

                return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during PUT {Endpoint}", endpoint);
                RecordFailure(endpoint);
                return default;
            }
        }

        // ===========================================================
        // 🔧 CIRCUIT BREAKER HELPERS
        // ===========================================================

        public void ResetCircuitBreaker(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                _logger.LogWarning("ResetCircuitBreaker called with empty endpoint.");
                return;
            }

            lock (_failureCounts)
            {
                _logger.LogInformation(
                    "Manually resetting circuit breaker for {Endpoint}",
                    endpoint
                );
                _failureCounts.Remove(endpoint);
                _lastFailureTimes.Remove(endpoint);
            }
        }

        private bool IsCircuitOpen(string endpoint)
        {
            lock (_failureCounts)
            {
                if (!_failureCounts.ContainsKey(endpoint))
                    return false;

                if (_failureCounts[endpoint] < MAX_FAILURES)
                    return false;

                // Check if enough time has passed to reset the circuit
                if (_lastFailureTimes.ContainsKey(endpoint))
                {
                    var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTimes[endpoint];
                    if (timeSinceLastFailure.TotalMinutes >= CIRCUIT_BREAKER_TIMEOUT_MINUTES)
                    {
                        _logger.LogInformation("Circuit breaker RESET for {Endpoint}", endpoint);
                        _failureCounts.Remove(endpoint);
                        _lastFailureTimes.Remove(endpoint);
                        return false;
                    }
                }

                return true;
            }
        }

        private void RecordFailure(string endpoint)
        {
            lock (_failureCounts)
            {
                if (!_failureCounts.ContainsKey(endpoint))
                    _failureCounts[endpoint] = 0;

                _failureCounts[endpoint]++;
                _lastFailureTimes[endpoint] = DateTime.UtcNow;

                _logger.LogWarning(
                    "Recorded failure #{Count} for {Endpoint}",
                    _failureCounts[endpoint],
                    endpoint
                );

                if (_failureCounts[endpoint] >= MAX_FAILURES)
                {
                    _logger.LogError(
                        "Circuit breaker OPENED for {Endpoint} after {Count} failures",
                        endpoint,
                        _failureCounts[endpoint]
                    );
                }
            }
        }

        private void ResetFailureCount(string endpoint)
        {
            lock (_failureCounts)
            {
                if (_failureCounts.ContainsKey(endpoint))
                {
                    _logger.LogInformation(
                        "Circuit breaker RESET for {Endpoint} - request succeeded",
                        endpoint
                    );
                    _failureCounts.Remove(endpoint);
                    _lastFailureTimes.Remove(endpoint);
                }
            }
        }
    }
}
