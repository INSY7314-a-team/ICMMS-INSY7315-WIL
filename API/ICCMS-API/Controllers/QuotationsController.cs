using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ICCMS_API.Models;
using ICCMS_API.Services;
using ICCMS_API.Helpers;
using ICCMS_API.Auth;
using System.Security.Claims;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // all actions require auth; per-action roles below
    public class QuotationsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly IQuoteWorkflowService _quoteWorkflow;

        public QuotationsController(IFirebaseService firebaseService, IQuoteWorkflowService quoteWorkflow)
        {
            _firebaseService = firebaseService;
            _quoteWorkflow = quoteWorkflow;
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
                var quotation = await _firebaseService.GetDocumentAsync<Quotation>("quotations", id);
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
        public async Task<ActionResult<List<Quotation>>> GetQuotationsByMaintenanceRequest(string maintenanceRequestId)
        {
            try
            {
                var quotations = await _firebaseService.GetCollectionAsync<Quotation>("quotations");
                var maintenanceQuotations = quotations.Where(q => q.MaintenanceRequestId == maintenanceRequestId).ToList();
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
                // IMPORTANT: quotation.ClientId must be set to the client's UserId
                quotation.Status ??= "Draft";
                
                // Set timestamps and defaults
                quotation.CreatedAt = DateTime.UtcNow;
                quotation.UpdatedAt = DateTime.UtcNow;
                
                // Validate ValidUntil
                if (quotation.ValidUntil <= quotation.CreatedAt)
                {
                    return BadRequest("ValidUntil must be after CreatedAt");
                }

                // Recalculate pricing
                Pricing.Recalculate(quotation);

                var quotationId = await _firebaseService.AddDocumentAsync("quotations", quotation);
                return Ok(quotationId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("from-estimate/{estimateId}")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<ActionResult<string>> CreateQuotationFromEstimate(string estimateId, [FromBody] CreateQuotationFromEstimateRequest request)
        {
            try
            {
                var estimate = await _firebaseService.GetDocumentAsync<Estimate>("estimates", estimateId);
                if (estimate == null)
                    return NotFound("Estimate not found");

                // Convert EstimateLineItems to QuotationItems
                var quotationItems = estimate.LineItems.Select(item => new QuotationItem
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = 0.15, // Default 15% VAT
                    LineTotal = item.LineTotal
                }).ToList();

                // For testing purposes, use current user as client if in testing mode
                var currentUserId = User.UserId();
                var clientId = !string.IsNullOrEmpty(currentUserId) && currentUserId.Contains("tester") 
                    ? currentUserId 
                    : request.ClientId;

                var quotation = new Quotation
                {
                    ProjectId = estimate.ProjectId,
                    ClientId = clientId,
                    ContractorId = estimate.ContractorId,
                    Description = estimate.Description,
                    Items = quotationItems,
                    Status = "PendingPMApproval", // Set to correct status for PM approval workflow
                    ValidUntil = estimate.ValidUntil,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Currency = estimate.Currency,
                    IsAiGenerated = estimate.IsAiGenerated
                };

                // Calculate quotation totals
                Pricing.Recalculate(quotation);

                var quotationId = await _firebaseService.AddDocumentAsync("quotations", quotation);
                return Ok(quotationId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
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
                if (quotation == null) return NotFound();
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
                if (quotation == null) return NotFound();
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
                if (quotation == null) return NotFound();
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
                if (quotation == null) return NotFound();
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
        public async Task<IActionResult> ClientDecision(string id, [FromBody] ClientDecisionBody body)
        {
            try
            {
                // First check ownership
                var quotation = await _firebaseService.GetDocumentAsync<Quotation>("quotations", id);
                if (quotation == null) return NotFound();

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
                var isOwner = !string.IsNullOrEmpty(myId) && string.Equals(quotation.ClientId, myId, StringComparison.OrdinalIgnoreCase);
                
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
                if (result == null) return NotFound();
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
                if (result == null) return NotFound();
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
            return StatusCode(410, "This endpoint has been deprecated. Use /submit-for-approval instead.");
        }

        [HttpPost("{id}/admin-approve")]
        public IActionResult AdminApproveDeprecated(string id)
        {
            return StatusCode(410, "This endpoint has been deprecated. Use /pm-approve instead.");
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
