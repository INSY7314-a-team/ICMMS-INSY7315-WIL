using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Tester")]
    public class TestingController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public TestingController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> ApiTest()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    TempData["ErrorMessage"] = "Please log in to test API endpoints.";
                    return View();
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Test basic connectivity
                var testResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/admin/users");
                if (testResponse.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] =
                        "API connection successful! You can now test endpoints.";
                }
                else
                {
                    TempData["ErrorMessage"] =
                        "API connection failed. Please check if the API is running.";
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> DatabaseTest()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    TempData["ErrorMessage"] = "Please log in to test database connections.";
                    return View();
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Test database connectivity through API
                var testResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/users");
                if (testResponse.IsSuccessStatusCode)
                {
                    var responseBody = await testResponse.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<User>>(
                        responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    TempData["SuccessMessage"] =
                        $"Database connection successful! Retrieved {users?.Count ?? 0} users.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Database connection failed through API.";
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> IntegrationTest()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    TempData["ErrorMessage"] = "Please log in to test integration.";
                    return View();
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Test multiple endpoints for integration
                var endpoints = new[]
                {
                    "/api/admin",
                    "/api/clients",
                    "/api/projects",
                    "/api/contractors",
                };

                var results = new List<string>();
                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync($"{_apiBaseUrl}{endpoint}");
                        if (response.IsSuccessStatusCode)
                        {
                            results.Add($"✅ {endpoint} - Working");
                        }
                        else
                        {
                            results.Add($"❌ {endpoint} - Failed ({response.StatusCode})");
                        }
                    }
                    catch
                    {
                        results.Add($"❌ {endpoint} - Error");
                    }
                }

                TempData["IntegrationResults"] = JsonSerializer.Serialize(results);
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestEndpoint(
            string endpoint,
            string method = "GET",
            string requestBody = ""
        )
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(
                        new { success = false, message = "Please log in to test endpoints." }
                    );
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                HttpResponseMessage response;
                switch (method.ToUpper())
                {
                    case "GET":
                        response = await _httpClient.GetAsync($"{_apiBaseUrl}{endpoint}");
                        break;
                    case "POST":
                        var content = new StringContent(
                            requestBody,
                            System.Text.Encoding.UTF8,
                            "application/json"
                        );
                        response = await _httpClient.PostAsync($"{_apiBaseUrl}{endpoint}", content);
                        break;
                    case "PUT":
                        var putContent = new StringContent(
                            requestBody,
                            System.Text.Encoding.UTF8,
                            "application/json"
                        );
                        response = await _httpClient.PutAsync(
                            $"{_apiBaseUrl}{endpoint}",
                            putContent
                        );
                        break;
                    case "DELETE":
                        response = await _httpClient.DeleteAsync($"{_apiBaseUrl}{endpoint}");
                        break;
                    default:
                        return Json(new { success = false, message = "Unsupported HTTP method." });
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = new
                {
                    success = response.IsSuccessStatusCode,
                    statusCode = (int)response.StatusCode,
                    statusText = response.StatusCode.ToString(),
                    responseBody = responseBody,
                    headers = response.Headers.ToDictionary(h => h.Key, h => h.Value),
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
