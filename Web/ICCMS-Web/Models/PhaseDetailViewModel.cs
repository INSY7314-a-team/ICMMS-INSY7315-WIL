using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class PhaseDetailViewModel
    {
        [JsonPropertyName("phase")]
        public PhaseDto Phase { get; set; } = new();

        [JsonPropertyName("project")]
        public ProjectDto Project { get; set; } = new();

        [JsonPropertyName("tasks")]
        public List<ProjectTaskDto> Tasks { get; set; } = new();

        [JsonPropertyName("contractorMap")]
        public Dictionary<string, UserDto> ContractorMap { get; set; } = new();

        [JsonPropertyName("progress")]
        public int Progress { get; set; }

        [JsonPropertyName("totalTasks")]
        public int TotalTasks { get; set; }

        [JsonPropertyName("completedTasks")]
        public int CompletedTasks { get; set; }

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

        public string GetContractorName(string contractorId)
        {
            return ContractorMap.TryGetValue(contractorId, out var contractor)
                ? contractor.FullName
                : "Unknown";
        }

        public bool IsTaskOverdue(ProjectTaskDto task)
        {
            return task.DueDate < DateTime.UtcNow && task.Status != "Completed";
        }
    }
}

