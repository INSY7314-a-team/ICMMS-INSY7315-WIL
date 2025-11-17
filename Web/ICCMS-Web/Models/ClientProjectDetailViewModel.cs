using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class ClientProjectDetailViewModel
    {
        private int _overallProgress;
        private List<UserDto> _contractors = new();
        private Dictionary<string, UserDto> _contractorMap = new(StringComparer.OrdinalIgnoreCase);

        [JsonPropertyName("project")]
        public ProjectDto Project { get; set; } = new();

        [JsonPropertyName("phases")]
        public List<PhaseDto> Phases { get; set; } = new();

        [JsonPropertyName("tasks")]
        public List<ProjectTaskDto> Tasks { get; set; } = new();

        [JsonPropertyName("progressReports")]
        public List<ProgressReportDto> ProgressReports { get; set; } = new();

        [JsonPropertyName("maintenanceRequests")]
        public List<MaintenanceRequestDto> MaintenanceRequests { get; set; } = new();

        [JsonPropertyName("quotations")]
        public List<QuotationDto> Quotations { get; set; } = new();

        [JsonPropertyName("invoices")]
        public List<InvoiceDto> Invoices { get; set; } = new();

        [JsonPropertyName("overallProgress")]
        public int OverallProgress
        {
            get => _overallProgress > 0 ? _overallProgress : Statistics?.OverallProgress ?? 0;
            set => _overallProgress = value;
        }

        [JsonPropertyName("statistics")]
        public ProjectStatisticsViewModel? Statistics { get; set; }

        [JsonPropertyName("contractorMap")]
        public Dictionary<string, UserDto> ContractorMap
        {
            get => _contractorMap;
            set
            {
                _contractorMap = value ?? new(StringComparer.OrdinalIgnoreCase);
                if (!_contractors.Any() && _contractorMap.Count > 0)
                {
                    _contractors = _contractorMap.Values.ToList();
                }
            }
        }

        [JsonPropertyName("contractors")]
        public List<UserDto> Contractors
        {
            get => _contractors.Any() ? _contractors : ContractorMap.Values.ToList();
            set => _contractors = value ?? new List<UserDto>();
        }

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

        [JsonPropertyName("completedTasks")]
        public List<ProjectTaskDto> CompletedTasksPrecomputed { get; set; } = new();

        [JsonPropertyName("overdueTasks")]
        public List<ProjectTaskDto> OverdueTasksPrecomputed { get; set; } = new();

        [JsonPropertyName("pendingTasks")]
        public List<ProjectTaskDto> PendingTasksPrecomputed { get; set; } = new();

        [JsonPropertyName("ratedTasks")]
        public Dictionary<string, bool> RatedTasks { get; set; } = new();

        public bool IsTaskRated(string taskId)
        {
            return RatedTasks.ContainsKey(taskId) && RatedTasks[taskId];
        }

        public string GetContractorName(string assignedTo)
        {
            if (string.IsNullOrEmpty(assignedTo))
                return "Unassigned";

            if (ContractorMap.TryGetValue(assignedTo, out var mapped))
            {
                return mapped.FullName ?? $"User {assignedTo}";
            }

            var contractor = Contractors.FirstOrDefault(c => c.UserId == assignedTo);
            return contractor?.FullName ?? $"User {assignedTo}";
        }

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

            var allTasksCompleted = phaseTasks.All(t =>
                t.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
            );

            if (allTasksCompleted)
                return 100;

            return (int)phaseTasks.Average(t => t.Progress);
        }
    }
}
