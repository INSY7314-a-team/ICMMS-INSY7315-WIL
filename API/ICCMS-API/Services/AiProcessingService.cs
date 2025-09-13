using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public class AiProcessingService : IAiProcessingService
    {
        private readonly IMaterialDatabaseService _materialDatabaseService;

        public AiProcessingService(IMaterialDatabaseService materialDatabaseService)
        {
            _materialDatabaseService = materialDatabaseService;
        }

        public async Task<Estimate> ProcessBlueprintToEstimateAsync(
            string blueprintUrl,
            string projectId,
            string contractorId
        )
        {
            try
            {
                Console.WriteLine($"Processing blueprint: {blueprintUrl} for project: {projectId}");

                // Step 1: Extract line items from blueprint (AI processing)
                var lineItems = await ExtractLineItemsFromBlueprintAsync(blueprintUrl);
                Console.WriteLine($"Extracted {lineItems.Count} line items");

                // Step 2: Get pricing for line items
                var pricedLineItems = await GetPricingForLineItemsAsync(lineItems);
                Console.WriteLine($"Priced {pricedLineItems.Count} line items");

                // Step 3: Create estimate
                var estimate = new Estimate
                {
                    ProjectId = projectId,
                    ContractorId = contractorId,
                    Description = "AI-generated estimate from blueprint",
                    LineItems = pricedLineItems,
                    IsAiGenerated = true,
                    BlueprintUrl = blueprintUrl,
                    Status = "Draft",
                    ValidUntil = DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    Currency = "ZAR",
                };

                // Calculate totals
                CalculateEstimateTotals(estimate);

                Console.WriteLine($"Created estimate with total: {estimate.TotalAmount}");
                return estimate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessBlueprintToEstimateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EstimateLineItem>> ExtractLineItemsFromBlueprintAsync(
            string blueprintUrl
        )
        {
            // TODO: Implement AI blueprint processing
            // For now, return mock data based on the example you provided

            var mockLineItems = new List<EstimateLineItem>
            {
                new EstimateLineItem
                {
                    ItemId = Guid.NewGuid().ToString(),
                    Name = "Cement",
                    Description = "Portland cement for construction",
                    Quantity = 100,
                    Unit = "KG",
                    Category = "Concrete",
                    IsAiGenerated = true,
                    AiConfidence = 0.95,
                    Notes = "Extracted from blueprint - foundation work",
                },
                new EstimateLineItem
                {
                    ItemId = Guid.NewGuid().ToString(),
                    Name = "Bricks",
                    Description = "Standard red clay bricks",
                    Quantity = 4000,
                    Unit = "PIECE",
                    Category = "Masonry",
                    IsAiGenerated = true,
                    AiConfidence = 0.92,
                    Notes = "Extracted from blueprint - wall construction",
                },
                new EstimateLineItem
                {
                    ItemId = Guid.NewGuid().ToString(),
                    Name = "Paint",
                    Description = "Interior/exterior paint",
                    Quantity = 30,
                    Unit = "LITER",
                    Category = "Finishing",
                    IsAiGenerated = true,
                    AiConfidence = 0.88,
                    Notes = "Extracted from blueprint - painting work",
                },
            };

            return await Task.FromResult(mockLineItems);
        }

        public async Task<List<EstimateLineItem>> GetPricingForLineItemsAsync(
            List<EstimateLineItem> lineItems
        )
        {
            var pricedLineItems = new List<EstimateLineItem>();

            foreach (var item in lineItems)
            {
                // Try to find matching material in database
                var material = await _materialDatabaseService.GetMaterialByNameAsync(item.Name);

                if (material != null)
                {
                    item.UnitPrice = material.UnitPrice;
                    item.MaterialDatabaseId = material.Id;
                    item.Description = material.Description;
                }
                else
                {
                    // If not found, use default pricing or mark for manual review
                    item.UnitPrice = 0.0; // Mark for manual pricing
                    item.Notes += " [REQUIRES MANUAL PRICING]";
                }

                item.LineTotal = item.Quantity * item.UnitPrice;
                pricedLineItems.Add(item);
            }

            return pricedLineItems;
        }

        public async Task<Estimate> ConvertEstimateToQuotationAsync(
            Estimate estimate,
            string clientId
        )
        {
            // Convert EstimateLineItems to QuotationItems
            var quotationItems = estimate
                .LineItems.Select(item => new QuotationItem
                {
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = 0.15, // Default 15% VAT
                    LineTotal = item.LineTotal,
                })
                .ToList();

            // Create quotation from estimate
            var quotation = new Quotation
            {
                ProjectId = estimate.ProjectId,
                ClientId = clientId,
                ContractorId = estimate.ContractorId,
                Description = estimate.Description,
                Items = quotationItems,
                Status = "Draft",
                ValidUntil = estimate.ValidUntil,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Currency = estimate.Currency,
                IsAiGenerated = true,
            };

            // Calculate quotation totals
            CalculateQuotationTotals(quotation);

            return await Task.FromResult(estimate); // Return original estimate for now
        }

        private void CalculateEstimateTotals(Estimate estimate)
        {
            estimate.Subtotal = estimate.LineItems.Sum(item => item.LineTotal);
            estimate.TaxTotal = estimate.Subtotal * 0.15; // 15% VAT
            estimate.TotalAmount = estimate.Subtotal + estimate.TaxTotal;
        }

        private void CalculateQuotationTotals(Quotation quotation)
        {
            quotation.Subtotal = quotation.Items.Sum(item => item.LineTotal);
            quotation.TaxTotal = quotation.Subtotal * 0.15; // 15% VAT
            quotation.GrandTotal = quotation.Subtotal + quotation.TaxTotal;
            quotation.Total = quotation.GrandTotal; // For backward compatibility
        }
    }
}
