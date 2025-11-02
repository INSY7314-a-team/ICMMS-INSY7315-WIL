using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// DTO representing an Estimate document.
    /// Mirrors ICCMS_API.Models.Estimate exactly — used by Web app
    /// to communicate with Firestore via the API.
    /// </summary>
    public class EstimateDto
    {
        // 🔑 Primary identifier for this estimate
        [JsonPropertyName("estimateId")]
        public string EstimateId { get; set; } = string.Empty;

        // 🔗 Linked project ID (each estimate belongs to a project)
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        // 🔗 Contractor assigned to produce this estimate
        [JsonPropertyName("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        // 📝 Description or summary
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        // 💰 Total amount (subtotal + tax)
        [JsonPropertyName("totalAmount")]
        public double TotalAmount { get; set; }

        // 📊 Status (Draft, Submitted, Approved, Rejected, etc.)
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Draft";

        // 📅 Expiry date for the estimate validity
        [JsonPropertyName("validUntil")]
        public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddDays(14);

        // 🕓 Created timestamp
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 📋 Collection of line items (materials, labor, etc.)
        [JsonPropertyName("lineItems")]
        public List<EstimateLineItemDto> LineItems { get; set; } = new();

        // 🧮 Subtotal before tax
        [JsonPropertyName("subtotal")]
        public double Subtotal { get; set; }

        // 🧾 Tax total
        [JsonPropertyName("taxTotal")]
        public double TaxTotal { get; set; }

        // 💱 Currency
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "ZAR";

        // 🤖 Whether this estimate was AI-generated
        [JsonPropertyName("isAiGenerated")]
        public bool IsAiGenerated { get; set; }

        // 🗺️ Optional blueprint file link for AI parsing
        [JsonPropertyName("blueprintUrl")]
        public string? BlueprintUrl { get; set; }

        // 💬 AI-generated processing or parsing notes
        [JsonPropertyName("aiProcessingNotes")]
        public string? AiProcessingNotes { get; set; }

        // === Derived convenience field (not stored in Firestore) ===
        [JsonIgnore]
        public string DisplayTotal => $"R {TotalAmount:N2}";
    }
}
