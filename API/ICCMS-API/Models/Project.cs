using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class Project
    {
        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("projectManagerId")]
        public string ProjectManagerId { get; set; } = string.Empty;

        [FirestoreProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("budgetPlanned")]
        public double BudgetPlanned { get; set; }

        [FirestoreProperty("budgetActual")]
        public double BudgetActual { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("isDraft")]
        public bool IsDraft { get; set; } = false;

        [FirestoreProperty("startDatePlanned")]
        public DateTime StartDate { get; set; }

        [FirestoreProperty("endDatePlanned")]
        public DateTime EndDatePlanned { get; set; }

        [FirestoreProperty("endDateActual")]
        public DateTime? EndDateActual { get; set; }

        // Draft/ownership/housekeeping fields
        [FirestoreProperty("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [FirestoreProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
