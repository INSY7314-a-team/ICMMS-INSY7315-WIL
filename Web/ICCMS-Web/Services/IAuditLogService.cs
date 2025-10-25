using System.Security.Claims;

namespace ICCMS_Web.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
            string logType,
            string title,
            string description,
            string userId,
            string entityId,
            ClaimsPrincipal user
        );
        Task LogLoginSuccessAsync(string userId, string email, ClaimsPrincipal user);
        Task LogLoginFailureAsync(string email, string reason);
        Task LogProjectCreatedAsync(string projectId, string projectName, ClaimsPrincipal user);
        Task LogProjectUpdatedAsync(string projectId, string projectName, ClaimsPrincipal user);
        Task LogTaskStartedAsync(
            string taskId,
            string taskName,
            string projectId,
            ClaimsPrincipal user
        );
        Task LogContractorAssignmentAsync(
            string taskId,
            string contractorId,
            string projectId,
            ClaimsPrincipal user
        );
        Task LogBlueprintProcessingAsync(
            string projectId,
            string blueprintUrl,
            ClaimsPrincipal user
        );
        Task LogQuotationActionAsync(
            string quotationId,
            bool approved,
            string clientId,
            ClaimsPrincipal user
        );
        Task LogFileUploadAsync(
            string documentId,
            string fileName,
            string projectId,
            ClaimsPrincipal user
        );
        Task LogFileDownloadAsync(string documentId, string fileName, ClaimsPrincipal user);
        Task LogFileAccessAsync(string documentId, string fileName, ClaimsPrincipal user);
        Task LogProgressReportAsync(string taskId, string contractorId, ClaimsPrincipal user);
        Task LogCompletionReportAsync(string taskId, string contractorId, ClaimsPrincipal user);
        Task LogTaskCompletionApprovalAsync(string taskId, bool approved, ClaimsPrincipal user);
        Task LogMaintenanceRequestAsync(string requestId, string projectId, ClaimsPrincipal user);
    }
}
