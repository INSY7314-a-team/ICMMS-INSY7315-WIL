using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class EstimateLineItem
    {
        [FirestoreProperty("itemId")]
        public string ItemId { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("quantity")]
        public double Quantity { get; set; }

        [FirestoreProperty("unit")]
        public string Unit { get; set; } = string.Empty;

        [FirestoreProperty("category")]
        public string Category { get; set; } = string.Empty;

        [FirestoreProperty("unitPrice")]
        public double UnitPrice { get; set; }

        [FirestoreProperty("lineTotal")]
        public double LineTotal { get; set; }

        [FirestoreProperty("isAiGenerated")]
        public bool IsAiGenerated { get; set; } = true;

        [FirestoreProperty("aiConfidence")]
        public double AiConfidence { get; set; } = 0.0;

        [FirestoreProperty("materialDatabaseId")]
        public string? MaterialDatabaseId { get; set; }

        [FirestoreProperty("notes")]
        public string Notes { get; set; } = string.Empty;
    }
}
