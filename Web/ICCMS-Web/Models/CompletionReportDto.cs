using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class CompletionReportDto
    {
        [JsonPropertyName("completionReportId")]
        public string CompletionReportId { get; set; } = string.Empty;

        [JsonPropertyName("taskId")]
        public string TaskId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("submittedBy")]
        public string SubmittedBy { get; set; } = string.Empty;

        [JsonPropertyName("submittedAt")]
        public DateTime SubmittedAt { get; set; }

        [JsonPropertyName("completionDate")]
        public DateTime CompletionDate { get; set; }

        [JsonPropertyName("finalHours")]
        public double FinalHours { get; set; }

        [JsonPropertyName("completionSummary")]
        public string CompletionSummary { get; set; } = string.Empty;

        [JsonPropertyName("qualityCheck")]
        public string QualityCheck { get; set; } = string.Empty;

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
