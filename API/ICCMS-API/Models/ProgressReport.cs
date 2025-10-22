using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class ProgressReport
    {
        [FirestoreProperty("progressReportId")]
        public string ProgressReportId { get; set; } = string.Empty;

        [FirestoreProperty("taskId")]
        public string TaskId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("submittedBy")]
        public string SubmittedBy { get; set; } = string.Empty;

        [FirestoreProperty("submittedAt")]
        public DateTime SubmittedAt { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("hoursWorked")]
        public double HoursWorked { get; set; }

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
