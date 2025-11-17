using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ICCMS_API.Models;
using ICCMS_API.Models.ProjectDetail;
using Microsoft.Extensions.Logging;

namespace ICCMS_API.Services;

public class ProjectDetailService : IProjectDetailService
{
    private readonly IFirebaseService _firebaseService;
    private readonly ILogger<ProjectDetailService> _logger;

    public ProjectDetailService(
        IFirebaseService firebaseService,
        ILogger<ProjectDetailService> logger
    )
    {
        _firebaseService = firebaseService;
        _logger = logger;
    }

    public Task<ProjectDetailDataDto> BuildClientProjectDetailAsync(
        Project project,
        string clientId
    )
    {
        return BuildProjectDetailAsync(
            project,
            new ProjectDetailOptions
            {
                IncludeRatings = true,
                RatingClientId = clientId,
                IncludeManagerExtras = false,
            }
        );
    }

    public Task<ProjectDetailDataDto> BuildManagerProjectDetailAsync(Project project)
    {
        return BuildProjectDetailAsync(
            project,
            new ProjectDetailOptions
            {
                IncludeRatings = false,
                RatingClientId = null,
                IncludeManagerExtras = true,
            }
        );
    }

    private async Task<ProjectDetailDataDto> BuildProjectDetailAsync(
        Project project,
        ProjectDetailOptions options
    )
    {
        var projectId = project.ProjectId;

        var phasesTask = _firebaseService.GetCollectionAsync<Phase>("phases");
        var tasksTask = _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
        var progressReportsTask = _firebaseService.GetCollectionAsync<ProgressReport>(
            "progressReports"
        );
        var maintenanceRequestsTask = _firebaseService.GetCollectionAsync<MaintenanceRequest>(
            "maintenanceRequests"
        );
        var quotationsTask = _firebaseService.GetCollectionAsync<Quotation>("quotations");
        var invoicesTask = _firebaseService.GetCollectionAsync<Invoice>("invoices");
        var estimatesTask = _firebaseService.GetCollectionAsync<Estimate>("estimates");
        var completionReportsTask = _firebaseService.GetCollectionAsync<CompletionReport>(
            "completionReports"
        );
        var ratingSubmissionsTask = options.IncludeRatings
            ? _firebaseService.GetCollectionAsync<RatingSubmission>("ratingSubmissions")
            : Task.FromResult(new List<RatingSubmission>());

        var tasks = new List<Task>
        {
            phasesTask,
            tasksTask,
            progressReportsTask,
            maintenanceRequestsTask,
            quotationsTask,
            invoicesTask,
            estimatesTask,
            completionReportsTask,
            ratingSubmissionsTask,
        };

        await Task.WhenAll(tasks);

        var phases = (await phasesTask).Where(p => p.ProjectId == projectId).ToList();
        var projectTasks = (await tasksTask).Where(t => t.ProjectId == projectId).ToList();
        var progressReports = (await progressReportsTask)
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToList();
        var maintenanceRequests = (await maintenanceRequestsTask)
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        var quotations = (await quotationsTask)
            .Where(q => q.ProjectId == projectId)
            .OrderByDescending(q => q.CreatedAt)
            .ToList();
        var invoices = (await invoicesTask)
            .Where(i => i.ProjectId == projectId)
            .OrderByDescending(i => i.IssuedDate)
            .ToList();
        var estimates = (await estimatesTask)
            .Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.CreatedAt)
            .ToList();
        var completionReports = (await completionReportsTask)
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.SubmittedAt)
            .ToList();
        var ratingSubmissions = await ratingSubmissionsTask;

        var taskIds = projectTasks
            .Select(t => t.TaskId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tasksByPhase = GroupByKey(
            projectTasks,
            t => string.IsNullOrWhiteSpace(t.PhaseId) ? "unassigned" : t.PhaseId
        );
        var tasksByStatus = GroupByKey(projectTasks, t => (t.Status ?? "Unknown").Trim());
        var tasksByContractor = GroupByKey(
            projectTasks.Where(t => !string.IsNullOrWhiteSpace(t.AssignedTo)),
            t => t.AssignedTo
        );

        var contractorIds = projectTasks
            .Where(t => !string.IsNullOrWhiteSpace(t.AssignedTo))
            .Select(t => t.AssignedTo)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var contractorMap = await LoadUsersMapAsync(contractorIds);

        var phaseSummaries = new Dictionary<string, PhaseWithTasksDto>(
            StringComparer.OrdinalIgnoreCase
        );
        foreach (var phase in phases)
        {
            var phaseTasks = tasksByPhase.TryGetValue(phase.PhaseId, out var list)
                ? list
                : new List<ProjectTask>();
            phaseSummaries[phase.PhaseId] = new PhaseWithTasksDto
            {
                Phase = phase,
                Tasks = phaseTasks,
                TaskCount = phaseTasks.Count,
                CompletedTaskCount = phaseTasks.Count(t =>
                    string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                ),
                Progress = phaseTasks.Any()
                    ? (int)Math.Round(phaseTasks.Average(t => t.Progress))
                    : 0,
            };
        }

        if (tasksByPhase.TryGetValue("unassigned", out var unassigned))
        {
            phaseSummaries["unassigned"] = new PhaseWithTasksDto
            {
                Phase = new Phase
                {
                    PhaseId = "unassigned",
                    Name = "Unassigned Tasks",
                    ProjectId = projectId,
                    Status = "Pending",
                },
                Tasks = unassigned,
                TaskCount = unassigned.Count,
                CompletedTaskCount = unassigned.Count(t =>
                    string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                ),
                Progress = unassigned.Any()
                    ? (int)Math.Round(unassigned.Average(t => t.Progress))
                    : 0,
            };
        }

        var completedTasks = projectTasks
            .Where(t => string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var overdueTasks = projectTasks
            .Where(t =>
                t.DueDate < DateTime.UtcNow
                && !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase)
            )
            .ToList();
        var pendingTasks = projectTasks
            .Where(t => string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var statistics = BuildStatistics(projectTasks, phases, tasksByStatus, phaseSummaries);

        var dto = new ProjectDetailDataDto
        {
            Project = project,
            Phases = phases,
            Tasks = projectTasks,
            ProgressReports = progressReports,
            MaintenanceRequests = maintenanceRequests,
            Quotations = quotations,
            Invoices = invoices,
            Estimates = options.IncludeManagerExtras ? estimates : new List<Estimate>(),
            CompletionReports = options.IncludeManagerExtras
                ? completionReports
                : new List<CompletionReport>(),
        };

        dto.TasksByPhase = tasksByPhase;
        dto.TasksByStatus = tasksByStatus;
        dto.TasksByContractor = tasksByContractor;
        dto.PhaseSummaries = phaseSummaries;
        dto.CompletedTasks = completedTasks;
        dto.OverdueTasks = overdueTasks;
        dto.PendingTasks = pendingTasks;
        dto.Statistics = statistics;
        dto.ContractorMap = contractorMap;

        if (options.IncludeManagerExtras)
        {
            dto.PendingProgressReports = progressReports
                .Where(r =>
                    string.Equals(r.Status, "Pending", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(r.Status, "Submitted", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            var tasksAwaitingCompletion = projectTasks
                .Where(t =>
                    string.Equals(t.Status, "Awaiting Approval", StringComparison.OrdinalIgnoreCase)
                    && completionReports.Any(cr =>
                        string.Equals(cr.TaskId, t.TaskId, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(cr.Status, "Submitted", StringComparison.OrdinalIgnoreCase)
                    )
                )
                .ToList();

            dto.TasksAwaitingCompletion = tasksAwaitingCompletion;
            dto.Client = await _firebaseService.GetDocumentAsync<User>("users", project.ClientId);
        }

        if (options.IncludeRatings && !string.IsNullOrWhiteSpace(options.RatingClientId))
        {
            var ratedTasks = ratingSubmissions
                .Where(rs =>
                    taskIds.Contains(rs.TaskId)
                    && string.Equals(
                        rs.RatedBy,
                        options.RatingClientId,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .GroupBy(rs => rs.TaskId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, _ => true, StringComparer.OrdinalIgnoreCase);

            dto.RatedTasks = ratedTasks;
        }

        return dto;
    }

    private async Task<Dictionary<string, User>> LoadUsersMapAsync(IEnumerable<string> userIds)
    {
        var map = new ConcurrentDictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        var fetchTasks = userIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(async id =>
            {
                try
                {
                    var user = await _firebaseService.GetDocumentAsync<User>("users", id);
                    if (user != null)
                    {
                        map[id] = user;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load user {UserId}", id);
                }
            });

        await Task.WhenAll(fetchTasks);
        return map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, List<ProjectTask>> GroupByKey(
        IEnumerable<ProjectTask> source,
        Func<ProjectTask, string> keySelector
    )
    {
        return source
            .GroupBy(keySelector, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    private static ProjectStatisticsDto BuildStatistics(
        List<ProjectTask> tasks,
        List<Phase> phases,
        Dictionary<string, List<ProjectTask>> tasksByStatus,
        Dictionary<string, PhaseWithTasksDto> phaseSummaries
    )
    {
        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t =>
            string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase)
        );
        var inProgressTasks = tasks.Count(t =>
            string.Equals(t.Status, "In Progress", StringComparison.OrdinalIgnoreCase)
        );
        var pendingTasks = tasks.Count(t =>
            string.Equals(t.Status, "Pending", StringComparison.OrdinalIgnoreCase)
        );
        var overdueTasks = tasks.Count(t =>
            t.DueDate < DateTime.UtcNow
            && !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase)
        );
        var overallProgress = totalTasks > 0 ? (int)Math.Round(tasks.Average(t => t.Progress)) : 0;
        var totalPhases = phases.Count;
        var completedPhases = phases.Count(p =>
            string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase)
        );

        return new ProjectStatisticsDto
        {
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            InProgressTasks = inProgressTasks,
            PendingTasks = pendingTasks,
            OverdueTasks = overdueTasks,
            OverallProgress = overallProgress,
            TotalPhases = totalPhases,
            CompletedPhases = completedPhases,
            TasksByStatusCount = tasksByStatus.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count,
                StringComparer.OrdinalIgnoreCase
            ),
            PhaseProgress = phaseSummaries.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Progress,
                StringComparer.OrdinalIgnoreCase
            ),
        };
    }

    private sealed record ProjectDetailOptions
    {
        public bool IncludeRatings { get; init; }
        public string? RatingClientId { get; init; }
        public bool IncludeManagerExtras { get; init; }
    }
}
