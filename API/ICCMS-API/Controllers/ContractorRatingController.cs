using System.Security.Claims;
using ICCMS_API.Auth;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/contractorrating")]
    [Authorize]
    public class ContractorRatingController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;
        private readonly IAuditLogService _auditLogService;

        public ContractorRatingController(
            IFirebaseService firebaseService,
            IAuditLogService auditLogService
        )
        {
            _firebaseService = firebaseService;
            _auditLogService = auditLogService;
        }

        [HttpGet("me")]
        [Authorize(Roles = "Contractor,Tester")]
        public async Task<ActionResult<ContractorRating>> GetMyRating()
        {
            try
            {
                var contractorId = User.UserId();
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Try to get the rating document using contractorId as the document ID
                var rating = await _firebaseService.GetDocumentAsync<ContractorRating>(
                    "contractorRatings",
                    contractorId
                );

                if (rating == null)
                {
                    // Return a default rating if none exists
                    return Ok(
                        new ContractorRating
                        {
                            ContractorRatingId = contractorId,
                            ContractorId = contractorId,
                            AverageRating = 0.0,
                            TotalRatings = 0,
                        }
                    );
                }

                // Ensure ContractorRatingId matches the document ID
                rating.ContractorRatingId = contractorId;
                return Ok(rating);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{contractorId}")]
        public async Task<ActionResult<ContractorRating>> GetContractorRating(string contractorId)
        {
            try
            {
                if (string.IsNullOrEmpty(contractorId))
                {
                    return BadRequest(new { error = "Contractor ID is required" });
                }

                var rating = await _firebaseService.GetDocumentAsync<ContractorRating>(
                    "contractorRatings",
                    contractorId
                );

                if (rating == null)
                {
                    // Return a default rating if none exists
                    return Ok(
                        new ContractorRating
                        {
                            ContractorRatingId = contractorId,
                            ContractorId = contractorId,
                            AverageRating = 0.0,
                            TotalRatings = 0,
                        }
                    );
                }

                // Ensure ContractorRatingId matches the document ID
                rating.ContractorRatingId = contractorId;
                return Ok(rating);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Client,Tester")]
        public async Task<ActionResult<ContractorRating>> SubmitRating(
            [FromBody] SubmitRatingRequest request
        )
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.ContractorId))
                {
                    return BadRequest(new { error = "Contractor ID is required" });
                }

                if (request.RatingValue < 1 || request.RatingValue > 5)
                {
                    return BadRequest(new { error = "Rating value must be between 1 and 5" });
                }

                var clientId = User.UserId();
                if (string.IsNullOrEmpty(clientId))
                {
                    return Unauthorized(new { error = "Client ID not found" });
                }

                // Get existing rating or create new one
                var existingRating = await _firebaseService.GetDocumentAsync<ContractorRating>(
                    "contractorRatings",
                    request.ContractorId
                );

                ContractorRating rating;

                if (existingRating == null)
                {
                    // Create new rating
                    rating = new ContractorRating
                    {
                        ContractorRatingId = request.ContractorId,
                        ContractorId = request.ContractorId,
                        AverageRating = request.RatingValue,
                        TotalRatings = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                }
                else
                {
                    // Update existing rating using incremental calculation
                    var oldAverage = existingRating.AverageRating;
                    var oldCount = existingRating.TotalRatings;
                    var newAverage = (oldAverage * oldCount + request.RatingValue) / (oldCount + 1);

                    rating = new ContractorRating
                    {
                        ContractorRatingId = request.ContractorId, // Match document ID
                        ContractorId = request.ContractorId,
                        AverageRating = newAverage,
                        TotalRatings = oldCount + 1,
                        CreatedAt = existingRating.CreatedAt,
                        UpdatedAt = DateTime.UtcNow,
                    };
                }

                // Save to Firestore using contractorId as document ID
                if (existingRating == null)
                {
                    await _firebaseService.AddDocumentWithIdAsync(
                        "contractorRatings",
                        request.ContractorId,
                        rating
                    );
                }
                else
                {
                    await _firebaseService.UpdateDocumentAsync(
                        "contractorRatings",
                        request.ContractorId,
                        rating
                    );
                }

                // Log the rating submission
                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Contractor Rating",
                    "Rating Submitted",
                    $"Rating {request.RatingValue} submitted for contractor {request.ContractorId}",
                    userId ?? "system",
                    request.ContractorId
                );

                return Ok(rating);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
