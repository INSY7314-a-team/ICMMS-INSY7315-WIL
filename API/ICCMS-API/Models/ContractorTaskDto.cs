using System.Text.Json.Serialization;

namespace ICCMS_API.Models
{
    public class ContractorTaskDto
    {
        [JsonPropertyName("taskId")]
        public string TaskId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("phaseId")]
        public string PhaseId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("assignedTo")]
        public string AssignedTo { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        [JsonPropertyName("completedDate")]
        public DateTime? CompletedDate { get; set; }

        [JsonPropertyName("progress")]
        public int Progress { get; set; } = 0;

        [JsonPropertyName("estimatedHours")]
        public double EstimatedHours { get; set; }

        [JsonPropertyName("actualHours")]
        public double ActualHours { get; set; }

        // Additional properties for contractor-specific functionality
        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("projectBudget")]
        public decimal ProjectBudget { get; set; }

        [JsonPropertyName("progressReportCount")]
        public int ProgressReportCount { get; set; }

        [JsonPropertyName("isOverdue")]
        public bool IsOverdue { get; set; }

        [JsonPropertyName("daysUntilDue")]
        public int DaysUntilDue { get; set; }

        [JsonPropertyName("statusBadgeClass")]
        public string StatusBadgeClass { get; set; } = "badge-light";

        [JsonPropertyName("canSubmitProgress")]
        public bool CanSubmitProgress { get; set; }

        [JsonPropertyName("canRequestCompletion")]
        public bool CanRequestCompletion { get; set; }
    }
}
