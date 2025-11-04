using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// ViewModel for Project Manager's Project Analytics page
    /// Contains comprehensive project KPIs, charts data, and financial tracking
    /// </summary>
    public class ProjectAnalyticsViewModel
    {
        // Core project information
        [JsonPropertyName("project")]
        public ProjectDto Project { get; set; } = new();

        // Budget metrics
        [JsonPropertyName("budgetPlanned")]
        public decimal BudgetPlanned { get; set; }

        [JsonPropertyName("budgetActual")]
        public decimal BudgetActual { get; set; }

        [JsonPropertyName("budgetVariance")]
        public decimal BudgetVariance { get; set; }

        [JsonPropertyName("budgetUtilizationPercentage")]
        public decimal BudgetUtilizationPercentage { get; set; }

        // Historical budget data for line chart (monthly/quarterly)
        [JsonPropertyName("budgetHistory")]
        public List<BudgetHistoryPoint> BudgetHistory { get; set; } = new();

        // Project statistics
        [JsonPropertyName("overallProgress")]
        public int OverallProgress { get; set; }

        [JsonPropertyName("totalTasks")]
        public int TotalTasks { get; set; }

        [JsonPropertyName("completedTasks")]
        public int CompletedTasks { get; set; }

        [JsonPropertyName("inProgressTasks")]
        public int InProgressTasks { get; set; }

        [JsonPropertyName("pendingTasks")]
        public int PendingTasks { get; set; }

        [JsonPropertyName("overdueTasks")]
        public int OverdueTasks { get; set; }

        [JsonPropertyName("totalPhases")]
        public int TotalPhases { get; set; }

        [JsonPropertyName("completedPhases")]
        public int CompletedPhases { get; set; }

        // Task status breakdown for pie chart
        [JsonPropertyName("taskStatusBreakdown")]
        public Dictionary<string, int> TaskStatusBreakdown { get; set; } = new();

        // Contractor ratings
        [JsonPropertyName("contractorRatings")]
        public List<ContractorRatingSummary> ContractorRatings { get; set; } = new();

        [JsonPropertyName("averageContractorRating")]
        public double AverageContractorRating { get; set; }

        // Phases and tasks for Gantt chart
        [JsonPropertyName("phases")]
        public List<PhaseDto> Phases { get; set; } = new();

        [JsonPropertyName("tasks")]
        public List<ProjectTaskDto> Tasks { get; set; } = new();

        // Gantt chart data
        [JsonPropertyName("ganttData")]
        public List<GanttTask> GanttData { get; set; } = new();

        // Budget by phase for bar chart
        [JsonPropertyName("budgetByPhase")]
        public List<PhaseBudgetData> BudgetByPhase { get; set; } = new();

        // Quotes data
        [JsonPropertyName("quotations")]
        public List<QuotationDto> Quotations { get; set; } = new();

        [JsonPropertyName("pendingQuotesCount")]
        public int PendingQuotesCount { get; set; }

        [JsonPropertyName("approvedQuotesCount")]
        public int ApprovedQuotesCount { get; set; }

        [JsonPropertyName("rejectedQuotesCount")]
        public int RejectedQuotesCount { get; set; }

        // Invoices data
        [JsonPropertyName("invoices")]
        public List<InvoiceDto> Invoices { get; set; } = new();

        [JsonPropertyName("unpaidInvoicesCount")]
        public int UnpaidInvoicesCount { get; set; }

        [JsonPropertyName("paidInvoicesCount")]
        public int PaidInvoicesCount { get; set; }

        [JsonPropertyName("overdueInvoicesCount")]
        public int OverdueInvoicesCount { get; set; }

        // Helper methods
        public string GetStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "draft" => "badge-secondary",
                "planning" => "badge-info",
                "active" => "badge-primary",
                "completed" => "badge-success",
                "maintenance" => "badge-warning",
                "cancelled" => "badge-danger",
                _ => "badge-light",
            };
        }

        public string GetQuoteStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "draft" => "badge-secondary",
                "pendingpmapproval" => "badge-warning",
                "senttoclient" => "badge-info",
                "approved" => "badge-success",
                "rejected" or "pmrejected" => "badge-danger",
                "declined" => "badge-danger",
                _ => "badge-light",
            };
        }

        public string GetInvoiceStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "draft" => "badge-secondary",
                "sent" => "badge-info",
                "paid" => "badge-success",
                "overdue" => "badge-danger",
                _ => "badge-light",
            };
        }
    }

    /// <summary>
    /// Data point for budget history line chart
    /// </summary>
    public class BudgetHistoryPoint
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("planned")]
        public decimal Planned { get; set; }

        [JsonPropertyName("actual")]
        public decimal Actual { get; set; }
    }

    /// <summary>
    /// Contractor rating summary for display
    /// </summary>
    public class ContractorRatingSummary
    {
        [JsonPropertyName("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [JsonPropertyName("contractorName")]
        public string ContractorName { get; set; } = string.Empty;

        [JsonPropertyName("averageRating")]
        public double AverageRating { get; set; }

        [JsonPropertyName("totalRatings")]
        public int TotalRatings { get; set; }
    }

    /// <summary>
    /// Gantt chart task data
    /// </summary>
    public class GanttTask
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("start")]
        public DateTime Start { get; set; }

        [JsonPropertyName("end")]
        public DateTime End { get; set; }

        [JsonPropertyName("progress")]
        public int Progress { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "phase" or "task"

        [JsonPropertyName("parentId")]
        public string? ParentId { get; set; }
    }

    /// <summary>
    /// Budget data by phase for bar chart
    /// </summary>
    public class PhaseBudgetData
    {
        [JsonPropertyName("phaseId")]
        public string PhaseId { get; set; } = string.Empty;

        [JsonPropertyName("phaseName")]
        public string PhaseName { get; set; } = string.Empty;

        [JsonPropertyName("plannedBudget")]
        public decimal PlannedBudget { get; set; }

        [JsonPropertyName("actualBudget")]
        public decimal ActualBudget { get; set; }
    }
}

