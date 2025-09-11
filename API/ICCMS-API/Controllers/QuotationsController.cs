using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ICCMS_API.Models;
using ICCMS_API.Services;
using ICCMS_API.Helpers;
using ICCMS_API.Auth;

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
                if (string.IsNullOrEmpty(myId) || !string.Equals(quotation.ClientId, myId, StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

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
}
