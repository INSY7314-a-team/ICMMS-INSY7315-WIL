using System.Security.Claims;
using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public class MessagingService : IMessagingService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<MessagingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MessagingService(
            IApiClient apiClient,
            ILogger<MessagingService> logger,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _apiClient = apiClient;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> SendDirectMessageAsync(
            string senderId,
            string receiverId,
            string projectId,
            string subject,
            string content
        )
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No authenticated user found for sending direct message");
                    return false;
                }

                var messageRequest = new CreateMessageRequest
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    ProjectId = projectId,
                    Subject = subject,
                    Content = content,
                    MessageType = "direct",
                    ThreadParticipants = new List<string> { senderId, receiverId },
                };

                _logger.LogInformation(
                    "Sending direct message: Sender={SenderId}, Receiver={ReceiverId}, Subject={Subject}",
                    senderId,
                    receiverId,
                    subject
                );

                var result = await _apiClient.PostAsync<CreateMessageResponse>(
                    "/api/messages",
                    messageRequest,
                    currentUser
                );

                _logger.LogInformation("Message creation result: {Result}", result);

                if (result != null && !string.IsNullOrEmpty(result.MessageId))
                {
                    _logger.LogInformation(
                        "Message sent successfully from {SenderId} to {ReceiverId}",
                        senderId,
                        receiverId
                    );
                    return true;
                }

                _logger.LogWarning(
                    "Failed to send message from {SenderId} to {ReceiverId}",
                    senderId,
                    receiverId
                );
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending message from {SenderId} to {ReceiverId}",
                    senderId,
                    receiverId
                );
                return false;
            }
        }

        public async Task<List<ThreadDto>> GetUserThreadsAsync(string userId, string userRole)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No authenticated user found for getting user threads");
                    return new List<ThreadDto>();
                }

                // Get all threads from API
                var allThreads = await _apiClient.GetAsync<List<ThreadDto>>(
                    "/api/messages/threads",
                    currentUser
                );

                if (allThreads == null)
                {
                    allThreads = new List<ThreadDto>();
                }

                // Get workflow messages for this user
                var workflowMessages = await _apiClient.GetAsync<List<WorkflowMessageDto>>(
                    "/api/messages/workflow",
                    currentUser
                );

                if (workflowMessages != null)
                {
                    // Convert workflow messages to thread format
                    var workflowThreads = ConvertWorkflowMessagesToThreads(
                        workflowMessages,
                        userId
                    );
                    allThreads.AddRange(workflowThreads);
                }

                // Filter threads based on user role and participation
                var filteredThreads = FilterThreadsForUser(allThreads, userId, userRole);

                _logger.LogInformation(
                    "Filtered {FilteredCount} threads from {TotalCount} total threads for user {UserId} with role {UserRole}",
                    filteredThreads.Count,
                    allThreads.Count,
                    userId,
                    userRole
                );

                return filteredThreads;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting threads for user {UserId}", userId);
                return new List<ThreadDto>();
            }
        }

        public async Task<List<MessageWithSenderDto>> GetThreadMessagesAsync(
            string threadId,
            string userId
        )
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No authenticated user found for getting thread messages");
                    return new List<MessageWithSenderDto>();
                }

                var messages = await _apiClient.GetAsync<List<MessageWithSenderDto>>(
                    $"/api/messages/thread/{threadId}",
                    currentUser
                );

                if (messages == null || messages.Count == 0)
                {
                    // It might be a workflow message. Let's check.
                    var workflowMessage = await _apiClient.GetAsync<WorkflowMessageDto>(
                        $"/api/messages/workflow/{threadId}",
                        currentUser
                    );

                    if (workflowMessage != null && workflowMessage.Recipients.Contains(userId))
                    {
                        var workflowAsMessage = new MessageWithSenderDto
                        {
                            MessageId = workflowMessage.WorkflowMessageId,
                            ThreadId = workflowMessage.WorkflowMessageId,
                            SenderId = "system",
                            SenderName = "System",
                            ReceiverId = userId,
                            ProjectId = workflowMessage.ProjectId,
                            Subject = workflowMessage.Subject,
                            Content = workflowMessage.Content,
                            SentAt = workflowMessage.CreatedAt,
                            IsRead = true,
                            MessageType = "workflow",
                        };
                        return new List<MessageWithSenderDto> { workflowAsMessage };
                    }
                }

                return messages ?? new List<MessageWithSenderDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for thread {ThreadId}", threadId);
                return new List<MessageWithSenderDto>();
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No authenticated user found for getting unread count");
                    return 0;
                }

                var messages = await _apiClient.GetAsync<List<MessageDto>>(
                    $"/api/messages/user/{userId}",
                    currentUser
                );
                if (messages == null)
                    return 0;

                // Handle duplicates: group by MessageId and count unique unread messages
                return messages
                    .GroupBy(m => m.MessageId)
                    .Count(msgGroup =>
                        msgGroup.Any(m =>
                            !m.IsRead
                            && m.SenderId != userId
                            && (
                                m.ReceiverId == userId
                                || (
                                    m.ThreadParticipants != null
                                    && m.ThreadParticipants.Contains(userId)
                                )
                            )
                        )
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<MarkAsReadResponse?> MarkThreadAsReadAsync(string threadId, string userId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No authenticated user found for marking thread as read");
                    return null;
                }

                var response = await _apiClient.PostAsync<MarkAsReadResponse>(
                    $"/api/messages/thread/{threadId}/mark-as-read",
                    new { }, // Empty body
                    currentUser
                );

                if (response != null)
                {
                    _logger.LogInformation(
                        $"[MarkThreadAsReadAsync] Successfully marked thread {threadId} as read. New unread count from API: {response.UnreadCount}"
                    );
                    return response;
                }

                _logger.LogWarning(
                    $"[MarkThreadAsReadAsync] Failed to mark thread {threadId} as read. API response was null."
                );
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking thread {ThreadId} as read", threadId);
                return null;
            }
        }

        public bool CanUserSendMessage(string senderRole, string receiverRole)
        {
            // Admin can message all roles
            if (senderRole == "Admin")
                return true;

            // PM can message all roles
            if (senderRole == "Project Manager")
                return true;

            // Contractors and Clients can message PM, Admin, and other users in their projects
            if (senderRole == "Contractor" || senderRole == "Client")
            {
                return receiverRole == "Admin"
                    || receiverRole == "Project Manager"
                    || receiverRole == "Contractor"
                    || receiverRole == "Client";
            }

            return false;
        }

        private ClaimsPrincipal? GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User;
        }

        /// <summary>
        /// Filter threads based on user role and participation
        /// </summary>
        private List<ThreadDto> FilterThreadsForUser(
            List<ThreadDto> allThreads,
            string userId,
            string userRole
        )
        {
            // Admins can see all messages regardless of participation
            if (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "Admin user {UserId} can see all {ThreadCount} threads",
                    userId,
                    allThreads.Count
                );
                return allThreads;
            }

            var filteredThreads = new List<ThreadDto>();

            foreach (var thread in allThreads)
            {
                bool canViewThread = false;

                // Check if user is a participant in the thread
                if (thread.Participants != null && thread.Participants.Contains(userId))
                {
                    canViewThread = true;
                }

                // For workflow messages, check if user is a recipient
                if (
                    !canViewThread
                    && thread.ThreadType?.Equals("workflow", StringComparison.OrdinalIgnoreCase)
                        == true
                )
                {
                    // For workflow messages, we need to check if the user is a recipient
                    // This would require additional API call to get message details
                    // For now, we'll include workflow messages if the user is mentioned in the thread
                    canViewThread = thread.Participants?.Contains(userId) == true;
                }

                if (canViewThread)
                {
                    filteredThreads.Add(thread);
                }
            }

            _logger.LogInformation(
                "User {UserId} with role {UserRole} can view {FilteredCount} out of {TotalCount} threads",
                userId,
                userRole,
                filteredThreads.Count,
                allThreads.Count
            );

            return filteredThreads;
        }

        /// <summary>
        /// Convert workflow messages to thread format for display
        /// </summary>
        private List<ThreadDto> ConvertWorkflowMessagesToThreads(
            List<WorkflowMessageDto> workflowMessages,
            string userId
        )
        {
            var threads = new List<ThreadDto>();

            foreach (var workflowMessage in workflowMessages)
            {
                // Check if user is a recipient of this workflow message
                if (workflowMessage.Recipients.Contains(userId))
                {
                    var thread = new ThreadDto
                    {
                        ThreadId = workflowMessage.WorkflowMessageId,
                        Subject = workflowMessage.Subject,
                        ProjectId = workflowMessage.ProjectId,
                        Participants = workflowMessage.Recipients,
                        ThreadType = "workflow",
                        LastMessageAt = workflowMessage.CreatedAt,
                        LastMessagePreview =
                            workflowMessage.Content.Length > 100
                                ? workflowMessage.Content.Substring(0, 100) + "..."
                                : workflowMessage.Content,
                        UnreadCount = 0, // Workflow messages are always "read" by the system
                        HasUnreadMessages = false,
                    };

                    threads.Add(thread);
                }
            }

            return threads;
        }
    }
}
