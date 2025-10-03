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
            _logger.LogInformation("🔥 Submitting Estimate for Quote {QuotationId}", id);
            _logger.LogInformation("🚨 [POST] Estimate received with TaxRate={TaxRate}, MarkupRate={MarkupRate}", 
                model.TaxRate, model.MarkupRate);

            // 🔹 Check route vs model consistency first
            if (id != model.QuotationId)
            {
                _logger.LogWarning("⚠️ Mismatched quotation ID: route={RouteId}, model={ModelId}", id, model.QuotationId);
                return BadRequest("Mismatched quotation ID.");
            }

            // ✅ Force UTC for ValidUntil before building DTO
            if (model.ValidUntil != default)
            {
                var original = model.ValidUntil;
                model.ValidUntil = DateTime.SpecifyKind(model.ValidUntil, DateTimeKind.Utc);
                _logger.LogInformation("🌍 Normalized ValidUntil from {Original} (Kind={Kind}) → {UtcValue} (UTC)",
                    original, original.Kind, model.ValidUntil);
            }

            // 🔹 Validate BEFORE touching percentages
            if (!ModelState.IsValid)
            {
                foreach (var kvp in ModelState)
                {
                    foreach (var error in kvp.Value.Errors)
                    {
                        _logger.LogWarning("⚠️ Validation error on field '{Field}': {ErrorMessage}", kvp.Key, error.ErrorMessage);
                    }
                }

                _logger.LogWarning("❌ Validation failed for Estimate {QuotationId}", id);
                return View("Estimate", model);
            }

            try
            {
                // ======================
                // ✅ CLEANED CALCULATION
                // ======================

                var taxRateDecimal = model.TaxRate / 100;
                var markupRateDecimal = model.MarkupRate / 100;

                // 1) Apply markup at line-item level
                var markedUpItems = model.Items?.Select(e =>
                {
                    var baseLineTotal = e.Quantity * e.UnitPrice;
                    var finalLineTotal = baseLineTotal * (1 + markupRateDecimal);

                    _logger.LogInformation("💡 Item '{Name}' → Qty={Qty}, UnitPrice={Price}, BaseTotal={Base}, Markup%={MarkupRate}, FinalTotal={Final}",
                        e.Name, e.Quantity, e.UnitPrice, baseLineTotal, model.MarkupRate, finalLineTotal);
                    _logger.LogInformation("DEBUG TAX → Model.TaxRate={TaxRate}", model.TaxRate);


                    return new QuotationItemDto
                    {
                        ItemId = e.ItemId,
                        Name = e.Name,
                        Description = e.Description,
                        Quantity = e.Quantity,
                        Unit = e.Unit,
                        Category = e.Category,
                        UnitPrice = e.UnitPrice,
                        LineTotal = finalLineTotal,   // ✅ with markup baked in

                        IsAiGenerated = e.IsAiGenerated,
                        AiConfidence = e.AiConfidence,
                        MaterialDatabaseId = e.MaterialDatabaseId,
                        Notes = e.Notes,

                        TaxRate = model.TaxRate / 100   // ✅ FIX
                    };


                }).ToList() ?? new List<QuotationItemDto>();


                // 2) Subtotal = sum of marked-up items
                var subtotal = markedUpItems.Sum(i => i.LineTotal);
                _logger.LogInformation("📊 STEP 2 → Subtotal (markup included) = {Subtotal}", subtotal);

                // 3) Tax = subtotal × tax rate
                var taxTotal = subtotal * taxRateDecimal;
                _logger.LogInformation("📊 STEP 3 → Tax {Rate}% on Subtotal {Subtotal} = {TaxTotal}", model.TaxRate, subtotal, taxTotal);

                // 4) GrandTotal = subtotal + tax
                var grandTotal = subtotal + taxTotal;
                _logger.LogInformation("📊 STEP 4 → Grand Total = {Subtotal} + {TaxTotal} = {GrandTotal}", subtotal, taxTotal, grandTotal);

                // === Build Quotation DTO to send to API ===
                var quotation = new QuotationDto
                {
                    QuotationId   = model.QuotationId,

                    // ✅ Preserve associations like CreateDraft
                    ProjectId     = model.ProjectId,
                    ClientId      = model.ClientId,
                    ContractorId  = model.ContractorId,

                    Description   = model.Description,

                    // ✅ Items with markup baked in
                    Items         = markedUpItems,

                    // ✅ Totals
                    Subtotal      = subtotal,
                    TaxTotal      = taxTotal,
                    GrandTotal    = grandTotal,
                    Total         = grandTotal, // Firestore "total" = GrandTotal

                    // ✅ Dates
                    ValidUntil    = model.ValidUntil,
                    CreatedAt     = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    UpdatedAt     = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),

                    Currency      = "ZAR",
                    Status        = "Draft"
                };

                _logger.LogInformation("📤 Sending PUT to API for Quote {QuotationId}", id);
                _logger.LogInformation("===== 🔍 DEBUG BEFORE PUT DTO =====");
                _logger.LogInformation("QuotationId : {QuotationId}", quotation.QuotationId);
                _logger.LogInformation("Subtotal    : {Subtotal}", quotation.Subtotal);
                _logger.LogInformation("TaxTotal    : {TaxTotal}", quotation.TaxTotal);
                _logger.LogInformation("GrandTotal  : {GrandTotal}", quotation.GrandTotal);
                _logger.LogInformation("Total       : {Total}", quotation.Total);
                _logger.LogInformation("===== END DEBUG BEFORE PUT DTO =====");

                // 🔹 1) Save/Update Quotation
                var updated = await _apiClient.PutAsync<object>($"/api/quotations/{id}", quotation, User);

                if (updated != null)
                {
                    _logger.LogWarning("🤔 API returned a body for PUT quotation {QuotationId}, expected 204", id);
                }

                _logger.LogInformation("✅ Successfully updated estimate for quotation {QuotationId}", id);

                // 🔹 2) Submit for Approval
                _logger.LogInformation("🚀 Submitting quotation {QuotationId} for PM approval...", id);
                var submitRes = await _apiClient.PostAsync<QuotationDto>($"/api/quotations/{id}/submit-for-approval", null!, User);
                if (submitRes != null)
                {
                    _logger.LogError("💥 Workflow step failed: submit-for-approval for {QuotationId}", id);
                    ModelState.AddModelError("", "Failed at submit-for-approval step.");
                    return View("Estimate", model);
                }
                _logger.LogInformation("✅ Quotation {QuotationId} moved to PendingPMApproval", id);

                // 🔹 3) PM Approve
                _logger.LogInformation("📝 Auto-approving quotation {QuotationId} by PM...", id);
                var approveRes = await _apiClient.PostAsync<QuotationDto>($"/api/quotations/{id}/pm-approve", null!, User);
                if (approveRes != null)
                {
                    _logger.LogError("💥 Workflow step failed: pm-approve for {QuotationId}", id);
                    ModelState.AddModelError("", "Failed at pm-approve step.");
                    return View("Estimate", model);
                }
                _logger.LogInformation("✅ Quotation {QuotationId} approved by PM", id);

                // 🔹 4) Send to Client
                _logger.LogInformation("📨 Sending quotation {QuotationId} to client...", id);
                var sendRes = await _apiClient.PostAsync<QuotationDto>($"/api/quotations/{id}/send-to-client", null!, User);
                if (sendRes != null)
                {
                    _logger.LogError("💥 Workflow step failed: send-to-client for {QuotationId}", id);
                    ModelState.AddModelError("", "Failed at send-to-client step.");
                    return View("Estimate", model);
                }
                _logger.LogInformation("✅ Quotation {QuotationId} successfully sent to client", id);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Unexpected error during Estimate workflow for {QuotationId}", id);
                ModelState.AddModelError("", $"Unexpected error: {ex.Message}");
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
        // DUPLICATE QUOTE
        // ============================
        [HttpGet("duplicate/{id}")]
        public async Task<IActionResult> Duplicate(string id)
        {
            _logger.LogInformation("🔄 Starting duplication of quotation {Id}...", id);

            try
            {
                // 1. Load the existing quotation
                var original = await _apiClient.GetAsync<QuotationDto>($"/api/quotations/{id}", User);
                if (original == null)
                {
                    _logger.LogWarning("⚠️ Quotation {Id} not found for duplication", id);
                    return NotFound();
                }

                _logger.LogInformation("✅ Loaded quotation {Id} for duplication: ProjectId={ProjectId}, ClientId={ClientId}, Items={ItemCount}",
                    id, original.ProjectId ?? "<null>", original.ClientId ?? "<null>", original.Items?.Count ?? 0);

                // 2. Build a NEW quotation (no reuse of ID or timestamps!)
                var newQuotation = new QuotationDto
                {
                    ProjectId   = original.ProjectId,
                    ClientId    = original.ClientId,
                    ContractorId = original.ContractorId,
                    Description = original.Description,

                    Items       = original.Items?.Select(i => new QuotationItemDto
                    {
                        ItemId              = null, // new Firestore will assign
                        Name                = i.Name,
                        Description         = i.Description,
                        Quantity            = i.Quantity,
                        Unit                = i.Unit,
                        Category            = i.Category,
                        UnitPrice           = i.UnitPrice,
                        TaxRate            = i.TaxRate,
                        LineTotal           = i.LineTotal,
                        IsAiGenerated       = i.IsAiGenerated,
                        AiConfidence        = i.AiConfidence,
                        MaterialDatabaseId  = i.MaterialDatabaseId,
                        Notes               = i.Notes
                    }).ToList() ?? new List<QuotationItemDto>(),

                    Status      = "Draft",
                    Currency    = original.Currency ?? "ZAR",

                    // 🔑 New timestamps
                    CreatedAt   = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    UpdatedAt   = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),

                    // Reset validity
                    ValidUntil  = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(30), DateTimeKind.Utc)
                };

                // 3. Save the NEW quotation via API
                var createdId = await _apiClient.PostAsync<string>("/api/quotations", newQuotation, User);

                if (string.IsNullOrWhiteSpace(createdId))
                {
                    _logger.LogError("💥 Failed to create duplicated quotation from {Id}", id);
                    return RedirectToAction("Index");
                }

                newQuotation.QuotationId = createdId;
                _logger.LogInformation("🎉 Successfully duplicated quotation {OldId} → NewId={NewId}", id, createdId);

                // 4. Redirect straight to Estimate for the NEW quotation
                return RedirectToAction("Estimate", new { id = createdId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Exception while duplicating quotation {Id}", id);
                return RedirectToAction("Index");
            }
        }




    }
}
