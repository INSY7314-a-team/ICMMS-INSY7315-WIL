using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class CreateProjectViewModel
    {
        public ProjectDto Project { get; set; } = new();
        public List<UserDto> Clients { get; set; } = new();
        public bool IsDraft { get; set; }
        public int CurrentPhase { get; set; } = 1;
    }
}
