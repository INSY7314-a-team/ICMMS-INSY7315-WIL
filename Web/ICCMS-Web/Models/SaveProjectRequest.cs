using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class SaveProjectRequest
    {
        public ProjectDto Project { get; set; } = new ProjectDto();
        public List<PhaseDto> Phases { get; set; } = new List<PhaseDto>();
        public List<ProjectTaskDto> Tasks { get; set; } = new List<ProjectTaskDto>();
    }
}
