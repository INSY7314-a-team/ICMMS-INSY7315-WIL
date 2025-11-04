using System;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// Data Transfer Object (DTO) for Project Tasks.
    /// Mirrors ICCMS_API.Models.ProjectTask so that the Web app
    /// can communicate directly with API endpoints.
    /// </summary>
    public class ProjectTaskDto
    {
        // 🔑 Primary key – generated GUID if not set
        [JsonPropertyName("taskId")]
        public string TaskId { get; set; } = string.Empty;

        // 🔗 Parent Project ID
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        // 🔗 Optional link to Phase (may be empty)
        [JsonPropertyName("phaseId")]
        public string PhaseId { get; set; } = string.Empty;

        // 📌 Task name (required)
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // 📝 Task description (optional)
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        // 👷 Assigned user (userId string; may be empty initially)
        [JsonPropertyName("assignedTo")]
        public string AssignedTo { get; set; } = string.Empty;

        // 🚨 Priority (e.g., Low, Medium, High)
        [JsonPropertyName("priority")]
        public string Priority { get; set; } = "Medium";

        // ⚙️ Current task status (e.g., Pending, InProgress, Completed)
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Pending";

        // 🕓 Planned start date
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        // ⏰ Due date – default one week after start
        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);

        // ✅ Completion date (nullable)
        [JsonPropertyName("completedDate")]
        public DateTime? CompletedDate { get; set; }

        // 📊 Progress percentage (0–100)
        [JsonPropertyName("progress")]
        public int Progress { get; set; } = 0;

        // ⏱️ Estimated work hours (default 8)
        [JsonPropertyName("estimatedHours")]
        public double EstimatedHours { get; set; } = 8.0;

        // 🧾 Actual work hours logged
        [JsonPropertyName("actualHours")]
        public double ActualHours { get; set; } = 0.0;

        // 💰 Task budget
        [JsonPropertyName("budget")]
        public double Budget { get; set; } = 0.0;

        // 💵 Task spent amount
        [JsonPropertyName("spentAmount")]
        public double SpentAmount { get; set; } = 0.0;
    }
}
