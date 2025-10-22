using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public class ContractorService : IContractorService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ContractorService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ContractorService(
            IApiClient apiClient,
            ILogger<ContractorService> logger,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _apiClient = apiClient;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ContractorDashboardViewModel> GetDashboardDataAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting dashboard data for contractor {UserId}", userId);

                // Get dashboard data from API
                _logger.LogInformation("Calling API endpoint: /api/contractors/dashboard");
                var currentUser = _httpContextAccessor.HttpContext?.User;
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
                        userId
                    );

                    // Try to reset circuit breaker and retry once
                    _logger.LogInformation("Attempting to reset circuit breaker and retry...");
                    if (_apiClient is ApiClient apiClient)
                    {
                        apiClient.ResetCircuitBreaker("/api/contractors/dashboard");

                        // Retry the request
                        dashboardData = await _apiClient.GetAsync<object>(
                            "/api/contractors/dashboard",
                            currentUser
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
                            contractorTask.Status == "In Progress"
                            || contractorTask.Status == "InProgress";
                        contractorTask.CanRequestCompletion =
                            contractorTask.Status == "In Progress"
                            || contractorTask.Status == "InProgress";

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
                    userId
                );
                return new ContractorDashboardViewModel();
            }
        }

        public async Task<List<ContractorTaskDto>> GetAssignedTasksAsync(string userId)
        {
            try
            {
                var currentUser = _httpContextAccessor.HttpContext?.User;
                var tasks = await _apiClient.GetAsync<List<ProjectTaskDto>>(
                    "/api/contractors/tasks/assigned",
                    currentUser
                );
                if (tasks == null)
                    return new List<ContractorTaskDto>();

                return tasks
                    .Select(task => new ContractorTaskDto
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
                        IsOverdue = task.DueDate < DateTime.UtcNow && task.Status != "Completed",
                        DaysUntilDue = Math.Max(0, (task.DueDate - DateTime.UtcNow).Days),
                        StatusBadgeClass = GetStatusBadgeClass(task.Status),
                        CanSubmitProgress =
                            task.Status == "In Progress" || task.Status == "InProgress",
                        CanRequestCompletion =
                            task.Status == "In Progress" || task.Status == "InProgress",
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting assigned tasks for contractor {UserId}",
                    userId
                );
                return new List<ContractorTaskDto>();
            }
        }

        public async Task<ContractorTaskDto?> GetTaskWithProjectAsync(string taskId, string userId)
        {
            try
            {
                var currentUser = _httpContextAccessor.HttpContext?.User;
                var task = await _apiClient.GetAsync<ProjectTaskDto>(
                    $"/api/contractors/task/{taskId}",
                    currentUser
                );
                if (task == null)
                    return null;

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
                    CanSubmitProgress = task.Status == "In Progress" || task.Status == "InProgress",
                    CanRequestCompletion =
                        task.Status == "In Progress" || task.Status == "InProgress",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting task {TaskId} for contractor {UserId}",
                    taskId,
                    userId
                );
                return null;
            }
        }

        public async Task<ProgressReportDto> SubmitProgressReportAsync(
            ProgressReportDto report,
            string userId
        )
        {
            try
            {
                var currentUser = _httpContextAccessor.HttpContext?.User;
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

        public async Task<object> RequestCompletionAsync(
            string taskId,
            string notes,
            string? documentId,
            string userId
        )
        {
            try
            {
                var requestData = new { notes, documentId };
                var currentUser = _httpContextAccessor.HttpContext?.User;
                var result = await _apiClient.PutAsync<object>(
                    $"/api/contractors/task/{taskId}/request-completion",
                    requestData,
                    currentUser
                );
                return result ?? new { message = "Completion request submitted" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting completion for task {TaskId}", taskId);
                throw;
            }
        }

        public async Task<List<ProgressReportDto>> GetProgressReportsAsync(
            string taskId,
            string userId
        )
        {
            try
            {
                var currentUser = _httpContextAccessor.HttpContext?.User;
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

        public async Task<object> GetTaskProjectBudgetAsync(string taskId, string userId)
        {
            try
            {
                var currentUser = _httpContextAccessor.HttpContext?.User;
                var budget = await _apiClient.GetAsync<object>(
                    $"/api/contractors/task/{taskId}/project-budget",
                    currentUser
                );
                return budget
                    ?? new
                    {
                        projectId = "",
                        projectName = "Unknown",
                        budgetPlanned = 0m,
                    };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project budget for task {TaskId}", taskId);
                return new
                {
                    projectId = "",
                    projectName = "Unknown",
                    budgetPlanned = 0m,
                };
            }
        }

        private string GetStatusBadgeClass(string status)
        {
            return status.ToLower() switch
            {
                "pending" => "badge-secondary",
                "in progress" or "inprogress" => "badge-warning",
                "awaiting approval" => "badge-info",
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
