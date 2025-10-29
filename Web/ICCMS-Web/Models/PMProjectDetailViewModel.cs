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
        public Dictionary<string, UserDto> ContractorMap { get; set; } = new();

        // Client information
        [JsonPropertyName("client")]
        public UserDto? Client { get; set; }

        // Summary statistics
        [JsonPropertyName("totalTasks")]
        public int TotalTasks { get; set; }

        [JsonPropertyName("completedTasks")]
        public int CompletedTasks { get; set; }

        [JsonPropertyName("inProgressTasks")]
        public int InProgressTasks { get; set; }

        [JsonPropertyName("pendingTasks")]
        public int PendingTasks { get; set; }

        [JsonPropertyName("overdueTasks")]
        public int OverdueTasks { get; set; }

        [JsonPropertyName("overallProgress")]
        public int OverallProgress { get; set; }

        [JsonPropertyName("totalPhases")]
        public int TotalPhases { get; set; }

        [JsonPropertyName("completedPhases")]
        public int CompletedPhases { get; set; }

        // Project estimates
        [JsonPropertyName("estimates")]
        public List<EstimateDto> Estimates { get; set; } = new();

        // Project invoices
        [JsonPropertyName("invoices")]
        public List<InvoiceDto> Invoices { get; set; } = new();

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
            return Tasks.Where(t => t.PhaseId == phaseId).ToList();
        }

        public int GetPhaseProgress(string phaseId)
        {
            var phaseTasks = GetTasksForPhase(phaseId);
            if (!phaseTasks.Any())
                return 0;

            // If all tasks are completed (status = "Completed"), phase is 100% complete
            var allTasksCompleted = phaseTasks.All(t => 
                t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
            
            if (allTasksCompleted)
                return 100;

            // Calculate average progress of tasks in the phase
            // This will reflect task completion naturally
            return (int)phaseTasks.Average(t => t.Progress);
        }

        public string GetContractorName(string contractorId)
        {
            return ContractorMap.TryGetValue(contractorId, out var contractor)
                ? contractor.FullName
                : "Unknown";
        }
    }
}
