using Google.Cloud.Firestore;

namespace ICCMS_Web.Models
{
    [FirestoreData]
    public class Quotation
    {
        [FirestoreProperty("quotationId")]
        public string QuotationId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("maintenanceRequestId")]
        public string MaintenanceRequestId { get; set; } = string.Empty;

        [FirestoreProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [FirestoreProperty("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [FirestoreProperty("adminApproverUserId")]
        public string AdminApproverUserId { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("total")]
        public double Total { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("validUntil")]
        public DateTime ValidUntil { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("sentAt")]
        public DateTime? SentAt { get; set; }

        [FirestoreProperty("approvedAt")]
        public DateTime? ApprovedAt { get; set; }
    }
}