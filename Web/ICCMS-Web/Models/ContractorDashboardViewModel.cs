namespace ICCMS_Web.Models
{
    public class ContractorDashboardViewModel
    {
        public List<ContractorTaskDto> AssignedTasks { get; set; } = new();
        public List<ContractorProjectCardDto> Projects { get; set; } = new();
        public Dictionary<string, ProjectDto> TaskProjects { get; set; } = new();
        public Dictionary<string, int> TaskProgressReportCounts { get; set; } = new();

        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int PendingTasks { get; set; }
        public int AwaitingApprovalTasks { get; set; }

        // Filter options
        public List<string> AvailableStatuses { get; set; } =
            new() { "All", "Pending", "In Progress", "Awaiting Approval", "Completed", "Overdue" };

        // Helper methods
        public string GetStatusBadgeClass(string status)
        {
            return status.ToLower() switch
            {
                "pending" => "badge-secondary",
                "in progress" or "inprogress" => "badge-warning",
                "awaiting approval" => "badge-info",
                "completed" => "badge-success",
                "overdue" => "badge-danger",
                _ => "badge-light",
            };
        }

        public bool IsTaskOverdue(ContractorTaskDto task)
        {
            return task.DueDate < DateTime.UtcNow && task.Status != "Completed";
        }

        public int CalculateDaysUntilDue(ContractorTaskDto task)
        {
            var days = (task.DueDate - DateTime.UtcNow).Days;
            return Math.Max(0, days);
        }
    }
}
