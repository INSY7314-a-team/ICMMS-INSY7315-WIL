using System.Security.Claims;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Client")] // Only clients can access this controller
    public class ClientsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public ClientsController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
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
                var quotation = await _firebaseService.GetDocumentAsync<Quotation>(
                    "quotations",
                    id
                );
                return Ok(quotation);
            }
            catch (Exception ex)
            {
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
                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("maintenanceReports")]
        public async Task<ActionResult> GetMaintenanceReports()
        {
            try
            {
                var maintenanceReports =
                    await _firebaseService.GetCollectionAsync<MaintenanceRequest>(
                        "maintenanceRequests"
                    );
                return Ok(maintenanceReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("maintenanceReport/{id}")]
        public async Task<ActionResult> GetMaintenanceReport(string id)
        {
            try
            {
                var maintenanceReport = await _firebaseService.GetDocumentAsync<MaintenanceRequest>(
                    "maintenanceRequests",
                    id
                );
                return Ok(maintenanceReport);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/maintenanceReport")]
        public async Task<ActionResult> CreateMaintenanceRequest(
            [FromBody] MaintenanceRequest maintenanceRequest
        )
        {
            try
            {
                var maintenanceReport = await _firebaseService.AddDocumentAsync<MaintenanceRequest>(
                    "maintenanceRequests",
                    maintenanceRequest
                );
                return Ok(maintenanceReport);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("maintenanceReport/{id}")]
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
                await _firebaseService.UpdateDocumentAsync(
                    "maintenanceRequests",
                    id,
                    maintenanceRequest
                );
                return Ok(new { message = "Maintenance request updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update/quotation/{id}")]
        public async Task<ActionResult> UpdateQuotation(string id, [FromBody] Quotation quotation)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("quotations", id, quotation);
                return Ok(new { message = "Quotation updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete/maintenanceReport/{id}")]
        public async Task<ActionResult> DeleteMaintenanceRequest(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { error = "Maintenance request ID is required" });
            }

            try
            {
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
