namespace ICCMS_API.Models
{
    public class Project
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectManagerId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double BudgetPlanned { get; set; }
        public double BudgetActual { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDatePlanned { get; set; }
        public DateTime? EndDateActual { get; set; }
    }
}
