using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class ContractorProjectCardDto
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public decimal ProjectBudget { get; set; }
        public int OverallProgress { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public List<ContractorTaskDto> Tasks { get; set; } = new List<ContractorTaskDto>();
    }
}
