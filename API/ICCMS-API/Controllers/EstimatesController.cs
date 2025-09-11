using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ICCMS_API.Models;
using ICCMS_API.Services;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")] // All authenticated users can access estimates
    public class EstimatesController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public EstimatesController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Estimate>>> GetEstimates()
        {
            try
            {
                var estimates = await _firebaseService.GetCollectionAsync<Estimate>("estimates");
                return Ok(estimates);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Estimate>> GetEstimate(string id)
        {
            try
            {
                var estimate = await _firebaseService.GetDocumentAsync<Estimate>("estimates", id);
                if (estimate == null)
                    return NotFound();
                return Ok(estimate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<Estimate>>> GetEstimatesByProject(string projectId)
        {
            try
            {
                var estimates = await _firebaseService.GetCollectionAsync<Estimate>("estimates");
                var projectEstimates = estimates.Where(e => e.ProjectId == projectId).ToList();
                return Ok(projectEstimates);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateEstimate([FromBody] Estimate estimate)
        {
            try
            {
                estimate.CreatedAt = DateTime.UtcNow;
                var estimateId = await _firebaseService.AddDocumentAsync("estimates", estimate);
                return Ok(estimateId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEstimate(string id, [FromBody] Estimate estimate)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("estimates", id, estimate);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEstimate(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("estimates", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
