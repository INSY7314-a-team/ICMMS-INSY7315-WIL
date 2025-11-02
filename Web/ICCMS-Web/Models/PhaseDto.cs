using System;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// Data Transfer Object for Project Phases.
    /// Mirrors ICCMS_API.Models.Phase (Firestore entity).
    /// </summary>
    public class PhaseDto
    {
        [JsonPropertyName("phaseId")]
        public string PhaseId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7);

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Draft";

        [JsonPropertyName("progress")]
        public int Progress { get; set; } = 0;

        [JsonPropertyName("budget")]
        public double Budget { get; set; } = 0.0;

        [JsonPropertyName("assignedTo")]
        public string AssignedTo { get; set; } = string.Empty;
    }
}
