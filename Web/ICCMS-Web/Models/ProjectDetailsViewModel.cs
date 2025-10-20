using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class ProjectDetailsViewModel
    {
        public ProjectDto Project { get; set; }
        public List<PhaseDto> Phases { get; set; } = new();
        public List<ProjectTaskDto> Tasks { get; set; } = new();
        public List<QuotationDto> Quotes { get; set; } = new();
    }
}
