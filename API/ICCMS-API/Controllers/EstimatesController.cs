using ICCMS_API.Auth;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")] // All authenticated users can access estimates
    public class EstimatesController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly IAiProcessingService _aiProcessingService;
        private readonly IMaterialDatabaseService _materialDatabaseService;
        private readonly IAuditLogService _auditLogService;

        public EstimatesController(
            IFirebaseService firebaseService,
            IAiProcessingService aiProcessingService,
            IMaterialDatabaseService materialDatabaseService,
            IAuditLogService auditLogService
        )
        {
            _firebaseService = firebaseService;
            _aiProcessingService = aiProcessingService;
            _materialDatabaseService = materialDatabaseService;
            _auditLogService = auditLogService;
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
                estimate.EstimateId = estimateId;
                await _firebaseService.UpdateDocumentAsync("estimates", estimateId, estimate);
                
                var userId = User.UserId();
                _auditLogService.LogAsync("Estimate", "Estimate Created", $"Estimate {estimateId} created for project {estimate.ProjectId}", userId ?? "system", estimateId);
                
                Console.WriteLine("Estimate created with ID: " + estimateId);
                return Ok(estimate);
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

        [HttpPost("process-blueprint")]
        [Authorize(Roles = "Project Manager,Contractor,Tester")]
        public async Task<ActionResult<Estimate>> ProcessBlueprint(
            [FromBody] ProcessBlueprintRequest request
        )
        {
            try
            {
                var estimate = await _aiProcessingService.ProcessBlueprintToEstimateAsync(
                    request.BlueprintUrl,
                    request.ProjectId,
                    request.ContractorId
                );

                // Ensure BlueprintUrl is explicitly set from request
                estimate.BlueprintUrl = request.BlueprintUrl;
                estimate.CreatedAt = DateTime.UtcNow;
                var estimateId = await _firebaseService.AddDocumentAsync("estimates", estimate);
                estimate.EstimateId = estimateId;
                await _firebaseService.UpdateDocumentAsync("estimates", estimateId, estimate);
                
                var userId = User.UserId();
                _auditLogService.LogAsync("Estimate", "Blueprint Processed", $"Blueprint processed and estimate {estimateId} created for project {request.ProjectId}", userId ?? "system", estimateId);
                
                Console.WriteLine("Estimate created with ID: " + estimateId);

                return Ok(estimate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("{id}/convert-to-quotation")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<ActionResult<string>> ConvertToQuotation(
            string id,
            [FromBody] ConvertToQuotationRequest request
        )
        {
            try
            {
                var estimate = await _firebaseService.GetDocumentAsync<Estimate>("estimates", id);
                if (estimate == null)
                    return NotFound();

                var quotation = await _aiProcessingService.ConvertEstimateToQuotationAsync(
                    estimate,
                    request.ClientId
                );
                var quotationId = await _firebaseService.AddDocumentAsync("quotations", quotation);
                
                var userId = User.UserId();
                _auditLogService.LogAsync("Estimate", "Estimate Converted to Quotation", $"Estimate {id} converted to quotation {quotationId}", userId ?? "system", id);
                
                return Ok(quotationId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("materials")]
        [Authorize(Roles = "Project Manager,Contractor,Tester")]
        public async Task<ActionResult<List<MaterialItem>>> GetMaterials()
        {
            try
            {
                var materials = await _materialDatabaseService.GetAllMaterialsAsync();
                return Ok(materials);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("materials/category/{category}")]
        [Authorize(Roles = "Project Manager,Contractor,Tester")]
        public async Task<ActionResult<List<MaterialItem>>> GetMaterialsByCategory(string category)
        {
            try
            {
                var materials = await _materialDatabaseService.GetMaterialsByCategoryAsync(
                    category
                );
                return Ok(materials);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("materials/categories")]
        [Authorize(Roles = "Project Manager,Contractor,Tester")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            try
            {
                var categories = await _materialDatabaseService.GetCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
