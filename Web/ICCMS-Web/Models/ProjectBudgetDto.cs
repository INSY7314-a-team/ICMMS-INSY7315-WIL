namespace ICCMS_Web.Models
{
    public class ProjectBudgetDto
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public decimal BudgetPlanned { get; set; }
    }
}
