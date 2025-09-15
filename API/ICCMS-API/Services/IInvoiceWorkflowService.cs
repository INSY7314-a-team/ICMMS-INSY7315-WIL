using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public interface IInvoiceWorkflowService
    {
        Task<Invoice?> IssueAsync(string invoiceId);          // Draft -> Issued (requires DueDate)
        Task<Invoice?> MarkPaidAsync(string invoiceId, DateTime paidDate, string paidBy); // Issued -> Paid
        Task<Invoice?> CancelAsync(string invoiceId);         // cancel rules as you had
    }
}
