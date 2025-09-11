using ICCMS_API.Models;
using ICCMS_API.Helpers;

namespace ICCMS_API.Services
{
    public class QuoteWorkflowService : IQuoteWorkflowService
    {
        private readonly IFirebaseService _firebaseService;

        public QuoteWorkflowService(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        public async Task<Quotation?> SubmitForApprovalAsync(string quotationId)
        {
            var quotation = await _firebaseService.GetDocumentAsync<Quotation>("quotations", quotationId);
            if (quotation == null) return null;

            // Check if already in correct status (idempotent)
            if (quotation.Status == "PendingPMApproval")
            {
                return quotation;
            }

            // Validate current status
            if (quotation.Status != "Draft")
            {
                throw new InvalidOperationException("Quotation must be in Draft status to submit for approval");
            }

            // Validate requirements
            if (!quotation.Items.Any() || quotation.GrandTotal <= 0)
            {
                throw new InvalidOperationException("Quotation must have items and GrandTotal > 0 to submit for approval");
            }

            // Update status
            quotation.Status = "PendingPMApproval";
            quotation.UpdatedAt = DateTime.UtcNow;

            await _firebaseService.UpdateDocumentAsync("quotations", quotationId, quotation);
            return quotation;
        }

        public async Task<Quotation?> PmApproveAsync(string quotationId)
        {
            var quotation = await _firebaseService.GetDocumentAsync<Quotation>("quotations", quotationId);
            if (quotation == null) return null;

            // Check if already in correct status (idempotent)
            if (quotation.Status == "SentToClient")
            {
                return quotation;
            }

            // Validate current status
            if (quotation.Status != "PendingPMApproval")
            {
                throw new InvalidOperationException("Quotation must be in PendingPMApproval status to approve");
            }

            // Update status and timestamps
            quotation.Status = "SentToClient";
            quotation.AdminApprovedAt = DateTime.UtcNow; // Keep the field name for backward compatibility
            quotation.SentAt = DateTime.UtcNow;
            quotation.UpdatedAt = DateTime.UtcNow;

            await _firebaseService.UpdateDocumentAsync("quotations", quotationId, quotation);
            return quotation;
        }

        public async Task<Quotation?> PmRejectAsync(string quotationId, string? reason = null)
        {
            var quotation = await _firebaseService.GetDocumentAsync<Quotation>("quotations", quotationId);
            if (quotation == null) return null;

            // Check if already in correct status (idempotent)
            if (quotation.Status == "PMRejected")
            {
                return quotation;
            }

            // Validate current status
            if (quotation.Status != "PendingPMApproval")
            {
                throw new InvalidOperationException("Quotation must be in PendingPMApproval status to reject");
            }

            // Update status and timestamps
            quotation.Status = "PMRejected";
            quotation.PmRejectedAt = DateTime.UtcNow;
            quotation.PmRejectReason = reason;
            quotation.UpdatedAt = DateTime.UtcNow;

            await _firebaseService.UpdateDocumentAsync("quotations", quotationId, quotation);
            return quotation;
        }

        public async Task<Quotation?> SendToClientAsync(string quotationId)
        {
            var quotation = await _firebaseService.GetDocumentAsync<Quotation>("quotations", quotationId);
            if (quotation == null) return null;

            // Check if already in correct status (idempotent)
            if (quotation.Status == "SentToClient")
            {
                return quotation;
            }

            // Validate current status
            if (quotation.Status != "PendingPMApproval")
            {
                throw new InvalidOperationException("Quotation must be in PendingPMApproval status to send to client");
            }

            // Update status and timestamps
            quotation.Status = "SentToClient";
            quotation.AdminApprovedAt = DateTime.UtcNow;
            quotation.SentAt = DateTime.UtcNow;
            quotation.UpdatedAt = DateTime.UtcNow;

            await _firebaseService.UpdateDocumentAsync("quotations", quotationId, quotation);
            return quotation;
        }

        public async Task<Quotation?> ClientDecisionAsync(string quotationId, bool accept, string? note)
        {
            var quotation = await _firebaseService.GetDocumentAsync<Quotation>("quotations", quotationId);
            if (quotation == null) return null;

            // Validate current status
            if (quotation.Status != "SentToClient")
            {
                throw new InvalidOperationException("Quotation must be in SentToClient status for client decision");
            }

            // Check if not expired
            if (quotation.ValidUntil < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Quotation has expired");
            }

            // Check if already decided (idempotent)
            if (quotation.Status == "ClientAccepted" || quotation.Status == "ClientDeclined")
            {
                return quotation;
            }

            // Update status and timestamps
            quotation.ClientRespondedAt = DateTime.UtcNow;
            quotation.ClientDecisionNote = note;
            quotation.UpdatedAt = DateTime.UtcNow;

            if (accept)
            {
                quotation.Status = "ClientAccepted";
            }
            else
            {
                quotation.Status = "ClientDeclined";
            }

            await _firebaseService.UpdateDocumentAsync("quotations", quotationId, quotation);
            return quotation;
        }

        public async Task<(string invoiceId, Invoice invoice)?> ConvertToInvoiceAsync(string quotationId)
        {
            var quotation = await _firebaseService.GetDocumentAsync<Quotation>("quotations", quotationId);
            if (quotation == null) return null;

            // Validate status
            if (quotation.Status != "ClientAccepted")
            {
                throw new InvalidOperationException("Only ClientAccepted quotations can be converted to invoice");
            }

            // Check if already converted (idempotency)
            var allInvoices = await _firebaseService.GetCollectionAsync<Invoice>("invoices");
            var existing = allInvoices.FirstOrDefault(i => i.QuotationId == quotation.QuotationId);
            if (existing != null)
            {
                return (existing.InvoiceId, existing);
            }

            // Create new invoice from quotation
            var invoice = new Invoice
            {
                ProjectId = quotation.ProjectId,
                ClientId = quotation.ClientId, // Carry the same ClientId for ownership
                ContractorId = quotation.ContractorId,
                Description = quotation.Description,
                QuotationId = quotation.QuotationId,
                Currency = quotation.Currency,
                Items = quotation.Items.Select(i => new InvoiceItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TaxRate = i.TaxRate
                }).ToList(),
                Status = "Draft",
                IssuedDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30), // 30 days from now
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Calculate totals
            Pricing.Recalculate(invoice);

            // Generate invoice number
            invoice.InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

            var invoiceId = await _firebaseService.AddDocumentAsync("invoices", invoice);
            return (invoiceId, invoice);
        }
    }
}
