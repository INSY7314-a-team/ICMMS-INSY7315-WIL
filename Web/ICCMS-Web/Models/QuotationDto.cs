using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    // Mirrors API/ICCMS-API/Models/Quotation.cs payload
    public class QuotationDto
    {
        [JsonPropertyName("quotationId")]
        public string QuotationId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("maintenanceRequestId")]
        public string MaintenanceRequestId { get; set; } = string.Empty;

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [JsonPropertyName("adminApproverUserId")]
        public string AdminApproverUserId { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public double Total { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("validUntil")]
        public DateTime ValidUntil { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("sentAt")]
        public DateTime? SentAt { get; set; }

        [JsonPropertyName("approvedAt")]
        public DateTime? ApprovedAt { get; set; }
    }
}
