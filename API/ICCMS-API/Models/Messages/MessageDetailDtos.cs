using System.Text.Json.Serialization;
using ICCMS_API.Models;

namespace ICCMS_API.Models.Messages;

public class MessageThreadSummaryDto
{
    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = string.Empty;

    [JsonPropertyName("threadType")]
    public string ThreadType { get; set; } = "general";

    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; }

    [JsonPropertyName("lastMessageAt")]
    public DateTime LastMessageAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("participants")]
    public List<string> Participants { get; set; } = new();

    [JsonPropertyName("participantNames")]
    public List<string> ParticipantNames { get; set; } = new();

    [JsonPropertyName("hasUnreadMessages")]
    public bool HasUnreadMessages { get; set; }

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }
}

public class MessageStatisticsDto
{
    [JsonPropertyName("totalThreads")]
    public int TotalThreads { get; set; }

    [JsonPropertyName("totalMessages")]
    public int TotalMessages { get; set; }

    [JsonPropertyName("unreadMessages")]
    public int UnreadMessages { get; set; }

    [JsonPropertyName("activeThisWeek")]
    public int ActiveThisWeek { get; set; }
}

public class MessageDashboardDataDto
{
    [JsonPropertyName("threads")]
    public List<MessageThreadSummaryDto> Threads { get; set; } = new();

    [JsonPropertyName("statistics")]
    public MessageStatisticsDto Statistics { get; set; } = new();

    [JsonPropertyName("availableProjects")]
    public List<ProjectSummaryDto> AvailableProjects { get; set; } = new();

    [JsonPropertyName("availableUsers")]
    public List<UserSummaryDto> AvailableUsers { get; set; } = new();

    [JsonPropertyName("participantMap")]
    public Dictionary<string, UserSummaryDto> ParticipantMap { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

public class UserMessagesDetailDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("workflowThreads")]
    public List<MessageThreadSummaryDto> WorkflowThreads { get; set; } = new();

    [JsonPropertyName("directThreads")]
    public List<MessageThreadSummaryDto> DirectThreads { get; set; } = new();

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("statistics")]
    public MessageStatisticsDto Statistics { get; set; } = new();
}

public class MessageThreadDetailDto
{
    [JsonPropertyName("thread")]
    public MessageThreadSummaryDto Thread { get; set; } = new();

    [JsonPropertyName("messages")]
    public List<MessageWithSender> Messages { get; set; } = new();

    [JsonPropertyName("participants")]
    public Dictionary<string, UserSummaryDto> Participants { get; set; } = new();
}

public class UserSummaryDto
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

public class ProjectSummaryDto
{
    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = string.Empty;
}
