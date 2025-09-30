using System;
using System.Collections.Generic;
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

        // 🔑 New: line items in this quotation
        [JsonPropertyName("items")]
        public List<QuotationItemDto> Items { get; set; } = new();

        // 🔑 Totals expected by Views
        [JsonPropertyName("subtotal")]
        public double Subtotal { get; set; }

        [JsonPropertyName("taxTotal")]
        public double TaxTotal { get; set; }

        [JsonPropertyName("grandTotal")]
        public double GrandTotal { get; set; }

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
