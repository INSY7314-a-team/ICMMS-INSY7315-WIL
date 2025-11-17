using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// ViewModel for Project Manager's Project Detail page
    /// Contains all project information, phases, tasks, and pending approvals
    /// </summary>
    public class PMProjectDetailViewModel
    {
        // Core project information
        private int? _totalTasks;
        private int? _completedTasks;
        private int? _inProgressTasks;
        private int? _pendingTasks;
        private int? _overdueTasks;
        private int? _overallProgress;
        private int? _totalPhases;
        private int? _completedPhases;
        private Dictionary<string, UserDto> _contractorMap = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("project")]
        public ProjectDto Project { get; set; } = new();

        // Project phases
        [JsonPropertyName("phases")]
        public List<PhaseDto> Phases { get; set; } = new();

        // All project tasks
        [JsonPropertyName("tasks")]
        public List<ProjectTaskDto> Tasks { get; set; } = new();

        // Pending progress reports awaiting approval
        [JsonPropertyName("pendingProgressReports")]
        public List<ProgressReportDto> PendingProgressReports { get; set; } = new();

        // Tasks awaiting completion approval
        [JsonPropertyName("tasksAwaitingCompletion")]
        public List<ProjectTaskDto> TasksAwaitingCompletion { get; set; } = new();

        // Contractor information for display names
        [JsonPropertyName("contractorMap")]
        public Dictionary<string, UserDto> ContractorMap
        {
            get => _contractorMap;
            set => _contractorMap = value ?? new(StringComparer.OrdinalIgnoreCase);
        }

        // Client information
        [JsonPropertyName("client")]
        public UserDto? Client { get; set; }

        [JsonPropertyName("statistics")]
        public ProjectStatisticsViewModel? Statistics { get; set; }

        [JsonPropertyName("tasksByPhase")]
        public Dictionary<string, List<ProjectTaskDto>> TasksByPhase { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("tasksByStatus")]
        public Dictionary<string, List<ProjectTaskDto>> TasksByStatus { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("tasksByContractor")]
        public Dictionary<string, List<ProjectTaskDto>> TasksByContractor { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("phaseSummaries")]
        public Dictionary<string, PhaseSummaryViewModel> PhaseSummaries { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("progressReports")]
        public List<ProgressReportDto> ProgressReports { get; set; } = new();

        [JsonPropertyName("completionReports")]
        public List<CompletionReportDto> CompletionReports { get; set; } = new();

        [JsonPropertyName("completedTasks")]
        public List<ProjectTaskDto> CompletedTaskItems { get; set; } = new();

        [JsonPropertyName("overdueTasks")]
        public List<ProjectTaskDto> OverdueTaskItems { get; set; } = new();

        [JsonPropertyName("pendingTasks")]
        public List<ProjectTaskDto> PendingTaskItems { get; set; } = new();

        // Summary statistics
        [JsonIgnore]
        public int TotalTasks
        {
            get => _totalTasks ?? Statistics?.TotalTasks ?? Tasks.Count;
            set => _totalTasks = value;
        }

        [JsonIgnore]
        public int CompletedTasks
        {
            get => _completedTasks ?? Statistics?.CompletedTasks ?? CompletedTaskItems.Count;
            set => _completedTasks = value;
        }

        [JsonIgnore]
        public int InProgressTasks
        {
            get => _inProgressTasks ?? Statistics?.InProgressTasks ?? 0;
            set => _inProgressTasks = value;
        }

        [JsonIgnore]
        public int PendingTasks
        {
            get => _pendingTasks ?? Statistics?.PendingTasks ?? PendingTaskItems.Count;
            set => _pendingTasks = value;
        }

        [JsonIgnore]
        public int OverdueTasks
        {
            get => _overdueTasks ?? Statistics?.OverdueTasks ?? OverdueTaskItems.Count;
            set => _overdueTasks = value;
        }

        [JsonIgnore]
        public int OverallProgress
        {
            get => _overallProgress ?? Statistics?.OverallProgress ?? 0;
            set => _overallProgress = value;
        }

        [JsonIgnore]
        public int TotalPhases
        {
            get => _totalPhases ?? Statistics?.TotalPhases ?? Phases.Count;
            set => _totalPhases = value;
        }

        [JsonIgnore]
        public int CompletedPhases
        {
            get => _completedPhases ?? Statistics?.CompletedPhases ?? 0;
            set => _completedPhases = value;
        }

        // Project estimates
        [JsonPropertyName("estimates")]
        public List<EstimateDto> Estimates { get; set; } = new();

        // Project invoices
        [JsonPropertyName("invoices")]
        public List<InvoiceDto> Invoices { get; set; } = new();

        // Maintenance requests
        [JsonPropertyName("maintenanceRequests")]
        public List<MaintenanceRequestDto> MaintenanceRequests { get; set; } = new();

        // Helper methods for UI
        public string GetStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "draft" => "badge-secondary",
                "planning" => "badge-info",
                "active" => "badge-primary",
                "completed" => "badge-success",
                "maintenance" => "badge-warning",
                "cancelled" => "badge-danger",
                _ => "badge-light",
            };
        }

        public string GetTaskStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "pending" => "badge-secondary",
                "in progress" or "inprogress" or "in-progress" => "badge-warning",
                "awaiting approval" or "awaiting-approval" or "awaitingapproval" => "badge-info",
                "completed" => "badge-success",
                "overdue" => "badge-danger",
                _ => "badge-light",
            };
        }

        public string GetPriorityBadgeClass(string priority)
        {
            return priority?.ToLowerInvariant() switch
            {
                "high" => "badge-danger",
                "medium" => "badge-warning",
                "low" => "badge-success",
                _ => "badge-secondary",
            };
        }

        public bool IsTaskOverdue(ProjectTaskDto task)
        {
            return task.DueDate < DateTime.UtcNow && task.Status != "Completed";
        }

        public List<ProjectTaskDto> GetTasksForPhase(string phaseId)
        {
            if (
                !string.IsNullOrWhiteSpace(phaseId)
                && TasksByPhase.TryGetValue(phaseId, out var cached)
            )
            {
                return cached;
            }

            return Tasks.Where(t => t.PhaseId == phaseId).ToList();
        }

        public int GetPhaseProgress(string phaseId)
        {
            if (
                PhaseSummaries.TryGetValue(phaseId, out var summary)
                && summary != null
                && summary.Progress >= 0
            )
            {
                return summary.Progress;
            }

            var phaseTasks = GetTasksForPhase(phaseId);
            if (!phaseTasks.Any())
                return 0;

            // If all tasks are completed (status = "Completed"), phase is 100% complete
            var allTasksCompleted = phaseTasks.All(t =>
                t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
            );

            if (allTasksCompleted)
                return 100;

            // Calculate average progress of tasks in the phase
            // This will reflect task completion naturally
            return (int)phaseTasks.Average(t => t.Progress);
        }

        public string GetContractorName(string contractorId)
        {
            if (ContractorMap.TryGetValue(contractorId, out var contractor))
            {
                return contractor.FullName ?? "Unknown";
            }

            return "Unknown";
        }

        public string GetMaintenanceRequestStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "pending" or "submitted" => "badge-warning",
                "assigned" => "badge-info",
                "in progress" or "inprogress" => "badge-primary",
                "completed" or "resolved" => "badge-success",
                "rejected" or "cancelled" => "badge-danger",
                _ => "badge-secondary",
            };
        }
    }
}
