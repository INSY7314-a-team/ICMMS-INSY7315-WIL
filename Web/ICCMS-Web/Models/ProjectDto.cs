using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// Data Transfer Object (DTO) for Projects.
    /// Mirrors ICCMS_API.Models.Project so Web app can communicate
    /// directly with API endpoints (JSON contract must match).
    /// </summary>
    public class ProjectDto
    {
        // 🔑 Primary Key – GUID string
        // If empty on POST, generate before calling API.
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        // ⚠️ Do NOT bind in forms.
        // API automatically sets this from logged-in user (Claims).
        [JsonPropertyName("projectManagerId")]
        public string ProjectManagerId { get; set; } = string.Empty;

        // Must be set when creating project (dropdown/select of Clients in UI).
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = string.Empty;

        // Project name (required, user input).
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // Description/details (optional, user input).
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        // Planned budget – user sets when creating project.
        [JsonPropertyName("budgetPlanned")]
        public double BudgetPlanned { get; set; }

        // Actual budget – set by API or updated as project progresses.
        [JsonPropertyName("budgetActual")]
        public double BudgetActual { get; set; } = 0;

        // Default project status when created (e.g., Draft, Active, Pending).
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Draft";

        // Planned start date (required, user input).
        [JsonPropertyName("startDatePlanned")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        // Planned end date (required, user input).
        [JsonPropertyName("endDatePlanned")]
        public DateTime EndDatePlanned { get; set; } = DateTime.UtcNow.AddMonths(1);

        // Actual end date (nullable, only filled when project completes).
        [JsonPropertyName("endDateActual")]
        public DateTime? EndDateActual { get; set; }
    }
}
