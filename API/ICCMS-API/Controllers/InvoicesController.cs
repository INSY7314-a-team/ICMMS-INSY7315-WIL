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
    [Authorize]
    public class InvoicesController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly IInvoiceWorkflowService _invoiceWorkflow;

        public InvoicesController(
            IFirebaseService firebaseService,
            IInvoiceWorkflowService invoiceWorkflow
        )
        {
            _firebaseService = firebaseService;
            _invoiceWorkflow = invoiceWorkflow;
        }

        [HttpGet]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<List<Invoice>>> GetInvoices()
        {
            try
            {
                var invoices = await _firebaseService.GetCollectionAsync<Invoice>("invoices");
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<Invoice>> GetInvoice(string id)
        {
            try
            {
                var invoice = await _firebaseService.GetDocumentAsync<Invoice>("invoices", id);
                if (invoice == null)
                    return NotFound();
                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("project/{projectId}")]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<List<Invoice>>> GetByProject(string projectId)
        {
            try
            {
                var invoices = await _firebaseService.GetCollectionAsync<Invoice>("invoices");
                var projectInvoices = invoices.Where(i => i.ProjectId == projectId).ToList();
                return Ok(projectInvoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Project Manager,Admin,Tester")]
        public async Task<ActionResult<List<Invoice>>> GetByClient(string clientId)
        {
            try
            {
                var invoices = await _firebaseService.GetCollectionAsync<Invoice>("invoices");
                var clientInvoices = invoices.Where(i => i.ClientId == clientId).ToList();
                return Ok(clientInvoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("me")]
        [Authorize(Roles = "Client,Tester")]
        public async Task<ActionResult<List<Invoice>>> GetMyInvoices()
        {
            try
            {
                var myId = User.UserId();
                if (string.IsNullOrEmpty(myId))
                    return Forbid();

                var all = await _firebaseService.GetCollectionAsync<Invoice>("invoices");
                return Ok(all.Where(i => i.ClientId == myId).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<ActionResult<string>> CreateInvoice([FromBody] Invoice invoice)
        {
            try
            {
                // ensure invoice.ClientId is set to the client's UserId (copied from the quotation)
                // Set timestamps and defaults
                invoice.IssuedDate = DateTime.UtcNow;
                invoice.UpdatedAt = DateTime.UtcNow;
                invoice.Status = "Draft";

                // Recalculate pricing
                Pricing.Recalculate(invoice);

                var invoiceId = await _firebaseService.AddDocumentAsync("invoices", invoice);
                return Ok(invoiceId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> UpdateInvoice(string id, [FromBody] Invoice invoice)
        {
            try
            {
                // Pricing.Recalculate() if applicable
                Pricing.Recalculate(invoice);

                await _firebaseService.UpdateDocumentAsync("invoices", id, invoice);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> DeleteInvoice(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("invoices", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/issue")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> Issue(string id)
        {
            try
            {
                var invoice = await _invoiceWorkflow.IssueAsync(id);
                if (invoice == null)
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

        [HttpPost("{id}/mark-paid")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> MarkPaid(string id, [FromBody] MarkPaidBody body)
        {
            try
            {
                var invoice = await _invoiceWorkflow.MarkPaidAsync(id, body.PaidDate, body.PaidBy);
                if (invoice == null)
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

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> Cancel(string id)
        {
            try
            {
                var invoice = await _invoiceWorkflow.CancelAsync(id);
                if (invoice == null)
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
    }

    public class MarkPaidBody
    {
        public DateTime PaidDate { get; set; }
        public string PaidBy { get; set; } = string.Empty;
    }
}
