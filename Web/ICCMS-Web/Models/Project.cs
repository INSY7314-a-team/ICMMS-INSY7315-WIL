namespace ICCMS_Web.Models;

public class Project
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public decimal Budget { get; set; }
    public decimal Spent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalTasks { get; set; }
    public int Contractors { get; set; }
    public List<ProjectPhase> Phases { get; set; } = new();
}

public class ProjectPhase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PhaseStatus Status { get; set; }
    public string Duration { get; set; } = string.Empty;
    public decimal Budget { get; set; }
}

public enum ProjectStatus
{
    OnTrack,
    AtRisk,
    Delayed,
    Completed,
}

public enum PhaseStatus
{
    Completed,
    InProgress,
    Pending,
    Delayed,
}
