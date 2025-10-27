using System.Security.Claims;
using ICCMS_API.Auth;
using ICCMS_API.Helpers;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // all actions require auth; per-action roles below
    public class QuotationsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly IQuoteWorkflowService _quoteWorkflow;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IAuditLogService _auditLogService;

        public QuotationsController(
            IFirebaseService firebaseService,
            IQuoteWorkflowService quoteWorkflow,
            IWorkflowMessageService workflowMessageService,
            IAuditLogService auditLogService
        )
        {
            _firebaseService = firebaseService;
            _quoteWorkflow = quoteWorkflow;
            _workflowMessageService = workflowMessageService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<List<Quotation>>> GetQuotations()
        {
            try
            {
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                return Ok(quotations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<Quotation>> GetQuotation(string id)
        {
            try
            {
                var quotation = await _firebaseService.GetDocumentAsync<Quotation>(
                    "quotations",
                    id
                );
                if (quotation == null)
                    return NotFound();
                return Ok(quotation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("project/{projectId}")]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<List<Quotation>>> GetByProject(string projectId)
        {
            try
            {
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var projectQuotations = quotations.Where(q => q.ProjectId == projectId).ToList();
                return Ok(projectQuotations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("maintenance/{maintenanceRequestId}")]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<List<Quotation>>> GetQuotationsByMaintenanceRequest(
            string maintenanceRequestId
        )
        {
            try
            {
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var maintenanceQuotations = quotations
                    .Where(q => q.MaintenanceRequestId == maintenanceRequestId)
                    .ToList();
                return Ok(maintenanceQuotations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<List<Quotation>>> GetByClient(string clientId)
        {
            try
            {
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var clientQuotations = quotations.Where(q => q.ClientId == clientId).ToList();
                return Ok(clientQuotations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("me")]
        [Authorize(Roles = "Client,Tester")]
        public async Task<ActionResult<List<Quotation>>> GetMyQuotations()
        {
            try
            {
                var myId = User.UserId();
                if (string.IsNullOrEmpty(myId))
                    return Forbid();

                var all = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                return Ok(all.Where(q => q.ClientId == myId).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<ActionResult<string>> CreateQuotation([FromBody] Quotation quotation)
        {
            try
            {
                // ✅ Ensure required fields and defaults
                quotation.Status ??= "Draft";

                // Set timestamps and defaults
                quotation.CreatedAt = DateTime.UtcNow;
                quotation.UpdatedAt = DateTime.UtcNow;

                // Validate ValidUntil
                if (quotation.ValidUntil <= quotation.CreatedAt)
                    return BadRequest("ValidUntil must be after CreatedAt");

                // ✅ Recalculate totals
                Pricing.Recalculate(quotation);

                // ✅ Step 1: Add the quotation to Firestore (generates a doc ID)
                var quotationId = await _firebaseService.AddDocumentAsync("quotations", quotation);

                // ✅ Step 2: Save the generated Firestore ID back into the document
                quotation.QuotationId = quotationId;

                // ✅ Step 3: Update Firestore with this new field
                await _firebaseService.UpdateDocumentAsync("quotations", quotationId, quotation);

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Quotation",
                    "Quotation Created",
                    $"Quotation {quotationId} created for client {quotation.ClientId}",
                    userId ?? "system",
                    quotationId
                );

                // ✅ Step 4: Return the quotation ID to the caller
                return Ok(quotationId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("from-estimate/{estimateId}")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<ActionResult<Quotation>> CreateQuotationFromEstimate(
            string estimateId,
            [FromBody] CreateQuotationFromEstimateRequest request
        )
        {
            try
            {
                // ===== 1️⃣ Fetch the Estimate =====
                var estimate = await _firebaseService.GetDocumentAsync<Estimate>(
                    "estimates",
                    estimateId
                );
                if (estimate == null)
                    return NotFound(new { error = "Estimate not found" });

                // ===== 2️⃣ Fetch the Project to access ClientId =====
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    estimate.ProjectId
                );
                if (project == null)
                    return NotFound(new { error = "Project not found for estimate" });

                // ===== 3️⃣ Convert Estimate → Quotation Items =====
                var quotationItems = estimate
                    .LineItems.Select(item => new QuotationItem
                    {
                        Name = item.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TaxRate = 0.15,
                        LineTotal = item.LineTotal,
                    })
                    .ToList();

                // ===== 4️⃣ Create the Quotation =====
                var quotation = new Quotation
                {
                    QuotationId = Guid.NewGuid().ToString(),
                    EstimateId = estimateId,
                    ProjectId = estimate.ProjectId,
                    ClientId = project.ClientId, // ✅ from Project
                    ContractorId = estimate.ContractorId,
                    Description = estimate.Description,
                    Items = quotationItems,
                    Status = "PendingPMApproval",
                    ValidUntil = estimate.ValidUntil,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Currency = estimate.Currency ?? "ZAR",
                    IsAiGenerated = estimate.IsAiGenerated,
                };

                // ===== 5️⃣ Calculate totals =====
                Pricing.Recalculate(quotation);

                // ===== 6️⃣ Save to Firestore =====
                var quotationId = await _firebaseService.AddDocumentAsync("quotations", quotation);

                // update record to include the Firestore-generated quotationId
                quotation.QuotationId = quotationId;
                await _firebaseService.UpdateDocumentAsync("quotations", quotationId, quotation);

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Quotation",
                    "Quotation Created From Estimate",
                    $"Quotation {quotationId} created from estimate {estimateId} for client {project.ClientId}",
                    userId ?? "system",
                    quotationId
                );

                // ===== 7️⃣ Return JSON (expected by Web side) =====
                return Ok(new { quotationId, quotation });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"💥 Exception while creating quotation from estimate {estimateId}: {ex.Message}"
                );
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> UpdateQuotation(string id, [FromBody] Quotation quotation)
        {
            try
            {
                // Recalculate pricing
                Pricing.Recalculate(quotation);

                await _firebaseService.UpdateDocumentAsync("quotations", id, quotation);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> DeleteQuotation(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("quotations", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Draft -> PendingPMApproval
        [HttpPost("{id}/submit-for-approval")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> SubmitForApproval(string id)
        {
            try
            {
                var quotation = await _quoteWorkflow.SubmitForApprovalAsync(id);
                if (quotation == null)
                    return NotFound();
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // PendingPMApproval -> SentToClient (also set ApprovedAt/SentAt)
        [HttpPost("{id}/pm-approve")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> PmApprove(string id)
        {
            try
            {
                var quotation = await _quoteWorkflow.PmApproveAsync(id);
                if (quotation == null)
                    return NotFound();

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Quotation",
                    "Quotation Approved",
                    $"Quote {id} approved by PM",
                    userId ?? "system",
                    id
                );

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // PendingPMApproval -> PMRejected
        [HttpPost("{id}/pm-reject")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> PmReject(string id, [FromBody] PmRejectRequest request)
        {
            try
            {
                var quotation = await _quoteWorkflow.PmRejectAsync(id, request.Reason);
                if (quotation == null)
                    return NotFound();

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Quotation",
                    "Quotation Rejected",
                    $"Quote {id} rejected by PM: {request.Reason}",
                    userId ?? "system",
                    id
                );

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Optional: ensure SentToClient (idempotent)
        [HttpPost("{id}/send-to-client")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> SendToClient(string id)
        {
            try
            {
                var quotation = await _quoteWorkflow.SendToClientAsync(id);
                if (quotation == null)
                    return NotFound();

                // Send workflow notification to client
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    await _workflowMessageService.SendQuotationSentNotificationAsync(
                        quotation.QuotationId,
                        currentUserId
                    );
                }

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Client decision — Client only + ownership (ClientId == UserId)
        [HttpPost("{id}/client-decision")]
        [Authorize(Roles = "Client,Tester")]
        public async Task<IActionResult> ClientDecision(
            string id,
            [FromBody] ClientDecisionBody body
        )
        {
            try
            {
                // First check ownership
                var quotation = await _firebaseService.GetDocumentAsync<Quotation>(
                    "quotations",
                    id
                );
                if (quotation == null)
                    return NotFound();

                var myId = User.UserId();

                // Debug all claims
                Console.WriteLine($"Client Decision - All Claims:");
                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"  {claim.Type}: {claim.Value}");
                }

                Console.WriteLine($"Client Decision - Current User ID: {myId}");
                Console.WriteLine($"Client Decision - Quotation Client ID: {quotation.ClientId}");

                // Bypass ownership check for tester users during testing
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                Console.WriteLine($"Client Decision - User Email: {userEmail}");
                var isTester = !string.IsNullOrEmpty(userEmail) && userEmail.Contains("tester");
                var isOwner =
                    !string.IsNullOrEmpty(myId)
                    && string.Equals(quotation.ClientId, myId, StringComparison.OrdinalIgnoreCase);

                Console.WriteLine($"Client Decision - Is Tester: {isTester}");
                Console.WriteLine($"Client Decision - Is Owner: {isOwner}");

                if (!isTester && !isOwner)
                {
                    Console.WriteLine("Client Decision - Access denied: Not tester and not owner");
                    return Forbid();
                }

                Console.WriteLine("Client Decision - Access granted");

                // Use workflow service for the decision
                var result = await _quoteWorkflow.ClientDecisionAsync(id, body.Accept, body.Note);
                if (result == null)
                    return NotFound();

                // Send workflow notification based on client decision
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    if (body.Accept)
                    {
                        await _workflowMessageService.SendQuotationApprovedNotificationAsync(
                            id,
                            currentUserId
                        );
                    }
                    else
                    {
                        await _workflowMessageService.SendQuotationRejectedNotificationAsync(
                            id,
                            currentUserId
                        );
                    }
                }

                // Notify the Project Manager about the client's decision
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    quotation.ProjectId
                );
                if (project != null && !string.IsNullOrEmpty(project.ProjectManagerId))
                {
                    var action = body.Accept ? "Accepted" : "Rejected";
                    await _workflowMessageService.SendQuoteApprovalNotificationAsync(
                        id,
                        action,
                        project.ProjectManagerId
                    );
                }

                var userId = User.UserId();
                var decision = body.Accept ? "approved" : "rejected";
                _auditLogService.LogAsync(
                    "Quotation",
                    $"Quotation {char.ToUpper(decision[0]) + decision.Substring(1)}",
                    $"Quote {id} {decision} by client: {body.Note}",
                    userId ?? "system",
                    id
                );

                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/convert-to-invoice")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<ActionResult<string>> ConvertToInvoice(string id)
        {
            try
            {
                var result = await _quoteWorkflow.ConvertToInvoiceAsync(id);
                if (result == null)
                    return NotFound();

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Quotation",
                    "Quotation Converted to Invoice",
                    $"Quote {id} converted to invoice {result.Value.invoiceId}",
                    userId ?? "system",
                    id
                );

                return Ok(result.Value.invoiceId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Remove/retire any old submit-for-admin / admin-approve routes or return 410 Gone
        [HttpPost("{id}/submit-for-admin")]
        public IActionResult SubmitForAdminDeprecated(string id)
        {
            return StatusCode(
                410,
                "This endpoint has been deprecated. Use /submit-for-approval instead."
            );
        }

        [HttpPost("{id}/admin-approve")]
        public IActionResult AdminApproveDeprecated(string id)
        {
            return StatusCode(410, "This endpoint has been deprecated. Use /pm-approve instead.");
        }

        [HttpGet("debug-claims")]
        [AllowAnonymous]
        public IActionResult DebugClaims()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }
    }

    public class ClientDecisionBody
    {
        public bool Accept { get; set; }
        public string? Note { get; set; }
    }

    public class CreateQuotationFromEstimateRequest
    {
        public string ClientId { get; set; } = string.Empty;
    }

    public class PmRejectRequest
    {
        public string? Reason { get; set; }
    }
}
