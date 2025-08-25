using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class Payment
    {
        [FirestoreProperty("paymentId")]
        public string PaymentId { get; set; } = string.Empty;

        [FirestoreProperty("invoiceId")]
        public string InvoiceId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [FirestoreProperty("amount")]
        public double Amount { get; set; }

        [FirestoreProperty("method")]
        public string Method { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [FirestoreProperty("paymentDate")]
        public DateTime PaymentDate { get; set; }

        [FirestoreProperty("processedAt")]
        public DateTime ProcessedAt { get; set; }

        [FirestoreProperty("notes")]
        public string Notes { get; set; } = string.Empty;

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
