using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using ICCMS_Web.Models;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Tester")] // Only Testers can access this controller
    public class AuditLogsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public AuditLogsController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    TempData["Error"] = "Firebase token not found. Please log in again.";
                    return View(new List<AuditLogViewModel>());
                }

                // Clear any existing headers to avoid conflicts
                _httpClient.DefaultRequestHeaders.Clear();
                
                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Get audit logs from API
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/auditlogs?limit=100");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var logs = JsonSerializer.Deserialize<List<AuditLogViewModel>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    TempData["Success"] = $"Successfully retrieved {logs?.Count ?? 0} audit logs";
                    return View(logs ?? new List<AuditLogViewModel>());
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] = "Authentication failed. Your session may have expired. Please log in again.";
                    return View(new List<AuditLogViewModel>());
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Failed to retrieve audit logs. Status: {response.StatusCode}. Error: {errorContent}";
                    return View(new List<AuditLogViewModel>());
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving audit logs: {ex.Message}";
                return View(new List<AuditLogViewModel>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateLog([FromBody] CreateAuditLogRequest request)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(new { success = false, message = "Firebase token not found" });
                }

                // Clear any existing headers to avoid conflicts
                _httpClient.DefaultRequestHeaders.Clear();
                
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/auditlogs", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<CreateAuditLogResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Json(new { success = true, message = "Audit log created successfully", id = result?.Id });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Failed to create audit log: {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLogTypes()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(new { success = false, message = "Firebase token not found" });
                }

                // Clear any existing headers to avoid conflicts
                _httpClient.DefaultRequestHeaders.Clear();
                
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/auditlogs/types");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var types = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return Json(new { success = true, types = types });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to retrieve log types" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }

    // View Models
    public class AuditLogViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string LogType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateAuditLogRequest
    {
        public string LogType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
    }

    public class CreateAuditLogResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}