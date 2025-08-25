using System.ComponentModel.DataAnnotations;

namespace ICCMS_Web.Models
{
    public class UpdateRoleViewModel
    {
        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "New Role")]
        public string NewRole { get; set; } = string.Empty;
    }
}
