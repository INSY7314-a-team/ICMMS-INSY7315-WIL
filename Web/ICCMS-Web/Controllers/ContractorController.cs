using System.Security.Claims;
using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Contractor,Tester")]
    public class ContractorController : Controller
    {
        private readonly IContractorService _contractorService;
        private readonly ILogger<ContractorController> _logger;
        private readonly IApiClient _apiClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ContractorController(
            IContractorService contractorService,
            ILogger<ContractorController> logger,
            IApiClient apiClient,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _contractorService = contractorService;
            _logger = logger;
            _apiClient = apiClient;
            _httpContextAccessor = httpContextAccessor;
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
                    return View("Error");
                }

                _logger.LogInformation("Loading contractor dashboard for user {UserId}", userId);

                var dashboardData = await _contractorService.GetDashboardDataAsync(userId);

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
                return View("Error");
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

                var tasks = await _contractorService.GetAssignedTasksAsync(userId);
                return Json(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting assigned tasks");
                return StatusCode(500, new { error = ex.Message });
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                var task = await _contractorService.GetTaskWithProjectAsync(taskId, userId);
                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }

                return Json(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task details for {TaskId}", taskId);
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

                var result = await _contractorService.SubmitProgressReportAsync(report, userId);

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
        public async Task<IActionResult> RequestCompletion([FromBody] object requestData)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User ID not found" });
                }

                // Parse request data
                var json = System.Text.Json.JsonSerializer.Serialize(requestData);
                var data =
                    System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

                var taskId = data.GetProperty("taskId").GetString();
                var notes = data.GetProperty("notes").GetString() ?? "";
                var documentId = data.TryGetProperty("documentId", out var docElement)
                    ? docElement.GetString()
                    : null;

                if (string.IsNullOrEmpty(taskId))
                {
                    return BadRequest(new { error = "Task ID is required" });
                }

                var result = await _contractorService.RequestCompletionAsync(
                    taskId,
                    notes,
                    documentId,
                    userId
                );

                _logger.LogInformation(
                    "Completion requested for task {TaskId} by contractor {UserId}",
                    taskId,
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

                var reports = await _contractorService.GetProgressReportsAsync(taskId, userId);
                return Json(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress reports for task {TaskId}", taskId);
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

                var budget = await _contractorService.GetTaskProjectBudgetAsync(taskId, userId);
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
