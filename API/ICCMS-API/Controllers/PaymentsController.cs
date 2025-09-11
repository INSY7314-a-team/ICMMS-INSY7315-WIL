using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ICCMS_API.Models;
using ICCMS_API.Services;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Client,Tester")] // Admin oversight, PM management, Client view, Tester access
    public class PaymentsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public PaymentsController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Payment>>> GetPayments()
        {
            try
            {
                var payments = await _firebaseService.GetCollectionAsync<Payment>("payments");
                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(string id)
        {
            try
            {
                var payment = await _firebaseService.GetDocumentAsync<Payment>("payments", id);
                if (payment == null)
                    return NotFound();
                return Ok(payment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("invoice/{invoiceId}")]
        public async Task<ActionResult<List<Payment>>> GetPaymentsByInvoice(string invoiceId)
        {
            try
            {
                var payments = await _firebaseService.GetCollectionAsync<Payment>("payments");
                var invoicePayments = payments.Where(p => p.InvoiceId == invoiceId).ToList();
                return Ok(invoicePayments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<Payment>>> GetPaymentsByProject(string projectId)
        {
            try
            {
                var payments = await _firebaseService.GetCollectionAsync<Payment>("payments");
                var projectPayments = payments.Where(p => p.ProjectId == projectId).ToList();
                return Ok(projectPayments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("client/{clientId}")]
        public async Task<ActionResult<List<Payment>>> GetPaymentsByClient(string clientId)
        {
            try
            {
                var payments = await _firebaseService.GetCollectionAsync<Payment>("payments");
                var clientPayments = payments.Where(p => p.ClientId == clientId).ToList();
                return Ok(clientPayments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreatePayment([FromBody] Payment payment)
        {
            try
            {
                payment.PaymentDate = DateTime.UtcNow;
                payment.ProcessedAt = DateTime.UtcNow;
                var paymentId = await _firebaseService.AddDocumentAsync("payments", payment);
                return Ok(paymentId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(string id, [FromBody] Payment payment)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("payments", id, payment);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("payments", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
