using System;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class DocumentDto
    {
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; } = string.Empty;

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        // API may return FileUrl; UI expects Url. We'll map in service, but keep both.
        [JsonPropertyName("fileUrl")]
        public string? FileUrl { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        // API uses UploadedAt
        [JsonPropertyName("uploadedAt")]
        public DateTime? UploadedAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }
    }
}
