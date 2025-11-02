using ICCMS_API.Helpers;
using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public class InvoiceWorkflowService : IInvoiceWorkflowService
    {
        private readonly IFirebaseService _firebaseService;

        public InvoiceWorkflowService(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        public async Task<Invoice?> IssueAsync(string invoiceId)
        {
            var invoice = await _firebaseService.GetDocumentAsync<Invoice>("invoices", invoiceId);
            if (invoice == null)
                return null;

            // Check if already in correct status (idempotent)
            if (invoice.Status == "Issued")
            {
                return invoice;
            }

            // Validate current status
            if (invoice.Status != "Draft")
            {
                throw new InvalidOperationException("Invoice must be in Draft status to issue");
            }

            // Validate DueDate is set
            if (invoice.DueDate == default(DateTime))
            {
                throw new InvalidOperationException("DueDate must be set before issuing invoice");
            }

            // Update status and timestamps
            invoice.Status = "Issued";
            invoice.IssuedDate = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _firebaseService.UpdateDocumentAsync("invoices", invoiceId, invoice);
            return invoice;
        }

        public async Task<Invoice?> MarkPaidAsync(
            string invoiceId,
            DateTime paidDate,
            string paidBy
        )
        {
            var invoice = await _firebaseService.GetDocumentAsync<Invoice>("invoices", invoiceId);
            if (invoice == null)
                return null;

            // Check if already in correct status (idempotent)
            if (invoice.Status == "Paid")
            {
                return invoice;
            }

            // Validate current status
            if (invoice.Status != "Issued")
            {
                throw new InvalidOperationException(
                    "Invoice must be in Issued status to mark as paid"
                );
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(paidBy))
            {
                throw new InvalidOperationException("PaidBy is required");
            }

            // Update status and timestamps
            invoice.Status = "Paid";
            invoice.PaidDate = paidDate;
            invoice.PaidBy = paidBy;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _firebaseService.UpdateDocumentAsync("invoices", invoiceId, invoice);
            return invoice;
        }

        public async Task<Invoice?> CancelAsync(string invoiceId)
        {
            var invoice = await _firebaseService.GetDocumentAsync<Invoice>("invoices", invoiceId);
            if (invoice == null)
                return null;

            // Check if already cancelled (idempotent)
            if (invoice.Status == "Cancelled")
            {
                return invoice;
            }

            // Validate current status - can only cancel Draft or Issued invoices
            if (invoice.Status != "Draft" && invoice.Status != "Issued")
            {
                throw new InvalidOperationException(
                    "Invoice can only be cancelled if in Draft or Issued status"
                );
            }

            // Update status
            invoice.Status = "Cancelled";
            invoice.UpdatedAt = DateTime.UtcNow;

            await _firebaseService.UpdateDocumentAsync("invoices", invoiceId, invoice);
            return invoice;
        }
    }
}
