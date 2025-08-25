using Microsoft.AspNetCore.Mvc;
using ICCMS_API.Models;
using ICCMS_API.Services;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public InvoicesController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
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
        public async Task<ActionResult<List<Invoice>>> GetInvoicesByProject(string projectId)
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
        public async Task<ActionResult<List<Invoice>>> GetInvoicesByClient(string clientId)
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

        [HttpPost]
        public async Task<ActionResult<string>> CreateInvoice([FromBody] Invoice invoice)
        {
            try
            {
                invoice.IssuedDate = DateTime.UtcNow;
                var invoiceId = await _firebaseService.AddDocumentAsync("invoices", invoice);
                return Ok(invoiceId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(string id, [FromBody] Invoice invoice)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("invoices", id, invoice);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
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
    }
}
