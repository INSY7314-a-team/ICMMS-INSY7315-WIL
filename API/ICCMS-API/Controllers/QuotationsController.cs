using Microsoft.AspNetCore.Mvc;
using ICCMS_API.Models;
using ICCMS_API.Services;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotationsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public QuotationsController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
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
        public async Task<ActionResult<List<Quotation>>> GetQuotationsByProject(string projectId)
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
        public async Task<ActionResult<List<Quotation>>> GetQuotationsByClient(string clientId)
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

        [HttpPost]
        public async Task<ActionResult<string>> CreateQuotation([FromBody] Quotation quotation)
        {
            try
            {
                quotation.CreatedAt = DateTime.UtcNow;
                var quotationId = await _firebaseService.AddDocumentAsync("quotations", quotation);
                return Ok(quotationId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuotation(string id, [FromBody] Quotation quotation)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("quotations", id, quotation);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
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
    }
}
