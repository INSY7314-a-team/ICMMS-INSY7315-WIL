using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class ProjectStatisticsViewModel
    {
        [JsonPropertyName("totalTasks")]
        public int TotalTasks { get; set; }

        [JsonPropertyName("completedTasks")]
        public int CompletedTasks { get; set; }

        [JsonPropertyName("inProgressTasks")]
        public int InProgressTasks { get; set; }

        [JsonPropertyName("pendingTasks")]
        public int PendingTasks { get; set; }

        [JsonPropertyName("overdueTasks")]
        public int OverdueTasks { get; set; }

        [JsonPropertyName("overallProgress")]
        public int OverallProgress { get; set; }

        [JsonPropertyName("totalPhases")]
        public int TotalPhases { get; set; }

        [JsonPropertyName("completedPhases")]
        public int CompletedPhases { get; set; }

        [JsonPropertyName("tasksByStatusCount")]
        public Dictionary<string, int> TasksByStatusCount { get; set; } = new();

        [JsonPropertyName("phaseProgress")]
        public Dictionary<string, int> PhaseProgress { get; set; } = new();
    }

    public class PhaseSummaryViewModel
    {
        [JsonPropertyName("phase")]
        public PhaseDto? Phase { get; set; }

        [JsonPropertyName("tasks")]
        public List<ProjectTaskDto> Tasks { get; set; } = new();

        [JsonPropertyName("taskCount")]
        public int TaskCount { get; set; }

        [JsonPropertyName("completedTaskCount")]
        public int CompletedTaskCount { get; set; }

        [JsonPropertyName("progress")]
        public int Progress { get; set; }
    }
}
