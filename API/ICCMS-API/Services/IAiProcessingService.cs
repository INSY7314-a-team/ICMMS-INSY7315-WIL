using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public interface IAiProcessingService
    {
        Task<Estimate> ProcessBlueprintToEstimateAsync(string blueprintUrl, string projectId, string contractorId);
        Task<List<EstimateLineItem>> ExtractLineItemsFromBlueprintAsync(string blueprintUrl);
        Task<List<EstimateLineItem>> GetPricingForLineItemsAsync(List<EstimateLineItem> lineItems);
        Task<Estimate> ConvertEstimateToQuotationAsync(Estimate estimate, string clientId);
    }
}
