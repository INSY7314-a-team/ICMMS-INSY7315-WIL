namespace ICCMS_Web.Models;

public class DashboardViewModel
{
    public List<Project> Projects { get; set; } = new();
    public DashboardMetrics Metrics { get; set; } = new();
    public List<RecentActivity> RecentActivities { get; set; } = new();
}

public class TimelineViewModel
{
    public List<Project> Projects { get; set; } = new();
}

public class ContractorViewModel
{
    public List<Contractor> Contractors { get; set; } = new();
}

public class FileReviewViewModel
{
    public List<Document> Documents { get; set; } = new();
}

public class DashboardMetrics
{
    public int TotalProjects { get; set; }
    public decimal ActiveBudget { get; set; }
    public int ActiveContractors { get; set; }
    public int PendingReviews { get; set; }
}

public class RecentActivity
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}

public class Contractor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public ContractorStatus Status { get; set; }
    public string? CurrentProject { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksTotal { get; set; }
    public string Avatar { get; set; } = string.Empty;
}

public class Document
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public DocumentStatus Status { get; set; }
    public string Project { get; set; } = string.Empty;
    public DocumentPriority Priority { get; set; }
}

public enum ContractorStatus
{
    Active,
    Available,
    Unavailable,
}

public enum DocumentStatus
{
    Pending,
    Approved,
    Rejected,
}

public enum DocumentPriority
{
    Low,
    Medium,
    High,
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
