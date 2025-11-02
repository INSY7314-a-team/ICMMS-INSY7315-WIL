using System.ComponentModel.DataAnnotations;

namespace ICCMS_Web.Models
{
    public class RequestCompletionRequest
    {
        [Required]
        public string TaskId { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string? DocumentId { get; set; }
    }
}
