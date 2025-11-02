using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

                // Get tasks for this project first - use the same data source as dashboard
                _logger.LogInformation(
                    "🔍 Making API call to get dashboard data for project {ProjectId}",
                    projectId
                );

                // Use the same endpoint as dashboard to ensure consistency
                var dashboardData = await _apiClient.GetAsync<object>(
                    "/api/contractors/dashboard",
                    currentUser
                );

                if (dashboardData == null)
                {
                    _logger.LogError(
                        "❌ Dashboard API call failed for project {ProjectId}",
                        projectId
                    );
                    ViewBag.ErrorMessage = "Failed to load project data. Please try again later.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                // Parse the dashboard response to extract tasks
                var json = JsonSerializer.Serialize(dashboardData);
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                var tasks = new List<ContractorTaskDto>();
                if (data.TryGetProperty("tasks", out var tasksElement))
                {
                    var taskList =
                        JsonSerializer.Deserialize<List<ProjectTaskDto>>(tasksElement.GetRawText())
                        ?? new();
                    foreach (var task in taskList)
                    {
                        var contractorTask = new ContractorTaskDto
                        {
                            TaskId = task.TaskId,
                            ProjectId = task.ProjectId,
                            Name = task.Name,
                            Description = task.Description,
                            AssignedTo = task.AssignedTo,
                            Priority = task.Priority,
                            Status = task.Status,
                            StartDate = task.StartDate,
                            DueDate = task.DueDate,
                            CompletedDate = task.CompletedDate,
                            Progress = task.Progress,
                            EstimatedHours = task.EstimatedHours,
                            ActualHours = task.ActualHours,
                        };

                        // Calculate IsOverdue before adding to the list
                        contractorTask.IsOverdue =
                            task.DueDate < DateTime.UtcNow && task.Status != "Completed";

                        tasks.Add(contractorTask);
                    }
                }

                var projects = new Dictionary<string, ProjectDto>();
                if (data.TryGetProperty("projects", out var projectsElement))
                {
                    var projectList =
                        JsonSerializer.Deserialize<List<ProjectDto>>(projectsElement.GetRawText())
                        ?? new();
                    foreach (var project in projectList)
                    {
                        if (project != null && !string.IsNullOrEmpty(project.ProjectId))
                        {
                            projects[project.ProjectId] = project;
                        }
                    }
                }

                _logger.LogInformation(
                    "📋 Extracted {Count} tasks from dashboard data",
                    tasks.Count
                );

                if (tasks == null || !tasks.Any())
                {
                    _logger.LogWarning("❌ No tasks found for user {UserId}", userId);
                    ViewBag.ErrorMessage = "No tasks found.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                // Log all project IDs for debugging
                var allProjectIds = tasks.Select(t => t.ProjectId).Distinct().ToList();
                _logger.LogInformation(
                    "🏗️ Available project IDs: {ProjectIds}",
                    string.Join(", ", allProjectIds)
                );

                var projectTasks = tasks.Where(t => t.ProjectId == projectId).ToList();
                _logger.LogInformation(
                    "🎯 Found {ProjectTaskCount} tasks for project {ProjectId}",
                    projectTasks.Count,
                    projectId
                );

                if (!projectTasks.Any())
                {
                    _logger.LogWarning(
                        "❌ No tasks found for project {ProjectId} and user {UserId}. Available projects: {AvailableProjects}",
                        projectId,
                        userId,
                        string.Join(", ", allProjectIds)
                    );
                    ViewBag.ErrorMessage =
                        $"No tasks found for this project. Available projects: {string.Join(", ", allProjectIds)}";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                // Get the first task to extract project information
                var firstTask = projectTasks.FirstOrDefault();
                if (firstTask == null)
                {
                    _logger.LogError(
                        "❌ No valid task found in project tasks for project {ProjectId}",
                        projectId
                    );
                    ViewBag.ErrorMessage = "No valid task data found.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }

                _logger.LogInformation(
                    "✅ First task found: {TaskName} (ID: {TaskId})",
                    firstTask.Name,
                    firstTask.TaskId
                );

                // Safely extract project information with null checks
                var projectInfo = projects.TryGetValue(projectId, out var proj) ? proj : null;

                var extractedProjectId = projectInfo?.ProjectId ?? firstTask.ProjectId ?? projectId;
                var projectName =
                    projectInfo?.Name
                    ?? $"Project {extractedProjectId.Substring(0, Math.Min(8, extractedProjectId.Length))}";
                var projectStatus = projectInfo?.Status ?? "Active"; // Fallback to Active
                var projectDescription =
                    projectInfo?.Description ?? $"Project tasks for {extractedProjectId}";
                var clientName = "Client"; // Placeholder - Name not available in ProjectDto
                var projectManagerName = "Project Manager"; // Placeholder - Name not available in ProjectDto
                var budgetPlanned = projectInfo?.BudgetPlanned ?? 0;
                var budgetActual = projectInfo?.BudgetActual ?? 0;

                _logger.LogInformation(
                    "🔍 Project Info - Name: {Name}, Status: {Status}",
                    projectName,
                    projectStatus
                );

                // Safely calculate dates with null checks
                DateTime minStartDate;
                DateTime maxDueDate;

                try
                {
                    var startDates = projectTasks
                        .Where(t => t.StartDate != default(DateTime))
                        .Select(t => t.StartDate)
                        .ToList();
                    var dueDates = projectTasks
                        .Where(t => t.DueDate != default(DateTime))
                        .Select(t => t.DueDate)
                        .ToList();

                    _logger.LogInformation(
                        "📅 Found {StartDateCount} start dates and {DueDateCount} due dates",
                        startDates.Count,
                        dueDates.Count
                    );

                    minStartDate = startDates.Any() ? startDates.Min() : DateTime.UtcNow;
                    maxDueDate = dueDates.Any() ? dueDates.Max() : DateTime.UtcNow.AddDays(30);

                    _logger.LogInformation(
                        "📅 Date range: {StartDate} to {EndDate}",
                        minStartDate,
                        maxDueDate
                    );
                }
                catch (Exception dateEx)
                {
                    _logger.LogError(
                        dateEx,
                        "❌ Error calculating dates for project {ProjectId}",
                        projectId
                    );
                    // Use default dates if calculation fails
                    minStartDate = DateTime.UtcNow;
                    maxDueDate = DateTime.UtcNow.AddDays(30);
                }

                // Create ProjectDetailDto from the task data
                try
                {
                    _logger.LogInformation(
                        "🏗️ Creating ProjectDetailDto for project {ProjectId}",
                        extractedProjectId
                    );

                    var projectDetails = new ProjectDetailDto
                    {
                        ProjectId = extractedProjectId,
                        Name = projectName,
                        Description = projectDescription,
                        BudgetPlanned = (decimal)budgetPlanned,
                        BudgetActual = (decimal)budgetActual,
                        Status = projectStatus,
                        StartDate = minStartDate,
                        EndDatePlanned = maxDueDate,
                        EndDateActual = projectInfo?.EndDateActual,
                        CompletionPhase = projectInfo?.CompletionPhase,
                        ClientId = projectInfo?.ClientId,
                        ClientName = clientName,
                        ProjectManagerId = projectInfo?.ProjectManagerId,
                        ProjectManagerName = projectManagerName,
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
                        "✅ ProjectDetailDto created successfully - Tasks: {TotalTasks}, Completed: {CompletedTasks}, InProgress: {InProgressTasks}, Pending: {PendingTasks}, Overdue: {OverdueTasks}, Progress: {OverallProgress}%",
                        projectDetails.TotalTasks,
                        projectDetails.CompletedTasks,
                        projectDetails.InProgressTasks,
                        projectDetails.PendingTasks,
                        projectDetails.OverdueTasks,
                        projectDetails.OverallProgress
                    );

                    return View(projectDetails);
                }
                catch (Exception dtoEx)
                {
                    _logger.LogError(
                        dtoEx,
                        "❌ Error creating ProjectDetailDto for project {ProjectId}",
                        projectId
                    );
                    ViewBag.ErrorMessage =
                        "Failed to create project details. Please try again later.";
                    return View(
                        "Error",
                        new ErrorViewModel { RequestId = HttpContext.TraceIdentifier }
                    );
                }
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

        public async Task<IActionResult> TaskDetails(string taskId)
        {
            try
            {
                _logger.LogInformation("🔍 TaskDetails page called for taskId: {TaskId}", taskId);

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("❌ User ID not found");
                    return Unauthorized(new { error = "User ID not found" });
                }

                var task = await _contractorService.GetTaskWithProjectAsync(taskId);
                if (task == null)
                {
                    _logger.LogWarning("❌ Task not found for taskId: {TaskId}", taskId);
                    return NotFound(new { error = "Task not found" });
                }

                // Get additional project information using contractor endpoints
                var projects = await _apiClient.GetAsync<List<ProjectDto>>(
                    "/api/contractors/projects",
                    User
                );

                // Find the specific project from the contractor's projects
                ProjectDto? specificProject = projects?.FirstOrDefault(p =>
                    p.ProjectId == task.ProjectId
                );

                // Get client information
                UserDto? client = null;
                if (!string.IsNullOrEmpty(specificProject?.ClientId))
                {
                    try
                    {
                        client = await _apiClient.GetAsync<UserDto>(
                            $"/api/users/{specificProject.ClientId}",
                            User
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Could not fetch client info for project {ProjectId}",
                            task.ProjectId
                        );
                    }
                }

                // Get project manager information
                UserDto? projectManager = null;
                if (!string.IsNullOrEmpty(specificProject?.ProjectManagerId))
                {
                    try
                    {
                        projectManager = await _apiClient.GetAsync<UserDto>(
                            $"/api/users/{specificProject.ProjectManagerId}",
                            User
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Could not fetch project manager info for project {ProjectId}",
                            task.ProjectId
                        );
                    }
                }

                var viewModel = new ContractorTaskDetailViewModel
                {
                    Task = task,
                    Client = client,
                    ProjectManager = projectManager,
                    Project = specificProject,
                };

                _logger.LogInformation("✅ Task found: {TaskName}", task.Name);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error loading task details page for taskId: {TaskId}",
                    taskId
                );
                return StatusCode(500, new { error = "Internal server error" });
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

                _logger.LogInformation(
                    "🔍 Updating task status for task {TaskId} to {Status}",
                    request.TaskId,
                    request.Status
                );

                // First, get the existing task to update it properly
                var existingTask = await _apiClient.GetAsync<ProjectTaskDto>(
                    $"/api/contractors/task/{request.TaskId}",
                    currentUser
                );

                if (existingTask == null)
                {
                    _logger.LogWarning("❌ Task {TaskId} not found", request.TaskId);
                    return NotFound(new { error = "Task not found" });
                }

                // Validate task start conditions
                if (
                    request.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase)
                    || request.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                    || request.Status.Equals("In-Progress", StringComparison.OrdinalIgnoreCase)
                )
                {
                    // Check if task is currently in Pending status
                    if (!existingTask.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "❌ Cannot start task {TaskId} - current status is {CurrentStatus}, must be Pending",
                            request.TaskId,
                            existingTask.Status
                        );
                        return BadRequest(
                            new
                            {
                                error = "Task can only be started if it's currently in Pending status",
                                currentStatus = existingTask.Status,
                            }
                        );
                    }

                    // Get project information to check project status
                    var projects = await _apiClient.GetAsync<List<ProjectDto>>(
                        "/api/contractors/projects",
                        currentUser
                    );

                    var project = projects?.FirstOrDefault(p =>
                        p.ProjectId == existingTask.ProjectId
                    );
                    if (project == null)
                    {
                        _logger.LogWarning(
                            "❌ Project {ProjectId} not found for task {TaskId}",
                            existingTask.ProjectId,
                            request.TaskId
                        );
                        return BadRequest(new { error = "Project information not found" });
                    }

                    // Check if project status allows task starting
                    var projectStatus = project.Status?.Trim();
                    if (
                        !projectStatus.Equals("Active", StringComparison.OrdinalIgnoreCase)
                        && !projectStatus.Equals("Maintenance", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        _logger.LogWarning(
                            "❌ Cannot start task {TaskId} - project {ProjectId} status is {ProjectStatus}, must be Active or Maintenance",
                            request.TaskId,
                            existingTask.ProjectId,
                            projectStatus
                        );
                        return BadRequest(
                            new
                            {
                                error = "Task can only be started if the project status is Active or Maintenance",
                                projectStatus = projectStatus,
                            }
                        );
                    }

                    _logger.LogInformation(
                        "✅ Task start validation passed - Task: {TaskStatus}, Project: {ProjectStatus}",
                        existingTask.Status,
                        projectStatus
                    );
                }

                // Update only the status field
                existingTask.Status = request.Status;

                // Update the task using the API client
                var updatedTask = await _apiClient.PutAsync<object>(
                    $"/api/contractors/update/project/task/{request.TaskId}",
                    existingTask,
                    currentUser
                );

                if (updatedTask == null)
                {
                    _logger.LogWarning(
                        "❌ Failed to update task {TaskId} - API returned null",
                        request.TaskId
                    );
                    return StatusCode(500, new { error = "Failed to update task" });
                }

                _logger.LogInformation(
                    "✅ Task {TaskId} status updated to {Status} by contractor {UserId}",
                    request.TaskId,
                    request.Status,
                    userId
                );

                return Json(new { success = true, task = updatedTask });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating task status for task {TaskId}",
                    request.TaskId
                );
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Submit a progress report for a task
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitProgressReport(
            [FromForm] SubmitProgressReportViewModel viewModel
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
                    return View(viewModel);
                }

                var report = new ProgressReportDto
                {
                    TaskId = viewModel.TaskId,
                    ProjectId = viewModel.ProjectId,
                    Description = viewModel.Description,
                    HoursWorked = viewModel.HoursWorked,
                    ProgressPercentage = viewModel.ProgressPercentage,
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow,
                    Status = "Submitted",
                };

                if (viewModel.AttachmentFile != null)
                {
                    _logger.LogInformation(
                        "Attempting to upload file: {FileName}, Size: {FileSize}",
                        viewModel.AttachmentFile.FileName,
                        viewModel.AttachmentFile.Length
                    );

                    var document = await UploadFileAsync(
                        viewModel.AttachmentFile,
                        viewModel.ProjectId,
                        $"Progress report for {viewModel.TaskName}"
                    );

                    if (document != null)
                    {
                        _logger.LogInformation(
                            "File upload successful. Document ID: {DocumentId}, File URL: {FileUrl}",
                            document.DocumentId,
                            document.FileUrl
                        );
                        report.AttachedDocumentIds.Add(document.FileUrl);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "File upload failed for file: {FileName}",
                            viewModel.AttachmentFile.FileName
                        );
                        ModelState.AddModelError(
                            "AttachmentFile",
                            "Failed to upload the attached file. Please try again."
                        );
                        return View(viewModel);
                    }
                }

                await _contractorService.SubmitProgressReportAsync(report);

                return RedirectToAction("ProjectDetail", new { projectId = viewModel.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting progress report");
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred while submitting the report."
                );
                return View(viewModel);
            }
        }

        /// <summary>
        /// Request completion of a task
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCompletion(
            [FromForm] RequestCompletionViewModel viewModel
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
                    return View(viewModel);
                }

                var report = new CompletionReportDto
                {
                    TaskId = viewModel.TaskId,
                    ProjectId = viewModel.ProjectId,
                    CompletionSummary = viewModel.CompletionSummary,
                    FinalHours = viewModel.FinalHours,
                    AmountSpent = viewModel.AmountSpent,
                    QualityCheck = viewModel.QualityCheck,
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow,
                    CompletionDate = DateTime.UtcNow,
                    Status = "Submitted",
                };

                if (viewModel.AttachmentFile != null)
                {
                    _logger.LogInformation(
                        "Attempting to upload file: {FileName}, Size: {FileSize}",
                        viewModel.AttachmentFile.FileName,
                        viewModel.AttachmentFile.Length
                    );

                    var document = await UploadFileAsync(
                        viewModel.AttachmentFile,
                        viewModel.ProjectId,
                        $"Completion evidence for {viewModel.TaskName}"
                    );

                    if (document != null)
                    {
                        _logger.LogInformation(
                            "File upload successful. Document ID: {DocumentId}, File URL: {FileUrl}",
                            document.DocumentId,
                            document.FileUrl
                        );
                        report.AttachedDocumentIds.Add(document.FileUrl);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "File upload failed for file: {FileName}",
                            viewModel.AttachmentFile.FileName
                        );
                        ModelState.AddModelError(
                            "AttachmentFile",
                            "Failed to upload the attached file. Please try again."
                        );
                        return View(viewModel);
                    }
                }

                await _contractorService.SubmitCompletionReportAsync(report);

                return RedirectToAction("ProjectDetail", new { projectId = viewModel.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting task completion");
                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred while requesting completion."
                );
                return View(viewModel);
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

        /// <summary>
        /// Displays the form to submit a progress report for a task.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SubmitProgressReport(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                _logger.LogWarning("SubmitProgressReport GET called with no taskId.");
                return BadRequest("Task ID is required.");
            }

            var task = await _contractorService.GetTaskWithProjectAsync(taskId);
            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for progress report.", taskId);
                return NotFound();
            }

            var viewModel = new SubmitProgressReportViewModel
            {
                TaskId = task.TaskId,
                TaskName = task.Name,
                ProjectId = task.ProjectId,
                ProjectName = task.ProjectName,
            };

            return View(viewModel);
        }

        /// <summary>
        /// Displays the form to request completion for a task.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RequestCompletion(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                _logger.LogWarning("RequestCompletion GET called with no taskId.");
                return BadRequest("Task ID is required.");
            }

            var task = await _contractorService.GetTaskWithProjectAsync(taskId);
            if (task == null)
            {
                _logger.LogWarning(
                    "Task with ID {TaskId} not found for completion request.",
                    taskId
                );
                return NotFound();
            }

            var viewModel = new RequestCompletionViewModel
            {
                TaskId = task.TaskId,
                TaskName = task.Name,
                ProjectId = task.ProjectId,
                ProjectName = task.ProjectName,
            };

            return View(viewModel);
        }

        private async Task<DocumentDto?> UploadFileAsync(
            IFormFile file,
            string projectId,
            string description
        )
        {
            try
            {
                // Get the current user for authentication
                var currentUser = _httpContextAccessor.HttpContext?.User;
                if (currentUser == null)
                {
                    _logger.LogError("No authenticated user found for file upload");
                    return null;
                }

                // Get the authentication token
                var token = currentUser.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError(
                        "No Firebase token found for file upload. Available claims: {Claims}",
                        string.Join(", ", currentUser.Claims.Select(c => c.Type))
                    );
                    return null;
                }

                _logger.LogInformation(
                    "Found Firebase token for file upload. Token length: {TokenLength}",
                    token.Length
                );

                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                content.Add(new StreamContent(fileStream), "file", file.FileName);
                content.Add(new StringContent(projectId), "projectId");
                content.Add(new StringContent(description), "description");

                // Create authenticated request
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_apiBaseUrl}/api/documents/upload"
                )
                {
                    Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                    Content = content,
                };

                _logger.LogInformation(
                    "Sending file upload request to: {Url}",
                    $"{_apiBaseUrl}/api/documents/upload"
                );
                var response = await _httpClient.SendAsync(request);
                _logger.LogInformation(
                    "File upload response status: {StatusCode}",
                    response.StatusCode
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<DocumentDto>(
                        responseString,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "File upload failed with status code {StatusCode}: {Reason}. Response: {ErrorContent}",
                        response.StatusCode,
                        response.ReasonPhrase,
                        errorContent
                    );
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during file upload");
                return null;
            }
        }
    }
}
