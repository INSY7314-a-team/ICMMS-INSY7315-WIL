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

                var result = await _apiClient.PostAsync<string>(
                    "/api/messages",
                    messageRequest,
                    currentUser
                );

                _logger.LogInformation("Message creation result: {Result}", result);

                if (!string.IsNullOrEmpty(result))
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

        public async Task<List<MessageDto>> GetThreadMessagesAsync(string threadId, string userId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No authenticated user found for getting thread messages");
                    return new List<MessageDto>();
                }

                // First try to get regular thread messages
                var messages = await _apiClient.GetAsync<List<MessageDto>>(
                    $"/api/messages/thread/{threadId}",
                    currentUser
                );

                // If no regular messages found, check if it's a workflow message
                if (messages == null || messages.Count == 0)
                {
                    var workflowMessage = await _apiClient.GetAsync<WorkflowMessageDto>(
                        $"/api/messages/workflow/{threadId}",
                        currentUser
                    );

                    if (workflowMessage != null && workflowMessage.Recipients.Contains(userId))
                    {
                        // Convert workflow message to MessageDto format
                        var workflowMessageDto = new MessageDto
                        {
                            MessageId = workflowMessage.WorkflowMessageId,
                            ThreadId = workflowMessage.WorkflowMessageId,
                            SenderId = "system",
                            SenderName = "System",
                            ReceiverId = userId,
                            ReceiverName = "You",
                            ProjectId = workflowMessage.ProjectId,
                            Subject = workflowMessage.Subject,
                            Content = workflowMessage.Content,
                            SentAt = workflowMessage.CreatedAt,
                            IsRead = true, // Workflow messages are always considered read
                            MessageType = "workflow",
                            Attachments = new List<MessageAttachmentDto>(),
                        };

                        return new List<MessageDto> { workflowMessageDto };
                    }
                }

                return messages ?? new List<MessageDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for thread {ThreadId}", threadId);
                return new List<MessageDto>();
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

                return messages.Count(m => !m.IsRead && m.ReceiverId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> MarkAsReadAsync(string messageId, string userId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No authenticated user found for marking message as read");
                    return false;
                }

                var message = await _apiClient.GetAsync<MessageDto>(
                    $"/api/messages/{messageId}",
                    currentUser
                );
                if (message == null)
                    return false;

                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;

                var result = await _apiClient.PutAsync<object>(
                    $"/api/messages/{messageId}",
                    message,
                    currentUser
                );
                return result != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
                return false;
            }
        }

        public async Task<bool> MarkThreadAsReadAsync(string threadId, string userId)
        {
            try
            {
                var currentUser = GetCurrentUser();
                if (currentUser == null)
                {
                    _logger.LogWarning("No authenticated user found for marking thread as read");
                    return false;
                }

                // Get all messages in the thread
                var messages = await _apiClient.GetAsync<List<MessageWithSender>>(
                    $"/api/messages/thread/{threadId}",
                    currentUser
                );

                if (messages == null || !messages.Any())
                    return false;

                // Mark all unread messages in the thread as read
                var unreadMessages = messages
                    .Where(m => !m.IsRead && m.ReceiverId == userId)
                    .ToList();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                    message.ReadAt = DateTime.UtcNow;

                    await _apiClient.PutAsync<object>(
                        $"/api/messages/{message.MessageId}",
                        message,
                        currentUser
                    );
                }

                _logger.LogInformation(
                    "Marked {Count} messages as read in thread {ThreadId}",
                    unreadMessages.Count,
                    threadId
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking thread {ThreadId} as read", threadId);
                return false;
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
