using System.Security.Claims;
using ICCMS_Web.Models;
using ICCMS_Web.Services;

namespace ICCMS_Web.Helpers
{
    public static class MessageHelper
    {
        /// <summary>
        /// Send a system message to a user
        /// </summary>
        public static async Task<bool> SendSystemMessageAsync(
            IMessagingService messagingService,
            string receiverId,
            string projectId,
            string subject,
            string content
        )
        {
            try
            {
                return await messagingService.SendSystemMessageAsync(
                    receiverId,
                    projectId,
                    subject,
                    content
                );
            }
            catch (Exception)
            {
                // Log error but don't throw to avoid breaking workflow
                return false;
            }
        }

        /// <summary>
        /// Send task assignment message to contractor
        /// </summary>
        public static async Task<bool> SendTaskAssignmentMessageAsync(
            IMessagingService messagingService,
            string contractorId,
            string projectId,
            string projectName,
            string taskName,
            string taskDescription,
            DateTime startDate,
            DateTime dueDate
        )
        {
            var subject = $"🎯 New Task Assignment: {taskName}";
            var content =
                $"You have been assigned a new task in project **{projectName}**.\n\n"
                + $"**Task:** {taskName}\n"
                + $"**Description:** {taskDescription}\n"
                + $"**Start Date:** {startDate:MMM dd, yyyy}\n"
                + $"**Due Date:** {dueDate:MMM dd, yyyy}\n\n"
                + "Please review the task details and begin work as soon as possible.";

            return await messagingService.SendWorkflowMessageAsync(
                contractorId,
                projectId,
                subject,
                content,
                "task_assignment" // Use specific workflow type
            );
        }

        /// <summary>
        /// Send quote sent to client message
        /// </summary>
        public static async Task<bool> SendQuoteSentMessageAsync(
            IMessagingService messagingService,
            string clientId,
            string projectId,
            string projectName,
            string quotationId,
            decimal totalAmount
        )
        {
            var subject = $"📋 New Quotation for Project: {projectName}";
            var content =
                $"A new quotation has been sent to you for project **{projectName}**.\n\n"
                + $"**Quotation ID:** {quotationId}\n"
                + $"**Total Amount:** {totalAmount:C}\n\n"
                + "Please review the quotation in your client dashboard and approve or reject it at your earliest convenience.";

            return await messagingService.SendWorkflowMessageAsync(
                clientId,
                projectId,
                subject,
                content,
                "quote_sent" // Use specific workflow type
            );
        }

        /// <summary>
        /// Send quote approved message to PM
        /// </summary>
        public static async Task<bool> SendQuoteApprovedMessageAsync(
            IMessagingService messagingService,
            string pmId,
            string projectId,
            string projectName,
            string clientName,
            string quotationId,
            decimal totalAmount
        )
        {
            var subject = $"✅ Quotation Approved: {projectName}";
            var content =
                $"Client **{clientName}** has approved quotation **{quotationId}** for project **{projectName}**.\n\n"
                + $"**Total Amount:** {totalAmount:C}\n\n"
                + "You can now proceed with project planning and task assignments.";

            return await messagingService.SendWorkflowMessageAsync(
                pmId,
                projectId,
                subject,
                content,
                "quote_approval" // Use specific workflow type
            );
        }

        /// <summary>
        /// Send quote rejected message to PM
        /// </summary>
        public static async Task<bool> SendQuoteRejectedMessageAsync(
            IMessagingService messagingService,
            string pmId,
            string projectId,
            string projectName,
            string clientName,
            string quotationId
        )
        {
            var subject = $"❌ Quotation Rejected: {projectName}";
            var content =
                $"Client **{clientName}** has rejected quotation **{quotationId}** for project **{projectName}**.\n\n"
                + "Please review the client's feedback and take appropriate action.";

            return await messagingService.SendWorkflowMessageAsync(
                pmId,
                projectId,
                subject,
                content,
                "quote_rejection" // Use specific workflow type
            );
        }

        /// <summary>
        /// Send task completed message to PM
        /// </summary>
        public static async Task<bool> SendTaskCompletedMessageAsync(
            IMessagingService messagingService,
            string pmId,
            string projectId,
            string projectName,
            string contractorName,
            string taskName
        )
        {
            var subject = $"✅ Task Completed: {taskName} in Project {projectName}";
            var content =
                $"Contractor **{contractorName}** has marked task **{taskName}** as completed in project **{projectName}**.\n\n"
                + "Please review the task and confirm its completion.";

            return await messagingService.SendWorkflowMessageAsync(
                pmId,
                projectId,
                subject,
                content,
                "task_completion" // Use specific workflow type
            );
        }

        /// <summary>
        /// Send project status update message to client
        /// </summary>
        public static async Task<bool> SendProjectStatusUpdateMessageAsync(
            IMessagingService messagingService,
            string clientId,
            string projectId,
            string projectName,
            string newStatus,
            string additionalDetails = ""
        )
        {
            var subject = $"Project Status Update: {projectName}";
            var content =
                $"Project {projectName} status has been updated to {newStatus}. {additionalDetails}";

            return await SendSystemMessageAsync(
                messagingService,
                clientId,
                projectId,
                subject,
                content
            );
        }

        /// <summary>
        /// Send progress report submitted message to PM
        /// </summary>
        public static async Task<bool> SendProgressReportMessageAsync(
            IMessagingService messagingService,
            string pmId,
            string projectId,
            string projectName,
            string contractorName,
            string taskName,
            int progressPercentage
        )
        {
            var subject = $"📊 Progress Report: {taskName} in Project {projectName}";
            var content =
                $"Contractor **{contractorName}** has submitted a progress report for task **{taskName}** in project **{projectName}**.\n\n"
                + $"**Current progress:** {progressPercentage}%\n\n"
                + "Please review the report for details.";

            return await messagingService.SendWorkflowMessageAsync(
                pmId,
                projectId,
                subject,
                content,
                "progress_report" // Use specific workflow type
            );
        }

        /// <summary>
        /// Send completion request message to PM
        /// </summary>
        public static async Task<bool> SendCompletionRequestMessageAsync(
            IMessagingService messagingService,
            string pmId,
            string projectId,
            string projectName,
            string contractorName,
            string taskName
        )
        {
            var subject = $"🔍 Completion Request: {taskName} in Project {projectName}";
            var content =
                $"Contractor **{contractorName}** has requested completion for task **{taskName}** in project **{projectName}**.\n\n"
                + "Please review the task and approve its completion.";

            return await messagingService.SendWorkflowMessageAsync(
                pmId,
                projectId,
                subject,
                content,
                "completion_request" // Use specific workflow type
            );
        }

        /// <summary>
        /// Get user details for message context
        /// </summary>
        public static async Task<UserDto?> GetUserDetailsAsync(
            HttpClient httpClient,
            string apiBaseUrl,
            string userId
        )
        {
            try
            {
                var response = await httpClient.GetAsync($"{apiBaseUrl}/api/admin/users");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var users = System.Text.Json.JsonSerializer.Deserialize<List<UserDto>>(
                        content,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        }
                    );
                    return users?.FirstOrDefault(u => u.UserId == userId);
                }
            }
            catch (Exception)
            {
                // Log error but return null
            }
            return null;
        }

        /// <summary>
        /// Get project details for message context
        /// </summary>
        public static async Task<ProjectDto?> GetProjectDetailsAsync(
            HttpClient httpClient,
            string apiBaseUrl,
            string projectId
        )
        {
            try
            {
                var response = await httpClient.GetAsync(
                    $"{apiBaseUrl}/api/projectmanager/projects"
                );
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var projects = System.Text.Json.JsonSerializer.Deserialize<List<ProjectDto>>(
                        content,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        }
                    );
                    return projects?.FirstOrDefault(p => p.ProjectId == projectId);
                }
            }
            catch (Exception)
            {
                // Log error but return null
            }
            return null;
        }

        /// <summary>
        /// Validate if user can send message based on communication hierarchy
        /// </summary>
        public static bool ValidateMessageRecipient(string senderRole, string receiverRole)
        {
            // Admin can message all roles
            if (senderRole == "Admin")
                return true;

            // PM can message all roles
            if (senderRole == "Project Manager")
                return true;

            // Contractors and Clients can only message PM and Admin
            if (senderRole == "Contractor" || senderRole == "Client")
            {
                return receiverRole == "Admin" || receiverRole == "Project Manager";
            }

            return false;
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        public static string? GetCurrentUserId(ClaimsPrincipal user)
        {
            return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Get current user role from claims
        /// </summary>
        public static string? GetCurrentUserRole(ClaimsPrincipal user)
        {
            return user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        }
    }
}
