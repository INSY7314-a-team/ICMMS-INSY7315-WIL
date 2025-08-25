using Google.Cloud.Firestore;

namespace ICCMS_API.Models
{
    [FirestoreData]
    public class DownloadLog
    {
        [FirestoreProperty("downloadId")]
        public string DownloadId { get; set; } = string.Empty;

        [FirestoreProperty("documentId")]
        public string DocumentId { get; set; } = string.Empty;

        [FirestoreProperty("downloadedBy")]
        public string DownloadedBy { get; set; } = string.Empty;

        [FirestoreProperty("downloadedAt")]
        public DateTime DownloadedAt { get; set; }

        [FirestoreProperty("ipAddress")]
        public string IpAddress { get; set; } = string.Empty;

        [FirestoreProperty("userAgent")]
        public string UserAgent { get; set; } = string.Empty;
    }
}
