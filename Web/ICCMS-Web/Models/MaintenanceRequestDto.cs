using System;
using System.ComponentModel.DataAnnotations;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// Data Transfer Object (DTO) for Maintenance Requests.
    /// Mirrors ICCMS_API.Models.MaintenanceRequest for Web ↔ API JSON contract.
    /// </summary>
    public class MaintenanceRequestDto
    {
        [Required]
        public string MaintenanceRequestId { get; set; } = string.Empty;

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string ProjectId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description too long (500 chars max).")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Priority is required")]
        public string Priority { get; set; } = "Medium";

        public string Status { get; set; } = "Pending";

        public string MediaUrl { get; set; } = string.Empty;

        public string RequestedBy { get; set; } = string.Empty;

        public string AssignedTo { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }
    }
}
