using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class QuotationItemDto
    {
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        [JsonPropertyName("quantity")]
        public double Quantity { get; set; }

        [Required(ErrorMessage = "Unit is required")]
        [JsonPropertyName("unit")]
        public string Unit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Unit Price must be greater than 0")]
        [JsonPropertyName("unitPrice")]
        public double UnitPrice { get; set; }

        [JsonPropertyName("lineTotal")]
        public double LineTotal { get; set; }

        // 🔥 NEW FIELD: Item-level TaxRate (so it syncs with Firestore QuotationItem)
        [Range(0, 100, ErrorMessage = "Tax Rate must be between 0 and 100")]
        [JsonPropertyName("taxRate")]
        public double TaxRate { get; set; }

        [JsonPropertyName("isAiGenerated")]
        public bool IsAiGenerated { get; set; }

        [JsonPropertyName("aiConfidence")]
        public double? AiConfidence { get; set; }

        [JsonPropertyName("materialDatabaseId")]
        public string? MaterialDatabaseId { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
