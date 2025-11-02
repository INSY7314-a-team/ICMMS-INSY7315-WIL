using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class DashboardViewModel
    {
        // === Project Summary ===
        public int TotalProjects { get; set; }
        public List<ProjectDto> RecentProjects { get; set; } = new();

        // === Pagination Properties ===
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        // === Status Options ===
        public List<string> AvailableStatuses { get; set; } =
            new() { "All", "Draft", "Active", "Completed", "Maintenance" };

        // === Grouped Project Lists ===
        public List<ProjectDto> DraftProjects { get; set; } = new();
        public List<ProjectDto> ActiveProjects { get; set; } = new();
        public List<ProjectDto> OtherProjects { get; set; } = new();
        public List<ProjectDto> FilteredProjects { get; set; } = new(); // Current page projects

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

        // 🆕 Added for consistent client access (like CreateProject)
        public List<UserDto> Clients { get; set; } = new();

        // === Project Details ===
        public Dictionary<string, ProjectDetails> ProjectDetails { get; set; } = new();

        // === Helper Properties ===
        public string GetStatusBadgeClass(string status)
        {
            return status.ToLower() switch
            {
                "draft" => "badge-secondary",
                "active" => "badge-success",
                "completed" => "badge-primary",
                "maintenance" => "badge-warning",
                _ => "badge-light",
            };
        }

        public double GetProjectProgress(ProjectDto project)
        {
            if (project.EndDateActual.HasValue)
                return 100; // Completed

            var totalDays = (project.EndDatePlanned - project.StartDate).TotalDays;
            var elapsedDays = (DateTime.UtcNow - project.StartDate).TotalDays;
            return Math.Min(100, Math.Max(0, (elapsedDays / totalDays) * 100));
        }
    }

    public class ProjectDetails
    {
        public List<PhaseDto> Phases { get; set; } = new();
        public List<ProjectTaskDto> Tasks { get; set; } = new();
        public EstimateDto? Estimate { get; set; }
        public double Progress { get; set; }
        public string StatusBadgeClass { get; set; } = "badge-light";
    }
}
