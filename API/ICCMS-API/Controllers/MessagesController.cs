using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")] // All authenticated users can access messages
    public class MessagesController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly INotificationService _notificationService;
        private readonly ISupabaseService _supabaseService;
        private readonly IMessageValidationService _validationService;
        private readonly IWorkflowMessageService _workflowService;

        public MessagesController(
            IFirebaseService firebaseService,
            INotificationService notificationService,
            ISupabaseService supabaseService,
            IMessageValidationService validationService,
            IWorkflowMessageService workflowService
        )
        {
            _firebaseService = firebaseService;
            _notificationService = notificationService;
            _supabaseService = supabaseService;
            _validationService = validationService;
            _workflowService = workflowService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Message>>> GetMessages()
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin,Tester")]
        public async Task<ActionResult<List<Message>>> GetAdminAllMessages(
            [FromQuery] string? messageType = null,
            [FromQuery] string? projectId = null,
            [FromQuery] string? senderId = null,
            [FromQuery] string? receiverId = null,
            [FromQuery] string? threadId = null,
            [FromQuery] bool? isRead = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null
        )
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                
                // Apply filters
                if (!string.IsNullOrEmpty(messageType))
                {
                    messages = messages.Where(m => m.MessageType.Equals(messageType, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                if (!string.IsNullOrEmpty(projectId))
                {
                    messages = messages.Where(m => m.ProjectId == projectId).ToList();
                }
                
                if (!string.IsNullOrEmpty(senderId))
                {
                    messages = messages.Where(m => m.SenderId == senderId).ToList();
                }
                
                if (!string.IsNullOrEmpty(receiverId))
                {
                    messages = messages.Where(m => m.ReceiverId == receiverId).ToList();
                }
                
                if (!string.IsNullOrEmpty(threadId))
                {
                    messages = messages.Where(m => m.ThreadId == threadId).ToList();
                }
                
                if (isRead.HasValue)
                {
                    messages = messages.Where(m => m.IsRead == isRead.Value).ToList();
                }
                
                if (startDate.HasValue)
                {
                    messages = messages.Where(m => m.SentAt >= startDate.Value).ToList();
                }
                
                if (endDate.HasValue)
                {
                    messages = messages.Where(m => m.SentAt <= endDate.Value).ToList();
                }
                
                return Ok(messages.OrderByDescending(m => m.SentAt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("admin/threads/all")]
        [Authorize(Roles = "Admin,Tester")]
        public async Task<ActionResult<List<MessageThread>>> GetAdminAllThreads(
            [FromQuery] string? projectId = null,
            [FromQuery] string? threadType = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null
        )
        {
            try
            {
                var threads = await _firebaseService.GetCollectionAsync<MessageThread>("threads");
                
                // Apply filters
                if (!string.IsNullOrEmpty(projectId))
                {
                    threads = threads.Where(t => t.ProjectId == projectId).ToList();
                }
                
                if (!string.IsNullOrEmpty(threadType))
                {
                    threads = threads.Where(t => t.ThreadType.Equals(threadType, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                
                if (startDate.HasValue)
                {
                    threads = threads.Where(t => t.CreatedAt >= startDate.Value).ToList();
                }
                
                if (endDate.HasValue)
                {
                    threads = threads.Where(t => t.CreatedAt <= endDate.Value).ToList();
                }
                
                return Ok(threads.Where(t => t.IsActive).OrderByDescending(t => t.LastMessageAt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("admin/threads/filtered")]
        [Authorize(Roles = "Admin,Tester")]
        public async Task<ActionResult<FilteredThreadsResponse>> GetFilteredThreads(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? projectId = null,
            [FromQuery] string? threadType = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? readStatus = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null
        )
        {
            try
            {
                // Get all threads
                var allThreads = await _firebaseService.GetCollectionAsync<MessageThread>("threads");
                var activeThreads = allThreads.Where(t => t.IsActive).ToList();

                // Get all users for name resolution
                var users = await _firebaseService.GetCollectionAsync<User>("users");
                var usersDict = users.ToDictionary(u => u.UserId, u => u.FullName ?? u.Email);

                // Get all projects for name resolution
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var projectsDict = projects.ToDictionary(p => p.ProjectId, p => p.Name);

                // Apply filters
                var filteredThreads = activeThreads.AsQueryable();

                // Project filter
                if (!string.IsNullOrEmpty(projectId) && projectId != "all")
                {
                    filteredThreads = filteredThreads.Where(t => t.ProjectId == projectId);
                }

                // Thread type filter
                if (!string.IsNullOrEmpty(threadType) && threadType != "all")
                {
                    filteredThreads = filteredThreads.Where(t => t.ThreadType.Equals(threadType, StringComparison.OrdinalIgnoreCase));
                }

                // User filter (check if user is in participants)
                if (!string.IsNullOrEmpty(userId) && userId != "all")
                {
                    filteredThreads = filteredThreads.Where(t => t.Participants.Contains(userId));
                }

                // Date filters
                if (startDate.HasValue)
                {
                    filteredThreads = filteredThreads.Where(t => t.LastMessageAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    filteredThreads = filteredThreads.Where(t => t.LastMessageAt <= endDate.Value);
                }

                // Search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    filteredThreads = filteredThreads.Where(t => 
                        t.Subject.ToLower().Contains(searchLower) ||
                        (projectsDict.ContainsKey(t.ProjectId) && projectsDict[t.ProjectId].ToLower().Contains(searchLower)) ||
                        t.Participants.Any(p => usersDict.ContainsKey(p) && usersDict[p].ToLower().Contains(searchLower))
                    );
                }

                // Read status filter (requires loading messages)
                if (!string.IsNullOrEmpty(readStatus) && readStatus != "all")
                {
                    var threadsWithReadStatus = new List<MessageThread>();
                    
                    foreach (var thread in filteredThreads.Take(100)) // Limit to 100 for performance
                    {
                        try
                        {
                            var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                            var threadMessages = messages.Where(m => m.ThreadId == thread.ThreadId).ToList();
                            
                            if (threadMessages.Any())
                            {
                                var hasUnreadMessages = threadMessages.Any(m => !m.IsRead);
                                var allRead = threadMessages.All(m => m.IsRead);
                                
                                if ((readStatus == "unread" && hasUnreadMessages) || 
                                    (readStatus == "read" && allRead))
                                {
                                    threadsWithReadStatus.Add(thread);
                                }
                            }
                        }
                        catch
                        {
                            // Skip threads that can't be loaded
                            continue;
                        }
                    }
                    
                    filteredThreads = threadsWithReadStatus.AsQueryable();
                }

                // Order by last message date
                var orderedThreads = filteredThreads.OrderByDescending(t => t.LastMessageAt).ToList();

                // Apply pagination
                var totalCount = orderedThreads.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var pagedThreads = orderedThreads
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Create response with thread summaries
                var threadSummaries = new List<ThreadSummary>();
                foreach (var thread in pagedThreads)
                {
                    var participantNames = thread.Participants
                        .Where(p => usersDict.ContainsKey(p))
                        .Select(p => usersDict[p])
                        .ToList();

                    var projectName = projectsDict.ContainsKey(thread.ProjectId) 
                        ? projectsDict[thread.ProjectId] 
                        : "Unknown Project";

                    threadSummaries.Add(new ThreadSummary
                    {
                        ThreadId = thread.ThreadId,
                        Subject = thread.Subject,
                        ProjectId = thread.ProjectId,
                        ProjectName = projectName,
                        ThreadType = thread.ThreadType,
                        Participants = thread.Participants,
                        ParticipantNames = participantNames,
                        MessageCount = thread.MessageCount,
                        LastMessageAt = thread.LastMessageAt,
                        CreatedAt = thread.CreatedAt,
                        IsActive = thread.IsActive
                    });
                }

                var response = new FilteredThreadsResponse
                {
                    Threads = threadSummaries,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Message>> GetMessage(string id)
        {
            try
            {
                var message = await _firebaseService.GetDocumentAsync<Message>("messages", id);
                if (message == null)
                    return NotFound();
                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Message>>> GetMessagesByUser(string userId)
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var userMessages = messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .ToList();
                return Ok(userMessages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<Message>>> GetMessagesByProject(string projectId)
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var projectMessages = messages.Where(m => m.ProjectId == projectId).ToList();
                return Ok(projectMessages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateMessage(
            [FromBody] CreateMessageRequest request
        )
        {
            try
            {
                // Validate the message
                var validationResult = await _validationService.ValidateMessageAsync(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(
                        new
                        {
                            errors = validationResult.Errors,
                            warnings = validationResult.Warnings,
                            severity = validationResult.Severity.ToString(),
                        }
                    );
                }

                // Create message from validated request
                var message = new Message
                {
                    MessageId = Guid.NewGuid().ToString(),
                    SenderId = request.SenderId,
                    ReceiverId = request.ReceiverId,
                    ProjectId = request.ProjectId,
                    Subject = request.Subject,
                    Content = request.Content,
                    SentAt = DateTime.UtcNow,
                    ThreadId = request.ThreadId ?? Guid.NewGuid().ToString(),
                    ParentMessageId = request.ParentMessageId,
                    IsThreadStarter = string.IsNullOrEmpty(request.ThreadId),
                    ThreadDepth = string.IsNullOrEmpty(request.ParentMessageId) ? 0 : 1,
                    MessageType = request.MessageType,
                    ThreadParticipants = request.ThreadParticipants,
                };

                // If this is a new thread starter, create thread ID
                if (string.IsNullOrEmpty(request.ThreadId))
                {
                    message.IsThreadStarter = true;
                    message.ThreadDepth = 0;
                    message.MessageType = "thread";
                }

                var messageId = await _firebaseService.AddDocumentAsync("messages", message);

                // Update thread information if this is a thread starter
                if (message.IsThreadStarter)
                {
                    await CreateOrUpdateThreadAsync(message);
                }
                else if (!string.IsNullOrEmpty(message.ThreadId))
                {
                    await UpdateThreadAsync(message);
                }

                // Send push notification to the receiver
                await SendMessageNotificationAsync(message);

                // Return warnings if any
                if (validationResult.Warnings.Any())
                {
                    return Ok(new { messageId, warnings = validationResult.Warnings });
                }

                return Ok(messageId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("broadcast")]
        public async Task<ActionResult<string>> BroadcastMessage(
            [FromBody] BroadcastMessageRequest request
        )
        {
            try
            {
                var messageId = Guid.NewGuid().ToString();
                var sentAt = DateTime.UtcNow;

                // Get all users in the project
                var users = await _firebaseService.GetCollectionAsync<User>("users");
                var projectUsers = users.Where(u => u.IsActive).ToList();

                var deviceTokens = new List<string>();
                var notifications = new List<Task>();

                foreach (var user in projectUsers)
                {
                    if (!string.IsNullOrEmpty(user.DeviceToken))
                    {
                        deviceTokens.Add(user.DeviceToken);
                    }

                    // Create individual message record for each user
                    var message = new Message
                    {
                        MessageId = $"{messageId}_{user.UserId}",
                        SenderId = request.SenderId,
                        ReceiverId = user.UserId,
                        ProjectId = request.ProjectId,
                        Subject = request.Subject,
                        Content = request.Content,
                        IsRead = false,
                        SentAt = sentAt,
                    };

                    await _firebaseService.AddDocumentAsync("messages", message);
                }

                // Send push notification to all users with device tokens
                if (deviceTokens.Any())
                {
                    var notificationData = new Dictionary<string, string>
                    {
                        { "messageId", messageId },
                        { "senderId", request.SenderId },
                        { "projectId", request.ProjectId },
                        { "type", "broadcast" },
                        { "action", "broadcast_message" },
                    };

                    await _notificationService.SendToMultipleDevicesAsync(
                        deviceTokens,
                        request.Subject,
                        request.Content,
                        notificationData
                    );
                }

                return Ok(new { messageId, recipientsCount = projectUsers.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("thread")]
        public async Task<ActionResult<string>> CreateThread([FromBody] CreateThreadRequest request)
        {
            try
            {
                var threadId = Guid.NewGuid().ToString();
                var messageId = Guid.NewGuid().ToString();
                var sentAt = DateTime.UtcNow;

                // Create the starter message
                var starterMessage = new Message
                {
                    MessageId = messageId,
                    SenderId = HttpContext.Items["UserId"] as string ?? string.Empty,
                    ReceiverId = request.Participants.FirstOrDefault() ?? string.Empty,
                    ProjectId = request.ProjectId,
                    Subject = request.Subject,
                    Content = request.Content,
                    SentAt = sentAt,
                    ThreadId = threadId,
                    IsThreadStarter = true,
                    ThreadDepth = 0,
                    MessageType = "thread",
                    ThreadParticipants = request.Participants,
                };

                await _firebaseService.AddDocumentAsync("messages", starterMessage);

                // Create thread record
                var thread = new MessageThread
                {
                    ThreadId = threadId,
                    ProjectId = request.ProjectId,
                    Subject = request.Subject,
                    StarterMessageId = messageId,
                    StarterUserId = starterMessage.SenderId,
                    Participants = request.Participants,
                    MessageCount = 1,
                    LastMessageAt = sentAt,
                    LastMessageId = messageId,
                    LastMessageSenderId = starterMessage.SenderId,
                    CreatedAt = sentAt,
                    ThreadType = request.ThreadType,
                    Tags = request.Tags,
                };

                await _firebaseService.AddDocumentAsync("threads", thread);

                // Send notifications to all participants
                await SendThreadNotificationAsync(starterMessage, request.Participants);

                return Ok(new { threadId, messageId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("reply")]
        public async Task<ActionResult<string>> ReplyToMessage(
            [FromBody] ReplyToMessageRequest request
        )
        {
            try
            {
                // Get the parent message
                var parentMessage = await _firebaseService
                    .GetCollectionAsync<Message>("messages")
                    .ContinueWith(t =>
                        t.Result.FirstOrDefault(m => m.MessageId == request.ParentMessageId)
                    );

                if (parentMessage == null)
                {
                    return NotFound("Parent message not found");
                }

                var messageId = Guid.NewGuid().ToString();
                var sentAt = DateTime.UtcNow;
                var senderId = HttpContext.Items["UserId"] as string ?? string.Empty;

                // Create reply message
                var replyMessage = new Message
                {
                    MessageId = messageId,
                    SenderId = senderId,
                    ReceiverId = parentMessage.SenderId, // Reply to original sender
                    ProjectId = parentMessage.ProjectId,
                    Subject = $"Re: {parentMessage.Subject}",
                    Content = request.Content,
                    SentAt = sentAt,
                    ThreadId = parentMessage.ThreadId,
                    ParentMessageId = request.ParentMessageId,
                    IsThreadStarter = false,
                    ThreadDepth = parentMessage.ThreadDepth + 1,
                    MessageType = "thread",
                    ThreadParticipants = parentMessage.ThreadParticipants,
                };

                await _firebaseService.AddDocumentAsync("messages", replyMessage);

                // Update thread information
                await UpdateThreadAsync(replyMessage);

                // Send notifications to thread participants
                var recipients = parentMessage
                    .ThreadParticipants.Where(p => p != senderId)
                    .ToList();
                await SendThreadNotificationAsync(replyMessage, recipients);

                return Ok(messageId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("threads")]
        public async Task<ActionResult<List<ThreadSummary>>> GetThreads(
            [FromQuery] string? projectId = null
        )
        {
            try
            {
                var threads = await _firebaseService.GetCollectionAsync<MessageThread>("threads");

                if (!string.IsNullOrEmpty(projectId))
                {
                    threads = threads.Where(t => t.ProjectId == projectId).ToList();
                }

                var threadSummaries = new List<ThreadSummary>();
                var currentUserId = HttpContext.Items["UserId"] as string;

                foreach (var thread in threads.Where(t => t.IsActive))
                {
                    // Get last message details
                    var lastMessage = await _firebaseService
                        .GetCollectionAsync<Message>("messages")
                        .ContinueWith(t =>
                            t.Result.FirstOrDefault(m => m.MessageId == thread.LastMessageId)
                        );

                    var sender = await _firebaseService.GetDocumentAsync<User>(
                        "users",
                        thread.LastMessageSenderId
                    );

                    // Count unread messages for current user
                    var unreadCount = await GetUnreadCountForThread(
                        thread.ThreadId,
                        currentUserId ?? string.Empty
                    );

                    threadSummaries.Add(
                        new ThreadSummary
                        {
                            ThreadId = thread.ThreadId,
                            Subject = thread.Subject,
                            ProjectId = thread.ProjectId,
                            MessageCount = thread.MessageCount,
                            LastMessageAt = thread.LastMessageAt,
                            LastMessageSenderName = sender?.FullName ?? "Unknown",
                            LastMessagePreview =
                                lastMessage?.Content?.Length > 50
                                    ? lastMessage.Content.Substring(0, 47) + "..."
                                    : lastMessage?.Content ?? "",
                            Participants = thread.Participants,
                            ThreadType = thread.ThreadType,
                            HasUnreadMessages = unreadCount > 0,
                            UnreadCount = unreadCount,
                        }
                    );
                }

                return Ok(threadSummaries.OrderByDescending(t => t.LastMessageAt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("thread/{threadId}")]
        public async Task<ActionResult<List<Message>>> GetThreadMessages(string threadId)
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var threadMessages = messages
                    .Where(m => m.ThreadId == threadId)
                    .OrderBy(m => m.SentAt)
                    .ToList();

                return Ok(threadMessages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("attachment")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<AttachmentResponse>> UploadAttachment(
            [FromForm] IFormFile file,
            [FromForm] string messageId,
            [FromForm] string description = "",
            [FromForm] string category = "general"
        )
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded");
                }

                // Validate file size (max 50MB)
                if (file.Length > 50 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds 50MB limit");
                }

                // Get the message to validate it exists
                var message = await _firebaseService
                    .GetCollectionAsync<Message>("messages")
                    .ContinueWith(t => t.Result.FirstOrDefault(m => m.MessageId == messageId));

                if (message == null)
                {
                    return NotFound("Message not found");
                }

                // Generate unique filename
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var originalFileName = file.FileName;

                // Upload file to Supabase
                using var stream = file.OpenReadStream();
                var fileUrl = await _supabaseService.UploadFileAsync(
                    "message-attachments",
                    fileName,
                    stream,
                    file.ContentType
                );

                // Create attachment record
                var attachment = new MessageAttachment
                {
                    AttachmentId = Guid.NewGuid().ToString(),
                    MessageId = messageId,
                    FileName = fileName,
                    OriginalFileName = originalFileName,
                    FileType = file.ContentType,
                    FileSize = file.Length,
                    FileUrl = fileUrl,
                    UploadedBy = HttpContext.Items["UserId"] as string ?? string.Empty,
                    UploadedAt = DateTime.UtcNow,
                    Description = description,
                    Category = category,
                    IsImage = IsImageFile(file.ContentType),
                    IsDocument = IsDocumentFile(file.ContentType),
                    Status = "active",
                };

                // Save attachment to Firebase
                await _firebaseService.AddDocumentAsync("message-attachments", attachment);

                // Update message to include attachment
                message.Attachments.Add(attachment);
                message.HasAttachments = true;
                await _firebaseService.UpdateDocumentAsync("messages", messageId, message);

                // Return attachment response
                var response = new AttachmentResponse
                {
                    AttachmentId = attachment.AttachmentId,
                    FileName = attachment.FileName,
                    OriginalFileName = attachment.OriginalFileName,
                    FileType = attachment.FileType,
                    FileSize = attachment.FileSize,
                    FileUrl = attachment.FileUrl,
                    ThumbnailUrl = attachment.ThumbnailUrl,
                    UploadedAt = attachment.UploadedAt,
                    Description = attachment.Description,
                    IsImage = attachment.IsImage,
                    IsDocument = attachment.IsDocument,
                    Category = attachment.Category,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("attachment/{attachmentId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<AttachmentResponse>> GetAttachment(string attachmentId)
        {
            try
            {
                var attachments = await _firebaseService.GetCollectionAsync<MessageAttachment>(
                    "message-attachments"
                );
                var attachment = attachments.FirstOrDefault(a => a.AttachmentId == attachmentId);

                if (attachment == null)
                {
                    return NotFound("Attachment not found");
                }

                var response = new AttachmentResponse
                {
                    AttachmentId = attachment.AttachmentId,
                    FileName = attachment.FileName,
                    OriginalFileName = attachment.OriginalFileName,
                    FileType = attachment.FileType,
                    FileSize = attachment.FileSize,
                    FileUrl = attachment.FileUrl,
                    ThumbnailUrl = attachment.ThumbnailUrl,
                    UploadedAt = attachment.UploadedAt,
                    Description = attachment.Description,
                    IsImage = attachment.IsImage,
                    IsDocument = attachment.IsDocument,
                    Category = attachment.Category,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("message/{messageId}/attachments")]
        public async Task<ActionResult<List<AttachmentResponse>>> GetMessageAttachments(
            string messageId
        )
        {
            try
            {
                var attachments = await _firebaseService.GetCollectionAsync<MessageAttachment>(
                    "message-attachments"
                );
                var messageAttachments = attachments
                    .Where(a => a.MessageId == messageId && a.Status == "active")
                    .OrderBy(a => a.UploadedAt)
                    .ToList();

                var responses = messageAttachments
                    .Select(a => new AttachmentResponse
                    {
                        AttachmentId = a.AttachmentId,
                        FileName = a.FileName,
                        OriginalFileName = a.OriginalFileName,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        FileUrl = a.FileUrl,
                        ThumbnailUrl = a.ThumbnailUrl,
                        UploadedAt = a.UploadedAt,
                        Description = a.Description,
                        IsImage = a.IsImage,
                        IsDocument = a.IsDocument,
                        Category = a.Category,
                    })
                    .ToList();

                return Ok(responses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("attachment/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(string attachmentId)
        {
            try
            {
                var attachments = await _firebaseService.GetCollectionAsync<MessageAttachment>(
                    "message-attachments"
                );
                var attachment = attachments.FirstOrDefault(a => a.AttachmentId == attachmentId);

                if (attachment == null)
                {
                    return NotFound("Attachment not found");
                }

                // Delete file from Supabase
                await _supabaseService.DeleteFileAsync("message-attachments", attachment.FileName);

                // Update attachment status to deleted
                attachment.Status = "deleted";
                await _firebaseService.UpdateDocumentAsync(
                    "message-attachments",
                    attachmentId,
                    attachment
                );

                // Update message attachments list
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var message = messages.FirstOrDefault(m => m.MessageId == attachment.MessageId);
                if (message != null)
                {
                    var messageAttachment = message.Attachments.FirstOrDefault(a =>
                        a.AttachmentId == attachmentId
                    );
                    if (messageAttachment != null)
                    {
                        messageAttachment.Status = "deleted";
                        message.HasAttachments = message.Attachments.Any(a => a.Status == "active");
                        await _firebaseService.UpdateDocumentAsync(
                            "messages",
                            message.MessageId,
                            message
                        );
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("workflow")]
        public async Task<ActionResult<List<WorkflowMessage>>> GetWorkflowMessages(
            [FromQuery] string? projectId = null,
            [FromQuery] string? workflowType = null
        )
        {
            try
            {
                var messages = await _workflowService.GetWorkflowMessagesAsync(
                    projectId,
                    workflowType
                );
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("workflow/quote-approval")]
        public async Task<ActionResult> SendQuoteApprovalNotification(
            [FromBody] QuoteApprovalRequest request
        )
        {
            try
            {
                var success = await _workflowService.SendQuoteApprovalNotificationAsync(
                    request.QuoteId,
                    request.Action,
                    request.UserId
                );

                if (success)
                {
                    return Ok(new { message = "Quote approval notification sent successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to send quote approval notification" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("workflow/invoice-payment")]
        public async Task<ActionResult> SendInvoicePaymentNotification(
            [FromBody] InvoicePaymentRequest request
        )
        {
            try
            {
                var success = await _workflowService.SendInvoicePaymentNotificationAsync(
                    request.InvoiceId,
                    request.Action,
                    request.UserId
                );

                if (success)
                {
                    return Ok(new { message = "Invoice payment notification sent successfully" });
                }
                else
                {
                    return BadRequest(
                        new { error = "Failed to send invoice payment notification" }
                    );
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("workflow/project-update")]
        public async Task<ActionResult> SendProjectUpdateNotification(
            [FromBody] ProjectUpdateRequest request
        )
        {
            try
            {
                var success = await _workflowService.SendProjectUpdateNotificationAsync(
                    request.ProjectId,
                    request.UpdateType,
                    request.UserId
                );

                if (success)
                {
                    return Ok(new { message = "Project update notification sent successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to send project update notification" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("workflow/system-alert")]
        public async Task<ActionResult> SendSystemAlert([FromBody] SystemAlertRequest request)
        {
            try
            {
                var success = await _workflowService.SendSystemAlertAsync(
                    request.AlertType,
                    request.Message,
                    request.Recipients
                );

                if (success)
                {
                    return Ok(new { message = "System alert sent successfully" });
                }
                else
                {
                    return BadRequest(new { error = "Failed to send system alert" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMessage(string id, [FromBody] Message message)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("messages", id, message);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("messages", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private static bool IsImageFile(string contentType)
        {
            var imageTypes = new[]
            {
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/gif",
                "image/bmp",
                "image/webp",
                "image/svg+xml",
            };
            return imageTypes.Contains(contentType.ToLower());
        }

        private static bool IsDocumentFile(string contentType)
        {
            var documentTypes = new[]
            {
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-powerpoint",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "text/plain",
                "text/csv",
                "application/rtf",
            };
            return documentTypes.Contains(contentType.ToLower());
        }

        private async Task CreateOrUpdateThreadAsync(Message message)
        {
            try
            {
                var thread = new MessageThread
                {
                    ThreadId = message.ThreadId,
                    ProjectId = message.ProjectId,
                    Subject = message.Subject,
                    StarterMessageId = message.MessageId,
                    StarterUserId = message.SenderId,
                    Participants = message.ThreadParticipants,
                    MessageCount = 1,
                    LastMessageAt = message.SentAt,
                    LastMessageId = message.MessageId,
                    LastMessageSenderId = message.SenderId,
                    CreatedAt = message.SentAt,
                    ThreadType = "general",
                };

                await _firebaseService.AddDocumentAsync("threads", thread);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating thread: {ex.Message}");
            }
        }

        private async Task UpdateThreadAsync(Message message)
        {
            try
            {
                var threads = await _firebaseService.GetCollectionAsync<MessageThread>("threads");
                var thread = threads.FirstOrDefault(t => t.ThreadId == message.ThreadId);

                if (thread != null)
                {
                    thread.MessageCount++;
                    thread.LastMessageAt = message.SentAt;
                    thread.LastMessageId = message.MessageId;
                    thread.LastMessageSenderId = message.SenderId;

                    // Update participants if new ones are added
                    foreach (var participant in message.ThreadParticipants)
                    {
                        if (!thread.Participants.Contains(participant))
                        {
                            thread.Participants.Add(participant);
                        }
                    }

                    await _firebaseService.UpdateDocumentAsync("threads", thread.ThreadId, thread);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating thread: {ex.Message}");
            }
        }

        private async Task SendThreadNotificationAsync(Message message, List<string> recipients)
        {
            try
            {
                var deviceTokens = new List<string>();

                foreach (var recipientId in recipients)
                {
                    var user = await _firebaseService.GetDocumentAsync<User>("users", recipientId);
                    if (user != null && !string.IsNullOrEmpty(user.DeviceToken))
                    {
                        deviceTokens.Add(user.DeviceToken);
                    }
                }

                if (deviceTokens.Any())
                {
                    var sender = await _firebaseService.GetDocumentAsync<User>(
                        "users",
                        message.SenderId
                    );
                    var senderName = sender?.FullName ?? "Unknown User";

                    var notificationData = new Dictionary<string, string>
                    {
                        { "messageId", message.MessageId },
                        { "threadId", message.ThreadId },
                        { "senderId", message.SenderId },
                        { "projectId", message.ProjectId },
                        { "type", "thread_message" },
                        { "action", "thread_reply" },
                    };

                    var title = $"New reply in thread: {message.Subject}";
                    var body = $"{senderName}: {message.Content}";

                    // Add attachment indicator to notification
                    if (
                        message.HasAttachments && message.Attachments.Any(a => a.Status == "active")
                    )
                    {
                        var attachmentCount = message.Attachments.Count(a => a.Status == "active");
                        body +=
                            $" 📎 ({attachmentCount} attachment{(attachmentCount > 1 ? "s" : "")})";
                    }

                    if (body.Length > 100)
                    {
                        body = body.Substring(0, 97) + "...";
                    }

                    await _notificationService.SendToMultipleDevicesAsync(
                        deviceTokens,
                        title,
                        body,
                        notificationData
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending thread notification: {ex.Message}");
            }
        }

        private async Task<int> GetUnreadCountForThread(string threadId, string userId)
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                var threadMessages = messages
                    .Where(m =>
                        m.ThreadId == threadId
                        && (m.ReceiverId == userId || m.ThreadParticipants.Contains(userId))
                        && !m.IsRead
                        && m.SenderId != userId
                    )
                    .ToList();

                return threadMessages.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting unread count: {ex.Message}");
                return 0;
            }
        }

        private async Task SendMessageNotificationAsync(Message message)
        {
            try
            {
                // Get receiver information
                var receiver = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    message.ReceiverId
                );
                if (receiver == null || string.IsNullOrEmpty(receiver.DeviceToken))
                {
                    // Log that no device token found for receiver
                    Console.WriteLine($"No device token found for receiver: {message.ReceiverId}");
                    return;
                }

                // Get sender information for notification title
                var sender = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    message.SenderId
                );
                var senderName = sender?.FullName ?? "Unknown User";

                // Prepare notification data
                var notificationData = new Dictionary<string, string>
                {
                    { "messageId", message.MessageId },
                    { "senderId", message.SenderId },
                    { "receiverId", message.ReceiverId },
                    { "projectId", message.ProjectId },
                    { "type", "message" },
                    { "action", "message_received" },
                };

                // Send push notification
                var title = $"New message from {senderName}";
                var body = !string.IsNullOrEmpty(message.Subject)
                    ? $"{message.Subject}: {message.Content}"
                    : message.Content;

                // Add attachment indicator to notification
                if (message.HasAttachments && message.Attachments.Any(a => a.Status == "active"))
                {
                    var attachmentCount = message.Attachments.Count(a => a.Status == "active");
                    body += $" 📎 ({attachmentCount} attachment{(attachmentCount > 1 ? "s" : "")})";
                }

                // Truncate body if too long
                if (body.Length > 100)
                {
                    body = body.Substring(0, 97) + "...";
                }

                await _notificationService.SendToDeviceAsync(
                    receiver.DeviceToken,
                    title,
                    body,
                    notificationData
                );

                Console.WriteLine(
                    $"Push notification sent successfully to {receiver.FullName} ({receiver.DeviceToken})"
                );
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the message creation
                Console.WriteLine($"Error sending push notification: {ex.Message}");
            }
        }
    }

    public class BroadcastMessageRequest
    {
        public string SenderId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
