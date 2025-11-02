using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class ClientProjectDetailViewModel
    {
        [JsonPropertyName("project")]
        public ProjectDto Project { get; set; } = new();

        [JsonPropertyName("phases")]
        public List<PhaseDto> Phases { get; set; } = new();

        [JsonPropertyName("tasks")]
        public List<ProjectTaskDto> Tasks { get; set; } = new();

        [JsonPropertyName("progressReports")]
        public List<ProgressReportDto> ProgressReports { get; set; } = new();

        [JsonPropertyName("maintenanceRequests")]
        public List<MaintenanceRequestDto> MaintenanceRequests { get; set; } = new();

        [JsonPropertyName("quotations")]
        public List<QuotationDto> Quotations { get; set; } = new();

        [JsonPropertyName("invoices")]
        public List<InvoiceDto> Invoices { get; set; } = new();

        [JsonPropertyName("overallProgress")]
        public int OverallProgress { get; set; }

        [JsonPropertyName("contractors")]
        public List<UserDto> Contractors { get; set; } = new();

        public string GetContractorName(string assignedTo)
        {
            if (string.IsNullOrEmpty(assignedTo))
                return "Unassigned";

            var contractor = Contractors.FirstOrDefault(c => c.UserId == assignedTo);
            return contractor?.FullName ?? $"User {assignedTo}";
        }

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

        public string GetTaskStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "pending" => "badge-secondary",
                "in progress" or "inprogress" or "in-progress" => "badge-warning",
                "awaiting approval" or "awaiting-approval" or "awaitingapproval" => "badge-info",
                "completed" => "badge-success",
                "overdue" => "badge-danger",
                _ => "badge-light",
            };
        }

        public string GetPriorityBadgeClass(string priority)
        {
            return priority?.ToLowerInvariant() switch
            {
                "high" => "badge-danger",
                "medium" => "badge-warning",
                "low" => "badge-success",
                _ => "badge-secondary",
            };
        }
    }
}
