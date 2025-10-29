using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public class ContractorService : IContractorService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ContractorService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public ContractorService(
            IApiClient apiClient,
            ILogger<ContractorService> logger,
            ICurrentUserService currentUserService
        )
        {
            _apiClient = apiClient;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<ContractorDashboardViewModel> GetDashboardDataAsync()
        {
            try
            {
                var currentUserId = _currentUserService.GetCurrentUserId();
                _logger.LogInformation(
                    "Getting dashboard data for contractor {UserId}",
                    currentUserId
                );

                // Get dashboard data from API
                _logger.LogInformation("Calling API endpoint: /api/contractors/dashboard");
                var currentUser = _currentUserService.GetCurrentUser();
                var dashboardData = await _apiClient.GetAsync<object>(
                    "/api/contractors/dashboard",
                    currentUser
                );

                _logger.LogInformation(
                    "API response received: {Response}",
                    dashboardData != null ? "Success" : "Null"
                );

                if (dashboardData == null)
                {
                    _logger.LogWarning(
                        "No dashboard data returned for contractor {UserId}. This might be due to circuit breaker or API connectivity issues.",
                        currentUserId
                    );

                    // Try to reset circuit breaker and retry once
                    _logger.LogInformation("Attempting to reset circuit breaker and retry...");
                    if (_apiClient is ApiClient apiClient)
                    {
                        apiClient.ResetCircuitBreaker("/api/contractors/dashboard");

                        // Retry the request
                        dashboardData = await _apiClient.GetAsync<object>(
                            "/api/contractors/dashboard",
                            _currentUserService.GetCurrentUser()
                        );

                        if (dashboardData == null)
                        {
                            _logger.LogError(
                                "API call failed even after circuit breaker reset. Returning empty dashboard."
                            );
                            return new ContractorDashboardViewModel();
                        }
                    }
                    else
                    {
                        return new ContractorDashboardViewModel();
                    }
                }

                // Parse the response
                var json = JsonSerializer.Serialize(dashboardData);
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                var tasks = new List<ContractorTaskDto>();
                var projects = new Dictionary<string, ProjectDto>();
                var progressReportCounts = new Dictionary<string, int>();

                // Extract tasks
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

                        // Calculate computed fields
                        contractorTask.IsOverdue =
                            contractorTask.DueDate < DateTime.UtcNow
                            && contractorTask.Status != "Completed";
                        contractorTask.DaysUntilDue = Math.Max(
                            0,
                            (contractorTask.DueDate - DateTime.UtcNow).Days
                        );
                        contractorTask.StatusBadgeClass = GetStatusBadgeClass(
                            contractorTask.Status
                        );
                        contractorTask.CanSubmitProgress =
                            contractorTask.Status.Equals(
                                "In Progress",
                                StringComparison.OrdinalIgnoreCase
                            )
                            || contractorTask.Status.Equals(
                                "InProgress",
                                StringComparison.OrdinalIgnoreCase
                            )
                            || contractorTask.Status.Equals(
                                "In-Progress",
                                StringComparison.OrdinalIgnoreCase
                            );

                        contractorTask.CanRequestCompletion =
                            contractorTask.Status.Equals(
                                "In Progress",
                                StringComparison.OrdinalIgnoreCase
                            )
                            || contractorTask.Status.Equals(
                                "InProgress",
                                StringComparison.OrdinalIgnoreCase
                            )
                            || contractorTask.Status.Equals(
                                "In-Progress",
                                StringComparison.OrdinalIgnoreCase
                            );

                        tasks.Add(contractorTask);
                    }
                }

                // Extract projects
                if (data.TryGetProperty("projects", out var projectsElement))
                {
                    var projectList =
                        JsonSerializer.Deserialize<List<ProjectDto>>(projectsElement.GetRawText())
                        ?? new();
                    foreach (var project in projectList)
                    {
                        projects[project.ProjectId] = project;
                    }
                }

                // Extract progress reports
                if (data.TryGetProperty("progressReports", out var reportsElement))
                {
                    var reportList =
                        JsonSerializer.Deserialize<List<ProgressReportDto>>(
                            reportsElement.GetRawText()
                        ) ?? new();
                    progressReportCounts = reportList
                        .GroupBy(r => r.TaskId)
                        .ToDictionary(g => g.Key, g => g.Count());
                }

                // Update tasks with project info and progress report counts
                foreach (var task in tasks)
                {
                    if (projects.TryGetValue(task.ProjectId, out var project))
                    {
                        task.ProjectName = project.Name;
                        task.ProjectBudget = (decimal)project.BudgetPlanned;
                    }
                    if (progressReportCounts.TryGetValue(task.TaskId, out var count))
                    {
                        task.ProgressReportCount = count;
                    }
                }

                // Calculate stats
                var totalTasks = data.TryGetProperty("totalTasks", out var totalElement)
                    ? totalElement.GetInt32()
                    : tasks.Count;
                var completedTasks = data.TryGetProperty("completedTasks", out var completedElement)
                    ? completedElement.GetInt32()
                    : tasks.Count(t => t.Status == "Completed");
                var inProgressTasks = data.TryGetProperty(
                    "inProgressTasks",
                    out var inProgressElement
                )
                    ? inProgressElement.GetInt32()
                    : tasks.Count(t => t.Status == "In Progress" || t.Status == "InProgress");
                var overdueTasks = data.TryGetProperty("overdueTasks", out var overdueElement)
                    ? overdueElement.GetInt32()
                    : tasks.Count(t => t.IsOverdue);

                // Group tasks by projects
                var projectCards = CreateProjectCards(tasks, projects);

                return new ContractorDashboardViewModel
                {
                    AssignedTasks = tasks,
                    Projects = projectCards,
                    TaskProjects = projects,
                    TaskProgressReportCounts = progressReportCounts,
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    InProgressTasks = inProgressTasks,
                    OverdueTasks = overdueTasks,
                    PendingTasks = tasks.Count(t => t.Status == "Pending"),
                    AwaitingApprovalTasks = tasks.Count(t => t.Status == "Awaiting Approval"),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting dashboard data for contractor {UserId}",
                    _currentUserService.GetCurrentUserId()
                );
                return new ContractorDashboardViewModel();
            }
        }

        public async Task<List<ContractorTaskDto>> GetAssignedTasksAsync()
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                _logger.LogInformation("🔍 Getting assigned tasks for user");

                var response = await _apiClient.GetAsync<PaginatedResponse<ContractorTaskDto>>(
                    "/api/contractors/tasks/assigned",
                    currentUser
                );

                _logger.LogInformation(
                    "📡 API response received: {Response}",
                    response != null ? "Success" : "Null"
                );

                if (response == null || response.Data == null)
                {
                    _logger.LogWarning("❌ No tasks returned from API");
                    return new List<ContractorTaskDto>();
                }

                _logger.LogInformation("✅ Found {Count} assigned tasks", response.Data.Count);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting assigned tasks for contractor {UserId}",
                    _currentUserService.GetCurrentUserId()
                );
                return new List<ContractorTaskDto>();
            }
        }

        public async Task<ContractorTaskDto?> GetTaskWithProjectAsync(string taskId)
        {
            try
            {
                Console.WriteLine(
                    $"[ContractorService] 🔍 Getting task details for taskId: {taskId}"
                );
                var currentUser = _currentUserService.GetCurrentUser();
                Console.WriteLine(
                    $"[ContractorService] 👤 Current user: {currentUser?.Identity?.Name}"
                );

                Console.WriteLine(
                    $"[ContractorService] 📡 Calling API: /api/contractors/task/{taskId}"
                );
                var task = await _apiClient.GetAsync<ProjectTaskDto>(
                    $"/api/contractors/task/{taskId}",
                    currentUser
                );

                if (task == null)
                {
                    Console.WriteLine($"[ContractorService] ❌ Task not found in API response");
                    return null;
                }

                Console.WriteLine($"[ContractorService] ✅ Task found: {task.Name}");

                var project = await _apiClient.GetAsync<ProjectDto>(
                    $"/api/projectmanager/project/{task.ProjectId}",
                    currentUser
                );

                return new ContractorTaskDto
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
                    ProjectName = project?.Name ?? "Unknown Project",
                    ProjectBudget = (decimal)(project?.BudgetPlanned ?? 0),
                    IsOverdue = task.DueDate < DateTime.UtcNow && task.Status != "Completed",
                    DaysUntilDue = Math.Max(0, (task.DueDate - DateTime.UtcNow).Days),
                    StatusBadgeClass = GetStatusBadgeClass(task.Status),
                    CanSubmitProgress =
                        task.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase)
                        || task.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                        || task.Status.Equals("In-Progress", StringComparison.OrdinalIgnoreCase),
                    CanRequestCompletion =
                        task.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase)
                        || task.Status.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                        || task.Status.Equals("In-Progress", StringComparison.OrdinalIgnoreCase),
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[ContractorService] ❌ Error getting task details: {ex.Message}"
                );
                _logger.LogError(
                    ex,
                    "Error getting task {TaskId} for contractor {UserId}",
                    taskId,
                    _currentUserService.GetCurrentUserId()
                );
                return null;
            }
        }

        public async Task<ProgressReportDto> SubmitProgressReportAsync(ProgressReportDto report)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                var result = await _apiClient.PostAsync<ProgressReportDto>(
                    $"/api/contractors/task/{report.TaskId}/progress-report",
                    report,
                    currentUser
                );
                return result ?? report;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error submitting progress report for task {TaskId}",
                    report.TaskId
                );
                throw;
            }
        }

        public async Task<CompletionReportDto> SubmitCompletionReportAsync(
            CompletionReportDto report
        )
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                var result = await _apiClient.PutAsync<CompletionReportDto>(
                    $"/api/contractors/task/{report.TaskId}/request-completion",
                    report,
                    currentUser
                );
                return result ?? report;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error submitting completion report for task {TaskId}",
                    report.TaskId
                );
                throw;
            }
        }

        public async Task<List<CompletionReportDto>> GetCompletionReportsAsync(string taskId)
        {
            try
            {
                _logger.LogInformation(
                    "🔍 GetCompletionReportsAsync called for task {TaskId}",
                    taskId
                );
                var currentUser = _currentUserService.GetCurrentUser();
                _logger.LogInformation(
                    "🔍 Current user: {UserId}",
                    currentUser?.Identity?.Name ?? "null"
                );

                var reports = await _apiClient.GetAsync<List<CompletionReportDto>>(
                    $"/api/contractors/task/{taskId}/completion-reports",
                    currentUser
                );

                _logger.LogInformation(
                    "🔍 API client returned {Count} completion reports",
                    reports?.Count ?? 0
                );
                return reports ?? new List<CompletionReportDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion reports for task {TaskId}", taskId);
                return new List<CompletionReportDto>();
            }
        }

        public async Task<List<ProgressReportDto>> GetProgressReportsAsync(string taskId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                var reports = await _apiClient.GetAsync<List<ProgressReportDto>>(
                    $"/api/contractors/task/{taskId}/progress-reports",
                    currentUser
                );
                return reports ?? new List<ProgressReportDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress reports for task {TaskId}", taskId);
                return new List<ProgressReportDto>();
            }
        }

        public async Task<ProjectBudgetDto> GetTaskProjectBudgetAsync(string taskId)
        {
            try
            {
                var currentUser = _currentUserService.GetCurrentUser();
                var budget = await _apiClient.GetAsync<ProjectBudgetDto>(
                    $"/api/contractors/task/{taskId}/project-budget",
                    currentUser
                );
                return budget
                    ?? new ProjectBudgetDto
                    {
                        ProjectId = "",
                        ProjectName = "Unknown",
                        BudgetPlanned = 0m,
                    };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project budget for task {TaskId}", taskId);
                return new ProjectBudgetDto
                {
                    ProjectId = "",
                    ProjectName = "Unknown",
                    BudgetPlanned = 0m,
                };
            }
        }

        private string GetStatusBadgeClass(string? status)
        {
            var s = status?.Trim().ToLowerInvariant();
            return s switch
            {
                "pending" => "badge-secondary",
                "in progress" or "inprogress" => "badge-warning",
                "awaiting approval" or "awaitingapproval" => "badge-info",
                "completed" => "badge-success",
                "overdue" => "badge-danger",
                _ => "badge-light",
            };
        }

        private List<ContractorProjectCardDto> CreateProjectCards(
            List<ContractorTaskDto> tasks,
            Dictionary<string, ProjectDto> projects
        )
        {
            var projectCards = new List<ContractorProjectCardDto>();

            // Group tasks by project
            var tasksByProject = tasks.GroupBy(t => t.ProjectId).ToList();

            foreach (var projectGroup in tasksByProject)
            {
                var projectId = projectGroup.Key;
                var projectTasks = projectGroup.ToList();
                var project = projects.ContainsKey(projectId) ? projects[projectId] : null;

                if (project == null)
                    continue;

                var projectCard = new ContractorProjectCardDto
                {
                    ProjectId = projectId,
                    ProjectName = project.Name,
                    ProjectBudget = (decimal)project.BudgetPlanned,
                    Tasks = projectTasks,
                    TotalTasks = projectTasks.Count,
                    CompletedTasks = projectTasks.Count(t => t.Status == "Completed"),
                    InProgressTasks = projectTasks.Count(t =>
                        t.Status == "In Progress" || t.Status == "InProgress"
                    ),
                    OverdueTasks = projectTasks.Count(t => t.IsOverdue),
                    OverallProgress = CalculateProjectProgress(projectTasks),
                };

                projectCards.Add(projectCard);
            }

            return projectCards;
        }

        private int CalculateProjectProgress(List<ContractorTaskDto> tasks)
        {
            if (!tasks.Any())
                return 0;

            var totalProgress = tasks.Sum(t => t.Progress);
            return (int)(totalProgress / tasks.Count);
        }
    }
}
