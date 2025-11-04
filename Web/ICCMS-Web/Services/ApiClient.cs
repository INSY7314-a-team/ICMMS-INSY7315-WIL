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

        // Less aggressive circuit breaker for critical endpoints
        private const int MAX_FAILURES_CRITICAL = 5;
        private const int CIRCUIT_BREAKER_TIMEOUT_MINUTES_CRITICAL = 2;

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

            // Trigger session expired modal via JavaScript
            // This will show the modal with countdown timer
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    // Add JavaScript to show the session expired modal
                    var script =
                        @"
                        <script>
                            if (typeof window.showSessionExpired === 'function') {
                                window.showSessionExpired();
                            } else {
                                console.warn('Session expired handler not available');
                            }
                        </script>";

                    // Store the script in TempData to be rendered
                    var tempData = _tempDataFactory.GetTempData(httpContext);
                    tempData["SessionExpiredScript"] = script;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to trigger session expired modal for {Endpoint}",
                    endpoint
                );
            }
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

                // Handle null data - send empty object instead of null
                var payload = data != null 
                    ? JsonSerializer.Serialize(data) 
                    : "{}";
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
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "PUT {Endpoint} failed ({Code}) {Reason}\n{Body}",
                        endpoint,
                        response.StatusCode,
                        response.ReasonPhrase,
                        errorBody
                    );
                    RecordFailure(endpoint);
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
        // 🔹 DELETE ASYNC
        // ===========================================================
        public async Task<T?> DeleteAsync<T>(string endpoint, ClaimsPrincipal user)
        {
            _logger.LogInformation("DELETE {Endpoint} ===", endpoint);

            try
            {
                var token = user.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}{endpoint}")
                {
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                };

                var response = await _httpClient.SendAsync(request);
                _logger.LogInformation("DELETE response: {Response}", response);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    HandleUnauthorized(endpoint);
                    return default;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "DELETE {Endpoint} failed ({Code}) {Reason}\n{Body}",
                        endpoint,
                        response.StatusCode,
                        response.ReasonPhrase,
                        body
                    );
                    return default;
                }

                // Handle 204 No Content responses
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogInformation(
                        "DELETE {Endpoint}: No Content (204) - operation successful",
                        endpoint
                    );
                    return default;
                }

                // Check if response has content before trying to deserialize
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength == 0 || contentLength == null)
                {
                    _logger.LogWarning("DELETE {Endpoint}: Empty response body", endpoint);
                    return default;
                }

                var bodyText = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(bodyText))
                {
                    _logger.LogWarning("DELETE {Endpoint}: Empty response body", endpoint);
                    return default;
                }

                return JsonSerializer.Deserialize<T>(
                    bodyText,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during DELETE {Endpoint}", endpoint);
                RecordFailure(endpoint);
                return default;
            }
        }

        // ===========================================================
        // 🔧 CIRCUIT BREAKER HELPERS
        // ===========================================================
        private bool IsCircuitOpen(string endpoint)
        {
            lock (_failureCounts)
            {
                if (!_failureCounts.ContainsKey(endpoint))
                    return false;

                // Use different thresholds for critical endpoints
                var isCriticalEndpoint = IsCriticalEndpoint(endpoint);
                var maxFailures = isCriticalEndpoint ? MAX_FAILURES_CRITICAL : MAX_FAILURES;
                var timeoutMinutes = isCriticalEndpoint
                    ? CIRCUIT_BREAKER_TIMEOUT_MINUTES_CRITICAL
                    : CIRCUIT_BREAKER_TIMEOUT_MINUTES;

                if (_failureCounts[endpoint] < maxFailures)
                    return false;

                // Check if enough time has passed to reset the circuit
                if (_lastFailureTimes.ContainsKey(endpoint))
                {
                    var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTimes[endpoint];
                    if (timeSinceLastFailure.TotalMinutes >= timeoutMinutes)
                    {
                        _logger.LogInformation(
                            "Circuit breaker RESET for {Endpoint} (timeout: {Timeout}min)",
                            endpoint,
                            timeoutMinutes
                        );
                        _failureCounts.Remove(endpoint);
                        _lastFailureTimes.Remove(endpoint);
                        return false;
                    }
                }

                return true;
            }
        }

        private bool IsCriticalEndpoint(string endpoint)
        {
            // Critical endpoints that should have less aggressive circuit breaker
            return endpoint.Contains("/api/admin/users")
                || endpoint.Contains("/api/projectmanager/projects")
                || endpoint.Contains("/api/contractor/projects")
                || endpoint.Contains("/api/client/projects");
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

        // Public method to manually reset circuit breaker
        public void ResetCircuitBreaker(string endpoint)
        {
            lock (_failureCounts)
            {
                if (_failureCounts.ContainsKey(endpoint))
                {
                    _logger.LogInformation(
                        "Manually resetting circuit breaker for {Endpoint}",
                        endpoint
                    );
                    _failureCounts.Remove(endpoint);
                    _lastFailureTimes.Remove(endpoint);
                }
            }
        }

        // Public method to reset all circuit breakers
        public void ResetAllCircuitBreakers()
        {
            lock (_failureCounts)
            {
                _logger.LogInformation("Resetting all circuit breakers");
                _failureCounts.Clear();
                _lastFailureTimes.Clear();
            }
        }
    }
}
