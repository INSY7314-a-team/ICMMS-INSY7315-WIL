namespace ICCMS_Web.Models
{
    // Web-facing Data Transfer Object (DTO) for Users
    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;   // ✅ Needed by Razor views
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }             // ✅ Needed by Razor views
    }
}
