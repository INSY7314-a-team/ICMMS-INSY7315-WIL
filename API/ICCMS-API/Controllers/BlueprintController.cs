using Microsoft.AspNetCore.Mvc;
using ICCMS_API.Services;
using ICCMS_API.Models;
using Microsoft.AspNetCore.Authorization;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Tester")]
    public class BlueprintController : ControllerBase
    {
        private readonly IAiProcessingService _aiProcessingService;

        public BlueprintController(IAiProcessingService aiProcessingService)
        {
            _aiProcessingService = aiProcessingService;
        }

        /// <summary>
        /// Process a blueprint from Supabase URL and generate an estimate
        /// </summary>
        /// <param name="request">Blueprint processing request</param>
        /// <returns>Generated estimate with line items</returns>
        [HttpPost("process-supabase-blueprint")]
        public async Task<IActionResult> ProcessSupabaseBlueprint([FromBody] ProcessBlueprintRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.BlueprintUrl))
                {
                    return BadRequest("Blueprint URL is required");
                }

                if (string.IsNullOrEmpty(request.ProjectId))
                {
                    return BadRequest("Project ID is required");
                }

                if (string.IsNullOrEmpty(request.ContractorId))
                {
                    return BadRequest("Contractor ID is required");
                }

                Console.WriteLine($"Processing Supabase blueprint: {request.BlueprintUrl}");
                Console.WriteLine($"Project ID: {request.ProjectId}");
                Console.WriteLine($"Contractor ID: {request.ContractorId}");

                // Process the blueprint using the AI service
                var estimate = await _aiProcessingService.ProcessBlueprintToEstimateAsync(
                    request.BlueprintUrl,
                    request.ProjectId,
                    request.ContractorId
                );

                return Ok(new
                {
                    success = true,
                    data = estimate,
                    message = "Blueprint processed successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Supabase blueprint: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to process blueprint",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Extract line items from a Supabase blueprint without creating an estimate
        /// </summary>
        /// <param name="request">Blueprint processing request</param>
        /// <returns>Extracted line items</returns>
        [HttpPost("extract-line-items")]
        public async Task<IActionResult> ExtractLineItems([FromBody] ExtractLineItemsRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.BlueprintUrl))
                {
                    return BadRequest("Blueprint URL is required");
                }

                Console.WriteLine($"Extracting line items from Supabase blueprint: {request.BlueprintUrl}");

                // Extract line items using the AI service
                var lineItems = await _aiProcessingService.ExtractLineItemsFromBlueprintAsync(request.BlueprintUrl);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        lineItems = lineItems,
                        totalItems = lineItems.Count,
                        extractedAt = DateTime.UtcNow
                    },
                    message = "Line items extracted successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting line items: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to extract line items",
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }

}
