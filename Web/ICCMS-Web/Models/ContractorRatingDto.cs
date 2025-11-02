using System;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class ContractorRatingDto
    {
        [JsonPropertyName("contractorRatingId")]
        public string ContractorRatingId { get; set; } = string.Empty;

        [JsonPropertyName("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [JsonPropertyName("averageRating")]
        public double AverageRating { get; set; }

        [JsonPropertyName("totalRatings")]
        public int TotalRatings { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class SubmitRatingRequest
    {
        [JsonPropertyName("contractorId")]
        public string ContractorId { get; set; } = string.Empty;

        [JsonPropertyName("ratingValue")]
        public int RatingValue { get; set; }
    }
}

