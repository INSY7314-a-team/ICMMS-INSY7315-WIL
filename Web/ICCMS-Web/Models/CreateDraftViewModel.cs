using System;
using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class CreateDraftViewModel
    {
        // === Dropdown Data ===
        public List<ProjectDto> Projects { get; set; } = new();
        public List<UserDto> Clients { get; set; } = new();

        // === Form Data ===
        public string? ProjectId { get; set; }
        public string? ClientId { get; set; }
        public string? Description { get; set; }
        public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddDays(30);
    }
}
