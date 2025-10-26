using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
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
                var receiverUser = await GetUserById(request.ReceiverId);
                if (receiverUser == null)
                {
                    return Json(new { success = false, message = "Recipient not found" });
                }

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
        public async Task<IActionResult> MarkAsRead(string messageId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var success = await _messagingService.MarkAsReadAsync(messageId, currentUserId);
                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as read");
                return Json(new { success = false, message = "Error updating message" });
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

                // Handle sample threads
                if (threadId == "sample-workflow-1")
                {
                    var sampleMessage = new MessageDto
                    {
                        MessageId = "sample-msg-1",
                        SenderId = "system",
                        SenderName = "System",
                        ReceiverId = currentUserId,
                        ReceiverName = "You",
                        ProjectId = "sample-project",
                        Subject = "Sample Workflow Message",
                        Content =
                            "This is a sample workflow message for testing purposes. This message was generated by the system to help you test the messaging functionality.",
                        SentAt = DateTime.UtcNow.AddHours(-1),
                        IsRead = true,
                        ThreadId = threadId,
                        MessageType = "workflow",
                    };
                    return Json(
                        new { success = true, messages = new List<MessageDto> { sampleMessage } }
                    );
                }

                if (threadId == "sample-direct-1")
                {
                    var sampleMessage = new MessageDto
                    {
                        MessageId = "sample-msg-2",
                        SenderId = "sender-1",
                        SenderName = "John Doe",
                        ReceiverId = currentUserId,
                        ReceiverName = "You",
                        ProjectId = "sample-project",
                        Subject = "Sample Direct Message",
                        Content =
                            "This is a sample direct message for testing purposes. This message was sent by another user to help you test the messaging functionality.",
                        SentAt = DateTime.UtcNow.AddHours(-2),
                        IsRead = false,
                        ThreadId = threadId,
                        MessageType = "direct",
                    };
                    return Json(
                        new { success = true, messages = new List<MessageDto> { sampleMessage } }
                    );
                }

                var messages = await _messagingService.GetThreadMessagesAsync(
                    threadId,
                    currentUserId
                );
                return Json(new { success = true, messages = messages });
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

                // Get messages using IApiClient with proper authentication
                var allMessages = await _apiClient.GetAsync<List<MessageDto>>(
                    $"/api/messages/user/{userId}",
                    User
                );

                if (allMessages != null)
                {
                    _logger.LogInformation(
                        "Deserialized {Count} total messages from messages API",
                        allMessages.Count
                    );

                    // Log all message types for debugging
                    var messageTypes = allMessages
                        .GroupBy(m => m.MessageType)
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .ToList();
                    _logger.LogInformation(
                        "Message types found: {MessageTypes}",
                        string.Join(", ", messageTypes.Select(mt => $"{mt.Type}: {mt.Count}"))
                    );

                    // Log some sample messages for debugging
                    var sampleMessages = allMessages
                        .Take(3)
                        .Select(m => new
                        {
                            MessageId = m.MessageId,
                            MessageType = m.MessageType,
                            ReceiverId = m.ReceiverId,
                            SenderId = m.SenderId,
                            Subject = m.Subject,
                        })
                        .ToList();
                    _logger.LogInformation(
                        "Sample messages: {SampleMessages}",
                        string.Join(
                            " | ",
                            sampleMessages.Select(sm => $"{sm.MessageType}:{sm.ReceiverId}")
                        )
                    );

                    _logger.LogInformation("Current user ID: {UserId}", userId);

                    // Check if there are any messages for this user
                    var userMessages = allMessages.Where(m => m.ReceiverId == userId).ToList();
                    _logger.LogInformation(
                        "Found {Count} messages for user {UserId}",
                        userMessages.Count,
                        userId
                    );

                    // Filter for workflow messages where user is the receiver
                    var userWorkflowMessages = allMessages
                        .Where(m => m.MessageType == "workflow" && m.ReceiverId == userId)
                        .ToList();

                    _logger.LogInformation(
                        "Found {Count} workflow messages for user {UserId} from messages API",
                        userWorkflowMessages.Count,
                        userId
                    );

                    if (userWorkflowMessages.Any())
                    {
                        // Group messages by thread
                        var threadGroups = userWorkflowMessages.GroupBy(m => m.ThreadId).ToList();

                        _logger.LogInformation(
                            "Found {Count} workflow message threads for user {UserId}",
                            threadGroups.Count,
                            userId
                        );

                        // Convert to ThreadDto format and return immediately
                        var realWorkflowThreads = threadGroups
                            .Select(group => new ThreadDto
                            {
                                ThreadId = group.Key,
                                Subject = group.First().Subject,
                                ProjectId = group.First().ProjectId,
                                ProjectName = "", // Will be populated from project data if needed
                                MessageCount = group.Count(),
                                LastMessageAt = group.Max(m => m.SentAt),
                                CreatedAt = group.Min(m => m.SentAt),
                                LastMessageSenderName = "System",
                                LastMessagePreview =
                                    group.OrderByDescending(m => m.SentAt).First().Content.Length
                                    > 50
                                        ? group
                                            .OrderByDescending(m => m.SentAt)
                                            .First()
                                            .Content.Substring(0, 47) + "..."
                                        : group.OrderByDescending(m => m.SentAt).First().Content,
                                Participants = new List<string> { userId },
                                ParticipantNames = new List<string>(),
                                ThreadType = "workflow",
                                HasUnreadMessages = group.Any(m => !m.IsRead),
                                UnreadCount = group.Count(m => !m.IsRead),
                                IsActive = false,
                            })
                            .OrderByDescending(t => t.LastMessageAt)
                            .ToList();

                        _logger.LogInformation(
                            "Returning {Count} real workflow threads for user {UserId}",
                            realWorkflowThreads.Count,
                            userId
                        );
                        return realWorkflowThreads;
                    }
                }
                else
                {
                    _logger.LogWarning("ApiClient returned null for workflow messages");
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
                    ThreadId = "sample-workflow-1",
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

                if (messages != null)
                {
                    // Group messages by thread
                    var threadGroups = messages
                        .Where(m => m.MessageType == "direct" && m.ReceiverId == userId)
                        .GroupBy(m => m.ThreadId)
                        .ToList();

                    return threadGroups
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
                            HasUnreadMessages = group.Any(m => !m.IsRead),
                            UnreadCount = group.Count(m => !m.IsRead),
                            IsActive = false,
                        })
                        .OrderByDescending(t => t.LastMessageAt)
                        .ToList();
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

        private async Task<UserDto?> GetUserById(string userId)
        {
            try
            {
                var users = await _apiClient.GetAsync<List<UserDto>>("/api/admin/users", User);
                return users?.FirstOrDefault(u => u.UserId == userId);
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
                        "/api/contractor/messaging/available-users",
                        User
                    );
                    if (contractorUsers != null)
                    {
                        allUsers = contractorUsers
                            .Select(u => new UserDto
                            {
                                UserId = ((JsonElement)u).GetProperty("UserId").GetString() ?? "",
                                FullName =
                                    ((JsonElement)u).GetProperty("FullName").GetString() ?? "",
                                Role = ((JsonElement)u).GetProperty("Role").GetString() ?? "",
                                Email = ((JsonElement)u).GetProperty("Email").GetString() ?? "",
                            })
                            .ToList();
                    }
                }
                else if (userRole == "Client")
                {
                    _logger.LogInformation("Using client-specific endpoint for users");
                    var clientUsers = await _apiClient.GetAsync<List<object>>(
                        "/api/client/messaging/available-users",
                        User
                    );
                    if (clientUsers != null)
                    {
                        allUsers = clientUsers
                            .Select(u => new UserDto
                            {
                                UserId = ((JsonElement)u).GetProperty("UserId").GetString() ?? "",
                                FullName =
                                    ((JsonElement)u).GetProperty("FullName").GetString() ?? "",
                                Role = ((JsonElement)u).GetProperty("Role").GetString() ?? "",
                                Email = ((JsonElement)u).GetProperty("Email").GetString() ?? "",
                            })
                            .ToList();
                    }
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

                var filteredUsers = new List<UserDto>();

                foreach (var user in allUsers)
                {
                    // Skip self
                    if (user.UserId == currentUserId)
                        continue;

                    // Apply role-based filtering
                    if (CanUserMessage(currentUserId, userRole, user.UserId, user.Role))
                    {
                        // For Contractors and Clients, further filter by project associations
                        if (userRole == "Contractor" || userRole == "Client")
                        {
                            _logger.LogInformation(
                                "Checking project associations for {Role} user {UserId} with {TargetUserRole} user {TargetUserId}",
                                userRole,
                                currentUserId,
                                user.Role,
                                user.UserId
                            );

                            if (
                                await AreUsersAssociatedWithSameProjects(
                                    currentUserId,
                                    user.UserId,
                                    userRole
                                )
                            )
                            {
                                filteredUsers.Add(user);
                                _logger.LogInformation(
                                    "Added user {UserId} ({Role}) to available users",
                                    user.UserId,
                                    user.Role
                                );
                            }
                            else
                            {
                                _logger.LogInformation(
                                    "User {UserId} ({Role}) not associated with same projects",
                                    user.UserId,
                                    user.Role
                                );
                            }
                        }
                        else
                        {
                            // Admins and PMs can message anyone they're allowed to
                            filteredUsers.Add(user);
                            _logger.LogInformation(
                                "Added user {UserId} ({Role}) to available users (Admin/PM access)",
                                user.UserId,
                                user.Role
                            );
                        }
                    }
                    else
                    {
                        _logger.LogInformation(
                            "User {UserId} ({Role}) filtered out due to role restrictions",
                            user.UserId,
                            user.Role
                        );
                    }
                }

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
                        "/api/contractor/messaging/available-projects",
                        User
                    );
                    if (contractorProjects != null)
                    {
                        allProjects = contractorProjects
                            .Select(p => new ProjectDto
                            {
                                ProjectId =
                                    ((JsonElement)p).GetProperty("ProjectId").GetString() ?? "",
                                Name = ((JsonElement)p).GetProperty("Name").GetString() ?? "",
                                Description =
                                    ((JsonElement)p).GetProperty("Description").GetString() ?? "",
                                Status = ((JsonElement)p).GetProperty("Status").GetString() ?? "",
                            })
                            .ToList();
                    }
                }
                else if (userRole == "Client")
                {
                    _logger.LogInformation("Using client-specific endpoint for projects");
                    var clientProjects = await _apiClient.GetAsync<List<object>>(
                        "/api/client/messaging/available-projects",
                        User
                    );
                    if (clientProjects != null)
                    {
                        allProjects = clientProjects
                            .Select(p => new ProjectDto
                            {
                                ProjectId =
                                    ((JsonElement)p).GetProperty("ProjectId").GetString() ?? "",
                                Name = ((JsonElement)p).GetProperty("Name").GetString() ?? "",
                                Description =
                                    ((JsonElement)p).GetProperty("Description").GetString() ?? "",
                                Status = ((JsonElement)p).GetProperty("Status").GetString() ?? "",
                            })
                            .ToList();
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

                var filteredProjects = new List<ProjectDto>();

                foreach (var project in allProjects)
                {
                    // For Contractors and Clients, only show projects they're associated with
                    if (userRole == "Contractor" || userRole == "Client")
                    {
                        _logger.LogInformation(
                            "Checking if {Role} user {UserId} is associated with project {ProjectId} ({ProjectName})",
                            userRole,
                            currentUserId,
                            project.ProjectId,
                            project.Name
                        );

                        if (
                            await IsUserAssociatedWithProject(
                                currentUserId,
                                project.ProjectId,
                                userRole
                            )
                        )
                        {
                            filteredProjects.Add(project);
                            _logger.LogInformation(
                                "Added project {ProjectId} ({ProjectName}) to available projects",
                                project.ProjectId,
                                project.Name
                            );
                        }
                        else
                        {
                            _logger.LogInformation(
                                "User {UserId} not associated with project {ProjectId} ({ProjectName})",
                                currentUserId,
                                project.ProjectId,
                                project.Name
                            );
                        }
                    }
                    else
                    {
                        // Admins and PMs can see all projects
                        filteredProjects.Add(project);
                        _logger.LogInformation(
                            "Added project {ProjectId} ({ProjectName}) to available projects (Admin/PM access)",
                            project.ProjectId,
                            project.Name
                        );
                    }
                }

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
            string userRole
        )
        {
            try
            {
                var userProjects = await GetUserProjects(userId, userRole);
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
                _logger.LogInformation(
                    "Getting projects for {Role} user {UserId}",
                    userRole,
                    userId
                );

                if (userRole == "Contractor")
                {
                    // Get projects where contractor has tasks
                    _logger.LogInformation(
                        "Calling contractor projects API: /api/contractor/projects/{UserId}",
                        userId
                    );
                    var contractorProjects = await _apiClient.GetAsync<List<ProjectDto>>(
                        $"/api/contractor/projects/{userId}",
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
    }
}
