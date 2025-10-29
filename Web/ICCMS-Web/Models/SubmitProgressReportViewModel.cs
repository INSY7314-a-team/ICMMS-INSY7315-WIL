using System.ComponentModel.DataAnnotations;

namespace ICCMS_Web.Models
{
    public class SubmitProgressReportViewModel
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "A description of the work performed is required.")]
        [StringLength(
            1000,
            MinimumLength = 10,
            ErrorMessage = "The description must be between 10 and 1000 characters."
        )]
        [Display(Name = "Progress Update Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the number of hours worked.")]
        [Range(0.1, 100, ErrorMessage = "Hours worked must be between 0.1 and 100.")]
        [Display(Name = "Hours Worked")]
        public double HoursWorked { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile? AttachmentFile { get; set; }

        [Range(0, 100, ErrorMessage = "Progress percentage must be between 0 and 100.")]
        [Display(Name = "Progress Percentage (%)")]
        public int? ProgressPercentage { get; set; }
    }
}
