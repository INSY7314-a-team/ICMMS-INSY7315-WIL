using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    public class Quote {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string ProjectId { get; set; } = "";
        public string? ServiceRequestId { get; set; }
        public string ClientName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Status { get; set; } = "Draft"; // Draft, InReview, Sent, Accepted, Rejected
        public List<QuoteItem> Items { get; set; } = new();
        public double Subtotal { get; set; }
        public double MarkupPercent { get; set; } = 10;
        public double TaxPercent { get; set; } = 15;
        public double Total { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
        public DateTime? DecidedAt { get; set; }
        public List<string> AttachmentUrls { get; set; } = new(); // plans, photos
    }

    public class QuoteItem {
    public string Type { get; set; } = "Material";
    public string Name { get; set; } = "";
    public string? Notes { get; set; }
    public double Qty { get; set; }
    public string Unit { get; set; } = "ea";
    public double UnitPrice { get; set; }
    public double LineTotal => Math.Round(Qty * UnitPrice, 2);

    // NEW: assignment
    public string? ContractorId { get; set; }
    public string? ContractorName { get; set; }

    public string? PhaseKey { get; set; }
    public string? PhaseName { get; set; }

}


    public class Rate {
        public string Code { get; set; } = ""; // e.g., MAT-STEEL-10MM
        public string Type { get; set; } = "Material";
        public string Name { get; set; } = "";
        public string Unit { get; set; } = "ea";
        public double UnitPrice { get; set; }
    }

    public class ServiceRequest {
        public string Id { get; set; } = "";
        public string ProjectId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "Normal"; // Low/Normal/High
        public string Status { get; set; } = "New";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
