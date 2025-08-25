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

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
