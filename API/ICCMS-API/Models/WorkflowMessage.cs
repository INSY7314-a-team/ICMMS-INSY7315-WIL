using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class WorkflowMessage
    {
        [FirestoreProperty("workflowMessageId")]
        public string WorkflowMessageId { get; set; } = string.Empty;

        [FirestoreProperty("workflowType")]
        public string WorkflowType { get; set; } = string.Empty; // "quote_approval", "invoice_payment", "project_update", "system_alert"

        [FirestoreProperty("entityId")]
        public string EntityId { get; set; } = string.Empty; // Quote ID, Invoice ID, Project ID, etc.

        [FirestoreProperty("entityType")]
        public string EntityType { get; set; } = string.Empty; // "quotation", "invoice", "project", "estimate"

        [FirestoreProperty("action")]
        public string Action { get; set; } = string.Empty; // "created", "approved", "rejected", "paid", "overdue"

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("recipients")]
        public List<string> Recipients { get; set; } = new List<string>();

        [FirestoreProperty("subject")]
        public string Subject { get; set; } = string.Empty;

        [FirestoreProperty("content")]
        public string Content { get; set; } = string.Empty;

        [FirestoreProperty("priority")]
        public string Priority { get; set; } = "normal"; // "low", "normal", "high", "urgent"

        [FirestoreProperty("isSystemGenerated")]
        public bool IsSystemGenerated { get; set; } = true;

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("sentAt")]
        public DateTime? SentAt { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = "pending"; // "pending", "sent", "failed", "cancelled"

        [FirestoreProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class WorkflowMessageTemplate
    {
        public string WorkflowType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string SubjectTemplate { get; set; } = string.Empty;
        public string ContentTemplate { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal";
        public List<string> DefaultRecipients { get; set; } = new List<string>();
        public Dictionary<string, string> Placeholders { get; set; } =
            new Dictionary<string, string>();
    }

    public class SystemEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
