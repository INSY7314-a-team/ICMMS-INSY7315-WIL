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

                // Send the workflow message
                await SendWorkflowMessageAsync(workflowMessage);

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
                var template = GetMessageTemplateAsync(
                    systemEvent.EntityType,
                    systemEvent.Action
                ).Result;
                if (template == null)
                {
                    Console.WriteLine(
                        $"No template found for {systemEvent.EntityType} - {systemEvent.Action}"
                    );
                    return false;
                }

                var workflowMessage = new WorkflowMessage
                {
                    WorkflowType = $"{systemEvent.EntityType}_{systemEvent.Action}",
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
                var quote = await _firebaseService
                    .GetCollectionAsync<Quotation>("quotations")
                    .ContinueWith(t => t.Result.FirstOrDefault(q => q.QuotationId == quoteId));

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
                var invoice = await _firebaseService
                    .GetCollectionAsync<Invoice>("invoices")
                    .ContinueWith(t => t.Result.FirstOrDefault(i => i.InvoiceId == invoiceId));

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
                var project = await _firebaseService
                    .GetCollectionAsync<Project>("projects")
                    .ContinueWith(t => t.Result.FirstOrDefault(p => p.ProjectId == projectId));

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
                        ThreadId = Guid.NewGuid().ToString(),
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

                // Add relevant users based on event type
                switch (systemEvent.EntityType)
                {
                    case "quotation":
                        var quote = await _firebaseService
                            .GetCollectionAsync<Quotation>("quotations")
                            .ContinueWith(t =>
                                t.Result.FirstOrDefault(q => q.QuotationId == systemEvent.EntityId)
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
                        var invoice = await _firebaseService
                            .GetCollectionAsync<Invoice>("invoices")
                            .ContinueWith(t =>
                                t.Result.FirstOrDefault(i => i.InvoiceId == systemEvent.EntityId)
                            );
                        if (invoice != null)
                        {
                            recipients.Add(invoice.ClientId);
                            recipients.Add(invoice.ContractorId);
                        }
                        break;

                    case "project":
                        // Add all project participants
                        recipients.AddRange(projectUsers.Select(u => u.UserId));
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
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation",
                    Action = "created",
                    SubjectTemplate = "New Quotation Created - {quoteId}",
                    ContentTemplate =
                        "A new quotation has been created for project {projectId}. Amount: {quoteTotal:C}. Please review and approve.",
                    Priority = "normal",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation",
                    Action = "approved",
                    SubjectTemplate = "Quotation Approved - {quoteId}",
                    ContentTemplate =
                        "Quotation {quoteId} has been approved. Amount: {quoteTotal:C}. You can proceed with the work.",
                    Priority = "high",
                },
                new WorkflowMessageTemplate
                {
                    WorkflowType = "quotation",
                    Action = "rejected",
                    SubjectTemplate = "Quotation Rejected - {quoteId}",
                    ContentTemplate =
                        "Quotation {quoteId} has been rejected. Please review and resubmit with necessary changes.",
                    Priority = "normal",
                },
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
