using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ICCMS_Web.Models
{
    /// <summary>
    /// ViewModel used in the Estimate step of the Quotation workflow.
    /// Holds all the data needed for calculating totals, displaying line items,
    /// and keeping track of related Project + Client info.
    /// </summary>
    public class EstimateViewModel
    {
        // 🔑 Firestore document ID (Quotation Id)
        // This is critical for updates & workflow steps.
        public string QuotationId { get; set; } = string.Empty;

        // 🔗 Project association (MUST be selected from draft stage)
        [Required(ErrorMessage = "Project is required")]
        public string ProjectId { get; set; } = string.Empty;

        // 🔗 Client association (was missing before!)
        [Required(ErrorMessage = "Client is required")]
        public string ClientId { get; set; } = string.Empty;
        
        // 🏷️ Current workflow status of the quotation (Draft, SentToClient, Declined, etc.)
        public string Status { get; set; } = "Draft";


        // 🔗 Contractor assigned to this quotation (optional / may be set later)
        public string ContractorId { get; set; } = string.Empty;

        // 📝 Description of the Quotation (eg: “Randburg Retail Complex”)
        public string Description { get; set; } = string.Empty;

        // 📅 Expiry date of the quote
        // Firestore needs this to be in UTC → set carefully in controllers
        public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddDays(30);

        // 📦 List of all line items in the estimate
        // Uses a DTO so we can pass API-friendly objects back and forth
        [JsonPropertyName("lineItems")]
        public List<EstimateLineItemDto> Items { get; set; } = new();

        // ⚖️ Tax + markup settings → adjustable if business rules change
        [Range(0, 100, ErrorMessage = "Tax must be between 0 and 100")]
        public double TaxRate { get; set; }

        [Range(0, 100, ErrorMessage = "Markup must be between 0 and 100")]
        public double MarkupRate { get; set; }


        // 💰 Calculated values → always recalculated server-side for integrity
        public double Subtotal { get; set; }
        public double TaxAmount { get; set; }
        public double MarkupAmount { get; set; }
        public double GrandTotal { get; set; }

        // 📊 Display-only fields for the UI (not persisted directly in Firestore)
        public string ProjectName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Recalculate totals based on line items.
        /// Always call this in Controller (never trust client values).
        /// </summary>
        public void RecalculateTotals()
        {
            // ✅ Sum up all line item totals
            Subtotal = Items?.Sum(i => i.LineTotal) ?? 0;

            // ✅ Apply tax + markup using configured rates
            TaxAmount = Subtotal * TaxRate;
            MarkupAmount = Subtotal * MarkupRate;

            // ✅ Final Grand Total
            GrandTotal = Subtotal + TaxAmount + MarkupAmount;
        }
    }
}
