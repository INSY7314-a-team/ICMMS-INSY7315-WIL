namespace ICCMS_API.Models
{
    public class ProcessBlueprintRequest
    {
        public string BlueprintUrl { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ContractorId { get; set; } = string.Empty;
    }

    public class ConvertToQuotationRequest
    {
        public string ClientId { get; set; } = string.Empty;
    }

    public class ExtractLineItemsRequest
    {
        public string BlueprintUrl { get; set; } = string.Empty;
    }
}
