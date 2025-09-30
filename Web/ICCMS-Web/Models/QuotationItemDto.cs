using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class QuotationItemDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public double Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public double UnitPrice { get; set; }

        [JsonPropertyName("taxRate")]
        public double TaxRate { get; set; }

        [JsonPropertyName("lineTotal")]
        public double LineTotal { get; set; }
    }
}
