using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class AuditLog
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty; // Firestore-assigned; saved back after create

        [FirestoreProperty("logType")]
        [Required]
        public string LogType { get; set; } = string.Empty; // must be one of Types.GetAuditLogTypes()

        [FirestoreProperty("title")]
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        [MaxLength(4000)]
        public string? Description { get; set; }

        [FirestoreProperty("userId")]
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Always set server-side
        [FirestoreProperty("timestampUtc")]
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        // Universal pointer to the referenced record (documentId, projectId, maintenanceId, quotationId, etc.)
        [FirestoreProperty("entityId")]
        [Required]
        public string EntityId { get; set; } = string.Empty;
    }
}
