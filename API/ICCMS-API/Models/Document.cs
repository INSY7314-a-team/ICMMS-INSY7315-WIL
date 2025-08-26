using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class Document
    {
        [FirestoreProperty("documentId")]
        public string DocumentId { get; set; } = string.Empty;

        [FirestoreProperty("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [FirestoreProperty("fileName")]
        public string FileName { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = string.Empty;

        [FirestoreProperty("fileType")]
        public string FileType { get; set; } = string.Empty;

        [FirestoreProperty("fileSize")]
        public long FileSize { get; set; }

        [FirestoreProperty("fileUrl")]
        public string FileUrl { get; set; } = string.Empty;

        [FirestoreProperty("uploadedBy")]
        public string UploadedBy { get; set; } = string.Empty;

        [FirestoreProperty("uploadedAt")]
        public DateTime UploadedAt { get; set; }

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;
    }
}
