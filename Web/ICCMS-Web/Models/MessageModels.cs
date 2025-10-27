namespace ICCMS_Web.Models
{
    public class MessageDto
    {
        public string MessageId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string ThreadId { get; set; } = string.Empty;
        public string? ParentMessageId { get; set; }
        public bool IsThreadStarter { get; set; } = false;
        public int ThreadDepth { get; set; } = 0;
        public int ReplyCount { get; set; } = 0;
        public DateTime? LastReplyAt { get; set; }
        public List<string> ThreadParticipants { get; set; } = new List<string>();
        public string MessageType { get; set; } = "direct";
        public List<MessageAttachmentDto> Attachments { get; set; } =
            new List<MessageAttachmentDto>();
        public bool HasAttachments { get; set; } = false;
        public int AttachmentCount => Attachments?.Count ?? 0;
    }

    public class MessageWithSender : MessageDto
    {
        // Inherits all properties from MessageDto including SenderName
    }

    public class MessageAttachmentDto
    {
        public string AttachmentId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "general";
        public bool IsImage { get; set; }
        public bool IsDocument { get; set; }
        public string Status { get; set; } = "active";
    }

    public class ThreadDto
    {
        public string ThreadId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public DateTime LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string LastMessageSenderName { get; set; } = string.Empty;
        public string LastMessagePreview { get; set; } = string.Empty;
        public string LastMessageSnippet { get; set; } = string.Empty;
        public List<string> Participants { get; set; } = new List<string>();
        public List<string> ParticipantNames { get; set; } = new List<string>();
        public string ThreadType { get; set; } = string.Empty;
        public bool HasUnreadMessages { get; set; }
        public int UnreadCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateMessageRequest
    {
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ThreadId { get; set; }
        public string? ParentMessageId { get; set; }
        public List<string> ThreadParticipants { get; set; } = new List<string>();
        public string MessageType { get; set; } = "direct";
    }

    public class SendMessageRequest
    {
        public string ReceiverId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ReplyToMessageRequest
    {
        public string ParentMessageId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class SendThreadMessageRequest
    {
        public string ThreadId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class UserMessagesViewModel
    {
        public List<ThreadDto> WorkflowThreads { get; set; } = new List<ThreadDto>();
        public List<ThreadDto> DirectThreads { get; set; } = new List<ThreadDto>();
        public List<MessageDto> SelectedThreadMessages { get; set; } = new List<MessageDto>();
        public string? SelectedThreadId { get; set; }
        public string? SelectedThreadSubject { get; set; }
        public List<UserDto> AvailableUsers { get; set; } = new List<UserDto>();
        public List<ProjectDto> AvailableProjects { get; set; } = new List<ProjectDto>();
        public int UnreadCount { get; set; }
        public string UserRole { get; set; } = string.Empty;
        public string CurrentView { get; set; } = "workflow"; // "workflow" or "direct"
    }

    public class MessageThreadViewModel
    {
        public string ThreadId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public DateTime LastMessageAt { get; set; }
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public List<string> Participants { get; set; } = new List<string>();
        public string ThreadType { get; set; } = "general";
        public bool HasUnreadMessages { get; set; }
        public int UnreadCount { get; set; }
    }

    public class MessageViewModel
    {
        public string MessageId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
        public int AttachmentCount { get; set; }
    }

    // Workflow Message Models
    public class WorkflowMessageDto
    {
        public string WorkflowMessageId { get; set; } = string.Empty;
        public string WorkflowType { get; set; } = string.Empty; // "quote_approval", "invoice_payment", "project_update", "system_alert", "task_assignment"
        public string EntityId { get; set; } = string.Empty; // Quote ID, Invoice ID, Project ID, Task ID, etc.
        public string EntityType { get; set; } = string.Empty; // "quotation", "invoice", "project", "estimate", "task"
        public string Action { get; set; } = string.Empty; // "created", "approved", "rejected", "paid", "overdue", "assigned", "completed"
        public string ProjectId { get; set; } = string.Empty;
        public List<string> Recipients { get; set; } = new List<string>();
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal"; // "low", "normal", "high", "urgent"
        public bool IsSystemGenerated { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string Status { get; set; } = "pending"; // "pending", "sent", "failed", "cancelled"
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class CreateWorkflowMessageRequest
    {
        public string WorkflowType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public List<string> Recipients { get; set; } = new List<string>();
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal";
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    // Message Dashboard Models
    public class MessageThreadsViewModel
    {
        public List<MessageThreadViewModel> Threads { get; set; } =
            new List<MessageThreadViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalThreads { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
        public List<string> AvailableMessageTypes { get; set; } = new List<string>();
        public List<ProjectSummary> AvailableProjects { get; set; } = new List<ProjectSummary>();
        public List<UserSummary> AvailableUsers { get; set; } = new List<UserSummary>();
        public string CurrentFilter { get; set; } = "all";
        public string ProjectFilter { get; set; } = "all";
        public string UserFilter { get; set; } = "all";
        public string ThreadFilter { get; set; } = "all";
        public string ReadFilter { get; set; } = "all";
        public string CurrentSearchTerm { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartIndex => (CurrentPage - 1) * PageSize + 1;
        public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalThreads);
    }

    public class ProjectSummary
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;

        // For backward compatibility with DocumentsController
        public string Id
        {
            get => ProjectId;
            set => ProjectId = value;
        }
        public string Name
        {
            get => ProjectName;
            set => ProjectName = value;
        }
    }

    public class UserSummary
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        // For backward compatibility with DocumentsController
        public string Id
        {
            get => UserId;
            set => UserId = value;
        }
        public string FullName
        {
            get => UserName;
            set => UserName = value;
        }
        public string Email
        {
            get => UserName;
            set => UserName = value;
        }
    }
}
