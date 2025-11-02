using System.ComponentModel.DataAnnotations;

namespace ICCMS_Web.Models
{
    public class RequestCompletionViewModel
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Completion notes are required.")]
        [StringLength(
            1000,
            MinimumLength = 10,
            ErrorMessage = "The notes must be between 10 and 1000 characters."
        )]
        [Display(Name = "Completion Summary")]
        public string CompletionSummary { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the final number of hours worked.")]
        [Range(0.1, 100, ErrorMessage = "Hours worked must be between 0.1 and 100.")]
        [Display(Name = "Final Hours Worked")]
        public double FinalHours { get; set; }

        [Required(ErrorMessage = "Please enter the amount spent.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount spent must be a positive value.")]
        [Display(Name = "Amount Spent (R)")]
        public decimal AmountSpent { get; set; }

        [Required(ErrorMessage = "Please provide a quality check summary.")]
        [StringLength(
            500,
            MinimumLength = 5,
            ErrorMessage = "The quality check must be between 5 and 500 characters."
        )]
        [Display(Name = "Quality Check Notes")]
        public string QualityCheck { get; set; } = string.Empty;

        [Display(Name = "Completion Evidence (Optional)")]
        public IFormFile? AttachmentFile { get; set; }
    }
}
