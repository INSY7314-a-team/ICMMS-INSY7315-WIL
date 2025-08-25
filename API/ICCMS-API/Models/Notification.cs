using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class Notification
    {
        [FirestoreProperty("notificationId")]
        public string NotificationId { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = string.Empty;

        [FirestoreProperty("message")]
        public string Message { get; set; } = string.Empty;

        [FirestoreProperty("type")]
        public string Type { get; set; } = string.Empty;

        [FirestoreProperty("isRead")]
        public bool IsRead { get; set; } = false;

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("readAt")]
        public DateTime? ReadAt { get; set; }

        [FirestoreProperty("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
