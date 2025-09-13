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

            // Add timeout and other configurations
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
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

                // Test multiple endpoints for integration - updated to current API structure
                var endpoints = new[]
                {
                    "/api/admin/dashboard",
                    "/api/users/profile",
                    "/api/quotations",
                    "/api/invoices",
                    "/api/documents",
                    "/api/estimates",
                    "/api/messages",
                    "/api/notifications",
                    "/api/payments",
                    "/api/auditlogs",
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

        public IActionResult WorkflowTest()
        {
            return View();
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
                        Console.WriteLine($"{_apiBaseUrl}{endpoint}");
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

        [HttpPost]
        [Route("Testing/FileOperation")]
        public async Task<IActionResult> FileOperation(
            string operation,
            string endpoint,
            string method,
            IFormFile? file = null,
            string? projectId = null,
            string? description = null
        )
        {
            try
            {
                // Add more debugging
                Console.WriteLine("=== FileOperation Method Called ===");
                Console.WriteLine($"User: {User.Identity?.Name}");
                Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
                Console.WriteLine(
                    $"Roles: {string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}"
                );
                Console.WriteLine($"Operation: {operation}");
                Console.WriteLine($"Endpoint: {endpoint}");
                Console.WriteLine($"Method: {method}");
                Console.WriteLine($"File: {file?.FileName} ({file?.Length} bytes)");
                Console.WriteLine($"ProjectId: {projectId}");
                Console.WriteLine($"Description: {description}");
                Console.WriteLine($"API Base URL: {_apiBaseUrl}");
                Console.WriteLine("=====================================");

                // Add debugging
                Console.WriteLine(
                    $"FileOperation called with operation: {operation}, endpoint: {endpoint}, method: {method}"
                );
                Console.WriteLine($"API Base URL: {_apiBaseUrl}");

                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    Console.WriteLine("No Firebase token found");
                    return Json(
                        new { success = false, message = "Please log in to test endpoints." }
                    );
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                if (operation == "upload")
                {
                    // Test basic API connectivity first
                    try
                    {
                        Console.WriteLine($"Testing basic connectivity to: {_apiBaseUrl}");

                        // Try a simple GET request to the root API endpoint
                        var basicTestResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api");
                        Console.WriteLine(
                            $"Basic connectivity test result: {basicTestResponse.StatusCode}"
                        );

                        // Try the documents endpoint
                        var testResponse = await _httpClient.GetAsync(
                            $"{_apiBaseUrl}/api/documents"
                        );
                        Console.WriteLine(
                            $"Documents endpoint test result: {testResponse.StatusCode}"
                        );

                        if (!testResponse.IsSuccessStatusCode)
                        {
                            var errorContent = await testResponse.Content.ReadAsStringAsync();
                            Console.WriteLine($"API test error content: {errorContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"API connectivity test failed: {ex.Message}");
                        Console.WriteLine($"Exception type: {ex.GetType().Name}");
                        return Json(
                            new
                            {
                                success = false,
                                message = $"Cannot connect to API at {_apiBaseUrl}. Error: {ex.Message}",
                            }
                        );
                    }

                    // Handle file upload
                    var multipartContent = new MultipartFormDataContent();

                    // Add file if present
                    if (file != null && file.Length > 0)
                    {
                        // Read the file into a byte array to avoid stream disposal issues
                        byte[] fileBytes;
                        using (var stream = file.OpenReadStream())
                        {
                            fileBytes = new byte[stream.Length];
                            await stream.ReadAsync(fileBytes, 0, fileBytes.Length);
                        }

                        var fileContent = new ByteArrayContent(fileBytes);
                        fileContent.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                        multipartContent.Add(fileContent, "file", file.FileName);
                    }

                    // Add other form fields
                    if (!string.IsNullOrEmpty(projectId))
                    {
                        multipartContent.Add(new StringContent(projectId), "projectId");
                    }

                    if (!string.IsNullOrEmpty(description))
                    {
                        multipartContent.Add(new StringContent(description), "description");
                    }

                    // Set default method if not provided
                    if (string.IsNullOrEmpty(method))
                    {
                        method = "POST";
                    }

                    HttpResponseMessage response;
                    switch (method.ToUpper())
                    {
                        case "POST":
                            Console.WriteLine($"Sending POST request to: {_apiBaseUrl}{endpoint}");
                            response = await _httpClient.PostAsync(
                                $"{_apiBaseUrl}{endpoint}",
                                multipartContent
                            );
                            break;
                        case "PUT":
                            response = await _httpClient.PutAsync(
                                $"{_apiBaseUrl}{endpoint}",
                                multipartContent
                            );
                            break;
                        default:
                            return Json(
                                new
                                {
                                    success = false,
                                    message = "Unsupported HTTP method for file upload.",
                                }
                            );
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
                else if (operation == "download")
                {
                    // Handle file download
                    if (string.IsNullOrEmpty(method))
                    {
                        method = "GET";
                    }

                    HttpResponseMessage response;
                    switch (method.ToUpper())
                    {
                        case "GET":
                            response = await _httpClient.GetAsync($"{_apiBaseUrl}{endpoint}");
                            break;
                        default:
                            return BadRequest("Unsupported HTTP method for file download.");
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var fileBytes = await response.Content.ReadAsByteArrayAsync();
                        var contentType =
                            response.Content.Headers.ContentType?.ToString()
                            ?? "application/octet-stream";

                        return File(fileBytes, contentType, "downloaded-file");
                    }
                    else
                    {
                        return StatusCode(
                            (int)response.StatusCode,
                            $"Failed to download file: {response.StatusCode}"
                        );
                    }
                }
                else
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Invalid operation. Use 'upload' or 'download'.",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FileOperation: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
