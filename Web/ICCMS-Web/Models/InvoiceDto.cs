using System;
using System.Collections.Generic;

namespace ICCMS_Web.Models
{
    public class InvoiceDto
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ContractorId { get; set; } = string.Empty;

        public string InvoiceNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public double Amount { get; set; }
        public double TaxAmount { get; set; }
        public double TotalAmount { get; set; }

        public double Subtotal { get; set; }
        public double TaxTotal { get; set; }
        public double SubtotalWithMarkup { get; set; }
        public double TaxTotalWithMarkup { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }

        public string PaidBy { get; set; } = string.Empty;
        public string Currency { get; set; } = "ZAR";
        public string? QuotationId { get; set; }

        public List<InvoiceItemDto>? Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InvoiceItemDto
    {
        public string Name { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TaxRate { get; set; }
        public double LineTotal { get; set; }
    }
}
