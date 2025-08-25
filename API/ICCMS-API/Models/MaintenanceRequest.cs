using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class MaintenanceRequest
    {
        [FirestoreProperty("maintenanceRequestId")]
        public string MaintenanceRequestId { get; set; } = string.Empty;

        [FirestoreProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("priority")]
        public string Priority { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("mediaUrl")]
        public string MediaUrl { get; set; } = string.Empty;

        [FirestoreProperty("requestedBy")]
        public string RequestedBy { get; set; } = string.Empty;

        [FirestoreProperty("assignedTo")]
        public string AssignedTo { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("resolvedAt")]
        public DateTime? ResolvedAt { get; set; }

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
