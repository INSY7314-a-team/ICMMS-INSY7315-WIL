using System.Security.Claims;
using ICCMS_Web.Models;

namespace ICCMS_Web.Services
{
    public interface IQuotationsService
    {
        Task<QuotationDto?> CreateFromEstimateAsync(string estimateId, ClaimsPrincipal user);
        Task<QuotationDto?> SendToClientAsync(string quotationId, ClaimsPrincipal user);
    }

    public class QuotationsService : IQuotationsService
    {
        private readonly IApiClient _apiClient;

        public QuotationsService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<QuotationDto?> CreateFromEstimateAsync(string estimateId, ClaimsPrincipal user)
        {
            return _apiClient.PostAsync<QuotationDto>(
                $"/api/quotations/from-estimate/{estimateId}",
                new { },
                user
            );
        }

        public async Task<QuotationDto?> SendToClientAsync(string quotationId, ClaimsPrincipal user)
        {
            var result = await _apiClient.PostAsync<QuotationDto>(
                $"/api/quotations/{quotationId}/send-to-client",
                new { },
                user
            );

            // ✅ Handle empty success response
            if (result == null)
            {
                Console.WriteLine($"⚠️ API returned empty response for quotation {quotationId} — assuming success.");
                return new QuotationDto { QuotationId = quotationId, Status = "SentToClient" };
            }

            return result;
        }

    }
}
