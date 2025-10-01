using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public interface IWorkflowMessageService
    {
        Task<string> CreateWorkflowMessageAsync(WorkflowMessage workflowMessage);
        Task<List<WorkflowMessage>> GetWorkflowMessagesAsync(
            string? projectId = null,
            string? workflowType = null
        );
        Task<WorkflowMessage?> GetWorkflowMessageAsync(string workflowMessageId);
        Task<bool> ProcessSystemEventAsync(SystemEvent systemEvent);
        Task<bool> SendQuoteApprovalNotificationAsync(string quoteId, string action, string userId);
        Task<bool> SendInvoicePaymentNotificationAsync(
            string invoiceId,
            string action,
            string userId
        );
        Task<bool> SendProjectUpdateNotificationAsync(
            string projectId,
            string updateType,
            string userId
        );
        Task<bool> SendSystemAlertAsync(string alertType, string message, List<string> recipients);
        Task<List<WorkflowMessageTemplate>> GetMessageTemplatesAsync();
        Task<WorkflowMessageTemplate?> GetMessageTemplateAsync(string workflowType, string action);
    }
}
