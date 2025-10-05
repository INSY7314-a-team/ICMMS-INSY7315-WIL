using Google.Cloud.Firestore;

namespace ICCMS_API.Models
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

        // New fields for enhanced workflow
        [FirestoreProperty("items")]
        public List<QuotationItem> Items { get; set; } = new List<QuotationItem>();

        [FirestoreProperty("subtotal")]
        public double Subtotal { get; set; }

        [FirestoreProperty("taxTotal")]
        public double TaxTotal { get; set; }

        [FirestoreProperty("grandTotal")]
        public double GrandTotal { get; set; }

        [FirestoreProperty("markupRate")]
        public double MarkupRate { get; set; } = 1.0; // Default to no markup (1.0 = 100%)

        [FirestoreProperty("currency")]
        public string Currency { get; set; } = "ZAR";

        [FirestoreProperty("adminApprovedAt")]
        public DateTime? AdminApprovedAt { get; set; }

        [FirestoreProperty("clientRespondedAt")]
        public DateTime? ClientRespondedAt { get; set; }

        [FirestoreProperty("clientDecisionNote")]
        public string? ClientDecisionNote { get; set; }

        [FirestoreProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        // New fields for AI workflow
        [FirestoreProperty("isAiGenerated")]
        public bool IsAiGenerated { get; set; } = false;

        [FirestoreProperty("estimateId")]
        public string? EstimateId { get; set; }

        [FirestoreProperty("pmEditedAt")]
        public DateTime? PmEditedAt { get; set; }

        [FirestoreProperty("pmEditNotes")]
        public string? PmEditNotes { get; set; }

        [FirestoreProperty("pmRejectedAt")]
        public DateTime? PmRejectedAt { get; set; }

        [FirestoreProperty("pmRejectReason")]
        public string? PmRejectReason { get; set; }
    }
}
