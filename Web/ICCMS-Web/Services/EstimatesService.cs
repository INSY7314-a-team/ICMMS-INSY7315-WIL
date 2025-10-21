using System.Security.Claims;
using ICCMS_Web.Models;
using Microsoft.Extensions.Logging;

namespace ICCMS_Web.Services
{
    public interface IEstimatesService
    {
        Task<EstimateDto?> GetByProjectAsync(string projectId, ClaimsPrincipal user);
        Task<EstimateDto?> SaveAsync(string estimateId, EstimateDto estimate, ClaimsPrincipal user);
        Task<EstimateDto?> ProcessBlueprintAsync(
            ProcessBlueprintRequest request,
            ClaimsPrincipal user
        );
    }

    public class EstimatesService : IEstimatesService
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<EstimatesService> _logger;

        public EstimatesService(IApiClient apiClient, ILogger<EstimatesService> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<EstimateDto?> GetByProjectAsync(string projectId, ClaimsPrincipal user)
        {
            // API returns a LIST of estimates for a project; pick the latest by CreatedAt
            var list =
                await _apiClient.GetAsync<List<EstimateDto>>(
                    $"/api/estimates/project/{projectId}",
                    user
                ) ?? new List<EstimateDto>();
            return list.OrderByDescending(e => e.CreatedAt).FirstOrDefault();
        }

        public async Task<EstimateDto?> SaveAsync(
            string estimateId,
            EstimateDto estimate,
            ClaimsPrincipal user
        )
        {
            var result = await _apiClient.PutAsync<EstimateDto>(
                $"/api/estimates/{estimateId}",
                estimate,
                user
            );

            // If API returns null (204 No Content), return the original estimate
            if (result == null)
            {
                _logger.LogInformation(
                    "✅ Save successful (204 No Content) - returning original estimate"
                );
                return estimate;
            }

            return result;
        }

        public Task<EstimateDto?> ProcessBlueprintAsync(
            ProcessBlueprintRequest request,
            ClaimsPrincipal user
        )
        {
            return _apiClient.PostAsync<EstimateDto>(
                "/api/estimates/process-blueprint",
                request,
                user
            );
        }
    }
}
