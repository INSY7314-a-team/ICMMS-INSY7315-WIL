using System;

namespace ICCMS_Web.Models
{
    // 👀 This view model is only for displaying rows in the Index.cshtml
    public class QuoteIndexViewModel
    {
        public string QuotationId { get; set; } = string.Empty;

        // Core quote data
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double GrandTotal { get; set; }
        public DateTime CreatedAt { get; set; }

        // Related Project
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = "Unknown Project";

        // Related Client
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = "Unknown Client";
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }

    }
}
