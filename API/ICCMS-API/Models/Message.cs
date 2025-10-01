using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class Message
    {
        [FirestoreProperty("messageId")]
        public string MessageId { get; set; } = string.Empty;

        [FirestoreProperty("senderId")]
        public string SenderId { get; set; } = string.Empty;

        [FirestoreProperty("receiverId")]
        public string ReceiverId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("subject")]
        public string Subject { get; set; } = string.Empty;

        [FirestoreProperty("content")]
        public string Content { get; set; } = string.Empty;

        [FirestoreProperty("isRead")]
        public bool IsRead { get; set; } = false;

        [FirestoreProperty("sentAt")]
        public DateTime SentAt { get; set; }

        [FirestoreProperty("readAt")]
        public DateTime? ReadAt { get; set; }

        // Threading fields
        [FirestoreProperty("threadId")]
        public string ThreadId { get; set; } = string.Empty;

        [FirestoreProperty("parentMessageId")]
        public string? ParentMessageId { get; set; }

        [FirestoreProperty("isThreadStarter")]
        public bool IsThreadStarter { get; set; } = false;

        [FirestoreProperty("threadDepth")]
        public int ThreadDepth { get; set; } = 0;

        [FirestoreProperty("replyCount")]
        public int ReplyCount { get; set; } = 0;

        [FirestoreProperty("lastReplyAt")]
        public DateTime? LastReplyAt { get; set; }

        [FirestoreProperty("threadParticipants")]
        public List<string> ThreadParticipants { get; set; } = new List<string>();

        [FirestoreProperty("messageType")]
        public string MessageType { get; set; } = "direct"; // "direct", "thread", "broadcast"

        // File attachment fields
        [FirestoreProperty("attachments")]
        public List<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();

        [FirestoreProperty("hasAttachments")]
        public bool HasAttachments { get; set; } = false;
    }
}
