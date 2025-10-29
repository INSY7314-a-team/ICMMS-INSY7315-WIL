using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class TaskDetailViewModel
    {
        [JsonPropertyName("task")]
        public ProjectTaskDto Task { get; set; } = new();

        [JsonPropertyName("project")]
        public ProjectDto Project { get; set; } = new();

        [JsonPropertyName("phase")]
        public PhaseDto? Phase { get; set; }

        [JsonPropertyName("contractor")]
        public UserDto? Contractor { get; set; }

        [JsonPropertyName("progressReports")]
        public List<ProgressReportDto> ProgressReports { get; set; } = new();

        [JsonPropertyName("completionReports")]
        public List<CompletionReportDto> CompletionReports { get; set; } = new();

        [JsonPropertyName("contractorMap")]
        public Dictionary<string, UserDto> ContractorMap { get; set; } = new();

        public string GetStatusBadgeClass(string status)
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

        public string GetProgressReportStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "submitted" => "badge-info",
                "approved" => "badge-success",
                "rejected" => "badge-danger",
                _ => "badge-secondary",
            };
        }

        public string GetCompletionReportStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "submitted" => "badge-warning",
                "approved" => "badge-success",
                "rejected" => "badge-danger",
                _ => "badge-secondary",
            };
        }

        public string GetContractorName(string contractorId)
        {
            return ContractorMap.TryGetValue(contractorId, out var contractor)
                ? contractor.FullName
                : "Unknown";
        }

        public bool IsOverdue()
        {
            return Task.DueDate < DateTime.UtcNow && Task.Status != "Completed";
        }
    }
}

