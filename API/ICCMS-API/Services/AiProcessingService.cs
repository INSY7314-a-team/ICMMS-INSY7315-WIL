using ICCMS_API.Models;

namespace ICCMS_API.Services
{
    public class AiProcessingService : IAiProcessingService
    {
        private readonly IMaterialDatabaseService _materialDatabaseService;
        private readonly SupabaseBlueprintService _supabaseBlueprintService;

        public AiProcessingService(
            IMaterialDatabaseService materialDatabaseService,
            SupabaseBlueprintService supabaseBlueprintService
        )
        {
            _materialDatabaseService = materialDatabaseService;
            _supabaseBlueprintService = supabaseBlueprintService;
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
            try
            {
                Console.WriteLine($"Calling GenKitMicroservice for blueprint: {blueprintUrl}");

                // Convert blueprint URL to base64 data (assuming it's a local file path for now)
                // In production, you'd download the file from the URL first
                Console.WriteLine($"📄 Converting blueprint URL to base64: {blueprintUrl}");
                var fileData = await ConvertUrlToBase64Async(blueprintUrl);
                var fileType = GetFileTypeFromUrl(blueprintUrl);
                Console.WriteLine(
                    $"📄 File type: {fileType}, Base64 data length: {fileData?.Length ?? 0} characters"
                );

                if (string.IsNullOrEmpty(fileData))
                {
                    throw new Exception(
                        $"Failed to download blueprint from URL: {blueprintUrl}. The file may not exist or the URL is invalid."
                    );
                }

                // Call GenKitMicroservice
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout for AI processing
                var requestBody = new
                {
                    fileData = fileData,
                    fileType = fileType,
                    projectContext = new
                    {
                        projectId = "AUTO_GENERATED",
                        projectType = "residential",
                        buildingType = "single_family_home",
                        location = "South Africa",
                        squareFootage = 2500,
                    },
                };

                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                Console.WriteLine(
                    $"📦 Request body preview: {json.Substring(0, Math.Min(200, json.Length))}..."
                );
                var content = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                // Assuming GenKitMicroservice runs on localhost:3001
                Console.WriteLine($"🚀 Making HTTP request to GenKitMicroservice...");
                Console.WriteLine($"📡 URL: http://localhost:3001/api/ai/extract-line-items");
                Console.WriteLine($"📦 Request body size: {json.Length} characters");

                var response = await httpClient.PostAsync(
                    "http://localhost:3001/api/ai/extract-line-items",
                    content
                );

                Console.WriteLine($"📡 HTTP Response Status: {response.StatusCode}");
                Console.WriteLine(
                    $"📡 HTTP Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ GenKitMicroservice error: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Error content: {errorContent}");
                    return await GetFallbackLineItems();
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(
                    $"✅ GenKitMicroservice response length: {responseContent.Length} characters"
                );
                Console.WriteLine(
                    $"✅ GenKitMicroservice response preview: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}..."
                );

                GenKitResponse result;
                try
                {
                    result = System.Text.Json.JsonSerializer.Deserialize<GenKitResponse>(
                        responseContent
                    );
                    Console.WriteLine(
                        $"🔍 Deserialized result - Success: {result?.Success}, LineItems count: {result?.LineItems?.Count}"
                    );
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine($"❌ JSON Deserialization Error: {jsonEx.Message}");
                    Console.WriteLine($"❌ Raw response that failed to parse: {responseContent}");
                    return await GetFallbackLineItems();
                }

                if (
                    result?.Success == true
                    && result.LineItems != null
                    && result.LineItems.Count > 0
                )
                {
                    Console.WriteLine(
                        $"Successfully extracted {result.LineItems.Count} line items from GenKitMicroservice"
                    );

                    // Convert GenKit response to EstimateLineItem format
                    var lineItems = result
                        .LineItems.Select(item => new EstimateLineItem
                        {
                            ItemId = item.ItemId,
                            Name = item.Name,
                            Description = item.Description,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            Category = item.Category,
                            UnitPrice = item.UnitPrice,
                            LineTotal = item.LineTotal,
                            IsAiGenerated = item.IsAiGenerated,
                            AiConfidence = item.AiConfidence,
                            Notes = item.Notes,
                            MaterialDatabaseId = item.MaterialDatabaseId,
                        })
                        .ToList();

                    return lineItems;
                }
                else
                {
                    Console.WriteLine(
                        $"GenKitMicroservice returned unsuccessful response - Success: {result?.Success}, LineItems: {result?.LineItems?.Count ?? 0}"
                    );
                    Console.WriteLine($"Full response: {responseContent}");
                    return await GetFallbackLineItems();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling GenKitMicroservice: {ex.Message}");
                return await GetFallbackLineItems();
            }
        }

        private async Task<string> ConvertUrlToBase64Async(string blueprintUrl)
        {
            try
            {
                // Check if it's a local file path
                if (File.Exists(blueprintUrl))
                {
                    var fileBytes = await File.ReadAllBytesAsync(blueprintUrl);
                    return Convert.ToBase64String(fileBytes);
                }

                // Check if it's a Supabase URL
                if (_supabaseBlueprintService.IsValidSupabaseUrl(blueprintUrl))
                {
                    return await _supabaseBlueprintService.DownloadBlueprintAsBase64Async(
                        blueprintUrl
                    );
                }

                // Check if it's a regular URL (http/https)
                if (blueprintUrl.StartsWith("http://") || blueprintUrl.StartsWith("https://"))
                {
                    Console.WriteLine($"Downloading blueprint from URL: {blueprintUrl}");

                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(blueprintUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var fileBytes = await response.Content.ReadAsByteArrayAsync();
                        Console.WriteLine(
                            $"Successfully downloaded {fileBytes.Length} bytes from URL"
                        );
                        return Convert.ToBase64String(fileBytes);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(
                            $"Failed to download from URL: {response.StatusCode} - {errorContent}"
                        );
                        return "";
                    }
                }

                // Fallback: return empty string for unknown file types
                Console.WriteLine($"Unknown file type or path: {blueprintUrl}");
                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting URL to base64: {ex.Message}");
                return "";
            }
        }

        private string GetFileTypeFromUrl(string blueprintUrl)
        {
            // Use SupabaseBlueprintService for Supabase URLs
            if (_supabaseBlueprintService.IsValidSupabaseUrl(blueprintUrl))
            {
                return _supabaseBlueprintService.GetFileTypeFromSupabaseUrl(blueprintUrl);
            }

            // Fallback for other URLs
            var urlWithoutQuery = blueprintUrl.Split('?')[0];
            var extension = Path.GetExtension(urlWithoutQuery).ToLower();

            return extension switch
            {
                ".pdf" => "pdf",
                ".jpg" or ".jpeg" => "image",
                ".png" => "image",
                ".dwg" => "dwg",
                ".dxf" => "dxf",
                _ => "pdf", // Default to PDF
            };
        }

        private async Task<List<EstimateLineItem>> GetFallbackLineItems()
        {
            Console.WriteLine(
                "⚠️ CRITICAL: Using fallback line items - GenKitMicroservice is unavailable!"
            );
            Console.WriteLine(
                "This should only happen when the GenKitMicroservice is completely down."
            );

            return await Task.FromResult(
                new List<EstimateLineItem>
                {
                    new EstimateLineItem
                    {
                        ItemId = Guid.NewGuid().ToString(),
                        Name = "⚠️ FALLBACK - GenKitMicroservice Unavailable",
                        Description =
                            "The AI service is not responding. Please check if GenKitMicroservice is running.",
                        Quantity = 0,
                        Unit = "N/A",
                        Category = "Error",
                        IsAiGenerated = false,
                        AiConfidence = 0.0,
                        Notes =
                            "CRITICAL: GenKitMicroservice is not responding - check service status",
                    },
                }
            );
        }

        // Response classes for GenKitMicroservice
        public class GenKitResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("success")]
            public bool Success { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("lineItems")]
            public List<GenKitLineItem> LineItems { get; set; } = new();

            [System.Text.Json.Serialization.JsonPropertyName("metadata")]
            public GenKitMetadata Metadata { get; set; } = new();
        }

        public class GenKitLineItem
        {
            [System.Text.Json.Serialization.JsonPropertyName("itemId")]
            public string ItemId { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("description")]
            public string Description { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("quantity")]
            public double Quantity { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("unit")]
            public string Unit { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("category")]
            public string Category { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("unitPrice")]
            public double UnitPrice { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("lineTotal")]
            public double LineTotal { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("isAiGenerated")]
            public bool IsAiGenerated { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("aiConfidence")]
            public double AiConfidence { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("notes")]
            public string Notes { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("materialDatabaseId")]
            public string? MaterialDatabaseId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("unitOfQuantity")]
            public string UnitOfQuantity { get; set; } = "";
        }

        public class GenKitMetadata
        {
            [System.Text.Json.Serialization.JsonPropertyName("totalItems")]
            public int TotalItems { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("processingTime")]
            public long ProcessingTime { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("confidence")]
            public double Confidence { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("blueprintTypes")]
            public List<string> BlueprintTypes { get; set; } = new();

            [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
            public string Timestamp { get; set; } = "";
        }

        public async Task<List<EstimateLineItem>> GetPricingForLineItemsAsync(
            List<EstimateLineItem> lineItems
        )
        {
            var pricedLineItems = new List<EstimateLineItem>();

            foreach (var item in lineItems)
            {
                // Check if this is a General work item (should NOT be priced from database)
                bool isGeneralWorkItem =
                    item.Category?.ToLower() == "general"
                    || (item.Name?.ToLower().Contains("preparation") == true)
                    || (item.Name?.ToLower().Contains("management") == true)
                    || (item.Name?.ToLower().Contains("supervision") == true)
                    || (item.Name?.ToLower().Contains("utilities") == true)
                    || (item.Name?.ToLower().Contains("equipment") == true)
                    || (item.Name?.ToLower().Contains("safety") == true)
                    || (item.Name?.ToLower().Contains("permits") == true);

                if (isGeneralWorkItem)
                {
                    // General work items keep their AI-generated pricing
                    Console.WriteLine(
                        $"General work item '{item.Name}' - keeping AI-generated pricing: R{item.UnitPrice}"
                    );
                    item.Notes += " [GENERAL WORK ITEM - AI PRICING]";
                }
                else
                {
                    // Material items - try to find pricing in database
                    var material = await _materialDatabaseService.GetMaterialByNameAsync(item.Name);

                    if (material != null)
                    {
                        item.UnitPrice = material.UnitPrice;
                        item.MaterialDatabaseId = material.Id;
                        item.Description = material.Description;
                        item.Unit = material.Unit; // Use the unit from the database
                        Console.WriteLine(
                            $"Found pricing for {item.Name}: R{material.UnitPrice} per {material.Unit}"
                        );
                    }
                    else
                    {
                        // Try fuzzy matching for similar names
                        var fuzzyMatch = await FindFuzzyMaterialMatch(item.Name);
                        if (fuzzyMatch != null)
                        {
                            item.UnitPrice = fuzzyMatch.UnitPrice;
                            item.MaterialDatabaseId = fuzzyMatch.Id;
                            item.Description = fuzzyMatch.Description;
                            item.Unit = fuzzyMatch.Unit;
                            item.Notes += $" [FUZZY MATCH: {fuzzyMatch.Name}]";
                            Console.WriteLine(
                                $"Fuzzy match found for {item.Name}: {fuzzyMatch.Name} - R{fuzzyMatch.UnitPrice} per {fuzzyMatch.Unit}"
                            );
                        }
                        else
                        {
                            // If not found, set price to 0 (as per requirements)
                            item.UnitPrice = 0.0;
                            item.Notes += " [NO PRICING FOUND - SET TO 0]";
                            Console.WriteLine($"No pricing found for {item.Name} - set to R0");
                        }
                    }
                }

                // Calculate line total
                item.LineTotal = item.Quantity * item.UnitPrice;
                pricedLineItems.Add(item);
            }

            return pricedLineItems;
        }

        private async Task<MaterialItem?> FindFuzzyMaterialMatch(string itemName)
        {
            // Get all materials from database
            var allMaterials = await _materialDatabaseService.GetAllMaterialsAsync();

            // Simple fuzzy matching - look for partial matches
            var normalizedItemName = itemName.ToLower().Trim();

            foreach (var material in allMaterials)
            {
                var normalizedMaterialName = material.Name.ToLower().Trim();

                // Check if item name contains material name or vice versa
                if (
                    normalizedItemName.Contains(normalizedMaterialName)
                    || normalizedMaterialName.Contains(normalizedItemName)
                )
                {
                    return material;
                }

                // Check for common variations
                if (
                    normalizedItemName.Contains("brick") && normalizedMaterialName.Contains("brick")
                )
                    return material;
                if (
                    normalizedItemName.Contains("concrete")
                    && normalizedMaterialName.Contains("concrete")
                )
                    return material;
                if (
                    normalizedItemName.Contains("paint") && normalizedMaterialName.Contains("paint")
                )
                    return material;
                if (
                    normalizedItemName.Contains("steel") && normalizedMaterialName.Contains("steel")
                )
                    return material;
                if (normalizedItemName.Contains("tile") && normalizedMaterialName.Contains("tile"))
                    return material;
                if (normalizedItemName.Contains("door") && normalizedMaterialName.Contains("door"))
                    return material;
                if (
                    normalizedItemName.Contains("window")
                    && normalizedMaterialName.Contains("window")
                )
                    return material;
            }

            return null;
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
