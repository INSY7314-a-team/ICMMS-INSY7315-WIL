using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class Invoice
    {
        [FirestoreProperty("invoiceId")]
        public string InvoiceId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [FirestoreProperty("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [FirestoreProperty("invoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("amount")]
        public double Amount { get; set; }

        [FirestoreProperty("taxAmount")]
        public double TaxAmount { get; set; }

        [FirestoreProperty("totalAmount")]
        public double TotalAmount { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("dueDate")]
        public DateTime DueDate { get; set; }

        [FirestoreProperty("issuedDate")]
        public DateTime IssuedDate { get; set; }

        [FirestoreProperty("paidDate")]
        public DateTime? PaidDate { get; set; }

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
