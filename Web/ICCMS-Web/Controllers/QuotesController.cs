using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    public class QuotesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<QuotesController> _logger;

        public QuotesController(IApiClient apiClient, ILogger<QuotesController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ============================
        // STEP 0: CREATE DRAFT
        // ============================

        [HttpGet("create-draft")]
        public async Task<IActionResult> CreateDraft()
        {
            _logger.LogInformation("Displaying draft quotation form...");

            // Fetch Projects + Clients securely via _apiClient (with User token)
            var projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                        ?? new List<ProjectDto>();
            var clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                        ?? new List<UserDto>();

            // Build the ViewModel for Razor
            var vm = new CreateDraftViewModel
            {
                Projects = projects,
                Clients = clients,
                ValidUntil = DateTime.UtcNow.AddDays(30)
            };

            _logger.LogInformation("Loaded {P} projects and {C} clients for CreateDraft form", 
                projects.Count, clients.Count);

            return View("CreateDraft", vm);
        }

        [HttpPost("create-draft")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDraft(CreateDraftViewModel model)
        {
            _logger.LogInformation($"📝 Submitting new draft quotation → Project={model.ProjectId}, Client={model.ClientId}");

            try
            {
                // === Validation ===
                if (string.IsNullOrWhiteSpace(model.ProjectId))
                    ModelState.AddModelError(nameof(model.ProjectId), "Project is required");
                if (string.IsNullOrWhiteSpace(model.ClientId))
                    ModelState.AddModelError(nameof(model.ClientId), "Client is required");
                if (string.IsNullOrWhiteSpace(model.Description))
                    ModelState.AddModelError(nameof(model.Description), "Description is required");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("⚠️ Validation failed for draft quotation — repopulating dropdowns...");
                    
                    // Re-populate dropdowns before returning view
                    model.Projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                                    ?? new List<ProjectDto>();
                    model.Clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                                ?? new List<UserDto>();
                    return View("CreateDraft", model);
                }

                // === Build QuotationDto for API ===
                var quotation = new QuotationDto
                {
                    ProjectId = model.ProjectId!,
                    ClientId = model.ClientId!,
                    Description = model.Description,
                    ValidUntil = DateTime.SpecifyKind(model.ValidUntil, DateTimeKind.Utc),
                    Status = "Draft",
                    Currency = "ZAR",
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
                };

                // === POST to API ===
                var createdId = await _apiClient.PostAsync<string>("/api/quotations", quotation, User);

                if (!string.IsNullOrWhiteSpace(createdId))
                {
                    // 🔹 Force the QuotationId to the Firestore doc ID we just got back
                    quotation.QuotationId = createdId;

                    _logger.LogInformation($"🎉 Draft quotation created successfully with Firestore ID={createdId}");

                    // 🔹 Redirect with the correct ID
                    return RedirectToAction("Estimate", new { id = createdId });
                }

                _logger.LogError("💥 API returned null when creating draft quotation.");
                ModelState.AddModelError(string.Empty, "Failed to create draft quotation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"🔥 Error creating draft quotation: {ex.Message}");
                ModelState.AddModelError(string.Empty, $"Unexpected error: {ex.Message}");
            }

            // Re-populate dropdowns if we fall through
            model.Projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                            ?? new List<ProjectDto>();
            model.Clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                        ?? new List<UserDto>();

            return View("CreateDraft", model);
        }

        // ============================
        // STEP 1: ESTIMATE (GET)
        // ============================
        [HttpGet("estimate/{id}")]
        public async Task<IActionResult> Estimate(string id, bool fromBlueprint = false)
        {
            _logger.LogInformation("➡️ Navigated to Estimate step for QuoteId={Id}", id);

            // 🔹 Fetch quotation
            var quotation = await _apiClient.GetAsync<QuotationDto>($"/api/quotations/{id}", User);
            if (quotation == null)
            {
                _logger.LogWarning("⚠️ Quotation with ID={Id} not found in API", id);
                return NotFound();
            }

            _logger.LogWarning("API QuotationDto dump: Id={Id}, ProjectId={ProjectId}, ClientId={ClientId}, Desc={Desc}, Items={Items}",
                quotation.QuotationId ?? "<null>",
                quotation.ProjectId ?? "<null>",
                quotation.ClientId ?? "<null>",
                quotation.Description ?? "<null>",
                quotation.Items?.Count ?? 0);

            // 🔹 Ensure QuotationId is set
            if (string.IsNullOrWhiteSpace(quotation.QuotationId))
            {
                _logger.LogWarning("⚠️ QuotationDto had no ID — falling back to route ID={Id}", id);
                quotation.QuotationId = id;
            }

            _logger.LogInformation("📦 Quotation fetched: Id={Id}, Desc={Desc}, Items={Items}",
                quotation.QuotationId ?? "<empty>",
                quotation.Description ?? "<none>",
                quotation.Items?.Count ?? 0);

            // 🔹 Fetch related Project + Client lists
            var projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                        ?? new List<ProjectDto>();
            var clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                        ?? new List<UserDto>();

            var project = projects.FirstOrDefault(p => p.ProjectId == quotation.ProjectId);
            var client = clients.FirstOrDefault(c => c.UserId == quotation.ClientId);

            _logger.LogInformation("🔍 Quotation raw IDs → ProjectId={ProjectId}, ClientId={ClientId}",
                quotation.ProjectId ?? "<null>", quotation.ClientId ?? "<null>");
            _logger.LogInformation("🔍 Available Projects → {Projects}",
                string.Join(", ", projects.Select(p => p.ProjectId + ":" + p.Name)));
            _logger.LogInformation("🔍 Available Clients → {Clients}",
                string.Join(", ", clients.Select(c => c.UserId + ":" + c.FullName)));

            // 🔹 Build VM
            var vm = new EstimateViewModel
            {
                QuotationId = string.IsNullOrWhiteSpace(quotation.QuotationId) ? id : quotation.QuotationId,
                ProjectId   = quotation.ProjectId ?? string.Empty,
                ClientId    = quotation.ClientId ?? string.Empty,
                ContractorId = string.IsNullOrWhiteSpace(quotation.ContractorId) 
                    ? "N/A"
                    : quotation.ContractorId,
                Description = quotation.Description,
                Items = quotation.Items?.Select(q => new EstimateLineItemDto
                {
                    ItemId = q.ItemId,
                    Name = q.Name,
                    Description = q.Description,
                    Quantity = q.Quantity,
                    Unit = q.Unit,
                    Category = q.Category,
                    UnitPrice = q.UnitPrice,
                    LineTotal = q.LineTotal,
                    IsAiGenerated = q.IsAiGenerated,
                    AiConfidence = q.AiConfidence ?? 0.0,
                    MaterialDatabaseId = q.MaterialDatabaseId,
                    Notes = string.IsNullOrWhiteSpace(q.Notes) ? "-" : q.Notes
                }).ToList() ?? new List<EstimateLineItemDto>(),

                TaxRate = 15,
                MarkupRate = 20,
                ValidUntil = quotation.ValidUntil != default
                    ? DateTime.SpecifyKind(quotation.ValidUntil, DateTimeKind.Utc)
                    : DateTime.UtcNow.AddDays(30),

                // ✅ Safer fallback: use quotation's own names if project/client not resolved
                ProjectName = project?.Name ?? "Unknown Project",
                ClientName  = client?.FullName ?? "Unknown Client"

            };
            _logger.LogInformation("🚨 [GET] Estimate built with TaxRate={TaxRate}, MarkupRate={MarkupRate}", 
                vm.TaxRate, vm.MarkupRate);


            // 🔹 Blueprint handling
            if (fromBlueprint)
            {
                _logger.LogInformation("📐 Blueprint flag active — sending for AI estimate generation...");

                var blueprintEstimate = await _apiClient.PostAsync<EstimateViewModel>(
                    "/api/estimates/process-blueprint",
                    new { blueprintUrl = "https://example.com/sample.pdf", projectId = quotation.ProjectId, contractorId = quotation.ContractorId },
                    User
                );

                if (blueprintEstimate?.Items?.Any() == true)
                {
                    vm.Items = blueprintEstimate.Items;
                    vm.Description = "AI-generated from blueprint";
                    _logger.LogInformation("🤖 Blueprint processing succeeded — {Count} items generated", vm.Items.Count);
                }
                else
                {
                    _logger.LogWarning("🤖 Blueprint processing returned no items.");
                }
            }

            _logger.LogInformation("DEBUG 🕵️ ViewModel built with QuotationId={Id}", vm.QuotationId ?? "<null>");

            vm.RecalculateTotals();
            _logger.LogInformation("🧮 Totals recalculated: Subtotal={Subtotal}, Tax={Tax}, Markup={Markup}, Grand={Grand}",
                vm.Subtotal, vm.TaxAmount, vm.MarkupAmount, vm.GrandTotal);

            _logger.LogInformation("===== 🧾 FULL ESTIMATE VIEWMODEL DUMP =====");
            _logger.LogInformation("QuotationId   : {QuotationId}", vm.QuotationId);
            _logger.LogInformation("ProjectId     : {ProjectId}", vm.ProjectId);
            _logger.LogInformation("ClientId      : {ClientId}", vm.ClientId);
            _logger.LogInformation("ContractorId  : {ContractorId}", vm.ContractorId);
            _logger.LogInformation("Description   : {Description}", vm.Description);
            _logger.LogInformation("ValidUntil    : {ValidUntil} (Kind={Kind})", vm.ValidUntil, vm.ValidUntil.Kind);
            _logger.LogInformation("TaxRate       : {TaxRate}", vm.TaxRate);
            _logger.LogInformation("MarkupRate    : {MarkupRate}", vm.MarkupRate);
            _logger.LogInformation("Subtotal      : {Subtotal}", vm.Subtotal);
            _logger.LogInformation("TaxAmount     : {TaxAmount}", vm.TaxAmount);
            _logger.LogInformation("MarkupAmount  : {MarkupAmount}", vm.MarkupAmount);
            _logger.LogInformation("GrandTotal    : {GrandTotal}", vm.GrandTotal);
            _logger.LogInformation("ProjectName   : {ProjectName}", vm.ProjectName);
            _logger.LogInformation("ClientName    : {ClientName}", vm.ClientName);

            if (vm.Items == null || vm.Items.Count == 0)
            {
                _logger.LogInformation("Line Items    : NONE");
            }
            else
            {
                _logger.LogInformation("Line Items    : {Count} item(s)", vm.Items.Count);
                int index = 1;
                foreach (var item in vm.Items)
                {
                    _logger.LogInformation(
                        "   #{Index} → ItemId={ItemId}, Name={Name}, Desc={Desc}, Qty={Qty}, Unit={Unit}, " +
                        "Category={Category}, UnitPrice={UnitPrice}, LineTotal={LineTotal}, " +
                        "IsAI={IsAiGenerated}, Confidence={AiConfidence}, DBRef={MaterialDatabaseId}, Notes={Notes}",
                        index++, item.ItemId, item.Name, item.Description, item.Quantity, item.Unit,
                        item.Category, item.UnitPrice, item.LineTotal, item.IsAiGenerated,
                        item.AiConfidence, item.MaterialDatabaseId, item.Notes
                    );
                }
            }
            _logger.LogInformation("===== END OF ESTIMATE VIEWMODEL DUMP =====");

            _logger.LogWarning("🚨 DEBUG: Passing VM to view with Contractor ID ={ContractorId}", vm.ContractorId ?? "<null>");

            return View("Estimate", vm);
        }

        // ============================
        // STEP 1: ESTIMATE (POST)
        // ============================
        [HttpPost("estimate/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Estimate(string id, EstimateViewModel model)
        {
            _logger.LogInformation("➡️ ENTER POST Estimate | routeId={RouteId} | modelId={ModelId}", id, model.QuotationId);

            try
            {
                // === Normalize ValidUntil ===
                if (model.ValidUntil == default)
                    model.ValidUntil = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(30), DateTimeKind.Utc);
                else
                    model.ValidUntil = DateTime.SpecifyKind(model.ValidUntil, DateTimeKind.Utc);

                // === Validation ===
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("❌ ModelState invalid for Estimate {RouteId}", id);

                    // 🔍 Loop through every key and error in ModelState
                    foreach (var state in ModelState)
                    {
                        if (state.Value.Errors.Count > 0)
                        {
                            foreach (var error in state.Value.Errors)
                            {
                                _logger.LogError("   • Field={Field} | AttemptedValue={Value} | Error={ErrorMessage}",
                                    state.Key,
                                    state.Value?.RawValue ?? "<null>",
                                    error.ErrorMessage);
                            }
                        }
                    }

                    // 🔎 Possible common reasons for invalid ModelState in this workflow
                    _logger.LogWarning("⚠️ ModelState may be invalid due to one or more of the following:");
                    _logger.LogWarning("   • Missing required ProjectId (hidden input not bound or lost)");
                    _logger.LogWarning("   • Missing required ClientId (hidden input not bound or lost)");
                    _logger.LogWarning("   • QuotationId mismatch (null when routeId != 'new')");
                    _logger.LogWarning("   • ProjectName/ClientName not bound back (display-only fields, not posted)");
                    _logger.LogWarning("   • Description missing when marked [Required]");
                    _logger.LogWarning("   • Items list empty (at least one line item is required)");
                    _logger.LogWarning("   • Item fields invalid (Name, Category, Unit, Quantity, UnitPrice, Notes)");
                    _logger.LogWarning("   • Quantity <= 0 or UnitPrice <= 0");
                    _logger.LogWarning("   • TaxRate or MarkupRate outside 0–100");
                    _logger.LogWarning("   • ValidUntil not after CreatedAt");
                    _logger.LogWarning("   • JSON binding issues (e.g., Items[] index mismatch after row removal)");
                    _logger.LogWarning("   • Hidden Status field missing or not mapped");
                    _logger.LogWarning("   • Anti-forgery token missing/invalid");

                    return View("Estimate", model);
                }


                // === Rebuild Items ===
                var items = new List<QuotationItemDto>();
                foreach (var e in model.Items)
                {
                    double baseTotal = e.Quantity * e.UnitPrice;
                    double finalTotal = baseTotal * (1 + (model.MarkupRate / 100));

                    items.Add(new QuotationItemDto
                    {
                        ItemId = e.ItemId,
                        Name = e.Name,
                        Description = e.Description,
                        Quantity = e.Quantity,
                        Unit = e.Unit,
                        Category = e.Category,
                        UnitPrice = e.UnitPrice,
                        LineTotal = finalTotal,
                        Notes = e.Notes,
                        IsAiGenerated = e.IsAiGenerated,
                        AiConfidence = e.AiConfidence,
                        MaterialDatabaseId = e.MaterialDatabaseId,
                        TaxRate = model.TaxRate / 100
                    });

                    _logger.LogInformation("Item Built → {Name} | Qty={Qty} | UnitPrice={Price} | LineTotal={LineTotal}",
                        e.Name, e.Quantity, e.UnitPrice, finalTotal);
                }

                // === Totals ===
                double subtotal = items.Sum(i => i.LineTotal);
                double taxTotal = subtotal * (model.TaxRate / 100);
                double grandTotal = subtotal + taxTotal;

                // === Build DTO ===
                var quotation = new QuotationDto
                {
                    QuotationId = model.QuotationId, // may be null/empty if new
                    ProjectId = model.ProjectId,
                    ClientId = model.ClientId,
                    ContractorId = model.ContractorId,
                    Description = model.Description,
                    Items = items,
                    Subtotal = subtotal,
                    TaxTotal = taxTotal,
                    GrandTotal = grandTotal,
                    Total = grandTotal,
                    ValidUntil = model.ValidUntil,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Currency = "ZAR",
                    Status = "SentToClient"
                };

                // === Decide whether to PUT or POST ===
                if (!string.IsNullOrWhiteSpace(model.QuotationId) && model.Status == "Draft")
                {
                    // CASE 1: Overwrite existing Draft
                    _logger.LogInformation("📝 Overwriting existing Draft quotation {Id} → SentToClient", model.QuotationId);
                    await _apiClient.PutAsync<object>($"/api/quotations/{model.QuotationId}", quotation, User);
                    return RedirectToAction("Index");
                }
                else
                {
                    // CASE 2: Prefilled / Duplicate → create new
                    _logger.LogInformation("📄 Creating NEW quotation (duplicate or reuse case)...");
                    var newId = await _apiClient.PostAsync<string>("/api/quotations", quotation, User);

                    if (!string.IsNullOrEmpty(newId))
                    {
                        _logger.LogInformation("✅ New quotation created → {NewId}", newId);
                        return RedirectToAction("Estimate", new { id = newId });
                    }
                    else
                    {
                        _logger.LogError("❌ API did not return a new ID when creating quotation.");
                        ModelState.AddModelError("", "Failed to create quotation.");
                        return View("Estimate", model);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 ERROR in Estimate POST for {RouteId}", id);
                return View("Estimate", model);
            }
        }


        [HttpPost("process-blueprint")]
        public async Task<IActionResult> ProcessBlueprint([FromBody] BlueprintRequest request)
        {
            _logger.LogInformation("Processing blueprint for Project {ProjectId}", request.ProjectId);

            // Call API with the current user token
            var result = await _apiClient.PostAsync<EstimateViewModel>(
                "/api/estimates/process-blueprint",
                new { blueprintUrl = request.BlueprintUrl, projectId = request.ProjectId},
                User
            );

            if (result == null)
            {
                _logger.LogError("Blueprint processing failed for Project {ProjectId}", request.ProjectId);
                return BadRequest(new { error = "Failed to process blueprint" });
            }

            return Ok(result);
        }

        public class BlueprintRequest
        {
            public string BlueprintUrl { get; set; }
            public string ProjectId { get; set; }
        }


        // ============================
        // STEP 2: CONVERT TO QUOTATION
        // ============================
        [HttpGet]
        public IActionResult Convert(string id)
        {
            _logger.LogInformation("Navigated to Convert step for Quote {QuotationId}", id);

            ViewBag.QuoteId = id;
            return View("Convert");
        }

        // ============================
        // STEP 3: PM REVIEW
        // ============================
        [HttpGet]
        public IActionResult Review(string id)
        {
            _logger.LogInformation("Navigated to Review step for Quote {QuotationId}", id);

            ViewBag.QuoteId = id;
            return View("Review");
        }

        // ============================
        // PREVIEW QUOTE
        // ============================
        [HttpGet]
        public async Task<IActionResult> Preview(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var quotation = await _apiClient.GetAsync<QuotationDto>($"/api/quotations/{id}", User);

            if (quotation == null)
                return NotFound();

            return View("Preview", quotation);
        }

        // ============================
        // QUOTE INDEX (with enrichment by QuotationId)
        // ============================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("➡️ Entered Quote Index action");

            // === 1. Fetch base list of quotations from API ===
            _logger.LogInformation("Fetching all quotations from API...");
            var allQuotes = await _apiClient.GetAsync<List<QuotationDto>>("/api/quotations", User)
                            ?? new List<QuotationDto>();
            _logger.LogInformation("✅ Retrieved {Q} base quotations", allQuotes.Count);

            // === 2. Fetch Projects + Clients for enrichment lookups ===
            _logger.LogInformation("Fetching related Projects and Clients...");
            var allProjects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                            ?? new List<ProjectDto>();
            var allClients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                            ?? new List<UserDto>();
            _logger.LogInformation("✅ Retrieved {P} projects and {C} clients for enrichment",
                allProjects.Count, allClients.Count);

            // === 3. Enrich each quotation individually (get full doc by QuotationId) ===
            var enrichedQuotes = new List<QuotationDto>();

            foreach (var q in allQuotes)
            {
                _logger.LogInformation("🔍 Enriching Quote {Id}...", q.QuotationId);

                try
                {
                    var fullQuote = await _apiClient.GetAsync<QuotationDto>(
                        $"/api/quotations/{q.QuotationId}", User);

                    if (fullQuote == null)
                    {
                        _logger.LogWarning("⚠️ Could not enrich Quote {Id} (API returned null)", q.QuotationId);
                        enrichedQuotes.Add(q); // fallback to base quote
                    }
                    else
                    {
                        _logger.LogInformation("✅ Enriched Quote {Id}: ProjectId={ProjectId}, ClientId={ClientId}",
                            fullQuote.QuotationId ?? "<null>",
                            fullQuote.ProjectId ?? "<null>",
                            fullQuote.ClientId ?? "<null>");
                        enrichedQuotes.Add(fullQuote);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error enriching Quote {Id}", q.QuotationId);
                    enrichedQuotes.Add(q); // still keep base quote if enrichment fails
                }
            }

            // === 4. Map enriched quotations into ViewModel for Razor ===
            var vm = enrichedQuotes.Select(q =>
            {
                var project = allProjects.FirstOrDefault(p => p.ProjectId == q.ProjectId);
                var client = allClients.FirstOrDefault(c => c.UserId == q.ClientId);

                _logger.LogInformation("Quote {Id} → ClientId={ClientId}, FoundClient={FoundClient}, ProjectId={ProjectId}, FoundProject={FoundProject}",
                    q.QuotationId,
                    q.ClientId ?? "<null>",
                    client?.FullName ?? "❌ not found",
                    q.ProjectId ?? "<null>",
                    project?.Name ?? "❌ not found");

                return new QuoteIndexViewModel
                {
                    QuotationId = q.QuotationId,
                    Description = q.Description,
                    Status = q.Status,
                    GrandTotal = q.GrandTotal,
                    CreatedAt = q.CreatedAt,
                    UpdatedAt = q.UpdatedAt,

                    ProjectId = q.ProjectId,
                    ProjectName = project?.Name ?? "Unknown Project",

                    ClientId = q.ClientId,
                    ClientName = client?.FullName ?? "Unknown Client",
                    ClientEmail = client?.Email ?? "",
                    ClientPhone = client?.Phone ?? ""
                };
            }).ToList();

            // === 5. Debug dump of final mapped ViewModel ===
            _logger.LogInformation("===== 🕵️ DEBUG: FULL INDEX VIEWMODEL DUMP =====");
            int idx = 1;
            foreach (var quote in vm)
            {
                _logger.LogInformation(
                    "   #{Index} QuoteId={Id}, Status={Status}, GrandTotal={GrandTotal}, " +
                    "Project={ProjectName} ({ProjectId}), Client={ClientName} ({ClientId}), " +
                    "Email={Email}, Phone={Phone}, CreatedAt={CreatedAt}, UpdatedAt={UpdatedAt}",
                    idx++,
                    quote.QuotationId,
                    quote.Status,
                    quote.GrandTotal,
                    quote.ProjectName,
                    quote.ProjectId,
                    quote.ClientName,
                    quote.ClientId,
                    quote.ClientEmail,
                    quote.ClientPhone,
                    quote.CreatedAt,
                    quote.UpdatedAt
                );
            }
            _logger.LogInformation("===== END INDEX VIEWMODEL DUMP =====");

            // === 6. Return Index view ===
            return View("Index", vm);
        }

        // ============================
        // DUPLICATE QUOTE (VIEWMODEL ONLY)
        // ============================
        [HttpGet("duplicate/{id}")]
        public async Task<IActionResult> Duplicate(string id)
        {
            _logger.LogInformation("🔄 Preparing duplication of quotation {Id} (no save yet)...", id);

            try
            {
                // 1. Load the existing quotation
                var original = await _apiClient.GetAsync<QuotationDto>($"/api/quotations/{id}", User);
                if (original == null)
                {
                    _logger.LogWarning("⚠️ Quotation {Id} not found for duplication", id);
                    return NotFound();
                }

                // 2. Fetch project + client lists so we can resolve names
                var projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                                ?? new List<ProjectDto>();
                var clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                                ?? new List<UserDto>();

                var project = projects.FirstOrDefault(p => p.ProjectId == original.ProjectId);
                var client  = clients.FirstOrDefault(c => c.UserId == original.ClientId);

                _logger.LogInformation("✅ Loaded quotation {Id}: ProjectId={ProjectId}, ClientId={ClientId}, Items={ItemCount}",
                    id, original.ProjectId ?? "<null>", original.ClientId ?? "<null>", original.Items?.Count ?? 0);

                // 3. Build prefilled EstimateViewModel
                var vm = new EstimateViewModel
                {
                    // 🚨 Force this as a NEW quote (blank QuotationId means POST branch will fire)
                    QuotationId   = string.Empty,

                    ProjectId     = original.ProjectId ?? string.Empty,
                    ClientId      = original.ClientId ?? string.Empty,
                    ContractorId  = original.ContractorId ?? string.Empty,
                    Description   = $"[DUPLICATE of {id}] {original.Description ?? string.Empty}",

                    Items = original.Items?.Select(i => new EstimateLineItemDto
                    {
                        ItemId             = null, // 🚨 Force new IDs on save
                        Name               = i.Name,
                        Description        = i.Description,
                        Quantity           = i.Quantity,
                        Unit               = i.Unit,
                        Category           = i.Category,
                        UnitPrice          = i.UnitPrice,
                        LineTotal          = i.LineTotal,
                        IsAiGenerated      = i.IsAiGenerated,
                        AiConfidence       = i.AiConfidence ?? 0.0,
                        MaterialDatabaseId = i.MaterialDatabaseId,
                        Notes              = i.Notes
                    }).ToList() ?? new List<EstimateLineItemDto>(),

                    // ✅ Default to Draft setup
                    TaxRate    = original.Items?.FirstOrDefault()?.TaxRate ?? 15, 
                    MarkupRate = 20,
                    ValidUntil = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(30), DateTimeKind.Utc),

                    // ✅ Real names pulled from associations
                    ProjectName = project?.Name ?? "Unknown Project",
                    ClientName  = client?.FullName ?? "Unknown Client"
                };

                // Force recalculation for duplicate
                vm.RecalculateTotals();

                _logger.LogInformation("📦 Prefilled EstimateViewModel ready for DUPLICATE: {ProjectName}, {ClientName}, {Count} items",
                    vm.ProjectName, vm.ClientName, vm.Items.Count);

                _logger.LogInformation("===== 🧾 DUPLICATE VIEWMODEL DUMP =====");
                _logger.LogInformation("QuotationId   : {QuotationId}", vm.QuotationId);
                _logger.LogInformation("ProjectId     : {ProjectId}", vm.ProjectId);
                _logger.LogInformation("ClientId      : {ClientId}", vm.ClientId);
                _logger.LogInformation("ContractorId  : {ContractorId}", vm.ContractorId);
                _logger.LogInformation("Description   : {Description}", vm.Description);
                _logger.LogInformation("ValidUntil    : {ValidUntil} (Kind={Kind})", vm.ValidUntil, vm.ValidUntil.Kind);
                _logger.LogInformation("TaxRate       : {TaxRate}", vm.TaxRate);
                _logger.LogInformation("MarkupRate    : {MarkupRate}", vm.MarkupRate);
                _logger.LogInformation("Subtotal      : {Subtotal}", vm.Subtotal);
                _logger.LogInformation("TaxAmount     : {TaxAmount}", vm.TaxAmount);
                _logger.LogInformation("GrandTotal    : {GrandTotal}", vm.GrandTotal);

                if (vm.Items.Count == 0)
                {
                    _logger.LogInformation("Line Items    : NONE");
                }
                else
                {
                    int idx = 1;
                    foreach (var item in vm.Items)
                    {
                        _logger.LogInformation(
                            "   #{Index} → Name={Name}, Desc={Desc}, Qty={Qty}, Unit={Unit}, Category={Category}, " +
                            "UnitPrice={UnitPrice}, LineTotal={LineTotal}, Notes={Notes}, TaxRate={TaxRate}",
                            idx++, item.Name, item.Description, item.Quantity, item.Unit,
                            item.Category, item.UnitPrice, item.LineTotal, item.Notes, vm.TaxRate
                        );
                    }
                }
                _logger.LogInformation("===== END DUPLICATE VIEWMODEL DUMP =====");

                return View("Estimate", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Exception while preparing duplication {Id}", id);
                return RedirectToAction("Index");
            }
        }






    }
}
