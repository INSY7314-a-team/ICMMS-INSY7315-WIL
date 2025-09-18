using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class MessageAttachment
    {
        [FirestoreProperty("attachmentId")]
        public string AttachmentId { get; set; } = string.Empty;

        [FirestoreProperty("messageId")]
        public string MessageId { get; set; } = string.Empty;

        [FirestoreProperty("fileName")]
        public string FileName { get; set; } = string.Empty;

        [FirestoreProperty("originalFileName")]
        public string OriginalFileName { get; set; } = string.Empty;

        [FirestoreProperty("fileType")]
        public string FileType { get; set; } = string.Empty;

        [FirestoreProperty("fileSize")]
        public long FileSize { get; set; }

        [FirestoreProperty("fileUrl")]
        public string FileUrl { get; set; } = string.Empty;

        [FirestoreProperty("thumbnailUrl")]
        public string? ThumbnailUrl { get; set; }

        [FirestoreProperty("uploadedBy")]
        public string UploadedBy { get; set; } = string.Empty;

        [FirestoreProperty("uploadedAt")]
        public DateTime UploadedAt { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("isImage")]
        public bool IsImage { get; set; } = false;

        [FirestoreProperty("isDocument")]
        public bool IsDocument { get; set; } = false;

        [FirestoreProperty("category")]
        public string Category { get; set; } = "general"; // "general", "blueprint", "photo", "document", "contract"

        [FirestoreProperty("status")]
        public string Status { get; set; } = "active"; // "active", "deleted", "archived"
    }

    public class AttachmentUploadRequest
    {
        public string MessageId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "general";
    }

    public class AttachmentResponse
    {
        public string AttachmentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsImage { get; set; }
        public bool IsDocument { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class MessageWithAttachments
    {
        public string MessageId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string ThreadId { get; set; } = string.Empty;
        public string? ParentMessageId { get; set; }
        public bool IsThreadStarter { get; set; }
        public int ThreadDepth { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public bool HasAttachments { get; set; }
        public List<AttachmentResponse> Attachments { get; set; } = new List<AttachmentResponse>();
    }
}
