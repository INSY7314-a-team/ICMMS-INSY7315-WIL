using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class CompletionReport
    {
        [FirestoreProperty("completionReportId")]
        public string CompletionReportId { get; set; } = string.Empty;

        [FirestoreProperty("taskId")]
        public string TaskId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("submittedBy")]
        public string SubmittedBy { get; set; } = string.Empty;

        [FirestoreProperty("submittedAt")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [FirestoreProperty("completionDate")]
        public DateTime CompletionDate { get; set; } = DateTime.UtcNow;

        [FirestoreProperty("finalHours")]
        public double FinalHours { get; set; }

        [FirestoreProperty("completionSummary")]
        public string CompletionSummary { get; set; } = string.Empty;

        [FirestoreProperty("qualityCheck")]
        public string QualityCheck { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = "Submitted";

        [FirestoreProperty("attachedDocumentIds")]
        public List<string> AttachedDocumentIds { get; set; } = new();

        [FirestoreProperty("reviewedBy")]
        public string? ReviewedBy { get; set; }

        [FirestoreProperty("reviewedAt")]
        public DateTime? ReviewedAt { get; set; }

        [FirestoreProperty("reviewNotes")]
        public string ReviewNotes { get; set; } = string.Empty;
    }
}
