using System.Linq;
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

                if (string.IsNullOrEmpty(request.TaskId))
                {
                    return BadRequest(new { error = "Task ID is required" });
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

                // Check if rating already exists for this task-contractor-client combination
                var submissionId = $"{request.TaskId}_{request.ContractorId}_{clientId}";
                var existingSubmission = await _firebaseService.GetDocumentAsync<RatingSubmission>(
                    "ratingSubmissions",
                    submissionId
                );

                if (existingSubmission != null)
                {
                    return BadRequest(
                        new { error = "You have already rated this contractor for this task." }
                    );
                }

                // Get all existing submissions for this contractor to recalculate average
                var allSubmissions = await _firebaseService.GetCollectionAsync<RatingSubmission>(
                    "ratingSubmissions"
                );
                var contractorSubmissions = allSubmissions
                    .Where(s => s.ContractorId == request.ContractorId)
                    .ToList();

                // Create new rating submission
                var newSubmission = new RatingSubmission
                {
                    RatingSubmissionId = submissionId,
                    ContractorId = request.ContractorId,
                    TaskId = request.TaskId,
                    RatedBy = clientId,
                    RatingValue = request.RatingValue,
                    CreatedAt = DateTime.UtcNow,
                };

                await _firebaseService.AddDocumentWithIdAsync(
                    "ratingSubmissions",
                    submissionId,
                    newSubmission
                );

                // Recalculate average rating from all submissions
                var allRatingsForContractor = contractorSubmissions
                    .Select(s => s.RatingValue)
                    .ToList();
                allRatingsForContractor.Add(request.RatingValue);

                var newAverage = allRatingsForContractor.Average();
                var newTotal = allRatingsForContractor.Count;

                // Update or create aggregate rating
                var existingRating = await _firebaseService.GetDocumentAsync<ContractorRating>(
                    "contractorRatings",
                    request.ContractorId
                );

                ContractorRating rating;

                if (existingRating == null)
                {
                    rating = new ContractorRating
                    {
                        ContractorRatingId = request.ContractorId,
                        ContractorId = request.ContractorId,
                        AverageRating = newAverage,
                        TotalRatings = newTotal,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    await _firebaseService.AddDocumentWithIdAsync(
                        "contractorRatings",
                        request.ContractorId,
                        rating
                    );
                }
                else
                {
                    rating = new ContractorRating
                    {
                        ContractorRatingId = request.ContractorId,
                        ContractorId = request.ContractorId,
                        AverageRating = newAverage,
                        TotalRatings = newTotal,
                        CreatedAt = existingRating.CreatedAt,
                        UpdatedAt = DateTime.UtcNow,
                    };
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
                    $"Rating {request.RatingValue} submitted for contractor {request.ContractorId} on task {request.TaskId}",
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

        [HttpGet("task/{taskId}/contractor/{contractorId}")]
        [Authorize(Roles = "Client,Tester")]
        public async Task<ActionResult<bool>> HasRatedTask(string taskId, string contractorId)
        {
            try
            {
                var clientId = User.UserId();
                if (string.IsNullOrEmpty(clientId))
                {
                    return Unauthorized(new { error = "Client ID not found" });
                }

                var submissionId = $"{taskId}_{contractorId}_{clientId}";
                var existingSubmission = await _firebaseService.GetDocumentAsync<RatingSubmission>(
                    "ratingSubmissions",
                    submissionId
                );

                return Ok(existingSubmission != null);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
