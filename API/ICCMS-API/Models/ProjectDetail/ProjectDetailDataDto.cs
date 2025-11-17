using System.Text.Json.Serialization;
using ICCMS_API.Models;

namespace ICCMS_API.Models.ProjectDetail;

public class ProjectDetailDataDto
{
    [JsonPropertyName("project")]
    public Project? Project { get; set; }

    [JsonPropertyName("phases")]
    public List<Phase> Phases { get; set; } = new();

    [JsonPropertyName("tasks")]
    public List<ProjectTask> Tasks { get; set; } = new();

    [JsonPropertyName("progressReports")]
    public List<ProgressReport> ProgressReports { get; set; } = new();

    [JsonPropertyName("pendingProgressReports")]
    public List<ProgressReport> PendingProgressReports { get; set; } = new();

    [JsonPropertyName("maintenanceRequests")]
    public List<MaintenanceRequest> MaintenanceRequests { get; set; } = new();

    [JsonPropertyName("quotations")]
    public List<Quotation> Quotations { get; set; } = new();

    [JsonPropertyName("invoices")]
    public List<Invoice> Invoices { get; set; } = new();

    [JsonPropertyName("estimates")]
    public List<Estimate> Estimates { get; set; } = new();

    [JsonPropertyName("completionReports")]
    public List<CompletionReport> CompletionReports { get; set; } = new();

    [JsonPropertyName("contractorMap")]
    public Dictionary<string, User> ContractorMap { get; set; } = new();

    [JsonPropertyName("tasksByPhase")]
    public Dictionary<string, List<ProjectTask>> TasksByPhase { get; set; } = new();

    [JsonPropertyName("tasksByStatus")]
    public Dictionary<string, List<ProjectTask>> TasksByStatus { get; set; } = new();

    [JsonPropertyName("tasksByContractor")]
    public Dictionary<string, List<ProjectTask>> TasksByContractor { get; set; } = new();

    [JsonPropertyName("phaseSummaries")]
    public Dictionary<string, PhaseWithTasksDto> PhaseSummaries { get; set; } = new();

    [JsonPropertyName("completedTasks")]
    public List<ProjectTask> CompletedTasks { get; set; } = new();

    [JsonPropertyName("overdueTasks")]
    public List<ProjectTask> OverdueTasks { get; set; } = new();

    [JsonPropertyName("pendingTasks")]
    public List<ProjectTask> PendingTasks { get; set; } = new();

    [JsonPropertyName("tasksAwaitingCompletion")]
    public List<ProjectTask> TasksAwaitingCompletion { get; set; } = new();

    [JsonPropertyName("statistics")]
    public ProjectStatisticsDto Statistics { get; set; } = new();

    [JsonPropertyName("ratedTasks")]
    public Dictionary<string, bool> RatedTasks { get; set; } = new();

    [JsonPropertyName("client")]
    public User? Client { get; set; }
}

public class ProjectStatisticsDto
{
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

    [JsonPropertyName("overallProgress")]
    public int OverallProgress { get; set; }

    [JsonPropertyName("totalPhases")]
    public int TotalPhases { get; set; }

    [JsonPropertyName("completedPhases")]
    public int CompletedPhases { get; set; }

    [JsonPropertyName("tasksByStatusCount")]
    public Dictionary<string, int> TasksByStatusCount { get; set; } = new();

    [JsonPropertyName("phaseProgress")]
    public Dictionary<string, int> PhaseProgress { get; set; } = new();
}

public class PhaseWithTasksDto
{
    [JsonPropertyName("phase")]
    public Phase? Phase { get; set; }

    [JsonPropertyName("tasks")]
    public List<ProjectTask> Tasks { get; set; } = new();

    [JsonPropertyName("taskCount")]
    public int TaskCount { get; set; }

    [JsonPropertyName("completedTaskCount")]
    public int CompletedTaskCount { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }
}
