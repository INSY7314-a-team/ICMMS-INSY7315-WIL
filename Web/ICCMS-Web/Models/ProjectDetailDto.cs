using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// Data Transfer Object for detailed project view with tasks
    /// </summary>
    public class ProjectDetailDto
    {
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("budgetPlanned")]
        public decimal BudgetPlanned { get; set; }

        [JsonPropertyName("budgetActual")]
        public decimal BudgetActual { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDatePlanned")]
        public DateTime EndDatePlanned { get; set; }

        [JsonPropertyName("endDateActual")]
        public DateTime? EndDateActual { get; set; }

        [JsonPropertyName("completionPhase")]
        public int? CompletionPhase { get; set; }

        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("clientName")]
        public string ClientName { get; set; } = string.Empty;

        [JsonPropertyName("projectManagerId")]
        public string ProjectManagerId { get; set; } = string.Empty;

        [JsonPropertyName("projectManagerName")]
        public string ProjectManagerName { get; set; } = string.Empty;

        // Calculated properties
        [JsonPropertyName("overallProgress")]
        public int OverallProgress { get; set; }

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

        // Tasks assigned to the contractor
        [JsonPropertyName("tasks")]
        public List<ContractorTaskDto> Tasks { get; set; } = new List<ContractorTaskDto>();
    }
}
