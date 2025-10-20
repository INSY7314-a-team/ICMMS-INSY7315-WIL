using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class ProjectsResponse
    {
        public List<ProjectDto> Projects { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalProjects { get; set; }
        public int TotalPages { get; set; }
    }
}
