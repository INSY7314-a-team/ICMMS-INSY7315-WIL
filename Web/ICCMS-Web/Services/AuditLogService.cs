using System.Security.Claims;

namespace ICCMS_Web.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(IApiClient apiClient, ILogger<AuditLogService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task LogAsync(
            string logType,
            string title,
            string description,
            string userId,
            string entityId,
            ClaimsPrincipal user
        )
        {
            try
            {
                _logger.LogInformation(
                    "Creating audit log: {LogType} - {Title} for user {UserId}",
                    logType,
                    title,
                    userId
                );

                var auditLog = new
                {
                    logType = logType,
                    title = title,
                    description = description,
                    userId = userId,
                    entityId = entityId,
                };

                var result = await _apiClient.PostAsync<string>("/api/auditlogs", auditLog, user);
                _logger.LogInformation(
                    "Audit log created successfully: {LogType} - {Title}, Result: {Result}",
                    logType,
                    title,
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create audit log: {LogType} - {Title} for user {UserId}",
                    logType,
                    title,
                    userId
                );
            }
        }

        public async Task LogLoginSuccessAsync(string userId, string email, ClaimsPrincipal user)
        {
            await LogAsync(
                "Login Success",
                $"User {email} logged in successfully",
                $"User {email} with ID {userId} successfully authenticated",
                userId,
                userId,
                user
            );
        }

        public async Task LogLoginFailureAsync(string email, string reason)
        {
            // Create a minimal ClaimsPrincipal for failed login events
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email),
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "Login");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await LogAsync(
                "Login Failure",
                $"Failed login attempt for {email}",
                $"Login failed for {email}. Reason: {reason}",
                email, // Use email as identifier for failed logins
                email,
                principal
            );
        }

        public async Task LogProjectCreatedAsync(
            string projectId,
            string projectName,
            ClaimsPrincipal user
        )
        {
            try
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                _logger.LogInformation(
                    "Logging project creation: {ProjectName} ({ProjectId}) by user {UserId}",
                    projectName,
                    projectId,
                    userId
                );

                await LogAsync(
                    "Project Created",
                    $"Project '{projectName}' created",
                    $"Project '{projectName}' with ID {projectId} was created",
                    userId,
                    projectId,
                    user
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to log project creation for {ProjectName} ({ProjectId})",
                    projectName,
                    projectId
                );
            }
        }

        public async Task LogProjectUpdatedAsync(
            string projectId,
            string projectName,
            ClaimsPrincipal user
        )
        {
            try
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                _logger.LogInformation(
                    "Logging project update: {ProjectName} ({ProjectId}) by user {UserId}",
                    projectName,
                    projectId,
                    userId
                );

                await LogAsync(
                    "Project Updated",
                    $"Project '{projectName}' updated",
                    $"Project '{projectName}' with ID {projectId} was updated",
                    userId,
                    projectId,
                    user
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to log project update for {ProjectName} ({ProjectId})",
                    projectName,
                    projectId
                );
            }
        }

        public async Task LogTaskStartedAsync(
            string taskId,
            string taskName,
            string projectId,
            ClaimsPrincipal user
        )
        {
            try
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                _logger.LogInformation(
                    "Logging task start: {TaskName} ({TaskId}) in project {ProjectId} by user {UserId}",
                    taskName,
                    taskId,
                    projectId,
                    userId
                );

                await LogAsync(
                    "Task Started",
                    $"Task '{taskName}' started",
                    $"Task '{taskName}' with ID {taskId} was started by contractor",
                    userId,
                    taskId,
                    user
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to log task start for {TaskName} ({TaskId})",
                    taskName,
                    taskId
                );
            }
        }

        public async Task LogContractorAssignmentAsync(
            string taskId,
            string contractorId,
            string projectId,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await LogAsync(
                "Contractor Assignment",
                $"Task {taskId} assigned to contractor {contractorId}",
                $"Task {taskId} in project {projectId} was assigned to contractor {contractorId}",
                userId,
                taskId,
                user
            );
        }

        public async Task LogBlueprintProcessingAsync(
            string projectId,
            string blueprintUrl,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await LogAsync(
                "Blueprint Processing",
                $"Blueprint processed for project {projectId}",
                $"Blueprint from URL {blueprintUrl} was processed for project {projectId}",
                userId,
                projectId,
                user
            );
        }

        public async Task LogQuotationActionAsync(
            string quotationId,
            bool approved,
            string clientId,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var action = approved ? "Approved" : "Rejected";
            var logType = approved ? "Quotation Approved" : "Quotation Rejected";

            await LogAsync(
                logType,
                $"Quotation {quotationId} {action.ToLower()}",
                $"Quotation {quotationId} was {action.ToLower()} by client {clientId}",
                userId,
                quotationId,
                user
            );
        }

        public async Task LogFileUploadAsync(
            string documentId,
            string fileName,
            string projectId,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await LogAsync(
                "Document Upload",
                $"File '{fileName}' uploaded",
                $"File '{fileName}' with ID {documentId} was uploaded for project {projectId}",
                userId,
                documentId,
                user
            );
        }

        public async Task LogFileDownloadAsync(
            string documentId,
            string fileName,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await LogAsync(
                "Document Download",
                $"File '{fileName}' downloaded",
                $"File '{fileName}' with ID {documentId} was downloaded",
                userId,
                documentId,
                user
            );
        }

        public async Task LogFileAccessAsync(
            string documentId,
            string fileName,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await LogAsync(
                "Document Access",
                $"File '{fileName}' accessed",
                $"File '{fileName}' with ID {documentId} was accessed",
                userId,
                documentId,
                user
            );
        }

        public async Task LogProgressReportAsync(
            string taskId,
            string contractorId,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await LogAsync(
                "Progress Report Submitted",
                $"Progress report submitted for task {taskId}",
                $"Progress report was submitted by contractor {contractorId} for task {taskId}",
                userId,
                taskId,
                user
            );
        }

        public async Task LogCompletionReportAsync(
            string taskId,
            string contractorId,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await LogAsync(
                "Completion Report Submitted",
                $"Completion report submitted for task {taskId}",
                $"Completion report was submitted by contractor {contractorId} for task {taskId}",
                userId,
                taskId,
                user
            );
        }

        public async Task LogTaskCompletionApprovalAsync(
            string taskId,
            bool approved,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var action = approved ? "Approved" : "Rejected";
            var logType = approved ? "Task Completion Approved" : "Task Completion Rejected";

            await LogAsync(
                logType,
                $"Task completion {action.ToLower()} for task {taskId}",
                $"Task completion for task {taskId} was {action.ToLower()} by project manager",
                userId,
                taskId,
                user
            );
        }

        public async Task LogMaintenanceRequestAsync(
            string requestId,
            string projectId,
            ClaimsPrincipal user
        )
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            await LogAsync(
                "Maintenance Request Created",
                $"Maintenance request {requestId} created",
                $"Maintenance request {requestId} was created for project {projectId}",
                userId,
                requestId,
                user
            );
        }
    }
}
