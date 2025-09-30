using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    // Data passed from controller → dashboard view
    public class DashboardViewModel
    {
        // === Projects ===
        public int TotalProjects { get; set; }
        public List<ProjectDto> RecentProjects { get; set; } = new();

        // === Quotes ===
        public int TotalQuotes { get; set; }
        public List<QuotationDto> RecentAcceptedQuotes  { get; set; } = new();
        public List<QuotationDto> AllQuotes { get; set; } = new();


        // === Clients ===
        public int TotalClients { get; set; }
        public List<UserDto> RecentClients { get; set; } = new();

        // === Contractors ===
        public int TotalContractors { get; set; }
        public List<UserDto> RecentContractors { get; set; } = new();
    }
}
