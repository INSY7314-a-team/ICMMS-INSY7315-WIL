using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public interface IQuoteWorkflowService
    {
        Task<Quotation?> SubmitForApprovalAsync(string quotationId);      // Draft -> PendingPMApproval
        Task<Quotation?> PmApproveAsync(string quotationId);              // PendingPMApproval -> SentToClient (sets ApprovedAt/SentAt)
        Task<Quotation?> SendToClientAsync(string quotationId);           // idempotent -> SentToClient
        Task<Quotation?> ClientDecisionAsync(string quotationId, bool accept, string? note); // SentToClient -> ClientAccepted/ClientDeclined
        Task<(string invoiceId, Invoice invoice)?> ConvertToInvoiceAsync(string quotationId); // if you added conversion
    }
}
