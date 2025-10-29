using System.Linq;
using System.Security.Claims;
using ICCMS_API.Auth;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Client,Tester")] // Only clients and testers can access this controller
    public class ClientsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly ISupabaseService _supabaseService;
        private readonly IQuoteWorkflowService _quoteWorkflow;
        private readonly IAuditLogService _auditLogService;
        private readonly IWorkflowMessageService _workflowMessageService;

        public ClientsController(
            IFirebaseService firebaseService,
            ISupabaseService supabaseService,
            IQuoteWorkflowService quoteWorkflow,
            IAuditLogService auditLogService,
            IWorkflowMessageService workflowMessageService
        )
        {
            _firebaseService = firebaseService;
            _supabaseService = supabaseService;
            _quoteWorkflow = quoteWorkflow;
            _auditLogService = auditLogService;
            _workflowMessageService = workflowMessageService;
        }

        [HttpGet("projects")]
        public async Task<ActionResult<List<Project>>> GetProjects()
        {
            try
            {
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var clientProjects = projects
                    .Where(p => p.ClientId == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(clientProjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{clientId}/projects")]
        public async Task<IActionResult> GetClientProjects(string clientId)
        {
            try
            {
                var currentUserId = User.UserId();
                if (currentUserId != clientId)
                {
                    return Forbid("You are not authorized to view these projects.");
                }

                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var clientProjects = projects.Where(p => p.ClientId == clientId).ToList();
                return Ok(clientProjects);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while fetching client projects.");
            }
        }

        [HttpGet("messaging/available-users")]
        public async Task<ActionResult<List<object>>> GetAvailableUsersForMessaging()
        {
            try
            {
                var clientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(clientId))
                {
                    return Unauthorized("Client ID not found");
                }

                // Get all projects for this client
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var clientProjects = projects.Where(p => p.ClientId == clientId).ToList();

                if (!clientProjects.Any())
                {
                    return Ok(new List<object>());
                }

                // Get unique Project Manager IDs from client's projects
                var pmIds = clientProjects
                    .Where(p => !string.IsNullOrEmpty(p.ProjectManagerId))
                    .Select(p => p.ProjectManagerId)
                    .Distinct()
                    .ToList();

                // Get all users to find Project Managers, contractors, and other clients
                var users = await _firebaseService.GetCollectionAsync<User>("users");

                // Get Project Managers and Admins
                var projectManagers = users
                    .Where(u =>
                        pmIds.Contains(u.UserId)
                        && (u.Role == "Project Manager" || u.Role == "Admin")
                    )
                    .ToList();

                // Get contractors working on client's projects
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var projectIds = clientProjects.Select(p => p.ProjectId).ToList();
                var contractorTasks = tasks.Where(t => projectIds.Contains(t.ProjectId)).ToList();
                var contractorIds = contractorTasks.Select(t => t.AssignedTo).Distinct().ToList();

                var contractors = users
                    .Where(u => contractorIds.Contains(u.UserId) && u.Role == "Contractor")
                    .ToList();

                // Get other clients (if any) - this might be rare but could happen
                var otherClientIds = clientProjects
                    .Where(p => !string.IsNullOrEmpty(p.ClientId) && p.ClientId != clientId)
                    .Select(p => p.ClientId)
                    .Distinct()
                    .ToList();

                var otherClients = users
                    .Where(u => otherClientIds.Contains(u.UserId) && u.Role == "Client")
                    .ToList();

                // Combine all available users
                var allAvailableUsers = projectManagers
                    .Concat(contractors)
                    .Concat(otherClients)
                    .Select(u => new
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Role = u.Role,
                        Email = u.Email,
                    })
                    .ToList();

                return Ok(allAvailableUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("messaging/available-projects")]
        public async Task<ActionResult<List<object>>> GetAvailableProjectsForMessaging()
        {
            try
            {
                var clientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(clientId))
                {
                    return Unauthorized("Client ID not found");
                }

                // Get all projects for this client
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var clientProjects = projects
                    .Where(p => p.ClientId == clientId)
                    .Select(p => new
                    {
                        ProjectId = p.ProjectId,
                        Name = p.Name,
                        Description = p.Description,
                        Status = p.Status,
                    })
                    .ToList();

                return Ok(clientProjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}")]
        public async Task<ActionResult<Project>> GetProject(string id)
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (project == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
                if (project.ClientId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return BadRequest(
                        new { error = "You are not authorized to view this project" }
                    );
                }
                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/phases")]
        public async Task<ActionResult<List<Phase>>> GetProjectPhases(string id)
        {
            try
            {
                var phases = await _firebaseService.GetCollectionAsync<Phase>("phases");
                var projectPhases = phases.Where(p => p.ProjectId == id).ToList();
                return Ok(projectPhases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/tasks")]
        public async Task<ActionResult<List<ProjectTask>>> GetProjectTasks(string id)
        {
            try
            {
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var projectTasks = tasks.Where(t => t.ProjectId == id).ToList();
                return Ok(projectTasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/quotations")]
        public async Task<ActionResult<List<Quotation>>> GetProjectQuotations(string id)
        {
            try
            {
                Console.WriteLine($"Getting quotations for project {id}");
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var projectQuotations = quotations.Where(q => q.ProjectId == id).ToList();
                Console.WriteLine($"Found {projectQuotations.Count} quotations for project {id}");
                foreach (var q in projectQuotations)
                {
                    Console.WriteLine($"  - Quotation {q.QuotationId} (Client: {q.ClientId})");
                }
                return Ok(projectQuotations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting quotations for project {id}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/invoices")]
        public async Task<ActionResult<List<Invoice>>> GetProjectInvoices(string id)
        {
            try
            {
                var invoices = await _firebaseService.GetCollectionAsync<Invoice>("invoices");
                var projectInvoices = invoices.Where(i => i.ProjectId == id).ToList();
                return Ok(projectInvoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/maintenance-requests")]
        public async Task<ActionResult<List<MaintenanceRequest>>> GetProjectMaintenanceRequests(
            string id
        )
        {
            try
            {
                var requests = await _firebaseService.GetCollectionAsync<MaintenanceRequest>(
                    "maintenanceRequests"
                );
                var projectRequests = requests.Where(r => r.ProjectId == id).ToList();
                return Ok(projectRequests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/progress-reports")]
        public async Task<ActionResult<List<ProgressReport>>> GetProjectProgressReports(string id)
        {
            try
            {
                Console.WriteLine($"Getting progress reports for project {id}");
                // Verify the client has access to this project
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (project == null)
                {
                    Console.WriteLine($"Project {id} not found");
                    return NotFound(new { error = "Project not found" });
                }

                var clientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (project.ClientId != clientId)
                {
                    Console.WriteLine(
                        $"Client {clientId} is not authorized to view progress reports for project {id}"
                    );
                    return BadRequest(
                        new
                        {
                            error = "You are not authorized to view progress reports for this project",
                        }
                    );
                }

                // Get progress reports for this project
                var reports = await _firebaseService.GetCollectionAsync<ProgressReport>(
                    "progressReports"
                );
                Console.WriteLine($"Found {reports.Count} total progress reports");
                var projectReports = reports
                    .Where(r => r.ProjectId == id)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToList();
                Console.WriteLine(
                    $"Found {projectReports.Count} progress reports for project {id}"
                );
                return Ok(projectReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("quotations")]
        public async Task<ActionResult<List<Quotation>>> GetQuotations()
        {
            try
            {
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var clientQuotations = quotations
                    .Where(q => q.ClientId == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(clientQuotations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("quotation/{id}")]
        public async Task<ActionResult<Quotation>> GetQuotation(string id)
        {
            try
            {
                Console.WriteLine($"Getting quotation {id}");
                var clientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(clientId))
                {
                    Console.WriteLine("Client ID is null or empty");
                    return BadRequest(new { error = "Client ID is required" });
                }
                Console.WriteLine($"Client ID: {clientId}");

                var quotation = await _firebaseService.GetDocumentAsync<Quotation>(
                    "quotations",
                    id
                );

                if (quotation == null)
                {
                    Console.WriteLine($"Quotation {id} not found in Firebase");
                    return NotFound(new { error = "Quotation not found" });
                }

                Console.WriteLine(
                    $"Quotation found. ClientId: {quotation.ClientId}, Requesting client: {clientId}"
                );
                if (quotation.ClientId != clientId)
                {
                    Console.WriteLine("Client not authorized to view this quotation");
                    return BadRequest(
                        new { error = "You are not authorized to view this quotation" }
                    );
                }

                // Get line items from the associated estimate
                var estimate = await _firebaseService.GetDocumentAsync<Estimate>(
                    "estimates",
                    quotation.EstimateId
                );

                if (estimate == null)
                {
                    Console.WriteLine(
                        $"Estimate {quotation.EstimateId} not found for quotation {id}"
                    );
                    return Ok(new { quotation = quotation, items = new List<object>() });
                }

                Console.WriteLine(
                    $"Found estimate {quotation.EstimateId} with {estimate.LineItems?.Count ?? 0} line items"
                );

                // Convert EstimateLineItem to a format the frontend expects
                var lineItems = new List<object>();
                if (estimate.LineItems != null)
                {
                    lineItems = estimate
                        .LineItems.Select(item => new
                        {
                            name = item.Name,
                            description = item.Description,
                            quantity = item.Quantity,
                            unit = item.Unit,
                            unitPrice = item.UnitPrice,
                            lineTotal = item.LineTotal,
                            category = item.Category,
                        })
                        .Cast<object>()
                        .ToList();
                }

                return Ok(new { quotation = quotation, items = lineItems });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting quotation {id}: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("invoices")]
        public async Task<ActionResult<List<Invoice>>> GetInvoices()
        {
            try
            {
                var invoices = await _firebaseService.GetCollectionAsync<Invoice>("invoices");
                var clientInvoices = invoices
                    .Where(i => i.ClientId == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(clientInvoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("invoice/{id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(string id)
        {
            try
            {
                var invoice = await _firebaseService.GetDocumentAsync<Invoice>("invoices", id);
                if (invoice == null)
                {
                    return NotFound(new { error = "Invoice not found" });
                }
                if (invoice.ClientId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return BadRequest(
                        new { error = "You are not authorized to view this invoice" }
                    );
                }
                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("maintenanceRequests")]
        public async Task<ActionResult> GetMaintenanceRequests()
        {
            try
            {
                var maintenanceRequests =
                    await _firebaseService.GetCollectionAsync<MaintenanceRequest>(
                        "maintenanceRequests"
                    );
                var clientMaintenanceRequests = maintenanceRequests
                    .Where(m => m.ClientId == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(clientMaintenanceRequests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("maintenanceRequest/{id}")]
        public async Task<ActionResult> GetMaintenanceRequest(string id)
        {
            var currentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"🔍 Current ClientId = {currentId}");

            try
            {
                var maintenanceRequest =
                    await _firebaseService.GetDocumentAsync<MaintenanceRequest>(
                        "maintenanceRequests",
                        id
                    );
                if (maintenanceRequest == null)
                {
                    return NotFound(new { error = "Maintenance request not found" });
                }
                if (maintenanceRequest.ClientId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return BadRequest(
                        new { error = "You are not authorized to view this maintenance request" }
                    );
                }
                return Ok(maintenanceRequest);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/maintenanceRequest")]
        public async Task<ActionResult> CreateMaintenanceRequest(
            [FromBody] MaintenanceRequest maintenanceRequest
        )
        {
            try
            {
                maintenanceRequest.ClientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                maintenanceRequest.Status = "Pending";
                maintenanceRequest.CreatedAt = DateTime.UtcNow;

                if (string.IsNullOrWhiteSpace(maintenanceRequest.MaintenanceRequestId))
                    maintenanceRequest.MaintenanceRequestId = Guid.NewGuid().ToString("N");

                // ✅ Save maintenance request
                await _firebaseService.AddDocumentWithIdAsync(
                    "maintenanceRequests",
                    maintenanceRequest.MaintenanceRequestId,
                    maintenanceRequest
                );

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Maintenance Update",
                    "Maintenance Request Created",
                    $"Maintenance request {maintenanceRequest.MaintenanceRequestId} created for project {maintenanceRequest.ProjectId}",
                    userId ?? "system",
                    maintenanceRequest.MaintenanceRequestId
                );

                Console.WriteLine(
                    $"✅ Created maintenance request with Firestore ID = {maintenanceRequest.MaintenanceRequestId}"
                );

                // ✅ Update linked project status to Maintenance
                if (!string.IsNullOrWhiteSpace(maintenanceRequest.ProjectId))
                {
                    var project = await _firebaseService.GetDocumentAsync<Project>(
                        "projects",
                        maintenanceRequest.ProjectId
                    );
                    if (project != null)
                    {
                        project.Status = "Maintenance";
                        await _firebaseService.UpdateDocumentAsync(
                            "projects",
                            maintenanceRequest.ProjectId,
                            project
                        );
                        Console.WriteLine(
                            $"🔧 Updated project {maintenanceRequest.ProjectId} status to 'Maintenance'"
                        );

                        // Notify Project Manager about maintenance request
                        if (!string.IsNullOrEmpty(project.ProjectManagerId))
                        {
                            var systemEvent = new SystemEvent
                            {
                                EventType = "project_update",
                                EntityId = maintenanceRequest.ProjectId,
                                EntityType = "project",
                                Action = "maintenance_request_created",
                                ProjectId = maintenanceRequest.ProjectId,
                                UserId = project.ProjectManagerId,
                                Data = new Dictionary<string, object>
                                {
                                    { "projectId", maintenanceRequest.ProjectId },
                                    {
                                        "updateType",
                                        $"New maintenance request created: {maintenanceRequest.Description}"
                                    },
                                    { "userId", project.ProjectManagerId },
                                },
                            };
                            await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                        }
                    }
                    else
                    {
                        Console.WriteLine(
                            $"⚠️ Project {maintenanceRequest.ProjectId} not found — skipping status update"
                        );
                    }
                }

                return Ok(new { maintenanceRequestId = maintenanceRequest.MaintenanceRequestId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error creating maintenance request: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("pay/invoice/{id}")]
        public async Task<ActionResult> PayInvoice(string id, [FromBody] Payment payment)
        {
            try
            {
                //check if invoice is already paid
                var invoice = await _firebaseService.GetDocumentAsync<Invoice>("invoices", id);
                if (invoice == null)
                {
                    return NotFound(new { error = "Invoice not found" });
                }
                if (invoice.ClientId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return BadRequest(new { error = "You are not authorized to pay this invoice" });
                }
                if (invoice.Status == "Paid")
                {
                    return BadRequest(new { error = "Invoice is already paid" });
                }

                // Create payment
                var newPayment = new Payment
                {
                    PaymentId = Guid.NewGuid().ToString(),
                    InvoiceId = id,
                    Amount = invoice.TotalAmount,
                    PaymentDate = DateTime.UtcNow,
                    Method = payment.Method,
                    Status = "Paid",
                    TransactionId = payment.TransactionId,
                    Notes = payment.Notes,
                    ProcessedAt = DateTime.UtcNow,
                    ProjectId = invoice.ProjectId,
                    ClientId = invoice.ClientId,
                };
                // Save payment
                await _firebaseService.AddDocumentAsync("payments", newPayment);

                // Update invoice
                invoice.Status = "Paid";
                invoice.PaidDate = DateTime.UtcNow;
                invoice.PaidBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _firebaseService.UpdateDocumentAsync("invoices", id, invoice);

                // Notify Project Manager about payment
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    invoice.ProjectId
                );
                if (project != null && !string.IsNullOrEmpty(project.ProjectManagerId))
                {
                    var systemEvent = new SystemEvent
                    {
                        EventType = "invoice_workflow",
                        EntityId = id,
                        EntityType = "invoice",
                        Action = "Paid",
                        ProjectId = invoice.ProjectId,
                        UserId = project.ProjectManagerId,
                        Data = new Dictionary<string, object>
                        {
                            { "invoiceId", id },
                            { "invoiceNumber", invoice.InvoiceNumber },
                            { "invoiceAmount", invoice.TotalAmount },
                            { "invoiceDescription", invoice.Description },
                            { "clientId", invoice.ClientId },
                            { "contractorId", invoice.ContractorId },
                            { "action", "Paid" },
                            { "userId", project.ProjectManagerId },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                }

                return Ok(newPayment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update/maintenanceRequest/{id}")]
        public async Task<ActionResult> UpdateMaintenanceRequest(
            string id,
            [FromBody] MaintenanceRequest maintenanceRequest
        )
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Maintenance request ID is required" });
            }

            try
            {
                var existingMaintenanceRequest =
                    await _firebaseService.GetDocumentAsync<MaintenanceRequest>(
                        "maintenanceRequests",
                        id
                    );
                if (existingMaintenanceRequest == null)
                {
                    return NotFound(new { error = "Maintenance request not found" });
                }
                if (
                    existingMaintenanceRequest.ClientId
                    != User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                )
                {
                    return BadRequest(
                        new { error = "You are not authorized to update this maintenance request" }
                    );
                }
                var oldStatus = existingMaintenanceRequest.Status;
                existingMaintenanceRequest = maintenanceRequest;
                await _firebaseService.UpdateDocumentAsync(
                    "maintenanceRequests",
                    id,
                    existingMaintenanceRequest
                );

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Maintenance Update",
                    "Maintenance Request Updated",
                    $"Maintenance request {id} status changed from {oldStatus} to {maintenanceRequest.Status}",
                    userId ?? "system",
                    id
                );

                return Ok(new { message = "Maintenance request updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("approve/quotation/{id}")]
        public async Task<ActionResult> ApproveQuotation(string id)
        {
            try
            {
                var quotation = await _firebaseService.GetDocumentAsync<Quotation>(
                    "quotations",
                    id
                );
                if (quotation == null)
                {
                    return NotFound(new { error = "Quotation not found" });
                }
                if (quotation.ClientId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return BadRequest(
                        new { error = "You are not authorized to approve this quotation" }
                    );
                }

                // Use the quote workflow to register the client's acceptance (this sets status to ClientAccepted)
                var accepted = await _quoteWorkflow.ClientDecisionAsync(id, true, null);
                if (accepted == null)
                {
                    return StatusCode(500, new { error = "Failed to apply client decision" });
                }

                // Notify Project Manager
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    quotation.ProjectId
                );
                if (project != null && !string.IsNullOrEmpty(project.ProjectManagerId))
                {
                    var systemEvent = new SystemEvent
                    {
                        EventType = "quotation_workflow",
                        EntityId = id,
                        EntityType = "quotation",
                        Action = "approved",
                        ProjectId = quotation.ProjectId,
                        UserId = project.ProjectManagerId,
                        Data = new Dictionary<string, object>
                        {
                            { "quotationId", id },
                            { "projectName", project.Name },
                            { "approvedByName", User.Identity.Name ?? "Client" },
                            { "totalAmount", quotation.GrandTotal },
                            { "projectManagerId", project.ProjectManagerId },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                }

                // Attempt to convert to invoice. This is idempotent in the workflow service.
                try
                {
                    var conversion = await _quoteWorkflow.ConvertToInvoiceAsync(id);
                    if (conversion != null)
                    {
                        // Return both the updated quotation and created invoice id
                        return Ok(
                            new { quotation = accepted, invoiceId = conversion.Value.invoiceId }
                        );
                    }
                }
                catch (InvalidOperationException iex)
                {
                    // If conversion isn't allowed, still return the accepted quotation
                    return Ok(new { quotation = accepted, conversionError = iex.Message });
                }

                return Ok(accepted);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class RejectQuotationRequest
        {
            public string? Reason { get; set; }
        }

        [HttpPut("reject/quotation/{id}")]
        public async Task<ActionResult> RejectQuotation(
            string id,
            [FromBody] RejectQuotationRequest request
        )
        {
            try
            {
                var quotation = await _firebaseService.GetDocumentAsync<Quotation>(
                    "quotations",
                    id
                );
                if (quotation == null)
                {
                    return NotFound(new { error = "Quotation not found" });
                }
                if (quotation.ClientId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return BadRequest(
                        new { error = "You are not authorized to reject this quotation" }
                    );
                }

                var rejected = await _quoteWorkflow.ClientDecisionAsync(id, false, request.Reason);
                if (rejected == null)
                {
                    return StatusCode(500, new { error = "Failed to apply client decision" });
                }

                // Notify Project Manager
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    quotation.ProjectId
                );
                var client = await _firebaseService.GetDocumentAsync<User>(
                    "users",
                    quotation.ClientId
                );

                if (project != null && !string.IsNullOrEmpty(project.ProjectManagerId))
                {
                    var systemEvent = new SystemEvent
                    {
                        EventType = "quotation_workflow",
                        EntityId = id,
                        EntityType = "quotation",
                        Action = "client_rejected",
                        ProjectId = quotation.ProjectId,
                        UserId = project.ProjectManagerId, // Notify the PM
                        Data = new Dictionary<string, object>
                        {
                            { "quotationId", id },
                            { "rejectedByName", client?.FullName ?? "Client" },
                            { "rejectionReason", request.Reason ?? "No reason provided." },
                            { "projectName", project.Name },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                }

                return Ok(rejected);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete/maintenanceRequest/{id}")]
        public async Task<ActionResult> DeleteMaintenanceRequest(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Maintenance request ID is required" });
            }

            try
            {
                var maintenanceRequest =
                    await _firebaseService.GetDocumentAsync<MaintenanceRequest>(
                        "maintenanceRequests",
                        id
                    );
                if (maintenanceRequest == null)
                {
                    return NotFound(new { error = "Maintenance request not found" });
                }
                if (maintenanceRequest.ClientId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return BadRequest(
                        new { error = "You are not authorized to delete this maintenance request" }
                    );
                }
                await _firebaseService.DeleteDocumentAsync("maintenanceRequests", id);
                return Ok(new { message = "Maintenance request deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
