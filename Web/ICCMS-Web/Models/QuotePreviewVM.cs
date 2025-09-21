namespace ICCMS_Web.Models
{
    public class QuotePreviewVM
    {
        // From wizard
        public string? ProjectId { get; set; }
        public string? Title { get; set; }
        public double MarkupPercent { get; set; } = 10;
        public double TaxPercent { get; set; } = 15;

        // Client details (from clients.json)
        public string? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientOrg { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientAddress { get; set; }

        // Items coming from the modal
        public List<PreviewItem> Items { get; set; } = new();

        // Calculated for preview
        public double Subtotal { get; set; }
        public double MarkupAmount { get; set; }
        public double TaxAmount { get; set; }
        public double Total { get; set; }

        // 🔹 NEW: when reopening, keep track of the old quote being replaced
        public string? OriginalQuoteId { get; set; }
    }

    public class PreviewItem
    {
        public string? Type { get; set; } = "Material";
        public string? Name { get; set; }
        public double Qty { get; set; }
        public string? Unit { get; set; } = "ea";
        public double UnitPrice { get; set; }
        public string? ContractorId { get; set; }
        public string? ContractorName { get; set; }
        public string? PhaseKey { get; set; }
        public string? PhaseName { get; set; }
    }
}
