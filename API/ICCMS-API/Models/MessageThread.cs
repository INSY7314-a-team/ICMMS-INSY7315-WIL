using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class MessageThread
    {
        [FirestoreProperty("threadId")]
        public string ThreadId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("subject")]
        public string Subject { get; set; } = string.Empty;

        [FirestoreProperty("starterMessageId")]
        public string StarterMessageId { get; set; } = string.Empty;

        [FirestoreProperty("starterUserId")]
        public string StarterUserId { get; set; } = string.Empty;

        [FirestoreProperty("participants")]
        public List<string> Participants { get; set; } = new List<string>();

        [FirestoreProperty("messageCount")]
        public int MessageCount { get; set; } = 0;

        [FirestoreProperty("lastMessageAt")]
        public DateTime LastMessageAt { get; set; }

        [FirestoreProperty("lastMessageId")]
        public string LastMessageId { get; set; } = string.Empty;

        [FirestoreProperty("lastMessageSenderId")]
        public string LastMessageSenderId { get; set; } = string.Empty;

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;

        [FirestoreProperty("threadType")]
        public string ThreadType { get; set; } = "general"; // "general", "support", "project_update", "quotation", "invoice"

        [FirestoreProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class CreateThreadRequest
    {
        [Required(ErrorMessage = "Project ID is required")]
        public string ProjectId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(
            MessageValidationRules.MaxSubjectLength,
            ErrorMessage = "Subject cannot exceed 200 characters"
        )]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(
            MessageValidationRules.MaxContentLength,
            MinimumLength = MessageValidationRules.MinContentLength,
            ErrorMessage = "Content must be between 1 and 5000 characters"
        )]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one participant is required")]
        [MinLength(1, ErrorMessage = "At least one participant is required")]
        public List<string> Participants { get; set; } = new List<string>();

        public string ThreadType { get; set; } = "general";
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class ReplyToMessageRequest
    {
        [Required(ErrorMessage = "Parent message ID is required")]
        public string ParentMessageId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(
            MessageValidationRules.MaxContentLength,
            MinimumLength = MessageValidationRules.MinContentLength,
            ErrorMessage = "Content must be between 1 and 5000 characters"
        )]
        public string Content { get; set; } = string.Empty;

        public List<string> AdditionalRecipients { get; set; } = new List<string>();
    }

    public class ThreadSummary
    {
        public string ThreadId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public DateTime LastMessageAt { get; set; }
        public string LastMessageSenderName { get; set; } = string.Empty;
        public string LastMessagePreview { get; set; } = string.Empty;
        public List<string> Participants { get; set; } = new List<string>();
        public string ThreadType { get; set; } = string.Empty;
        public bool HasUnreadMessages { get; set; }
        public int UnreadCount { get; set; }
    }
}
