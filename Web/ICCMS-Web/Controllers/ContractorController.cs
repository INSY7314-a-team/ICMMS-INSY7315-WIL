using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// Request model for updating task status
public class UpdateTaskStatusRequest
{
    public string TaskId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Contractor")]
    public class ContractorController : Controller
    {
        private readonly IContractorService _contractorService;
        private readonly ILogger<ContractorController> _logger;
        private readonly IApiClient _apiClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMessagingService _messagingService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public ContractorController(
            IContractorService contractorService,
            ILogger<ContractorController> logger,
            IApiClient apiClient,
            IHttpContextAccessor httpContextAccessor,
            IMessagingService messagingService,
            HttpClient httpClient,
            IConfiguration configuration
        )
        {
            _contractorService = contractorService;
            _logger = logger;
            _apiClient = apiClient;
            _httpContextAccessor = httpContextAccessor;
            _messagingService = messagingService;
            _httpClient = httpClient;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        /// <summary>
        /// Test API connectivity
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestApi()
        {
            try
            {
                _logger.LogInformation("Testing API connectivity...");

                // Test the health endpoint first
                var currentUser = _httpContextAccessor.HttpContext?.User;
                var healthResponse = await _apiClient.GetAsync<object>(
                    "/api/contractors/health",
                    currentUser
                );
                _logger.LogInformation(
                    "Health check response: {Response}",
                    healthResponse != null ? "Success" : "Failed"
                );

                if (healthResponse == null)
                {
                    return Json(
                        new
                        {
                            success = false,
                            error = "API health check failed - circuit breaker may be open or API not running",
                            suggestion = "Check if API server is running on https://localhost:7136",
                        }
                    );
                }

                return Json(
                    new
                    {
                        success = true,
                        message = "API connectivity test successful",
                        healthResponse,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API connectivity test failed");
                return Json(
                    new
                    {
                        success = false,
                        error = ex.Message,
                        suggestion = "Check API server status and network connectivity",
                    }
                );
            }
        }

        /// <summary>
        /// Main contractor dashboard showing assigned tasks
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID found for contractor dashboard");
                    ViewBag.ErrorMessage = "Authentication error. Please log in again.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                _logger.LogInformation("Loading contractor dashboard for user {UserId}", userId);

                var dashboardData = await _contractorService.GetDashboardDataAsync();

                if (dashboardData == null)
                {
                    _logger.LogWarning("Dashboard data was null for {UserId}", userId);
                    ViewBag.ErrorMessage = "No dashboard data available.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                _logger.LogInformation(
                    "Dashboard loaded with {TotalTasks} tasks for contractor {UserId}",
                    dashboardData.TotalTasks,
                    userId
                );

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contractor dashboard");
                ViewBag.ErrorMessage = "Failed to load dashboard. Please try again later.";
                return View(
                    "Error",
                    new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                );
            }
        }

        /// <summary>
        /// Project detail page showing full project information and tasks
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProjectDetail(string projectId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID found for project detail");
                    ViewBag.ErrorMessage = "Authentication error. Please log in again.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                if (string.IsNullOrEmpty(projectId))
                {
                    _logger.LogWarning("No project ID provided for project detail");
                    ViewBag.ErrorMessage = "Project ID is required.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                _logger.LogInformation(
                    "Loading project detail for project {ProjectId} and user {UserId}",
                    projectId,
                    userId
                );

                // Get the current user for API calls
                var currentUser = _httpContextAccessor.HttpContext?.User;
                if (currentUser == null)
                {
                    _logger.LogWarning("User not authenticated for project detail");
                    ViewBag.ErrorMessage = "Authentication error. Please log in again.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                // Get tasks for this project first
                var tasksResponse = await _apiClient.GetAsync<PaginatedResponse<ContractorTaskDto>>(
                    "/api/contractors/tasks/assigned",
                    currentUser
                );

                var tasks = tasksResponse?.Data ?? new List<ContractorTaskDto>();

                if (tasks == null || !tasks.Any())
                {
                    _logger.LogWarning("No tasks found for user {UserId}", userId);
                    ViewBag.ErrorMessage = "No tasks found.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                var projectTasks = tasks.Where(t => t.ProjectId == projectId).ToList();
                if (!projectTasks.Any())
                {
                    _logger.LogWarning(
                        "No tasks found for project {ProjectId} and user {UserId}",
                        projectId,
                        userId
                    );
                    ViewBag.ErrorMessage = "No tasks found for this project.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                // Get the first task to extract project information
                var firstTask = projectTasks.FirstOrDefault();
                if (firstTask == null)
                {
                    _logger.LogWarning("No valid task found in project tasks");
                    ViewBag.ErrorMessage = "No valid task data found.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                // Safely extract project information with null checks
                var extractedProjectId = !string.IsNullOrEmpty(firstTask.ProjectId)
                    ? firstTask.ProjectId
                    : projectId;

                // Log project ID for debugging
                _logger.LogInformation(
                    "🔍 Project ID Debug - Original: {OriginalId}, Task ProjectId: {TaskProjectId}, Extracted: {ExtractedId}",
                    projectId,
                    firstTask.ProjectId,
                    extractedProjectId
                );

                var projectName = !string.IsNullOrEmpty(firstTask.ProjectId)
                    ? $"Project {firstTask.ProjectId.Substring(0, Math.Min(8, firstTask.ProjectId.Length))}"
                    : "Unknown Project";

                // Safely calculate dates with null checks
                var startDates = projectTasks
                    .Where(t => t.StartDate != default(DateTime))
                    .Select(t => t.StartDate)
                    .ToList();
                var dueDates = projectTasks
                    .Where(t => t.DueDate != default(DateTime))
                    .Select(t => t.DueDate)
                    .ToList();

                var minStartDate = startDates.Any() ? startDates.Min() : DateTime.UtcNow;
                var maxDueDate = dueDates.Any() ? dueDates.Max() : DateTime.UtcNow.AddDays(30);

                // Create ProjectDetailDto from the task data
                var projectDetails = new ProjectDetailDto
                {
                    ProjectId = extractedProjectId,
                    Name = projectName,
                    Description = $"Project tasks for {extractedProjectId}",
                    BudgetPlanned = 0, // Default values since we don't have project budget info
                    BudgetActual = 0,
                    Status = "Active", // Default status
                    StartDate = minStartDate,
                    EndDatePlanned = maxDueDate,
                    EndDateActual = null,
                    CompletionPhase = null,
                    ClientId = "",
                    ClientName = "Client", // Default client name
                    ProjectManagerId = "",
                    ProjectManagerName = "Project Manager", // Default PM name
                    Tasks = projectTasks,
                    TotalTasks = projectTasks.Count,
                    CompletedTasks = projectTasks.Count(t =>
                        !string.IsNullOrEmpty(t.Status) && t.Status == "Completed"
                    ),
                    InProgressTasks = projectTasks.Count(t =>
                        !string.IsNullOrEmpty(t.Status) && t.Status == "In Progress"
                    ),
                    PendingTasks = projectTasks.Count(t =>
                        !string.IsNullOrEmpty(t.Status) && t.Status == "Pending"
                    ),
                    OverdueTasks = projectTasks.Count(t => t.IsOverdue),
                    OverallProgress = projectTasks.Any()
                        ? (int)projectTasks.Where(t => t.Progress >= 0).Average(t => t.Progress)
                        : 0,
                };

                _logger.LogInformation(
                    "Project detail loaded for project {ProjectId} with {TaskCount} tasks",
                    extractedProjectId,
                    projectDetails.Tasks?.Count ?? 0
                );

                return View(projectDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading project detail for project {ProjectId}",
                    projectId
                );
                ViewBag.ErrorMessage = "Failed to load project details. Please try again later.";
                return View(
                    "Error",
                    new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                );
            }
        }

        /// <summary>
        /// Get assigned tasks as JSON (for AJAX calls)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAssignedTasks()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                var tasks = await _contractorService.GetAssignedTasksAsync();
                return Json(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned tasks");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get task details for modal display
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTaskDetails(string taskId)
        {
            try
            {
                _logger.LogInformation("🔍 GetTaskDetails called for taskId: {TaskId}", taskId);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("❌ User ID not found");
                    return Unauthorized(new { error = "User ID not found" });
                }

                _logger.LogInformation("👤 User ID: {UserId}", userId);

                var task = await _contractorService.GetTaskWithProjectAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("❌ Task not found for taskId: {TaskId}", taskId);
                    return NotFound(new { error = "Task not found" });
                }

                _logger.LogInformation("✅ Task found: {TaskName}", task.Name);
                return Json(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting task details for {TaskId}", taskId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update task status (for starting tasks)
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateTaskStatus(
            [FromBody] UpdateTaskStatusRequest request
        )
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                if (string.IsNullOrEmpty(request.TaskId))
                {
                    return BadRequest(new { error = "Task ID is required" });
                }

                if (string.IsNullOrEmpty(request.Status))
                {
                    return BadRequest(new { error = "Status is required" });
                }

                // Get the current user for API calls
                var currentUser = _httpContextAccessor.HttpContext?.User;
                if (currentUser == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                _logger.LogInformation("🔍 Getting assigned tasks for UpdateTaskStatus");

                // Get the existing task first to ensure we have all required fields
                var existingTasksResponse = await _apiClient.GetAsync<
                    PaginatedResponse<ContractorTaskDto>
                >($"/api/contractors/tasks/assigned", currentUser);

                _logger.LogInformation(
                    "📡 API response received: {Response}",
                    existingTasksResponse != null ? "Success" : "Null"
                );

                if (existingTasksResponse == null || existingTasksResponse.Data == null)
                {
                    _logger.LogWarning("❌ No tasks returned from API");
                    return StatusCode(500, new { error = "Failed to get task data" });
                }

                _logger.LogInformation(
                    "✅ Found {Count} assigned tasks",
                    existingTasksResponse.Data.Count
                );

                // Find the specific task
                var taskToUpdate = existingTasksResponse.Data.FirstOrDefault(t =>
                    t.TaskId == request.TaskId
                );
                if (taskToUpdate == null)
                {
                    _logger.LogWarning(
                        "❌ Task {TaskId} not found in assigned tasks",
                        request.TaskId
                    );
                    _logger.LogWarning(
                        "📋 Available task IDs: {TaskIds}",
                        string.Join(", ", existingTasksResponse.Data.Select(t => t.TaskId))
                    );
                    return NotFound(new { error = "Task not found" });
                }

                _logger.LogInformation("✅ Found task to update: {TaskName}", taskToUpdate.Name);

                // Update only the status field
                taskToUpdate.Status = request.Status;

                // Update the task using the API client
                var updatedTask = await _apiClient.PutAsync<ProjectTaskDto>(
                    $"/api/contractors/update/project/task/{request.TaskId}",
                    taskToUpdate,
                    currentUser
                );

                if (updatedTask == null)
                {
                    return StatusCode(500, new { error = "Failed to update task" });
                }

                _logger.LogInformation(
                    "Task {TaskId} status updated to {Status} by contractor {UserId}",
                    request.TaskId,
                    request.Status,
                    userId
                );

                return Json(new { success = true, task = updatedTask });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Submit a progress report for a task
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitProgressReport([FromBody] ProgressReportDto report)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                if (string.IsNullOrEmpty(report.TaskId))
                {
                    return BadRequest(new { error = "Task ID is required" });
                }

                if (string.IsNullOrEmpty(report.Description))
                {
                    return BadRequest(new { error = "Description is required" });
                }

                if (report.HoursWorked <= 0)
                {
                    return BadRequest(new { error = "Hours worked must be greater than 0" });
                }

                // Set submission details
                report.SubmittedBy = userId;
                report.SubmittedAt = DateTime.UtcNow;
                report.Status = "Submitted";

                var result = await _contractorService.SubmitProgressReportAsync(report);

                _logger.LogInformation(
                    "Progress report submitted for task {TaskId} by contractor {UserId}",
                    report.TaskId,
                    userId
                );

                return Json(new { success = true, report = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting progress report");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Request completion of a task
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RequestCompletion(
            [FromBody] RequestCompletionRequest request
        )
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                    return BadRequest(new { error = "Validation failed", errors });
                }

                var result = await _contractorService.RequestCompletionAsync(
                    request.TaskId,
                    request.Notes ?? "",
                    request.DocumentId
                );

                _logger.LogInformation(
                    "Completion requested for task {TaskId} by contractor {UserId}",
                    request.TaskId,
                    userId
                );

                return Json(new { success = true, result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting task completion");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Submit a completion report for a task
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitCompletionReport(
            [FromBody] CompletionReportDto report
        )
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                if (string.IsNullOrEmpty(report.TaskId))
                {
                    return BadRequest(new { error = "Task ID is required" });
                }

                if (string.IsNullOrEmpty(report.CompletionSummary))
                {
                    return BadRequest(new { error = "Completion summary is required" });
                }

                if (report.FinalHours <= 0)
                {
                    return BadRequest(new { error = "Final hours must be greater than 0" });
                }

                // Set submission details
                report.SubmittedBy = userId;
                report.SubmittedAt = DateTime.UtcNow;
                report.Status = "Submitted";

                var result = await _contractorService.SubmitCompletionReportAsync(report);

                _logger.LogInformation(
                    "Completion report submitted for task {TaskId} by contractor {UserId}",
                    report.TaskId,
                    userId
                );

                return Json(
                    new
                    {
                        success = true,
                        report = result,
                        message = "Completion report submitted successfully",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting completion report");
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        error = ex.Message,
                        message = "Failed to submit completion report",
                    }
                );
            }
        }

        /// <summary>
        /// Get progress reports for a task
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProgressReports(string taskId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                var reports = await _contractorService.GetProgressReportsAsync(taskId);
                return Json(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress reports for task {TaskId}", taskId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get completion reports for a task
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCompletionReports(string taskId)
        {
            try
            {
                _logger.LogInformation(
                    "🔍 GetCompletionReports endpoint called for task {TaskId}",
                    taskId
                );

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found for completion reports request");
                    return Unauthorized(new { error = "User ID not found" });
                }

                var reports = await _contractorService.GetCompletionReportsAsync(taskId);
                _logger.LogInformation(
                    "Retrieved {Count} completion reports for task {TaskId}",
                    reports?.Count ?? 0,
                    taskId
                );

                return Json(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion reports for task {TaskId}", taskId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get project budget for a task
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTaskProjectBudget(string taskId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                var budget = await _contractorService.GetTaskProjectBudgetAsync(taskId);
                return Json(budget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project budget for task {TaskId}", taskId);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
