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

        [FirestoreProperty("paidBy")]
        public string PaidBy { get; set; } = string.Empty;

        // New fields for enhanced workflow
        [FirestoreProperty("items")]
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

        [FirestoreProperty("subtotal")]
        public double Subtotal { get; set; }

        [FirestoreProperty("taxTotal")]
        public double TaxTotal { get; set; }

        [FirestoreProperty("markupRate")]
        public double MarkupRate { get; set; } = 1.0; // Default to no markup (1.0 = 100%)

        [FirestoreProperty("subtotalWithMarkup")]
        public double SubtotalWithMarkup { get; set; }

        [FirestoreProperty("taxTotalWithMarkup")]
        public double TaxTotalWithMarkup { get; set; }

        [FirestoreProperty("currency")]
        public string Currency { get; set; } = "ZAR";

        [FirestoreProperty("quotationId")]
        public string? QuotationId { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
