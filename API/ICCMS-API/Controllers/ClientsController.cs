using System.Security.Claims;
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
                var clientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(clientId))
                {
                    return BadRequest(new { error = "Client ID is required" });
                }
                var quotation = await _firebaseService.GetDocumentAsync<Quotation>(
                    "quotations",
                    id
                );

                if (quotation == null)
                {
                    return NotFound(new { error = "Quotation not found" });
                }

                if (quotation.ClientId != clientId)
                {
                    return BadRequest(
                        new { error = "You are not authorized to view this quotation" }
                    );
                }

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
                var MR = new MaintenanceRequest
                {
                    ClientId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ProjectId = maintenanceRequest.ProjectId,
                    Description = maintenanceRequest.Description,
                    Priority = maintenanceRequest.Priority,
                    Status = "Pending",
                    MediaUrl = maintenanceRequest.MediaUrl,
                    RequestedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    AssignedTo = "",
                    CreatedAt = DateTime.UtcNow,
                };
                var maintenanceRequestId = await _firebaseService.AddDocumentAsync(
                    "maintenanceRequests",
                    MR
                );
                MR.MaintenanceRequestId = maintenanceRequestId;
                await _firebaseService.UpdateDocumentAsync(
                    "maintenanceRequests",
                    maintenanceRequestId,
                    MR
                );
                return Ok(maintenanceRequestId);
            }
            catch (Exception ex)
            {
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
                await _firebaseService.UpdateDocumentAsync(
                    "maintenanceRequests",
                    id,
                    existingMaintenanceRequest
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

                quotation.Status = "Approved";
                await _firebaseService.UpdateDocumentAsync("quotations", id, quotation);
                return Ok(quotation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("reject/quotation/{id}")]
        public async Task<ActionResult> RejectQuotation(string id)
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
                quotation.Status = "Rejected";
                await _firebaseService.UpdateDocumentAsync("quotations", id, quotation);
                return Ok(quotation);
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
