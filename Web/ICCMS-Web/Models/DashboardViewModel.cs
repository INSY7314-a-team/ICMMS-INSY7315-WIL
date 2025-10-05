using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class DashboardViewModel
    {
        // === Project Summary ===
        public int TotalProjects { get; set; }
        public List<ProjectDto> RecentProjects { get; set; } = new();

        // === Quote Summary ===
        public int TotalQuotes { get; set; }
        public List<QuotationDto> RecentAcceptedQuotes { get; set; } = new();
        public List<QuotationDto> AllQuotes { get; set; } = new();

        // === Client Summary ===
        public int TotalClients { get; set; }
        public List<UserDto> RecentClients { get; set; } = new();

        // === Contractor Summary ===
        public int TotalContractors { get; set; }
        public List<UserDto> RecentContractors { get; set; } = new();

        // === Project Lifecycle ===
        public Dictionary<string, List<PhaseDto>> ProjectPhases { get; set; } = new(); // key = ProjectId
        public Dictionary<string, List<ProjectTaskDto>> PhaseTasks { get; set; } = new(); // key = PhaseId
        public Dictionary<string, EstimateDto>? ProjectEstimates { get; set; } = new();

    }
}
