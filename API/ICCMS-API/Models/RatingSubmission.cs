using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class RatingSubmission
    {
        [FirestoreProperty("ratingSubmissionId")]
        public string RatingSubmissionId { get; set; } = string.Empty;

        [FirestoreProperty("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [FirestoreProperty("taskId")]
        public string TaskId { get; set; } = string.Empty;

        [FirestoreProperty("ratedBy")]
        public string RatedBy { get; set; } = string.Empty; // Client ID

        [FirestoreProperty("ratingValue")]
        public int RatingValue { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
