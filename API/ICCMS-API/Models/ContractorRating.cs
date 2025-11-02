using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class ContractorRating
    {
        [FirestoreProperty("contractorRatingId")]
        public string ContractorRatingId { get; set; } = string.Empty;

        [FirestoreProperty("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [FirestoreProperty("averageRating")]
        public double AverageRating { get; set; }

        [FirestoreProperty("totalRatings")]
        public int TotalRatings { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
