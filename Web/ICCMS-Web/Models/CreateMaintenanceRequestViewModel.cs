using System.ComponentModel.DataAnnotations;

namespace ICCMS_Web.Models
{
    public class CreateMaintenanceRequestViewModel
    {
        [Required(ErrorMessage = "Please select a project")]
        [Display(Name = "Project")]
        public string ProjectId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a description")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a priority level")]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = string.Empty;

        [Display(Name = "Media URL (Optional)")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? MediaUrl { get; set; }

        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; } = string.Empty;
    }
}
