using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class PaginatedResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new List<T>();

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage => Page < TotalPages;

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage => Page > 1;
    }
}
