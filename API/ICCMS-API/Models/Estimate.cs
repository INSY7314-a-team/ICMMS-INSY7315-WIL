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
    }
}
