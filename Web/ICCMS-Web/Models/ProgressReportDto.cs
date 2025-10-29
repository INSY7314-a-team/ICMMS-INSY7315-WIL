using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class ProgressReportDto
    {
        [JsonPropertyName("progressReportId")]
        public string ProgressReportId { get; set; } = string.Empty;

        [JsonPropertyName("taskId")]
        public string TaskId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("submittedBy")]
        public string SubmittedBy { get; set; } = string.Empty;

        [JsonPropertyName("submittedAt")]
        public DateTime SubmittedAt { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("hoursWorked")]
        public double HoursWorked { get; set; }

        [JsonPropertyName("progressPercentage")]
        public int? ProgressPercentage { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Submitted";

        [JsonPropertyName("attachedDocumentIds")]
        public List<string> AttachedDocumentIds { get; set; } = new();

        [JsonPropertyName("reviewedBy")]
        public string? ReviewedBy { get; set; }

        [JsonPropertyName("reviewedAt")]
        public DateTime? ReviewedAt { get; set; }

        [JsonPropertyName("reviewNotes")]
        public string ReviewNotes { get; set; } = string.Empty;
    }
}
