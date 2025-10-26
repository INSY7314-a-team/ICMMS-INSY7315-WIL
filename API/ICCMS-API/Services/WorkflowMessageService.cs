using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public class WorkflowMessageService : IWorkflowMessageService
    {
        private readonly IFirebaseService _firebaseService;
        private readonly INotificationService _notificationService;
        private readonly List<WorkflowMessageTemplate> _messageTemplates;

        public WorkflowMessageService(
            IFirebaseService firebaseService,
            INotificationService notificationService
        )
        {
            _firebaseService = firebaseService;
            _notificationService = notificationService;
            _messageTemplates = InitializeMessageTemplates();
        }

        public async Task<string> CreateWorkflowMessageAsync(WorkflowMessage workflowMessage)
        {
            try
            {
                workflowMessage.WorkflowMessageId = Guid.NewGuid().ToString();
                workflowMessage.CreatedAt = DateTime.UtcNow;
                workflowMessage.Status = "pending";

                var messageId = await _firebaseService.AddDocumentAsync(
                    "workflow-messages",
                    workflowMessage
                );

                // Update status to sent
                workflowMessage.Status = "sent";
                workflowMessage.SentAt = DateTime.UtcNow;

                await _firebaseService.UpdateDocumentAsync(
                    "workflow-messages",
                    messageId,
                    workflowMessage
                );

                return messageId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating workflow message: {ex.Message}");
                throw;
            }
        }

        public async Task<List<WorkflowMessage>> GetWorkflowMessagesAsync(
            string? projectId = null,
            string? workflowType = null
        )
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<WorkflowMessage>(
                    "workflow-messages"
                );

                if (!string.IsNullOrEmpty(projectId))
                {
                    messages = messages.Where(m => m.ProjectId == projectId).ToList();
                }

                if (!string.IsNullOrEmpty(workflowType))
                {
                    messages = messages.Where(m => m.WorkflowType == workflowType).ToList();
                }

                return messages.OrderByDescending(m => m.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting workflow messages: {ex.Message}");
                return new List<WorkflowMessage>();
            }
        }

        public async Task<WorkflowMessage?> GetWorkflowMessageAsync(string workflowMessageId)
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<WorkflowMessage>(
                    "workflow-messages"
                );
                return messages.FirstOrDefault(m => m.WorkflowMessageId == workflowMessageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting workflow message: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ProcessSystemEventAsync(SystemEvent systemEvent)
        {
            try
            {
                var template = await GetMessageTemplateAsync(
                    systemEvent.EventType,
                    systemEvent.Action
                );
                if (template == null)
                {
                    Console.WriteLine(
                        $"No template found for {systemEvent.EventType} - {systemEvent.Action}"
                    );
                    return false;
                }

                var workflowMessage = new WorkflowMessage
                {
                    WorkflowType = systemEvent.EventType,
                    EntityId = systemEvent.EntityId,
                    EntityType = systemEvent.EntityType,
                    Action = systemEvent.Action,
                    ProjectId = systemEvent.ProjectId,
                    Subject = ProcessTemplate(template.SubjectTemplate, systemEvent.Data),
                    Content = ProcessTemplate(template.ContentTemplate, systemEvent.Data),
                    Priority = template.Priority,
                    Recipients = await GetRecipientsForEventAsync(systemEvent),
                    IsSystemGenerated = true,
                    Metadata = systemEvent.Data,
                };

                await CreateWorkflowMessageAsync(workflowMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing system event: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendQuoteApprovalNotificationAsync(
            string quoteId,
            string action,
            string userId
        )
        {
            try
            {
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var quote = quotations.FirstOrDefault(q => q.QuotationId == quoteId);

                if (quote == null)
                {
                    Console.WriteLine($"Quote {quoteId} not found");
                    return false;
                }

                var systemEvent = new SystemEvent
                {
                    EventType = "quote_workflow",
                    EntityId = quoteId,
                    EntityType = "quotation",
                    Action = action,
                    ProjectId = quote.ProjectId,
                    UserId = userId,
                    Data = new Dictionary<string, object>
                    {
                        { "quoteId", quoteId },
                        { "quoteTotal", quote.GrandTotal },
                        { "quoteDescription", quote.Description },
                        { "clientId", quote.ClientId },
                        { "contractorId", quote.ContractorId },
                        { "action", action },
                        { "userId", userId },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending quote approval notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendInvoicePaymentNotificationAsync(
            string invoiceId,
            string action,
            string userId
        )
        {
            try
            {
                var invoices = await _firebaseService.GetCollectionAsync<Invoice>("invoices");
                var invoice = invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);

                if (invoice == null)
                {
                    Console.WriteLine($"Invoice {invoiceId} not found");
                    return false;
                }

                var systemEvent = new SystemEvent
                {
                    EventType = "invoice_workflow",
                    EntityId = invoiceId,
                    EntityType = "invoice",
                    Action = action,
                    ProjectId = invoice.ProjectId,
                    UserId = userId,
                    Data = new Dictionary<string, object>
                    {
                        { "invoiceId", invoiceId },
                        { "invoiceNumber", invoice.InvoiceNumber },
                        { "invoiceAmount", invoice.TotalAmount },
                        { "invoiceDescription", invoice.Description },
                        { "clientId", invoice.ClientId },
                        { "contractorId", invoice.ContractorId },
                        { "action", action },
                        { "userId", userId },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending invoice payment notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendProjectUpdateNotificationAsync(
            string projectId,
            string updateType,
            string userId
        )
        {
            try
            {
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var project = projects.FirstOrDefault(p => p.ProjectId == projectId);

                if (project == null)
                {
                    Console.WriteLine($"Project {projectId} not found");
                    return false;
                }

                var systemEvent = new SystemEvent
                {
                    EventType = "project_update",
                    EntityId = projectId,
                    EntityType = "project",
                    Action = updateType,
                    ProjectId = projectId,
                    UserId = userId,
                    Data = new Dictionary<string, object>
                    {
                        { "projectId", projectId },
                        { "projectName", project.Name },
                        { "projectDescription", project.Description },
                        { "updateType", updateType },
                        { "userId", userId },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending project update notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSystemAlertAsync(
            string alertType,
            string message,
            List<string> recipients
        )
        {
            try
            {
                var workflowMessage = new WorkflowMessage
                {
                    WorkflowType = "system_alert",
                    EntityType = "system",
                    Action = alertType,
                    Subject = $"System Alert: {alertType}",
                    Content = message,
                    Priority = "high",
                    Recipients = recipients,
                    IsSystemGenerated = true,
                    ProjectId = "system",
                };

                await CreateWorkflowMessageAsync(workflowMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending system alert: {ex.Message}");
                return false;
            }
        }

        // Task workflow notifications
        public async Task<bool> SendTaskAssignmentNotificationAsync(
            string taskId,
            string assignedToId,
            string assignedById
        )
        {
            try
            {
                // Get task details
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var task = tasks.FirstOrDefault(t => t.TaskId == taskId);

                if (task == null)
                {
                    Console.WriteLine($"Task {taskId} not found");
                    return false;
                }

                // Get project details
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var project = projects.FirstOrDefault(p => p.ProjectId == task.ProjectId);

                if (project == null)
                {
                    Console.WriteLine($"Project {task.ProjectId} not found");
                    return false;
                }

                // Get user details
                var assignedToUser = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    assignedToId
                );
                var assignedByUser = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    assignedById
                );

                var systemEvent = new SystemEvent
                {
                    EventType = "task_assignment",
                    EntityId = taskId,
                    EntityType = "task",
                    Action = "assigned",
                    ProjectId = task.ProjectId,
                    UserId = assignedById,
                    Data = new Dictionary<string, object>
                    {
                        { "taskId", taskId },
                        { "taskName", task.Name },
                        { "taskDescription", task.Description },
                        { "assignedToId", assignedToId },
                        { "assignedToName", assignedToUser?.FullName ?? "Contractor" },
                        { "assignedById", assignedById },
                        { "assignedByName", assignedByUser?.FullName ?? "Project Manager" },
                        { "projectId", task.ProjectId },
                        { "projectName", project.Name },
                        { "startDate", task.StartDate },
                        { "dueDate", task.DueDate },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending task assignment notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendTaskCompletionNotificationAsync(
            string taskId,
            string completedById
        )
        {
            try
            {
                // Get task details
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var task = tasks.FirstOrDefault(t => t.TaskId == taskId);

                if (task == null)
                {
                    Console.WriteLine($"Task {taskId} not found");
                    return false;
                }

                // Get project details
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var project = projects.FirstOrDefault(p => p.ProjectId == task.ProjectId);

                if (project == null)
                {
                    Console.WriteLine($"Project {task.ProjectId} not found");
                    return false;
                }

                // Get user details
                var completedByUser = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    completedById
                );

                var systemEvent = new SystemEvent
                {
                    EventType = "task_completion",
                    EntityId = taskId,
                    EntityType = "task",
                    Action = "completed",
                    ProjectId = task.ProjectId,
                    UserId = completedById,
                    Data = new Dictionary<string, object>
                    {
                        { "taskId", taskId },
                        { "taskName", task.Name },
                        { "completedById", completedById },
                        { "completedByName", completedByUser?.FullName ?? "Contractor" },
                        { "projectId", task.ProjectId },
                        { "projectName", project.Name },
                        { "projectManagerId", project.ProjectManagerId },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending task completion notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendProgressReportNotificationAsync(
            string reportId,
            string submittedById
        )
        {
            try
            {
                // Get progress report details
                var reports = await _firebaseService.GetCollectionAsync<ProgressReport>(
                    "progressReports"
                );
                var report = reports.FirstOrDefault(r => r.ProgressReportId == reportId);

                if (report == null)
                {
                    Console.WriteLine($"Progress report {reportId} not found");
                    return false;
                }

                // Get task details
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var task = tasks.FirstOrDefault(t => t.TaskId == report.TaskId);

                if (task == null)
                {
                    Console.WriteLine($"Task {report.TaskId} not found");
                    return false;
                }

                // Get project details
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var project = projects.FirstOrDefault(p => p.ProjectId == task.ProjectId);

                if (project == null)
                {
                    Console.WriteLine($"Project {task.ProjectId} not found");
                    return false;
                }

                // Get user details
                var submittedByUser = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    submittedById
                );

                var systemEvent = new SystemEvent
                {
                    EventType = "progress_report",
                    EntityId = reportId,
                    EntityType = "progress_report",
                    Action = "submitted",
                    ProjectId = task.ProjectId,
                    UserId = submittedById,
                    Data = new Dictionary<string, object>
                    {
                        { "reportId", reportId },
                        { "taskId", report.TaskId },
                        { "taskName", task.Name },
                        { "progressPercentage", "N/A" }, // ProgressReport doesn't have this property
                        { "submittedById", submittedById },
                        { "submittedByName", submittedByUser?.FullName ?? "Contractor" },
                        { "projectId", task.ProjectId },
                        { "projectName", project.Name },
                        { "projectManagerId", project.ProjectManagerId },
                        { "reportDate", report.SubmittedAt },
                        { "notes", report.Description },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending progress report notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendCompletionRequestNotificationAsync(
            string taskId,
            string requestedById
        )
        {
            try
            {
                // Get task details
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var task = tasks.FirstOrDefault(t => t.TaskId == taskId);

                if (task == null)
                {
                    Console.WriteLine($"Task {taskId} not found");
                    return false;
                }

                // Get project details
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var project = projects.FirstOrDefault(p => p.ProjectId == task.ProjectId);

                if (project == null)
                {
                    Console.WriteLine($"Project {task.ProjectId} not found");
                    return false;
                }

                // Get user details
                var requestedByUser = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    requestedById
                );

                var systemEvent = new SystemEvent
                {
                    EventType = "completion_request",
                    EntityId = taskId,
                    EntityType = "task",
                    Action = "completion_requested",
                    ProjectId = task.ProjectId,
                    UserId = requestedById,
                    Data = new Dictionary<string, object>
                    {
                        { "taskId", taskId },
                        { "taskName", task.Name },
                        { "requestedById", requestedById },
                        { "requestedByName", requestedByUser?.FullName ?? "Contractor" },
                        { "projectId", task.ProjectId },
                        { "projectName", project.Name },
                        { "projectManagerId", project.ProjectManagerId },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending completion request notification: {ex.Message}");
                return false;
            }
        }

        // Quotation workflow notifications
        public async Task<bool> SendQuotationSentNotificationAsync(
            string quotationId,
            string sentById
        )
        {
            try
            {
                // Get quotation details
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var quotation = quotations.FirstOrDefault(q => q.QuotationId == quotationId);

                if (quotation == null)
                {
                    Console.WriteLine($"Quotation {quotationId} not found");
                    return false;
                }

                // Get project details
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var project = projects.FirstOrDefault(p => p.ProjectId == quotation.ProjectId);

                if (project == null)
                {
                    Console.WriteLine($"Project {quotation.ProjectId} not found");
                    return false;
                }

                // Get user details
                var sentByUser = await _firebaseService.GetDocumentAsync<User>("users", sentById);

                var systemEvent = new SystemEvent
                {
                    EventType = "quotation_workflow",
                    EntityId = quotationId,
                    EntityType = "quotation",
                    Action = "sent",
                    ProjectId = quotation.ProjectId,
                    UserId = sentById,
                    Data = new Dictionary<string, object>
                    {
                        { "quotationId", quotationId },
                        { "quotationNumber", quotation.QuotationId },
                        { "totalAmount", quotation.GrandTotal },
                        { "clientId", quotation.ClientId },
                        { "sentById", sentById },
                        { "sentByName", sentByUser?.FullName ?? "Project Manager" },
                        { "projectId", quotation.ProjectId },
                        { "projectName", project.Name },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending quotation sent notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendQuotationApprovedNotificationAsync(
            string quotationId,
            string approvedById
        )
        {
            try
            {
                // Get quotation details
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var quotation = quotations.FirstOrDefault(q => q.QuotationId == quotationId);

                if (quotation == null)
                {
                    Console.WriteLine($"Quotation {quotationId} not found");
                    return false;
                }

                // Get project details
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var project = projects.FirstOrDefault(p => p.ProjectId == quotation.ProjectId);

                if (project == null)
                {
                    Console.WriteLine($"Project {quotation.ProjectId} not found");
                    return false;
                }

                // Get user details
                var approvedByUser = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    approvedById
                );

                var systemEvent = new SystemEvent
                {
                    EventType = "quotation_workflow",
                    EntityId = quotationId,
                    EntityType = "quotation",
                    Action = "approved",
                    ProjectId = quotation.ProjectId,
                    UserId = approvedById,
                    Data = new Dictionary<string, object>
                    {
                        { "quotationId", quotationId },
                        { "quotationNumber", quotation.QuotationId },
                        { "totalAmount", quotation.GrandTotal },
                        { "approvedById", approvedById },
                        { "approvedByName", approvedByUser?.FullName ?? "Client" },
                        { "projectId", quotation.ProjectId },
                        { "projectName", project.Name },
                        { "projectManagerId", project.ProjectManagerId },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending quotation approved notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendQuotationRejectedNotificationAsync(
            string quotationId,
            string rejectedById
        )
        {
            try
            {
                // Get quotation details
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var quotation = quotations.FirstOrDefault(q => q.QuotationId == quotationId);

                if (quotation == null)
                {
                    Console.WriteLine($"Quotation {quotationId} not found");
                    return false;
                }

                // Get project details
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var project = projects.FirstOrDefault(p => p.ProjectId == quotation.ProjectId);

                if (project == null)
                {
                    Console.WriteLine($"Project {quotation.ProjectId} not found");
                    return false;
                }

                // Get user details
                var rejectedByUser = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    rejectedById
                );

                var systemEvent = new SystemEvent
                {
                    EventType = "quotation_workflow",
                    EntityId = quotationId,
                    EntityType = "quotation",
                    Action = "rejected",
                    ProjectId = quotation.ProjectId,
                    UserId = rejectedById,
                    Data = new Dictionary<string, object>
                    {
                        { "quotationId", quotationId },
                        { "quotationNumber", quotation.QuotationId },
                        { "totalAmount", quotation.GrandTotal },
                        { "rejectedById", rejectedById },
                        { "rejectedByName", rejectedByUser?.FullName ?? "Client" },
                        { "projectId", quotation.ProjectId },
                        { "projectName", project.Name },
                        { "projectManagerId", project.ProjectManagerId },
                    },
                };

                return await ProcessSystemEventAsync(systemEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending quotation rejected notification: {ex.Message}");
                return false;
            }
        }

        public async Task<List<WorkflowMessageTemplate>> GetMessageTemplatesAsync()
        {
            return _messageTemplates;
        }

        public async Task<WorkflowMessageTemplate?> GetMessageTemplateAsync(
            string workflowType,
            string action
        )
        {
            return _messageTemplates.FirstOrDefault(t =>
                t.WorkflowType == workflowType && t.Action == action
            );
        }

        private async Task SendWorkflowMessageAsync(WorkflowMessage workflowMessage)
        {
            try
            {
                // Generate a single thread ID for all recipients of this workflow message
                var sharedThreadId = Guid.NewGuid().ToString();

                // Create regular message for each recipient
                foreach (var recipientId in workflowMessage.Recipients)
                {
                    var message = new Message
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        SenderId = "system",
                        ReceiverId = recipientId,
                        ProjectId = workflowMessage.ProjectId,
                        Subject = workflowMessage.Subject,
                        Content = workflowMessage.Content,
                        SentAt = DateTime.UtcNow,
                        MessageType = "workflow",
                        ThreadId = sharedThreadId, // Use the same thread ID for all recipients
                        IsThreadStarter = true,
                        ThreadDepth = 0,
                    };

                    await _firebaseService.AddDocumentAsync("messages", message);

                    // Send push notification
                    var user = await _firebaseService.GetDocumentAsync<User>("users", recipientId);
                    if (user != null && !string.IsNullOrEmpty(user.DeviceToken))
                    {
                        var notificationData = new Dictionary<string, string>
                        {
                            { "messageId", message.MessageId },
                            { "workflowType", workflowMessage.WorkflowType },
                            { "entityId", workflowMessage.EntityId },
                            { "entityType", workflowMessage.EntityType },
                            { "action", workflowMessage.Action },
                            { "type", "workflow_message" },
                            { "action", "workflow_notification" },
                        };

                        await _notificationService.SendToDeviceAsync(
                            user.DeviceToken,
                            workflowMessage.Subject,
                            workflowMessage.Content,
                            notificationData
                        );
                    }
                }

                // Update workflow message status
                workflowMessage.Status = "sent";
                workflowMessage.SentAt = DateTime.UtcNow;
                await _firebaseService.UpdateDocumentAsync(
                    "workflow-messages",
                    workflowMessage.WorkflowMessageId,
                    workflowMessage
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending workflow message: {ex.Message}");
                workflowMessage.Status = "failed";
                await _firebaseService.UpdateDocumentAsync(
                    "workflow-messages",
                    workflowMessage.WorkflowMessageId,
                    workflowMessage
                );
            }
        }

        private async Task<List<string>> GetRecipientsForEventAsync(SystemEvent systemEvent)
        {
            var recipients = new List<string>();

            try
            {
                // Get project users
                var users = await _firebaseService.GetCollectionAsync<User>("users");
                var projectUsers = users.Where(u => u.IsActive).ToList();

                // Add relevant users based on event type and action
                switch (systemEvent.EventType)
                {
                    case "task_assignment":
                        // Notify the assigned contractor
                        if (systemEvent.Data.ContainsKey("assignedToId"))
                        {
                            recipients.Add(systemEvent.Data["assignedToId"].ToString());
                        }
                        break;

                    case "task_completion":
                        // Notify the project manager
                        if (systemEvent.Data.ContainsKey("projectManagerId"))
                        {
                            recipients.Add(systemEvent.Data["projectManagerId"].ToString());
                        }
                        break;

                    case "progress_report":
                        // Notify the project manager
                        if (systemEvent.Data.ContainsKey("projectManagerId"))
                        {
                            recipients.Add(systemEvent.Data["projectManagerId"].ToString());
                        }
                        break;

                    case "completion_request":
                        // Notify the project manager
                        if (systemEvent.Data.ContainsKey("projectManagerId"))
                        {
                            recipients.Add(systemEvent.Data["projectManagerId"].ToString());
                        }
                        break;

                    case "quotation_workflow":
                        switch (systemEvent.Action)
                        {
                            case "sent":
                                // Notify the client
                                if (systemEvent.Data.ContainsKey("clientId"))
                                {
                                    recipients.Add(systemEvent.Data["clientId"].ToString());
                                }
                                break;
                            case "approved":
                            case "rejected":
                                // Notify the project manager
                                if (systemEvent.Data.ContainsKey("projectManagerId"))
                                {
                                    recipients.Add(systemEvent.Data["projectManagerId"].ToString());
                                }
                                break;
                        }
                        break;

                    case "quotation":
                        var quotations = await _firebaseService.GetCollectionAsync<Quotation>(
                            "quotations"
                        );
                        var quote = quotations.FirstOrDefault(q =>
                            q.QuotationId == systemEvent.EntityId
                        );
                        if (quote != null)
                        {
                            recipients.Add(quote.ClientId);
                            recipients.Add(quote.ContractorId);
                            if (!string.IsNullOrEmpty(quote.AdminApproverUserId))
                                recipients.Add(quote.AdminApproverUserId);
                        }
                        break;

                    case "invoice":
                        var invoices = await _firebaseService.GetCollectionAsync<Invoice>(
                            "invoices"
                        );
                        var invoice = invoices.FirstOrDefault(i =>
                            i.InvoiceId == systemEvent.EntityId
                        );
                        if (invoice != null)
                        {
                            recipients.Add(invoice.ClientId);
                            recipients.Add(invoice.ContractorId);
                        }
                        break;

                    case "project_update":
                        // Add all project participants
                        recipients.AddRange(projectUsers.Select(u => u.UserId));
                        break;

                    case "system_alert":
                        // Use the recipients from the system event
                        if (systemEvent.Data.ContainsKey("recipients"))
                        {
                            var recipientList = systemEvent.Data["recipients"] as List<string>;
                            if (recipientList != null)
                            {
                                recipients.AddRange(recipientList);
                            }
                        }
                        break;

                    default:
                        // Add all active users for system events
                        recipients.AddRange(projectUsers.Select(u => u.UserId));
                        break;
                }

                // Remove duplicates and ensure users exist
                recipients = recipients
                    .Distinct()
                    .Where(r => projectUsers.Any(u => u.UserId == r))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recipients: {ex.Message}");
            }

            return recipients;
        }

        private string ProcessTemplate(string template, Dictionary<string, object> data)
        {
            var result = template;
            foreach (var kvp in data)
            {
                result = result.Replace($"{{{kvp.Key}}}", kvp.Value?.ToString() ?? "");
            }
            return result;
        }

        private List<WorkflowMessageTemplate> InitializeMessageTemplates()
        {
            return new List<WorkflowMessageTemplate>
            {
                // Task workflow templates
                new WorkflowMessageTemplate
                {
                    WorkflowType = "task_assignment",
                    Action = "assigned",
                    SubjectTemplate = "🎯 New Task Assignment: {taskName}",
                    ContentTemplate =
                        "You have been assigned a new task in project **{projectName}**.\n\n"
                        + "**Task:** {taskName}\n"
                        + "**Description:** {taskDescription}\n"
                        + "**Start Date:** {startDate:MMM dd, yyyy}\n"
                        + "**Due Date:** {dueDate:MMM dd, yyyy}\n\n"
                        + "Please review the task details and begin work as soon as possible.",
                    Priority = "high",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "task_completion",
                    Action = "completed",
                    SubjectTemplate = "✅ Task Completed: {taskName} in Project {projectName}",
                    ContentTemplate =
                        "Contractor **{completedByName}** has marked task **{taskName}** as completed in project **{projectName}**.\n\n"
                        + "Please review the task and confirm its completion.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "progress_report",
                    Action = "submitted",
                    SubjectTemplate = "📊 Progress Report: {taskName} in Project {projectName}",
                    ContentTemplate =
                        "Contractor **{submittedByName}** has submitted a progress report for task **{taskName}** in project **{projectName}**.\n\n"
                        + "**Current progress:** {progressPercentage}%\n"
                        + "**Report Date:** {reportDate:MMM dd, yyyy}\n"
                        + "**Notes:** {notes}\n\n"
                        + "Please review the report for details.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "completion_request",
                    Action = "completion_requested",
                    SubjectTemplate = "🔍 Completion Request: {taskName} in Project {projectName}",
                    ContentTemplate =
                        "Contractor **{requestedByName}** has requested completion for task **{taskName}** in project **{projectName}**.\n\n"
                        + "Please review the task and approve its completion.",
                    Priority = "normal",
                },
                // Quotation workflow templates
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation_workflow",
                    Action = "sent",
                    SubjectTemplate = "📋 New Quotation for Project: {projectName}",
                    ContentTemplate =
                        "A new quotation has been sent to you for project **{projectName}**.\n\n"
                        + "**Quotation ID:** {quotationId}\n"
                        + "**Total Amount:** {totalAmount:C}\n\n"
                        + "Please review the quotation in your client dashboard and approve or reject it at your earliest convenience.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation_workflow",
                    Action = "approved",
                    SubjectTemplate = "✅ Quotation Approved: {projectName}",
                    ContentTemplate =
                        "Client **{approvedByName}** has approved quotation **{quotationId}** for project **{projectName}**.\n\n"
                        + "**Total Amount:** {totalAmount:C}\n\n"
                        + "You can now proceed with project planning and task assignments.",
                    Priority = "high",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation_workflow",
                    Action = "rejected",
                    SubjectTemplate = "❌ Quotation Rejected: {projectName}",
                    ContentTemplate =
                        "Client **{rejectedByName}** has rejected quotation **{quotationId}** for project **{projectName}**.\n\n"
                        + "Please review the client's feedback and take appropriate action.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation",
                    Action = "created",
                    SubjectTemplate = "New Quotation Created - {quotationId}",
                    ContentTemplate =
                        "A new quotation has been created for project {projectId}. Amount: {totalAmount:C}. Please review and approve.",
                    Priority = "normal",
                },
                // Invoice workflow templates
                new WorkflowMessageTemplate
                {
                    WorkflowType = "invoice",
                    Action = "created",
                    SubjectTemplate = "New Invoice Generated - {invoiceNumber}",
                    ContentTemplate =
                        "A new invoice has been generated. Invoice Number: {invoiceNumber}, Amount: {invoiceAmount:C}. Please process payment.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "invoice",
                    Action = "paid",
                    SubjectTemplate = "Invoice Paid - {invoiceNumber}",
                    ContentTemplate =
                        "Invoice {invoiceNumber} has been paid successfully. Amount: {invoiceAmount:C}. Thank you for your payment.",
                    Priority = "high",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "invoice",
                    Action = "overdue",
                    SubjectTemplate = "Invoice Overdue - {invoiceNumber}",
                    ContentTemplate =
                        "Invoice {invoiceNumber} is now overdue. Amount: {invoiceAmount:C}. Please process payment as soon as possible.",
                    Priority = "urgent",
                },
                // Project workflow templates
                new WorkflowMessageTemplate
                {
                    WorkflowType = "project",
                    Action = "status_changed",
                    SubjectTemplate = "Project Status Update - {projectName}",
                    ContentTemplate =
                        "Project {projectName} status has been updated. Please check the project dashboard for details.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "project",
                    Action = "milestone_reached",
                    SubjectTemplate = "Project Milestone Reached - {projectName}",
                    ContentTemplate =
                        "A milestone has been reached for project {projectName}. Great progress!",
                    Priority = "normal",
                },
                // System templates
                new WorkflowMessageTemplate
                {
                    WorkflowType = "system",
                    Action = "maintenance",
                    SubjectTemplate = "System Maintenance Scheduled",
                    ContentTemplate =
                        "System maintenance has been scheduled. The system may be temporarily unavailable during this time.",
                    Priority = "high",
                },
            };
        }
    }
}
