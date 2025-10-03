using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ICCMS_Web.Models
{
    public class QuotationDto
    {
        [Required]
        public string QuotationId { get; set; } = string.Empty;

        [Required]
        public string ProjectId { get; set; } = string.Empty;

        public string MaintenanceRequestId { get; set; } = string.Empty;

        [Required]
        public string ClientId { get; set; } = string.Empty;

        public string ContractorId { get; set; } = string.Empty;
        public string AdminApproverUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public double Total { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime ValidUntil { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<QuotationItemDto> Items { get; set; } = new();

        public double Subtotal { get; set; }
        public double TaxTotal { get; set; }
        public double GrandTotal { get; set; }

        public string Currency { get; set; } = "ZAR";

        public DateTime? AdminApprovedAt { get; set; }
        public DateTime? ClientRespondedAt { get; set; }
        public string? ClientDecisionNote { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsAiGenerated { get; set; } = false;
        public string? EstimateId { get; set; }
        public DateTime? PmEditedAt { get; set; }
        public string? PmEditNotes { get; set; }
        public DateTime? PmRejectedAt { get; set; }
        public string? PmRejectReason { get; set; }
    }
}
