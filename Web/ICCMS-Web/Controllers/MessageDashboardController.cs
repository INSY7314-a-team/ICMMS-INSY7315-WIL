using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Admin,Tester")] // Admins and Testers can access this controller
    public class MessageDashboardController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;
        private readonly ILogger<MessageDashboardController> _logger;

        public MessageDashboardController(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<MessageDashboardController> logger
        )
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        public async Task<IActionResult> Dashboard(
            int page = 1,
            int pageSize = 25,
            string filterType = "all",
            string projectFilter = "all",
            string userFilter = "all",
            string threadFilter = "all",
            string readFilter = "all",
            string searchTerm = "",
            DateTime? startDate = null,
            DateTime? endDate = null
        )
        {
            try
            {
                _logger.LogInformation(
                    "MessageDashboard Dashboard action called with page={Page}, pageSize={PageSize}",
                    page,
                    pageSize
                );

                // Get Firebase token from user claims
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    _logger.LogWarning("Firebase token not found in user claims");
                    TempData["Error"] = "Firebase token not found. Please log in again.";
                    return View(new MessageThreadsViewModel());
                }

                _logger.LogInformation(
                    "Firebase token found, length: {TokenLength}",
                    firebaseToken.Length
                );

                // Clear any existing headers to avoid conflicts
                _httpClient.DefaultRequestHeaders.Clear();

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Use the new consolidated dashboard data endpoint
                var dashboardUrl = $"{_apiBaseUrl}/api/messages/admin/dashboard-data";

                _logger.LogInformation("API Base URL: {ApiBaseUrl}", _apiBaseUrl);
                _logger.LogInformation("Dashboard URL: {DashboardUrl}", dashboardUrl);

                // Build query parameters for all filters
                var queryParams = new List<string> { $"page={page}", $"pageSize={pageSize}" };

                if (!string.IsNullOrEmpty(projectFilter) && projectFilter != "all")
                {
                    queryParams.Add($"projectId={projectFilter}");
                }

                if (!string.IsNullOrEmpty(filterType) && filterType != "all")
                {
                    queryParams.Add($"threadType={filterType}");
                }

                if (!string.IsNullOrEmpty(userFilter) && userFilter != "all")
                {
                    queryParams.Add($"userId={userFilter}");
                }

                if (!string.IsNullOrEmpty(readFilter) && readFilter != "all")
                {
                    queryParams.Add($"readStatus={readFilter}");
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
                }

                if (startDate.HasValue)
                {
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                }

                if (endDate.HasValue)
                {
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
                }

                dashboardUrl += "?" + string.Join("&", queryParams);

                _logger.LogInformation("Making API call to: {Url}", dashboardUrl);
                var response = await _httpClient.GetAsync(dashboardUrl);

                _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(
                        $"Dashboard API response: {content.Substring(0, Math.Min(500, content.Length))}..."
                    );

                    var dashboardData = JsonSerializer.Deserialize<MessageDashboardDataViewModel>(
                        content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (dashboardData != null)
                    {
                        _logger.LogInformation(
                            $"Received {dashboardData.Threads.Count} threads from API (page {dashboardData.Page} of {dashboardData.TotalPages})"
                        );

                        // Convert API thread summaries to view models
                        var threadViewModels = dashboardData
                            .Threads.Select(apiThread => new MessageThreadViewModel
                            {
                                ThreadId = apiThread.ThreadId,
                                Subject = apiThread.Subject,
                                ProjectId = apiThread.ProjectId,
                                ProjectName = apiThread.ProjectName,
                                MessageCount = apiThread.MessageCount,
                                LastMessageAt = apiThread.LastMessageAt,
                                Participants = apiThread.Participants,
                                ThreadType = apiThread.ThreadType,
                                Messages = new List<MessageDto>(),
                            })
                            .ToList();

                        var viewModel = new MessageThreadsViewModel
                        {
                            Threads = threadViewModels,
                            AvailableMessageTypes = new List<string>
                            {
                                "direct",
                                "thread",
                                "broadcast",
                                "general",
                            },
                            AvailableProjects = dashboardData.AvailableProjects,
                            AvailableUsers = dashboardData.AvailableUsers,
                            CurrentPage = dashboardData.Page,
                            TotalPages = dashboardData.TotalPages,
                            PageSize = dashboardData.PageSize,
                            TotalThreads = dashboardData.TotalCount,
                            CurrentFilter = filterType ?? "all",
                            ProjectFilter = projectFilter ?? "all",
                            UserFilter = userFilter ?? "all",
                            ReadFilter = readFilter ?? "all",
                            CurrentSearchTerm = searchTerm ?? "",
                            StartDate = startDate,
                            EndDate = endDate,
                            Statistics = dashboardData.Statistics,
                        };

                        var filterMessage =
                            filterType?.ToLower() != "all" ? $" (Filtered by: {filterType})" : "";
                        var searchMessage = !string.IsNullOrEmpty(searchTerm)
                            ? $" (Search: {searchTerm})"
                            : "";
                        TempData["Success"] =
                            $"Showing {threadViewModels.Count} of {dashboardData.TotalCount} message threads (Page {dashboardData.Page} of {dashboardData.TotalPages}){filterMessage}{searchMessage}";
                        return View(viewModel);
                    }
                    else
                    {
                        _logger.LogWarning("API returned null dashboard data");
                        return View(
                            new MessageThreadsViewModel
                            {
                                Threads = new List<MessageThreadViewModel>(),
                                AvailableMessageTypes = new List<string>
                                {
                                    "direct",
                                    "thread",
                                    "broadcast",
                                    "general",
                                },
                                AvailableProjects = new List<ProjectSummary>(),
                                AvailableUsers = new List<UserSummary>(),
                                CurrentPage = 1,
                                TotalPages = 1,
                                PageSize = pageSize,
                                TotalThreads = 0,
                                CurrentFilter = filterType ?? "all",
                                ProjectFilter = projectFilter ?? "all",
                                UserFilter = userFilter ?? "all",
                                ReadFilter = readFilter ?? "all",
                                CurrentSearchTerm = searchTerm ?? "",
                                StartDate = startDate,
                                EndDate = endDate,
                            }
                        );
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    TempData["Error"] =
                        "Authentication failed. Your session may have expired. Please log in again.";
                    return View(new MessageThreadsViewModel());
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] =
                        $"Failed to retrieve message threads. Status: {response.StatusCode}. Error: {errorContent}";
                    return View(new MessageThreadsViewModel());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving message threads: {Message}", ex.Message);
                TempData["Error"] = $"Error retrieving message threads: {ex.Message}";
                return View(new MessageThreadsViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetThreadMessages(string threadId)
        {
            try
            {
                // Get Firebase token from user claims (same as Dashboard method)
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(new { success = false, message = "Authentication required" });
                }

                // Clear any existing headers to avoid conflicts
                _httpClient.DefaultRequestHeaders.Clear();

                // Set authorization header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Get messages for this specific thread
                var messagesResponse = await _httpClient.GetAsync(
                    $"{_apiBaseUrl}/api/messages/thread/{threadId}"
                );

                if (messagesResponse.IsSuccessStatusCode)
                {
                    var messagesContent = await messagesResponse.Content.ReadAsStringAsync();
                    var apiMessages = JsonSerializer.Deserialize<List<dynamic>>(
                        messagesContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    // Get users for name mapping
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

                    // Convert API messages to view models
                    var messages = new List<object>();

                    if (apiMessages != null)
                    {
                        foreach (var apiMessage in apiMessages)
                        {
                            var message = new
                            {
                                messageId = apiMessage.GetProperty("messageId").GetString()
                                ?? string.Empty,
                                senderId = apiMessage.GetProperty("senderId").GetString()
                                ?? string.Empty,
                                receiverId = apiMessage.GetProperty("receiverId").GetString()
                                ?? string.Empty,
                                content = apiMessage.GetProperty("content").GetString()
                                ?? string.Empty,
                                isRead = apiMessage.GetProperty("isRead").GetBoolean(),
                                sentAt = apiMessage.GetProperty("sentAt").GetDateTime(),
                                attachmentCount = apiMessage
                                    .GetProperty("attachments")
                                    .GetArrayLength(),
                                senderName = usersDict.ContainsKey(
                                    apiMessage.GetProperty("senderId").GetString() ?? string.Empty
                                )
                                    ? usersDict[
                                        apiMessage.GetProperty("senderId").GetString()
                                        ?? string.Empty
                                    ]
                                    : "Unknown User",
                                receiverName = usersDict.ContainsKey(
                                    apiMessage.GetProperty("receiverId").GetString() ?? string.Empty
                                )
                                    ? usersDict[
                                        apiMessage.GetProperty("receiverId").GetString()
                                        ?? string.Empty
                                    ]
                                    : null,
                            };

                            messages.Add(message);
                        }
                    }

                    return Json(new { success = true, messages = messages });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to fetch messages" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage([FromBody] DeleteRequestModel model)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(new { success = false, message = "Firebase token not found" });
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.DeleteAsync(
                    $"{_apiBaseUrl}/api/messages/{model.MessageId}"
                );

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Message deleted successfully." });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(
                        new
                        {
                            success = false,
                            message = $"Failed to delete message: {errorContent}",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteThread([FromBody] DeleteThreadRequestModel model)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(new { success = false, message = "Firebase token not found" });
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.DeleteAsync(
                    $"{_apiBaseUrl}/api/messages/thread/{model.ThreadId}"
                );

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Thread deleted successfully." });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(
                        new
                        {
                            success = false,
                            message = $"Failed to delete thread: {errorContent}",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }

    public class DeleteRequestModel
    {
        public string MessageId { get; set; }
    }

    public class DeleteThreadRequestModel
    {
        public string ThreadId { get; set; }
    }
}
