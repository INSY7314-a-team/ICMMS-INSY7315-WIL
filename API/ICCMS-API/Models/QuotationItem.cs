using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class QuotationItem
    {
        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("quantity")]
        public double Quantity { get; set; }

        [FirestoreProperty("unitPrice")]
        public double UnitPrice { get; set; }

        [FirestoreProperty("taxRate")]
        public double TaxRate { get; set; }

        [FirestoreProperty("lineTotal")]
        public double LineTotal { get; set; }
    }
}
