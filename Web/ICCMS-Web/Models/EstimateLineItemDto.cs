using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class EstimateLineItemDto
    {
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public double Quantity { get; set; }

        [JsonPropertyName("unit")]
        public string Unit { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("unitPrice")]
        public double UnitPrice { get; set; }

        [JsonPropertyName("lineTotal")]
        public double LineTotal { get; set; }

        [JsonPropertyName("isAiGenerated")]
        public bool IsAiGenerated { get; set; } = false;


        [JsonPropertyName("aiConfidence")]
        public double AiConfidence { get; set; } = 0.0;

        [JsonPropertyName("materialDatabaseId")]
        public string? MaterialDatabaseId { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }
}
