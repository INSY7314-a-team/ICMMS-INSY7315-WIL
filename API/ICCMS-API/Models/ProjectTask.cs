using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class ProjectTask
    {
        [FirestoreProperty("taskId")]
        public string TaskId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("phaseId")]
        public string PhaseId { get; set; } = string.Empty;

        [FirestoreProperty("name")]
        public string Name { get; set; } = string.Empty;

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("assignedTo")]
        public string AssignedTo { get; set; } = string.Empty;

        [FirestoreProperty("priority")]
        public string Priority { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("startDate")]
        public DateTime StartDate { get; set; }

        [FirestoreProperty("dueDate")]
        public DateTime DueDate { get; set; }

        [FirestoreProperty("completedDate")]
        public DateTime? CompletedDate { get; set; }

        [FirestoreProperty("progress")]
        public int Progress { get; set; } = 0;

        [FirestoreProperty("estimatedHours")]
        public double EstimatedHours { get; set; }

        [FirestoreProperty("actualHours")]
        public double ActualHours { get; set; }
    }
}
