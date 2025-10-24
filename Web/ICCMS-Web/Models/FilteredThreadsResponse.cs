using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class FilteredThreadsResponse
    {
        public List<ThreadSummary> Threads { get; set; } = new List<ThreadSummary>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ThreadSummary
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
        public List<string> Participants { get; set; } = new List<string>();
        public List<string> ParticipantNames { get; set; } = new List<string>();
        public string ThreadType { get; set; } = string.Empty;
        public bool HasUnreadMessages { get; set; }
        public int UnreadCount { get; set; }
        public bool IsActive { get; set; }
    }
}
