using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    public class MarkThreadAsReadRequest
    {
        public string ThreadId { get; set; } = string.Empty;
    }

    [Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")] // All authenticated users can access this controller
    public class MessagesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<MessagesController> _logger;
        private readonly IMessagingService _messagingService;

        public MessagesController(
            IApiClient apiClient,
            ILogger<MessagesController> logger,
            IMessagingService messagingService
        )
        {
            _apiClient = apiClient;
            _logger = logger;
            _messagingService = messagingService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                if (string.IsNullOrEmpty(currentUserId))
                {
                    TempData["Error"] = "User not authenticated. Please log in again.";
                    return View(new UserMessagesViewModel());
                }

                // Get workflow messages for the user
                var workflowThreads = await GetWorkflowMessagesForUser(currentUserId);

                // Get direct messages for the user
                var directThreads = await GetDirectMessagesForUser(currentUserId);

                // Get unread count
                var unreadCount = await _messagingService.GetUnreadCountAsync(currentUserId);

                // Reset circuit breakers for critical endpoints to ensure dropdowns populate
                _apiClient.ResetCircuitBreaker("/api/admin/users");
                _apiClient.ResetCircuitBreaker("/api/projectmanager/projects");

                // Get available users and projects for sending messages based on role and project associations
                var availableUsers = await GetAvailableUsersForMessaging(currentUserId, userRole);
                var availableProjects = await GetAvailableProjectsForMessaging(
                    currentUserId,
                    userRole
                );

                // Fallback: If no users or projects found, try basic loading
                if (!availableUsers.Any())
                {
                    _logger.LogWarning(
                        "No users found with advanced filtering, trying basic user loading"
                    );
                    try
                    {
                        var allUsers =
                            await _apiClient.GetAsync<List<UserDto>>("/api/admin/users", User)
                            ?? new List<UserDto>();
                        availableUsers = allUsers.Where(u => u.UserId != currentUserId).ToList();
                        _logger.LogInformation(
                            "Fallback loaded {Count} users",
                            availableUsers.Count
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fallback user loading also failed");
                    }
                }

                if (!availableProjects.Any())
                {
                    _logger.LogWarning(
                        "No projects found with advanced filtering, trying basic project loading"
                    );
                    try
                    {
                        var allProjects =
                            await _apiClient.GetAsync<List<ProjectDto>>(
                                "/api/projectmanager/projects",
                                User
                            ) ?? new List<ProjectDto>();
                        availableProjects = allProjects;
                        _logger.LogInformation(
                            "Fallback loaded {Count} projects",
                            availableProjects.Count
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fallback project loading also failed");
                    }
                }

                var viewModel = new UserMessagesViewModel
                {
                    WorkflowThreads = workflowThreads,
                    DirectThreads = directThreads,
                    AvailableUsers = availableUsers,
                    AvailableProjects = availableProjects,
                    UnreadCount = unreadCount,
                    UserRole = userRole,
                    CurrentView = "workflow", // Default to workflow messages
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading messages for user");
                TempData["Error"] = "Error loading messages. Please try again.";
                return View(new UserMessagesViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserThreads()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var threads = await _messagingService.GetUserThreadsAsync(currentUserId, userRole);
                return Json(new { success = true, threads = threads });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user threads");
                return Json(new { success = false, message = "Error loading threads" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserThreadsByType(string messageType = "all")
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                List<ThreadDto> threads = new List<ThreadDto>();

                if (messageType == "workflow" || messageType == "all")
                {
                    var workflowThreads = await GetWorkflowMessagesForUser(currentUserId);
                    threads.AddRange(workflowThreads);
                }

                if (messageType == "direct" || messageType == "all")
                {
                    var directThreads = await GetDirectMessagesForUser(currentUserId);
                    threads.AddRange(directThreads);
                }

                return Json(new { success = true, threads = threads });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting user threads by type: {MessageType}",
                    messageType
                );
                return Json(new { success = false, message = "Error loading threads" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Validate communication hierarchy
                _logger.LogInformation(
                    "Validating recipient: {ReceiverId} for {Role} user {CurrentUserId}",
                    request.ReceiverId,
                    userRole,
                    currentUserId
                );

                // First, try to get the user from the available users list (more efficient)
                var availableUsers = await GetAvailableUsersForMessaging(currentUserId, userRole);
                var receiverUser = availableUsers.FirstOrDefault(u =>
                    u.UserId == request.ReceiverId
                );

                // If not found in available users, try the GetUserById method as fallback
                if (receiverUser == null)
                {
                    _logger.LogInformation(
                        "Recipient not found in available users, trying GetUserById"
                    );
                    receiverUser = await GetUserById(request.ReceiverId);
                }

                if (receiverUser == null)
                {
                    _logger.LogWarning(
                        "Recipient {ReceiverId} not found for {Role} user {CurrentUserId}",
                        request.ReceiverId,
                        userRole,
                        currentUserId
                    );
                    return Json(new { success = false, message = "Recipient not found" });
                }

                _logger.LogInformation(
                    "Recipient found: {FullName} ({Role})",
                    receiverUser.FullName,
                    receiverUser.Role
                );

                if (!_messagingService.CanUserSendMessage(userRole, receiverUser.Role))
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "You are not authorized to send messages to this user",
                        }
                    );
                }

                var success = await _messagingService.SendDirectMessageAsync(
                    currentUserId,
                    request.ReceiverId,
                    request.ProjectId,
                    request.Subject,
                    request.Content
                );

                if (success)
                {
                    return Json(new { success = true, message = "Message sent successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to send message" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendThreadMessage(
            [FromBody] SendThreadMessageRequest request
        )
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                _logger.LogInformation(
                    "Sending thread message to thread {ThreadId} by user {UserId}",
                    request.ThreadId,
                    currentUserId
                );

                // Let the API handle the logic of finding participants and details.
                // The web client only needs to provide the thread and the content.
                var messageRequest = new CreateMessageRequest
                {
                    Content = request.Content,
                    ThreadId = request.ThreadId,
                };

                var result = await _apiClient.PostAsync<CreateMessageResponse>(
                    "/api/messages",
                    messageRequest,
                    User
                );

                if (result != null && !string.IsNullOrEmpty(result.MessageId))
                {
                    _logger.LogInformation(
                        "Thread message sent successfully to thread {ThreadId}",
                        request.ThreadId
                    );
                    return Json(new { success = true, message = "Message sent successfully" });
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to send thread message to thread {ThreadId}",
                        request.ThreadId
                    );
                    return Json(new { success = false, message = "Failed to send message" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending thread message to thread {ThreadId}",
                    request.ThreadId
                );
                return Json(new { success = false, message = "Error sending message" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReplyToMessage([FromBody] ReplyToMessageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                _logger.LogInformation(
                    "Replying to message {ParentMessageId} by user {UserId}",
                    request.ParentMessageId,
                    currentUserId
                );

                // Get the parent message to validate it can be replied to
                var parentMessage = await GetMessageById(request.ParentMessageId);
                if (parentMessage == null)
                {
                    return Json(new { success = false, message = "Parent message not found" });
                }

                // Check if the message can be replied to (not system/workflow messages)
                if (
                    parentMessage.MessageType == "workflow"
                    || parentMessage.MessageType == "system"
                    || parentMessage.SenderId == "system"
                )
                {
                    return Json(
                        new { success = false, message = "Cannot reply to this type of message" }
                    );
                }

                // Create the reply request for the API
                var replyRequest = new
                {
                    parentMessageId = request.ParentMessageId,
                    content = request.Content,
                };

                var result = await _apiClient.PostAsync<CreateMessageResponse>(
                    "/api/messages/reply",
                    replyRequest,
                    User
                );

                if (result != null && !string.IsNullOrEmpty(result.MessageId))
                {
                    _logger.LogInformation(
                        "Reply sent successfully to message {ParentMessageId}",
                        request.ParentMessageId
                    );
                    return Json(new { success = true, message = "Reply sent successfully" });
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to send reply to message {ParentMessageId}",
                        request.ParentMessageId
                    );
                    return Json(new { success = false, message = "Failed to send reply" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error sending reply to message {ParentMessageId}",
                    request.ParentMessageId
                );
                return Json(new { success = false, message = "Error sending reply" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkThreadAsRead(
            [FromBody] MarkThreadAsReadRequest request
        )
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                if (string.IsNullOrEmpty(request.ThreadId))
                {
                    return Json(new { success = false, message = "Thread ID is required" });
                }

                _logger.LogInformation(
                    $"[MarkThreadAsRead] Attempting to mark thread {request.ThreadId} as read for user {currentUserId}."
                );

                var response = await _messagingService.MarkThreadAsReadAsync(
                    request.ThreadId,
                    currentUserId
                );

                if (response != null)
                {
                    _logger.LogInformation(
                        $"[MarkThreadAsRead] Service returned success. New unread count is {response.UnreadCount}."
                    );
                    return Json(
                        new
                        {
                            success = true,
                            message = "Thread marked as read",
                            unreadCount = response.UnreadCount,
                        }
                    );
                }
                else
                {
                    _logger.LogWarning(
                        $"[MarkThreadAsRead] Service returned failure for thread {request.ThreadId}."
                    );
                    return Json(
                        new
                        {
                            success = false,
                            message = "Failed to mark thread as read",
                            unreadCount = -1,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking thread as read");
                return Json(new { success = false, message = "Error updating thread" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, count = 0 });
                }

                var count = await _messagingService.GetUnreadCountAsync(currentUserId);
                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return Json(new { success = false, count = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetThreadMessages(string threadId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var messages = await _messagingService.GetThreadMessagesAsync(
                    threadId,
                    currentUserId
                );
                return Json(new { success = true, messages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting thread messages");
                return Json(new { success = false, message = "Error loading messages" });
            }
        }

        private async Task<List<ThreadDto>> GetWorkflowMessagesForUser(string userId)
        {
            try
            {
                _logger.LogInformation("Getting workflow messages for user {UserId}", userId);

                // Get workflow messages using the dedicated workflow endpoint
                var workflowMessages = await _apiClient.GetAsync<List<WorkflowMessageDto>>(
                    $"/api/messages/user/{userId}/workflow",
                    User
                );

                if (workflowMessages != null && workflowMessages.Any())
                {
                    _logger.LogInformation(
                        "Deserialized {Count} workflow messages from workflow API",
                        workflowMessages.Count
                    );

                    // Log some sample workflow messages for debugging
                    var sampleMessages = workflowMessages
                        .Take(3)
                        .Select(wm => new
                        {
                            WorkflowMessageId = wm.WorkflowMessageId,
                            WorkflowType = wm.WorkflowType,
                            Subject = wm.Subject,
                            RecipientsCount = wm.Recipients.Count,
                        })
                        .ToList();
                    _logger.LogInformation(
                        "Sample workflow messages: {SampleMessages}",
                        string.Join(
                            " | ",
                            sampleMessages.Select(sm =>
                                $"{sm.WorkflowType}:{sm.RecipientsCount} recipients"
                            )
                        )
                    );

                    _logger.LogInformation("Current user ID: {UserId}", userId);

                    // Convert workflow messages to ThreadDto format
                    var workflowThreads = workflowMessages
                        .Select(wm => new ThreadDto
                        {
                            ThreadId = string.Empty, // Workflow messages are not threads
                            Subject = wm.Subject,
                            ProjectId = wm.ProjectId,
                            ProjectName = "", // Will be populated from project data if needed
                            MessageCount = 1, // Each workflow message is a single message
                            LastMessageAt = wm.CreatedAt,
                            CreatedAt = wm.CreatedAt,
                            LastMessageSenderName = "System",
                            LastMessagePreview =
                                wm.Content.Length > 50
                                    ? wm.Content.Substring(0, 47) + "..."
                                    : wm.Content,
                            Participants = wm.Recipients,
                            ParticipantNames = new List<string>(),
                            ThreadType = "workflow",
                            HasUnreadMessages = false, // Workflow messages are always considered read
                            UnreadCount = 0,
                            IsActive = false,
                        })
                        .OrderByDescending(t => t.LastMessageAt)
                        .ToList();

                    _logger.LogInformation(
                        "Returning {Count} workflow threads for user {UserId}",
                        workflowThreads.Count,
                        userId
                    );
                    return workflowThreads;
                }
                else
                {
                    _logger.LogWarning("ApiClient returned null or empty for workflow messages");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workflow messages for user {UserId}", userId);
            }

            // If no workflow messages found, return some sample data for testing
            _logger.LogInformation("No workflow messages found, returning sample data for testing");
            return new List<ThreadDto>
            {
                new ThreadDto
                {
                    ThreadId = string.Empty, // Sample workflow message is not a thread
                    Subject = "Sample Workflow Message",
                    ProjectId = "sample-project",
                    ProjectName = "Sample Project",
                    MessageCount = 1,
                    LastMessageAt = DateTime.UtcNow.AddHours(-1),
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    LastMessageSenderName = "System",
                    LastMessagePreview = "This is a sample workflow message for testing purposes.",
                    Participants = new List<string> { userId },
                    ParticipantNames = new List<string>(),
                    ThreadType = "workflow",
                    HasUnreadMessages = false,
                    UnreadCount = 0,
                    IsActive = false,
                },
            };
        }

        private async Task<List<ThreadDto>> GetDirectMessagesForUser(string userId)
        {
            try
            {
                // Get direct messages from API
                _logger.LogInformation("Getting direct messages for user {UserId}", userId);
                // Get messages using IApiClient with proper authentication
                var messages = await _apiClient.GetAsync<List<MessageDto>>(
                    $"/api/messages/user/{userId}",
                    User
                );

                _logger.LogInformation(
                    "Retrieved {Count} messages from API for user {UserId}",
                    messages?.Count ?? 0,
                    userId
                );

                if (messages != null && messages.Any())
                {
                    _logger.LogInformation(
                        "Processing {Count} messages for user {UserId}",
                        messages.Count,
                        userId
                    );

                    // Log first few messages for debugging
                    foreach (var msg in messages.Take(3))
                    {
                        _logger.LogInformation(
                            "Message: {MessageId}, Sender: {SenderId}, Receiver: {ReceiverId}, Subject: {Subject}",
                            msg.MessageId,
                            msg.SenderId,
                            msg.ReceiverId,
                            msg.Subject
                        );
                    }
                }
                else
                {
                    _logger.LogWarning("No messages returned from API for user {UserId}", userId);
                }

                if (messages != null)
                {
                    // Group messages by thread - include messages where user is sender OR receiver
                    // Note: Direct messages are created as "thread" type in the API, so we need to filter for both
                    // But we want to distinguish between direct messages (2 participants) and project threads (multiple participants)
                    var directMessages = messages
                        .Where(m =>
                            (m.MessageType == "direct" || m.MessageType == "thread")
                            && (m.SenderId == userId || m.ReceiverId == userId)
                            && m.ThreadParticipants != null
                            && m.ThreadParticipants.Count == 2 // Only 2 participants = direct message
                        )
                        .ToList();

                    _logger.LogInformation(
                        "Found {Count} direct messages for user {UserId}",
                        directMessages.Count,
                        userId
                    );

                    // Log details about the filtering
                    var threadMessages = messages.Where(m => m.MessageType == "thread").ToList();
                    var directTypeMessages = messages
                        .Where(m => m.MessageType == "direct")
                        .ToList();
                    var twoParticipantMessages = messages
                        .Where(m => m.ThreadParticipants != null && m.ThreadParticipants.Count == 2)
                        .ToList();

                    _logger.LogInformation(
                        "Filtering details: Thread messages: {ThreadCount}, Direct messages: {DirectCount}, Two-participant messages: {TwoParticipantCount}",
                        threadMessages.Count,
                        directTypeMessages.Count,
                        twoParticipantMessages.Count
                    );

                    var threadGroups = directMessages.GroupBy(m => m.ThreadId).ToList();

                    var threads = threadGroups
                        .Select(group => new ThreadDto
                        {
                            ThreadId = group.Key,
                            Subject = group.First().Subject,
                            ProjectId = group.First().ProjectId,
                            ProjectName = "", // Will be populated from project data if needed
                            MessageCount = group.Count(),
                            LastMessageAt = group.Max(m => m.SentAt),
                            CreatedAt = group.Min(m => m.SentAt),
                            LastMessageSenderName =
                                group.OrderByDescending(m => m.SentAt).First().SenderName
                                ?? "Unknown",
                            LastMessagePreview =
                                group.OrderByDescending(m => m.SentAt).First().Content.Length > 50
                                    ? group
                                        .OrderByDescending(m => m.SentAt)
                                        .First()
                                        .Content.Substring(0, 47) + "..."
                                    : group.OrderByDescending(m => m.SentAt).First().Content,
                            Participants = group
                                .Select(m => m.SenderId)
                                .Union(group.Select(m => m.ReceiverId))
                                .Distinct()
                                .ToList(),
                            ParticipantNames = new List<string>(), // Will be populated if needed
                            ThreadType = "direct",
                            // Handle duplicates: group by MessageId and check if any duplicate is unread
                            HasUnreadMessages = group
                                .GroupBy(m => m.MessageId)
                                .Any(msgGroup =>
                                    msgGroup.Any(m => !m.IsRead && m.SenderId != userId)
                                ),
                            UnreadCount = group
                                .GroupBy(m => m.MessageId)
                                .Count(msgGroup =>
                                    msgGroup.Any(m => !m.IsRead && m.SenderId != userId)
                                ),
                            IsActive = false,
                        })
                        .OrderByDescending(t => t.LastMessageAt)
                        .ToList();

                    _logger.LogInformation(
                        "Created {Count} direct message threads for user {UserId}",
                        threads.Count,
                        userId
                    );
                    foreach (var thread in threads)
                    {
                        _logger.LogInformation(
                            "Thread: {ThreadId}, Subject: {Subject}, Messages: {Count}",
                            thread.ThreadId,
                            thread.Subject,
                            thread.MessageCount
                        );
                    }

                    return threads;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting direct messages for user {UserId}", userId);
            }

            // If no direct messages found, return some sample data for testing
            _logger.LogInformation("No direct messages found, returning sample data for testing");
            return new List<ThreadDto>
            {
                new ThreadDto
                {
                    ThreadId = "sample-direct-1",
                    Subject = "Sample Direct Message",
                    ProjectId = "sample-project",
                    ProjectName = "Sample Project",
                    MessageCount = 1,
                    LastMessageAt = DateTime.UtcNow.AddHours(-2),
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    LastMessageSenderName = "John Doe",
                    LastMessagePreview = "This is a sample direct message for testing purposes.",
                    Participants = new List<string> { userId, "sender-1" },
                    ParticipantNames = new List<string>(),
                    ThreadType = "direct",
                    HasUnreadMessages = true,
                    UnreadCount = 1,
                    IsActive = false,
                },
            };
        }

        private async Task<MessageDto?> GetMessageById(string messageId)
        {
            try
            {
                var messages = await _apiClient.GetAsync<List<MessageDto>>(
                    $"/api/messages/user/{User.FindFirst(ClaimTypes.NameIdentifier)?.Value}",
                    User
                );
                return messages?.FirstOrDefault(m => m.MessageId == messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message by ID: {MessageId}", messageId);
                return null;
            }
        }

        private async Task<UserDto?> GetUserById(string userId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                _logger.LogInformation(
                    "Getting user by ID: {UserId} for {Role} user {CurrentUserId}",
                    userId,
                    userRole,
                    currentUserId
                );

                // Use role-specific endpoints to get users
                if (userRole == "Contractor")
                {
                    var contractorUsers = await _apiClient.GetAsync<List<object>>(
                        "/api/contractors/messaging/available-users",
                        User
                    );
                    if (contractorUsers != null && contractorUsers.Any())
                    {
                        var user = contractorUsers.FirstOrDefault(u =>
                            GetPropertyValue(u, "UserId") == userId
                            || GetPropertyValue(u, "userId") == userId
                        );

                        if (user != null)
                        {
                            return new UserDto
                            {
                                UserId =
                                    GetPropertyValue(user, "UserId")
                                    ?? GetPropertyValue(user, "userId")
                                    ?? "",
                                FullName =
                                    GetPropertyValue(user, "FullName")
                                    ?? GetPropertyValue(user, "fullName")
                                    ?? "Unknown User",
                                Role =
                                    GetPropertyValue(user, "Role")
                                    ?? GetPropertyValue(user, "role")
                                    ?? "",
                                Email =
                                    GetPropertyValue(user, "Email")
                                    ?? GetPropertyValue(user, "email")
                                    ?? "",
                            };
                        }
                    }
                }
                else if (userRole == "Client")
                {
                    var clientUsers = await _apiClient.GetAsync<List<object>>(
                        "/api/clients/messaging/available-users",
                        User
                    );
                    if (clientUsers != null && clientUsers.Any())
                    {
                        var user = clientUsers.FirstOrDefault(u =>
                            GetPropertyValue(u, "UserId") == userId
                            || GetPropertyValue(u, "userId") == userId
                        );

                        if (user != null)
                        {
                            return new UserDto
                            {
                                UserId =
                                    GetPropertyValue(user, "UserId")
                                    ?? GetPropertyValue(user, "userId")
                                    ?? "",
                                FullName =
                                    GetPropertyValue(user, "FullName")
                                    ?? GetPropertyValue(user, "fullName")
                                    ?? "Unknown User",
                                Role =
                                    GetPropertyValue(user, "Role")
                                    ?? GetPropertyValue(user, "role")
                                    ?? "",
                                Email =
                                    GetPropertyValue(user, "Email")
                                    ?? GetPropertyValue(user, "email")
                                    ?? "",
                            };
                        }
                    }
                }
                else
                {
                    // For Admins and PMs, use the admin endpoint
                    var users = await _apiClient.GetAsync<List<UserDto>>("/api/admin/users", User);
                    return users?.FirstOrDefault(u => u.UserId == userId);
                }

                _logger.LogWarning(
                    "User {UserId} not found in available users for {Role} user {CurrentUserId}",
                    userId,
                    userRole,
                    currentUserId
                );
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
            }
            return null;
        }

        private async Task<List<UserDto>> GetAvailableUsersForMessaging(
            string currentUserId,
            string userRole
        )
        {
            try
            {
                _logger.LogInformation(
                    "Getting available users for {Role} user {UserId}",
                    userRole,
                    currentUserId
                );

                List<UserDto> allUsers = new List<UserDto>();

                // Use role-specific endpoints
                if (userRole == "Contractor")
                {
                    _logger.LogInformation("Using contractor-specific endpoint for users");
                    var contractorUsers = await _apiClient.GetAsync<List<object>>(
                        "/api/contractors/messaging/available-users",
                        User
                    );
                    if (contractorUsers != null && contractorUsers.Any())
                    {
                        _logger.LogInformation(
                            "Retrieved {Count} contractor users from API",
                            contractorUsers.Count
                        );

                        // Log the first user for debugging
                        if (contractorUsers.Any())
                        {
                            var firstUser = contractorUsers.First();
                            _logger.LogInformation(
                                "First user data: {UserData}",
                                JsonSerializer.Serialize(firstUser)
                            );
                        }

                        allUsers = contractorUsers
                            .Select(u => new UserDto
                            {
                                UserId =
                                    GetPropertyValue(u, "UserId")
                                    ?? GetPropertyValue(u, "userId")
                                    ?? "",
                                FullName =
                                    GetPropertyValue(u, "FullName")
                                    ?? GetPropertyValue(u, "fullName")
                                    ?? "Unknown User",
                                Role =
                                    GetPropertyValue(u, "Role")
                                    ?? GetPropertyValue(u, "role")
                                    ?? "",
                                Email =
                                    GetPropertyValue(u, "Email")
                                    ?? GetPropertyValue(u, "email")
                                    ?? "",
                            })
                            .ToList();

                        _logger.LogInformation(
                            "Processed {Count} users with names: {Names}",
                            allUsers.Count,
                            string.Join(", ", allUsers.Select(u => $"'{u.FullName}'"))
                        );
                    }
                    else
                    {
                        _logger.LogInformation(
                            "No contractor users returned from API or empty array"
                        );
                    }
                }
                else if (userRole == "Client")
                {
                    _logger.LogInformation("Using client-specific endpoint for users");
                    var clientUsers = await _apiClient.GetAsync<List<object>>(
                        "/api/clients/messaging/available-users",
                        User
                    );
                    if (clientUsers != null && clientUsers.Any())
                    {
                        allUsers = clientUsers
                            .Select(u => new UserDto
                            {
                                UserId = GetPropertyValue(u, "UserId") ?? "",
                                FullName = GetPropertyValue(u, "FullName") ?? "",
                                Role = GetPropertyValue(u, "Role") ?? "",
                                Email = GetPropertyValue(u, "Email") ?? "",
                            })
                            .ToList();
                    }
                    else
                    {
                        _logger.LogInformation("No client users returned from API or empty array");
                    }
                }
                else if (userRole == "Project Manager")
                {
                    _logger.LogInformation("Using project manager-specific endpoint for users");
                    allUsers =
                        await _apiClient.GetAsync<List<UserDto>>(
                            "/api/projectmanager/messaging/available-users",
                            User
                        ) ?? new List<UserDto>();
                    allUsers = allUsers.Where(u => u.UserId != currentUserId).ToList();
                }
                else
                {
                    // Admins and PMs use the original endpoint
                    _logger.LogInformation("Using admin endpoint for {Role} user", userRole);
                    allUsers =
                        await _apiClient.GetAsync<List<UserDto>>("/api/admin/users", User)
                        ?? new List<UserDto>();
                    // Filter out self
                    allUsers = allUsers.Where(u => u.UserId != currentUserId).ToList();
                }

                _logger.LogInformation("Retrieved {Count} total users from API", allUsers.Count);

                var filteredUsers = allUsers.Where(u => u.UserId != currentUserId).ToList();

                _logger.LogInformation(
                    "Filtered {Count} available users for {Role} user {UserId}",
                    filteredUsers.Count,
                    userRole,
                    currentUserId
                );

                return filteredUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting available users for messaging for {Role} user {UserId}",
                    userRole,
                    currentUserId
                );
                return new List<UserDto>();
            }
        }

        private async Task<List<ProjectDto>> GetAvailableProjectsForMessaging(
            string currentUserId,
            string userRole
        )
        {
            try
            {
                _logger.LogInformation(
                    "Getting available projects for {Role} user {UserId}",
                    userRole,
                    currentUserId
                );

                List<ProjectDto> allProjects = new List<ProjectDto>();

                // Use role-specific endpoints
                if (userRole == "Contractor")
                {
                    _logger.LogInformation("Using contractor-specific endpoint for projects");
                    var contractorProjects = await _apiClient.GetAsync<List<object>>(
                        "/api/contractors/messaging/available-projects",
                        User
                    );
                    if (contractorProjects != null && contractorProjects.Any())
                    {
                        _logger.LogInformation(
                            "Retrieved {Count} contractor projects from API",
                            contractorProjects.Count
                        );

                        // Log the first project for debugging
                        if (contractorProjects.Any())
                        {
                            var firstProject = contractorProjects.First();
                            _logger.LogInformation(
                                "First project data: {ProjectData}",
                                JsonSerializer.Serialize(firstProject)
                            );
                        }

                        allProjects = contractorProjects
                            .Select(p => new ProjectDto
                            {
                                ProjectId =
                                    GetPropertyValue(p, "ProjectId")
                                    ?? GetPropertyValue(p, "projectId")
                                    ?? "",
                                Name =
                                    GetPropertyValue(p, "Name")
                                    ?? GetPropertyValue(p, "name")
                                    ?? "Unnamed Project",
                                Description =
                                    GetPropertyValue(p, "Description")
                                    ?? GetPropertyValue(p, "description")
                                    ?? "",
                                Status =
                                    GetPropertyValue(p, "Status")
                                    ?? GetPropertyValue(p, "status")
                                    ?? "",
                            })
                            .ToList();

                        _logger.LogInformation(
                            "Processed {Count} projects with names: {Names}",
                            allProjects.Count,
                            string.Join(", ", allProjects.Select(p => $"'{p.Name}'"))
                        );
                    }
                    else
                    {
                        _logger.LogInformation(
                            "No contractor projects returned from API or empty array"
                        );
                    }
                }
                else if (userRole == "Client")
                {
                    _logger.LogInformation("Using client-specific endpoint for projects");
                    var clientProjects = await _apiClient.GetAsync<List<object>>(
                        "/api/clients/messaging/available-projects",
                        User
                    );
                    if (clientProjects != null && clientProjects.Any())
                    {
                        allProjects = clientProjects
                            .Select(p => new ProjectDto
                            {
                                ProjectId = GetPropertyValue(p, "ProjectId") ?? "",
                                Name = GetPropertyValue(p, "Name") ?? "",
                                Description = GetPropertyValue(p, "Description") ?? "",
                                Status = GetPropertyValue(p, "Status") ?? "",
                            })
                            .ToList();
                    }
                    else
                    {
                        _logger.LogInformation(
                            "No client projects returned from API or empty array"
                        );
                    }
                }
                else
                {
                    // Admins and PMs use the original endpoint
                    _logger.LogInformation(
                        "Using projectmanager endpoint for {Role} user",
                        userRole
                    );
                    allProjects =
                        await _apiClient.GetAsync<List<ProjectDto>>(
                            "/api/projectmanager/projects",
                            User
                        ) ?? new List<ProjectDto>();
                }

                _logger.LogInformation(
                    "Retrieved {Count} total projects from API",
                    allProjects.Count
                );

                // The API endpoints already handle project filtering for Contractors and Clients
                // No need for additional filtering here - trust the API
                var filteredProjects = allProjects;

                _logger.LogInformation(
                    "Filtered {Count} available projects for {Role} user {UserId}",
                    filteredProjects.Count,
                    userRole,
                    currentUserId
                );

                return filteredProjects;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting available projects for messaging for {Role} user {UserId}",
                    userRole,
                    currentUserId
                );
                return new List<ProjectDto>();
            }
        }

        private bool CanUserMessage(
            string senderId,
            string senderRole,
            string receiverId,
            string receiverRole
        )
        {
            // Use the existing messaging service logic
            return _messagingService.CanUserSendMessage(senderRole, receiverRole);
        }

        private async Task<bool> AreUsersAssociatedWithSameProjects(
            string userId1,
            string userId2,
            string userRole
        )
        {
            try
            {
                // Get projects for both users
                var user1Projects = await GetUserProjects(userId1, userRole);
                var user2Projects = await GetUserProjects(userId2, userRole);

                // Check if they have any projects in common
                var user1ProjectIds = user1Projects.Select(p => p.ProjectId).ToHashSet();
                var commonProjects = user2Projects.Any(p => user1ProjectIds.Contains(p.ProjectId));

                _logger.LogInformation(
                    "Users {UserId1} and {UserId2} have common projects: {HasCommon}",
                    userId1,
                    userId2,
                    commonProjects
                );

                return commonProjects;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error checking project associations between users {UserId1} and {UserId2}",
                    userId1,
                    userId2
                );
                return false;
            }
        }

        private async Task<bool> IsUserAssociatedWithProject(
            string userId,
            string projectId,
            string userRole,
            List<ProjectDto> userProjects
        )
        {
            try
            {
                return userProjects.Any(p => p.ProjectId == projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error checking if user {UserId} is associated with project {ProjectId}",
                    userId,
                    projectId
                );
                return false;
            }
        }

        private async Task<List<ProjectDto>> GetUserProjects(string userId, string userRole)
        {
            try
            {
                // Get projects for user

                if (userRole == "Contractor")
                {
                    // Get projects where contractor has tasks using the correct endpoint
                    // Call contractor projects API
                    var contractorProjects = await _apiClient.GetAsync<List<ProjectDto>>(
                        "/api/contractors/projects",
                        User
                    );

                    var result = contractorProjects ?? new List<ProjectDto>();
                    _logger.LogInformation(
                        "Contractor {UserId} has {Count} projects",
                        userId,
                        result.Count
                    );
                    return result;
                }
                else if (userRole == "Client")
                {
                    // Get projects where client is associated
                    _logger.LogInformation(
                        "Calling client projects API: /api/client/projects/{UserId}",
                        userId
                    );
                    var clientProjects = await _apiClient.GetAsync<List<ProjectDto>>(
                        $"/api/client/projects/{userId}",
                        User
                    );
                    var result = clientProjects ?? new List<ProjectDto>();
                    _logger.LogInformation(
                        "Client {UserId} has {Count} projects",
                        userId,
                        result.Count
                    );
                    return result;
                }
                else
                {
                    // For Admins and PMs, return all projects
                    _logger.LogInformation(
                        "Getting all projects for {Role} user {UserId}",
                        userRole,
                        userId
                    );
                    var allProjects = await _apiClient.GetAsync<List<ProjectDto>>(
                        "/api/projectmanager/projects",
                        User
                    );
                    var result = allProjects ?? new List<ProjectDto>();
                    _logger.LogInformation(
                        "{Role} user {UserId} has access to {Count} projects",
                        userRole,
                        userId,
                        result.Count
                    );
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error getting projects for user {UserId} with role {Role}",
                    userId,
                    userRole
                );
                return new List<ProjectDto>();
            }
        }

        [HttpGet("debug/messages")]
        public async Task<IActionResult> DebugMessages()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                // Get all messages for this user
                var messages = await _apiClient.GetAsync<List<MessageDto>>(
                    $"/api/messages/user/{currentUserId}",
                    User
                );

                var debugData = new
                {
                    UserId = currentUserId,
                    Role = userRole,
                    TotalMessages = messages?.Count ?? 0,
                    DirectMessages = messages
                        ?.Where(m =>
                            (m.MessageType == "direct" || m.MessageType == "thread")
                            && m.ThreadParticipants != null
                            && m.ThreadParticipants.Count == 2
                        )
                        .Count() ?? 0,
                    WorkflowMessages = messages?.Where(m => m.MessageType == "workflow").Count()
                        ?? 0,
                    SampleMessages = messages
                        ?.Take(5)
                        .Select(m => new
                        {
                            MessageId = m.MessageId,
                            SenderId = m.SenderId,
                            ReceiverId = m.ReceiverId,
                            Subject = m.Subject,
                            MessageType = m.MessageType,
                            ThreadId = m.ThreadId,
                            SentAt = m.SentAt,
                        })
                        .ToList(),
                };

                return Json(debugData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug messages endpoint");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("debug/contractor-data")]
        public async Task<IActionResult> DebugContractorData()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                if (userRole != "Contractor")
                {
                    return BadRequest("This endpoint is only for contractors");
                }

                // Get contractor users
                var contractorUsers = await _apiClient.GetAsync<List<object>>(
                    "/api/contractors/messaging/available-users",
                    User
                );

                // Get contractor projects
                var contractorProjects = await _apiClient.GetAsync<List<object>>(
                    "/api/contractors/messaging/available-projects",
                    User
                );

                var debugData = new
                {
                    UserId = currentUserId,
                    Role = userRole,
                    Users = new
                    {
                        Count = contractorUsers?.Count ?? 0,
                        RawData = contractorUsers?.Take(2).ToList(),
                        ProcessedData = contractorUsers
                            ?.Select(u => new
                            {
                                UserId = GetPropertyValue(u, "UserId"),
                                FullName = GetPropertyValue(u, "FullName"),
                                Role = GetPropertyValue(u, "Role"),
                                Email = GetPropertyValue(u, "Email"),
                            })
                            .Take(2)
                            .ToList(),
                    },
                    Projects = new
                    {
                        Count = contractorProjects?.Count ?? 0,
                        RawData = contractorProjects?.Take(2).ToList(),
                        ProcessedData = contractorProjects
                            ?.Select(p => new
                            {
                                ProjectId = GetPropertyValue(p, "ProjectId"),
                                Name = GetPropertyValue(p, "Name"),
                                Description = GetPropertyValue(p, "Description"),
                                Status = GetPropertyValue(p, "Status"),
                            })
                            .Take(2)
                            .ToList(),
                    },
                };

                return Json(debugData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug endpoint");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string? GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                if (obj is JsonElement jsonElement)
                {
                    // Try exact property name first
                    if (jsonElement.TryGetProperty(propertyName, out var property))
                    {
                        return property.GetString();
                    }

                    // Try case-insensitive search through all properties
                    foreach (var prop in jsonElement.EnumerateObject())
                    {
                        if (
                            string.Equals(
                                prop.Name,
                                propertyName,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        {
                            return prop.Value.GetString();
                        }
                    }

                    // Log for debugging
                    _logger.LogWarning(
                        "Property '{PropertyName}' not found in JsonElement. Available properties: {Properties}",
                        propertyName,
                        string.Join(", ", jsonElement.EnumerateObject().Select(p => p.Name))
                    );

                    return null;
                }
                else
                {
                    // Handle regular objects using reflection
                    var property = obj.GetType().GetProperty(propertyName);
                    return property?.GetValue(obj)?.ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error getting property '{PropertyName}' from object",
                    propertyName
                );
                return null;
            }
        }
    }
}
