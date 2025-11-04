using System.Text.RegularExpressions;
using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public class WorkflowMessageService : IWorkflowMessageService
    {
        private readonly IFirebaseService _firebaseService;
        private readonly INotificationService _notificationService;
        private readonly List<WorkflowMessageTemplate> _messageTemplates;
        private readonly object _lockObject = new object();
        private static readonly Dictionary<string, object> _locks =
            new Dictionary<string, object>();
        private static readonly Dictionary<string, int> _requestCounts =
            new Dictionary<string, int>();

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
                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] ENTRY - EntityType: {workflowMessage.EntityType}, EntityId: {workflowMessage.EntityId}, Action: {workflowMessage.Action} ========"
                );
                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] ENTRY - WorkflowMessageId: {workflowMessage.WorkflowMessageId} ========"
                );

                // Generate unique ID if not already set
                if (string.IsNullOrEmpty(workflowMessage.WorkflowMessageId))
                {
                    workflowMessage.WorkflowMessageId = Guid.NewGuid().ToString();
                    workflowMessage.CreatedAt = DateTime.UtcNow;
                    workflowMessage.Status = "pending";

                    Console.WriteLine(
                        $"======== [CreateWorkflowMessageAsync] Generated WorkflowMessageId: {workflowMessage.WorkflowMessageId} ========"
                    );
                }

                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] Starting workflow message creation for {workflowMessage.WorkflowType}/{workflowMessage.Action} ========"
                );
                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] Stack trace: {Environment.StackTrace} ========"
                );
                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] WorkflowType: {workflowMessage.WorkflowType}, Action: {workflowMessage.Action} ========"
                );
                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] Subject: '{workflowMessage.Subject}' ========"
                );
                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] Recipients: {workflowMessage.Recipients.Count} users ========"
                );

                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] About to save workflow message with ID: {workflowMessage.WorkflowMessageId} ========"
                );

                await _firebaseService.AddDocumentWithIdAsync(
                    "workflow-messages",
                    workflowMessage.WorkflowMessageId,
                    workflowMessage
                );

                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] Saved workflow message to Firebase with ID: {workflowMessage.WorkflowMessageId} ========"
                );
                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] WorkflowMessageId in object: {workflowMessage.WorkflowMessageId} ========"
                );

                // Set status to sent since we're not calling SendWorkflowMessageAsync anymore
                workflowMessage.Status = "sent";
                workflowMessage.SentAt = DateTime.UtcNow;

                await _firebaseService.UpdateDocumentAsync(
                    "workflow-messages",
                    workflowMessage.WorkflowMessageId,
                    workflowMessage
                );

                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] Updated workflow message status to 'sent' ========"
                );

                return workflowMessage.WorkflowMessageId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"======== [CreateWorkflowMessageAsync] Error: {ex.Message} ========"
                );
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

        public async Task<bool> CreateWorkflowMessageAsync(SystemEvent systemEvent)
        {
            // Use a unique key for this specific system event to prevent race conditions
            var lockKey = $"{systemEvent.EntityType}_{systemEvent.EntityId}_{systemEvent.Action}";
            var requestId = Guid.NewGuid().ToString("N")[..8]; // Short unique ID for this request

            // Use a static dictionary to store locks for different keys
            if (!_locks.ContainsKey(lockKey))
            {
                lock (_lockObject)
                {
                    if (!_locks.ContainsKey(lockKey))
                    {
                        _locks[lockKey] = new object();
                    }
                }
            }

            lock (_locks[lockKey])
            {
                try
                {
                    // Track how many times this specific SystemEvent has been processed
                    if (!_requestCounts.ContainsKey(lockKey))
                    {
                        _requestCounts[lockKey] = 0;
                    }
                    _requestCounts[lockKey]++;

                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Processing event: EventType={systemEvent.EventType}, Action={systemEvent.Action}, EntityId={systemEvent.EntityId} ========"
                    );
                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Lock acquired for key: {lockKey} ========"
                    );
                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - This is request #{_requestCounts[lockKey]} for this SystemEvent ========"
                    );
                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Stack trace: {Environment.StackTrace} ========"
                    );

                    // Check for recent duplicate messages at the SystemEvent level
                    var existingMessages = _firebaseService
                        .GetCollectionAsync<WorkflowMessage>("workflow-messages")
                        .Result; // Use .Result to make it synchronous within the lock

                    if (existingMessages == null)
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - WARNING: existingMessages is null, continuing without duplicate check ========"
                        );
                        existingMessages = new List<WorkflowMessage>();
                    }

                    var recentDuplicate = existingMessages.FirstOrDefault(m =>
                        m.EntityType == systemEvent.EntityType
                        && m.EntityId == systemEvent.EntityId
                        && m.Action == systemEvent.Action
                        && m.CreatedAt > DateTime.UtcNow.AddMinutes(-5)
                    );

                    if (recentDuplicate != null)
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - DUPLICATE DETECTED ========"
                        );
                        Console.WriteLine(
                            $"REQUEST {requestId} - EntityType: {systemEvent.EntityType}, EntityId: {systemEvent.EntityId}, Action: {systemEvent.Action}"
                        );
                        Console.WriteLine(
                            $"REQUEST {requestId} - Existing message ID: {recentDuplicate.WorkflowMessageId}, Created: {recentDuplicate.CreatedAt}"
                        );
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - SKIPPING CREATION ========"
                        );
                        return true; // Return true because the message already exists
                    }

                    var template = GetMessageTemplateAsync(
                        systemEvent.EventType,
                        systemEvent.Action
                    ).Result;
                    if (template == null)
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - No template found for {systemEvent.EventType} - {systemEvent.Action} ========"
                        );
                        return false;
                    }

                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Found template: {template.WorkflowType}/{template.Action} ========"
                    );

                    var recipients = GetRecipientsForEventAsync(systemEvent).Result;
                    if (recipients == null)
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - WARNING: recipients is null, using empty list ========"
                        );
                        recipients = new List<string>();
                    }
                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Resolved {recipients.Count} recipients: {string.Join(", ", recipients)} ========"
                    );

                    // Validate SystemEvent properties
                    if (string.IsNullOrEmpty(systemEvent.EventType))
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - ERROR: systemEvent.EventType is null or empty ========"
                        );
                        return false;
                    }
                    if (string.IsNullOrEmpty(systemEvent.EntityId))
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - ERROR: systemEvent.EntityId is null or empty ========"
                        );
                        return false;
                    }
                    if (string.IsNullOrEmpty(systemEvent.EntityType))
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - ERROR: systemEvent.EntityType is null or empty ========"
                        );
                        return false;
                    }
                    if (string.IsNullOrEmpty(systemEvent.Action))
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - ERROR: systemEvent.Action is null or empty ========"
                        );
                        return false;
                    }

                    var workflowMessage = new WorkflowMessage
                    {
                        WorkflowType = systemEvent.EventType,
                        EntityId = systemEvent.EntityId,
                        EntityType = systemEvent.EntityType,
                        Action = systemEvent.Action,
                        ProjectId = systemEvent.ProjectId ?? string.Empty,
                        Subject = ProcessTemplate(
                            template.SubjectTemplate,
                            systemEvent.Data ?? new Dictionary<string, object>()
                        ),
                        Content = ProcessTemplate(
                            template.ContentTemplate,
                            systemEvent.Data ?? new Dictionary<string, object>()
                        ),
                        Priority = template.Priority,
                        Recipients = recipients,
                        IsSystemGenerated = true,
                        Metadata = systemEvent.Data ?? new Dictionary<string, object>(),
                    };

                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Created workflow message: Subject='{workflowMessage.Subject}', Recipients={workflowMessage.Recipients.Count} ========"
                    );

                    try
                    {
                        CreateWorkflowMessageAsync(workflowMessage).Wait();
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Workflow message created successfully ========"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Error creating workflow message: {ex.Message} ========"
                        );
                        throw;
                    }

                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Successfully processed system event ========"
                    );
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"======== [CreateAndSendWorkflowMessageAsync] REQUEST {requestId} - Error: {ex.Message} ========"
                    );
                    return false;
                }
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

        public async Task<bool> MarkWorkflowMessageAsReadAsync(
            string workflowMessageId,
            string userId
        )
        {
            try
            {
                Console.WriteLine(
                    $"======== [MarkWorkflowMessageAsReadAsync] Marking workflow message {workflowMessageId} as read by user {userId} ========"
                );

                // Get the workflow message
                var workflowMessage = await GetWorkflowMessageAsync(workflowMessageId);

                if (workflowMessage == null)
                {
                    Console.WriteLine(
                        $"======== [MarkWorkflowMessageAsReadAsync] Workflow message {workflowMessageId} not found ========"
                    );
                    return false;
                }

                // Check if user is a recipient
                if (!workflowMessage.Recipients.Contains(userId))
                {
                    Console.WriteLine(
                        $"======== [MarkWorkflowMessageAsReadAsync] User {userId} is not a recipient of workflow message {workflowMessageId} ========"
                    );
                    return false;
                }

                // Check if already read
                if (workflowMessage.IsRead)
                {
                    Console.WriteLine(
                        $"======== [MarkWorkflowMessageAsReadAsync] Workflow message {workflowMessageId} is already marked as read ========"
                    );
                    return true;
                }

                // Update the read status
                workflowMessage.IsRead = true;
                workflowMessage.ReadAt = DateTime.UtcNow;
                workflowMessage.ReadBy = userId;

                // Save to database using WorkflowMessageId as document ID
                await _firebaseService.UpdateDocumentAsync(
                    "workflow-messages",
                    workflowMessageId,
                    workflowMessage
                );

                Console.WriteLine(
                    $"======== [MarkWorkflowMessageAsReadAsync] Successfully marked workflow message {workflowMessageId} as read ========"
                );

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"======== [MarkWorkflowMessageAsReadAsync] Error: {ex.Message} ========"
                );
                return false;
            }
        }

        private async Task<List<string>> GetRecipientsForEventAsync(SystemEvent systemEvent)
        {
            var recipients = new List<string>();

            try
            {
                Console.WriteLine(
                    $"======== [GetRecipientsForEventAsync] Processing EventType={systemEvent.EventType}, Action={systemEvent.Action} ========"
                );

                // Get project users
                var users = await _firebaseService.GetCollectionAsync<User>("users");
                var projectUsers = users.Where(u => u.IsActive).ToList();
                Console.WriteLine(
                    $"======== [GetRecipientsForEventAsync] Found {projectUsers.Count} active users ========"
                );

                // Add relevant users based on event type and action
                switch (systemEvent.EventType)
                {
                    case "task_assignment":
                        // Notify the assigned contractor
                        if (systemEvent.Data.ContainsKey("assignedToId"))
                        {
                            recipients.Add(systemEvent.Data["assignedToId"].ToString());
                            Console.WriteLine(
                                $"======== [GetRecipientsForEventAsync] Added assigned contractor: {systemEvent.Data["assignedToId"]} ========"
                            );
                        }
                        break;

                    case "task_completion":
                        // Notify the project manager
                        if (systemEvent.Data.ContainsKey("projectManagerId"))
                        {
                            recipients.Add(systemEvent.Data["projectManagerId"].ToString());
                            Console.WriteLine(
                                $"======== [GetRecipientsForEventAsync] Added project manager: {systemEvent.Data["projectManagerId"]} ========"
                            );
                        }
                        break;

                    case "progress_report":
                        // Notify the project manager
                        if (systemEvent.Data.ContainsKey("projectManagerId"))
                        {
                            recipients.Add(systemEvent.Data["projectManagerId"].ToString());
                            Console.WriteLine(
                                $"======== [GetRecipientsForEventAsync] Added project manager: {systemEvent.Data["projectManagerId"]} ========"
                            );
                        }
                        break;

                    case "completion_request":
                        // Notify the project manager
                        if (systemEvent.Data.ContainsKey("projectManagerId"))
                        {
                            recipients.Add(systemEvent.Data["projectManagerId"].ToString());
                            Console.WriteLine(
                                $"======== [GetRecipientsForEventAsync] Added project manager: {systemEvent.Data["projectManagerId"]} ========"
                            );
                        }
                        break;

                    case "quotation_workflow":
                        Console.WriteLine(
                            $"======== [GetRecipientsForEventAsync] Processing quotation_workflow with action: {systemEvent.Action} ========"
                        );
                        switch (systemEvent.Action)
                        {
                            case "sent":
                                // Notify the client
                                if (systemEvent.Data.ContainsKey("clientId"))
                                {
                                    recipients.Add(systemEvent.Data["clientId"].ToString());
                                    Console.WriteLine(
                                        $"======== [GetRecipientsForEventAsync] Added client: {systemEvent.Data["clientId"]} ========"
                                    );
                                }
                                break;
                            case "approved":
                            case "rejected":
                                // Notify the project manager
                                if (systemEvent.Data.ContainsKey("projectManagerId"))
                                {
                                    recipients.Add(systemEvent.Data["projectManagerId"].ToString());
                                    Console.WriteLine(
                                        $"======== [GetRecipientsForEventAsync] Added project manager: {systemEvent.Data["projectManagerId"]} ========"
                                    );
                                }
                                else
                                {
                                    Console.WriteLine(
                                        $"======== [GetRecipientsForEventAsync] No projectManagerId found in data for {systemEvent.Action} ========"
                                    );
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
                            Console.WriteLine(
                                $"======== [GetRecipientsForEventAsync] Added quotation recipients: Client={quote.ClientId}, Contractor={quote.ContractorId} ========"
                            );
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
                            Console.WriteLine(
                                $"======== [GetRecipientsForEventAsync] Added invoice recipients: Client={invoice.ClientId}, Contractor={invoice.ContractorId} ========"
                            );
                        }
                        break;

                    case "project_update":
                        // Add all project participants
                        recipients.AddRange(projectUsers.Select(u => u.UserId));
                        Console.WriteLine(
                            $"======== [GetRecipientsForEventAsync] Added all project users: {recipients.Count} recipients ========"
                        );
                        break;

                    case "system_alert":
                        // Use the recipients from the system event
                        if (systemEvent.Data.ContainsKey("recipients"))
                        {
                            var recipientList = systemEvent.Data["recipients"] as List<string>;
                            if (recipientList != null)
                            {
                                recipients.AddRange(recipientList);
                                Console.WriteLine(
                                    $"======== [GetRecipientsForEventAsync] Added system alert recipients: {recipientList.Count} recipients ========"
                                );
                            }
                        }
                        break;

                    default:
                        // Add all active users for system events
                        recipients.AddRange(projectUsers.Select(u => u.UserId));
                        Console.WriteLine(
                            $"======== [GetRecipientsForEventAsync] Added all active users for default case: {recipients.Count} recipients ========"
                        );
                        break;
                }

                // Remove duplicates and ensure users exist
                var beforeFilter = recipients.Count;
                recipients = recipients
                    .Distinct()
                    .Where(r => projectUsers.Any(u => u.UserId == r))
                    .ToList();

                Console.WriteLine(
                    $"======== [GetRecipientsForEventAsync] Filtered recipients: {beforeFilter} -> {recipients.Count} (removed non-existent users) ========"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"======== [GetRecipientsForEventAsync] Error: {ex.Message} ========"
                );
            }

            return recipients;
        }

        private string ProcessTemplate(string template, Dictionary<string, object> data)
        {
            return Regex.Replace(
                template,
                @"\{(.+?)\}",
                match =>
                {
                    string keyWithFormat = match.Groups[1].Value;
                    string[] parts = keyWithFormat.Split(new[] { ':' }, 2);
                    string key = parts[0];
                    string format = parts.Length > 1 ? parts[1] : null;

                    if (data.TryGetValue(key, out object value))
                    {
                        if (value == null)
                            return "";

                        if (!string.IsNullOrEmpty(format))
                        {
                            if (value is IFormattable formattableValue)
                            {
                                return formattableValue.ToString(
                                    format,
                                    new System.Globalization.CultureInfo("en-ZA")
                                );
                            }
                        }
                        return value.ToString();
                    }

                    return match.Value;
                }
            );
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
                        + "Task: {taskName}\n"
                        + "Description: {taskDescription}\n"
                        + "Start Date: {startDate:MMM dd, yyyy}\n"
                        + "Due Date: {dueDate:MMM dd, yyyy}\n\n"
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
                        "Contractor {submittedByName} has submitted a progress report for task {taskName} in project {projectName}.\n\n"
                        + "Current progress: {progressPercentage}%\n"
                        + "Report Date: {reportDate:MMM dd, yyyy}\n"
                        + "Notes: {notes}\n\n"
                        + "Please review the report for details.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "completion_request",
                    Action = "completion_requested",
                    SubjectTemplate = "🔍 Completion Request: {taskName} in Project {projectName}",
                    ContentTemplate =
                        "Contractor {requestedByName} has requested completion for task {taskName} in project {projectName}.\n\n"
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
                        "A new quotation has been sent to you for project {projectName}.\n\n"
                        + "Quotation ID: {quotationId}\n"
                        + "Total Amount: {totalAmount:C}\n\n"
                        + "Please review the quotation in your client dashboard and approve or reject it at your earliest convenience.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation_workflow",
                    Action = "approved",
                    SubjectTemplate = "✅ Quotation Approved: {projectName}",
                    ContentTemplate =
                        "🎉 Great news! Your quotation has been approved by the client.\n\n"
                        + "Project: {projectName}\n"
                        + "Quotation ID: {quotationId}\n"
                        + "Approved by: {approvedByName}\n"
                        + "Total Amount: {totalAmount:C}\n\n",
                    Priority = "high",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation_workflow",
                    Action = "rejected",
                    SubjectTemplate = "❌ Quotation Rejected: {projectName}",
                    ContentTemplate =
                        "⚠️ Quotation Update: Your quotation has been rejected by the client.\n\n"
                        + "Project: {projectName}\n"
                        + "Quotation ID: {quotationId}\n"
                        + "Rejected by: {rejectedByName}\n"
                        + "Total Amount: {totalAmount:C}\n\n"
                        + "Recommended Actions:\n"
                        + "• Review client feedback\n"
                        + "• Revise quotation details\n"
                        + "• Contact client for clarification\n"
                        + "• Resubmit updated quotation\n\n"
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
                // Maintenance request workflow templates
                new WorkflowMessageTemplate
                {
                    WorkflowType = "maintenance_request",
                    Action = "approved",
                    SubjectTemplate = "✅ Maintenance Request Approved - {projectName}",
                    ContentTemplate =
                        "Your maintenance request for project {projectName} has been approved.\n\n"
                        + "The project manager will now proceed with the maintenance work. You will be notified when the work is completed.\n\n"
                        + "Request ID: {requestId}\n"
                        + "Project: {projectName}",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "maintenance_request",
                    Action = "rejected",
                    SubjectTemplate = "❌ Maintenance Request Rejected - {projectName}",
                    ContentTemplate =
                        "We regret to inform you that your maintenance request for project {projectName} has been rejected.\n\n"
                        + "Request ID: {requestId}\n"
                        + "Project: {projectName}\n\n"
                        + "If you have any questions or concerns, please contact your project manager.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "maintenance_request",
                    Action = "completed",
                    SubjectTemplate = "🎉 Maintenance Work Completed - {projectName}",
                    ContentTemplate =
                        "Great news! All maintenance work for project {projectName} has been completed.\n\n"
                        + "All phases and tasks have been finished. The project status has been updated to Completed.\n\n"
                        + "Project: {projectName}\n"
                        + "Thank you for your patience.",
                    Priority = "high",
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
