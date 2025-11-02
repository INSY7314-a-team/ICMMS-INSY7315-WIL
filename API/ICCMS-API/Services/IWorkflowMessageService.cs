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
        Task<bool> CreateWorkflowMessageAsync(SystemEvent systemEvent);

        Task<List<WorkflowMessageTemplate>> GetMessageTemplatesAsync();
        Task<WorkflowMessageTemplate?> GetMessageTemplateAsync(string workflowType, string action);
        Task<bool> MarkWorkflowMessageAsReadAsync(string workflowMessageId, string userId);
    }
}
