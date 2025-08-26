using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class Phase
    {
        [FirestoreProperty("phaseId")]
        public string PhaseId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("startDate")]
        public DateTime StartDate { get; set; }

        [FirestoreProperty("endDate")]
        public DateTime EndDate { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("progress")]
        public int Progress { get; set; } = 0;

        [FirestoreProperty("budget")]
        public double Budget { get; set; }

        [FirestoreProperty("assignedTo")]
        public string AssignedTo { get; set; } = string.Empty;
    }
}
