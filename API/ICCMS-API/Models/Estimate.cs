using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class Estimate
    {
        [FirestoreProperty("estimateId")]
        public string EstimateId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("totalAmount")]
        public double TotalAmount { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("validUntil")]
        public DateTime ValidUntil { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        // New fields for AI-generated line items
        [FirestoreProperty("lineItems")]
        public List<EstimateLineItem> LineItems { get; set; } = new List<EstimateLineItem>();

        [FirestoreProperty("subtotal")]
        public double Subtotal { get; set; }

        [FirestoreProperty("taxTotal")]
        public double TaxTotal { get; set; }

        [FirestoreProperty("currency")]
        public string Currency { get; set; } = "ZAR";

        [FirestoreProperty("isAiGenerated")]
        public bool IsAiGenerated { get; set; } = false;

        [FirestoreProperty("blueprintUrl")]
        public string? BlueprintUrl { get; set; }

        [FirestoreProperty("aiProcessingNotes")]
        public string? AiProcessingNotes { get; set; }
    }
}
