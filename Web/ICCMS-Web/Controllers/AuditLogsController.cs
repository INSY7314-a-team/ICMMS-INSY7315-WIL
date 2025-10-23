using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Admin,Tester")] // Admins and Testers can access this controller
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

        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 25,
            string filterType = "all",
            string searchTerm = ""
        )
        {
            try
            {
                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    TempData["Error"] = "Firebase token not found. Please log in again.";
                    return View(new AuditLogsViewModel());
                }

                // Clear any existing headers to avoid conflicts
                _httpClient.DefaultRequestHeaders.Clear();

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Get audit logs from API
                var response = await _httpClient.GetAsync(
                    $"{_apiBaseUrl}/api/auditlogs?limit=1000"
                );

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiLogs = JsonSerializer.Deserialize<List<dynamic>>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    // Get all users to populate full names
                    var usersResponse = await _httpClient.GetAsync(
                        $"{_apiBaseUrl}/api/admin/users"
                    );
                    var usersDict = new Dictionary<string, string>();

                    if (usersResponse.IsSuccessStatusCode)
                    {
                        var usersContent = await usersResponse.Content.ReadAsStringAsync();
                        var users = JsonSerializer.Deserialize<List<dynamic>>(
                            usersContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (users != null)
                        {
                            foreach (var user in users)
                            {
                                var userId = user.GetProperty("userId").GetString() ?? string.Empty;
                                var fullName =
                                    user.GetProperty("fullName").GetString() ?? string.Empty;
                                if (
                                    !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(fullName)
                                )
                                {
                                    usersDict[userId] = fullName;
                                }
                            }
                        }
                    }

                    // Convert API logs to view models and populate user full names
                    var allLogs = new List<AuditLogViewModel>();

                    if (apiLogs != null)
                    {
                        foreach (var apiLog in apiLogs)
                        {
                            var log = new AuditLogViewModel
                            {
                                Id = apiLog.GetProperty("id").GetString() ?? string.Empty,
                                LogType = apiLog.GetProperty("logType").GetString() ?? string.Empty,
                                Title = apiLog.GetProperty("title").GetString() ?? string.Empty,
                                Description = apiLog.GetProperty("description").GetString(),
                                UserId = apiLog.GetProperty("userId").GetString() ?? string.Empty,
                                EntityId =
                                    apiLog.GetProperty("entityId").GetString() ?? string.Empty,
                                TimestampUtc = apiLog.GetProperty("timestampUtc").GetDateTime(),
                                IsActive = true, // Default to active
                            };

                            // Set user full name from dictionary
                            if (usersDict.ContainsKey(log.UserId))
                            {
                                log.UserFullName = usersDict[log.UserId];
                            }

                            allLogs.Add(log);
                        }
                    }

                    // Apply filtering first
                    var filteredLogs = allLogs;

                    // Apply log type filter
                    if (!string.IsNullOrEmpty(filterType) && filterType.ToLower() != "all")
                    {
                        filteredLogs = filteredLogs
                            .Where(l =>
                                l.LogType.Equals(filterType, StringComparison.OrdinalIgnoreCase)
                            )
                            .ToList();
                    }

                    // Apply search filter
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        filteredLogs = filteredLogs
                            .Where(l =>
                                l.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                                || (
                                    l.Description != null
                                    && l.Description.Contains(
                                        searchTerm,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                                || l.UserId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                                || l.EntityId.Contains(
                                    searchTerm,
                                    StringComparison.OrdinalIgnoreCase
                                )
                                || (
                                    l.UserFullName != null
                                    && l.UserFullName.Contains(
                                        searchTerm,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                )
                            )
                            .ToList();
                    }

                    // Apply pagination to filtered results
                    var totalFilteredLogs = filteredLogs.Count;
                    var totalPages = (int)Math.Ceiling((double)totalFilteredLogs / pageSize);

                    // Ensure page is within valid range
                    page = Math.Max(1, Math.Min(page, totalPages));

                    var pagedLogs = filteredLogs
                        .OrderByDescending(l => l.TimestampUtc) // Ensure newest first
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                    // Get available log types from all logs (need to fetch all logs for this)
                    var allLogsResponse = await _httpClient.GetAsync(
                        $"{_apiBaseUrl}/api/auditlogs?limit=1000"
                    );
                    var availableLogTypes = new List<string>();

                    if (allLogsResponse.IsSuccessStatusCode)
                    {
                        var allLogsContent = await allLogsResponse.Content.ReadAsStringAsync();
                        var allLogsData = JsonSerializer.Deserialize<List<JsonElement>>(
                            allLogsContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (allLogsData != null)
                        {
                            availableLogTypes = allLogsData
                                .Select(l => l.GetProperty("logType").GetString() ?? string.Empty)
                                .Where(t => !string.IsNullOrEmpty(t))
                                .Distinct()
                                .OrderBy(t => t)
                                .ToList();
                        }
                    }

                    var viewModel = new AuditLogsViewModel
                    {
                        Logs = pagedLogs,
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalLogs = totalFilteredLogs,
                        TotalPages = totalPages,
                        AvailableLogTypes = availableLogTypes,
                        CurrentFilter = filterType,
                        CurrentSearchTerm = searchTerm,
                    };

                    var filterMessage =
                        filterType.ToLower() != "all" ? $" (Filtered by: {filterType})" : "";
                    var searchMessage = !string.IsNullOrEmpty(searchTerm)
                        ? $" (Search: {searchTerm})"
                        : "";
                    TempData["Success"] =
                        $"Showing {pagedLogs.Count} of {totalFilteredLogs} audit logs (Page {page} of {totalPages}){filterMessage}{searchMessage}";
                    return View(viewModel);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] =
                        "Authentication failed. Your session may have expired. Please log in again.";
                    return View(new AuditLogsViewModel());
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] =
                        $"Failed to retrieve audit logs. Status: {response.StatusCode}. Error: {errorContent}";
                    return View(new AuditLogsViewModel());
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error retrieving audit logs: {ex.Message}";
                return View(new AuditLogsViewModel());
            }
        }
    }

    // View Models
    public class AuditLogsViewModel
    {
        public List<AuditLogViewModel> Logs { get; set; } = new List<AuditLogViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalLogs { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public List<string> AvailableLogTypes { get; set; } = new List<string>();
        public string CurrentFilter { get; set; } = "all";
        public string CurrentSearchTerm { get; set; } = "";

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartIndex => (CurrentPage - 1) * PageSize + 1;
        public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalLogs);
    }

    public class AuditLogViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string LogType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserFullName { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
