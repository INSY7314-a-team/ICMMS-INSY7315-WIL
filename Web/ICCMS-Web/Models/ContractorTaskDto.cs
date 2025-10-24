using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class ContractorTaskDto : ProjectTaskDto
    {
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
