using System.IO;
using System.Text;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using ICCMS_Web.Models;
using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ICCMS_Web.Controllers
{
    [Route("[controller]")]
    public class QuotesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<QuotesController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConverter _pdfConverter;
        private readonly ICompositeViewEngine _viewEngine;

        public QuotesController(
            IApiClient apiClient,
            ILogger<QuotesController> logger,
            IWebHostEnvironment env,
            IConverter pdfConverter,
            ICompositeViewEngine viewEngine
        )
        {
            _apiClient = apiClient;
            _logger = logger;
            _env = env;
            _pdfConverter = pdfConverter;
            _viewEngine = viewEngine;
        }

        // ============================
        // STEP 0: CREATE DRAFT
        // ============================

        [HttpGet("create-draft")]
        public async Task<IActionResult> CreateDraft()
        {
            _logger.LogInformation("Displaying draft quotation form...");

            // Fetch Projects + Clients securely via _apiClient (with User token)
            var projects =
                await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                ?? new List<ProjectDto>();
            var clients =
                await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                ?? new List<UserDto>();

            // Build the ViewModel for Razor
            var vm = new CreateDraftViewModel
            {
                Projects = projects,
                Clients = clients,
                ValidUntil = DateTime.UtcNow.AddDays(30),
            };

            _logger.LogInformation(
                "Loaded {P} projects and {C} clients for CreateDraft form",
                projects.Count,
                clients.Count
            );

            return View("CreateDraft", vm);
        }

        [HttpPost("create-draft")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDraft(CreateDraftViewModel model)
        {
            _logger.LogInformation(
                $"📝 Submitting new draft quotation → Project={model.ProjectId}, Client={model.ClientId}"
            );

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
                    _logger.LogWarning(
                        "⚠️ Validation failed for draft quotation — repopulating dropdowns..."
                    );

                    // Re-populate dropdowns before returning view
                    model.Projects =
                        await _apiClient.GetAsync<List<ProjectDto>>(
                            "/api/projectmanager/projects",
                            User
                        ) ?? new List<ProjectDto>();
                    model.Clients =
                        await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
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
                    UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                };

                // === POST to API ===
                var createdId = await _apiClient.PostAsync<string>(
                    "/api/quotations",
                    quotation,
                    User
                );

                if (!string.IsNullOrWhiteSpace(createdId))
                {
                    // 🔹 Force the QuotationId to the Firestore doc ID we just got back
                    quotation.QuotationId = createdId;

                    _logger.LogInformation(
                        $"🎉 Draft quotation created successfully with Firestore ID={createdId}"
                    );

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
            model.Projects =
                await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                ?? new List<ProjectDto>();
            model.Clients =
                await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
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

            _logger.LogWarning(
                "API QuotationDto dump: Id={Id}, ProjectId={ProjectId}, ClientId={ClientId}, Desc={Desc}, Items={Items}",
                quotation.QuotationId ?? "<null>",
                quotation.ProjectId ?? "<null>",
                quotation.ClientId ?? "<null>",
                quotation.Description ?? "<null>",
                quotation.Items?.Count ?? 0
            );

            // 🔹 Ensure QuotationId is set
            if (string.IsNullOrWhiteSpace(quotation.QuotationId))
            {
                _logger.LogWarning("⚠️ QuotationDto had no ID — falling back to route ID={Id}", id);
                quotation.QuotationId = id;
            }

            _logger.LogInformation(
                "📦 Quotation fetched: Id={Id}, Desc={Desc}, Items={Items}",
                quotation.QuotationId ?? "<empty>",
                quotation.Description ?? "<none>",
                quotation.Items?.Count ?? 0
            );

            // 🔹 Fetch related Project + Client lists
            var projects =
                await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                ?? new List<ProjectDto>();
            var clients =
                await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                ?? new List<UserDto>();

            var project = projects.FirstOrDefault(p => p.ProjectId == quotation.ProjectId);
            var client = clients.FirstOrDefault(c => c.UserId == quotation.ClientId);

            _logger.LogInformation(
                "🔍 Quotation raw IDs → ProjectId={ProjectId}, ClientId={ClientId}",
                quotation.ProjectId ?? "<null>",
                quotation.ClientId ?? "<null>"
            );
            _logger.LogInformation(
                "🔍 Available Projects → {Projects}",
                string.Join(", ", projects.Select(p => p.ProjectId + ":" + p.Name))
            );
            _logger.LogInformation(
                "🔍 Available Clients → {Clients}",
                string.Join(", ", clients.Select(c => c.UserId + ":" + c.FullName))
            );

            // 🔹 Build VM
            var vm = new EstimateViewModel
            {
                QuotationId = string.IsNullOrWhiteSpace(quotation.QuotationId)
                    ? id
                    : quotation.QuotationId,
                ProjectId = quotation.ProjectId ?? string.Empty,
                ClientId = quotation.ClientId ?? string.Empty,
                ContractorId = string.IsNullOrWhiteSpace(quotation.ContractorId)
                    ? "N/A"
                    : quotation.ContractorId,
                Description = quotation.Description,
                Items =
                    quotation
                        .Items?.Select(q => new EstimateLineItemDto
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
                            Notes = string.IsNullOrWhiteSpace(q.Notes) ? "-" : q.Notes,
                        })
                        .ToList() ?? new List<EstimateLineItemDto>(),

                TaxRate = 15,
                MarkupRate = 20,
                ValidUntil =
                    quotation.ValidUntil != default
                        ? DateTime.SpecifyKind(quotation.ValidUntil, DateTimeKind.Utc)
                        : DateTime.UtcNow.AddDays(30),

                // ✅ Safer fallback: use quotation's own names if project/client not resolved
                ProjectName = project?.Name ?? "Unknown Project",
                ClientName = client?.FullName ?? "Unknown Client",
            };
            _logger.LogInformation(
                "🚨 [GET] Estimate built with TaxRate={TaxRate}, MarkupRate={MarkupRate}",
                vm.TaxRate,
                vm.MarkupRate
            );

            // 🔹 Blueprint handling
            if (fromBlueprint)
            {
                _logger.LogInformation(
                    "📐 Blueprint flag active — sending for AI estimate generation..."
                );

                var blueprintEstimate = await _apiClient.PostAsync<EstimateViewModel>(
                    "/api/estimates/process-blueprint",
                    new
                    {
                        blueprintUrl = "https://example.com/sample.pdf",
                        projectId = quotation.ProjectId,
                        contractorId = quotation.ContractorId,
                    },
                    User
                );

                if (blueprintEstimate?.Items?.Any() == true)
                {
                    vm.Items = blueprintEstimate.Items;
                    vm.Description = "AI-generated from blueprint";
                    _logger.LogInformation(
                        "🤖 Blueprint processing succeeded — {Count} items generated",
                        vm.Items.Count
                    );
                }
                else
                {
                    _logger.LogWarning("🤖 Blueprint processing returned no items.");
                }
            }

            _logger.LogInformation(
                "DEBUG 🕵️ ViewModel built with QuotationId={Id}",
                vm.QuotationId ?? "<null>"
            );

            vm.RecalculateTotals();
            _logger.LogInformation(
                "🧮 Totals recalculated: Subtotal={Subtotal}, Tax={Tax}, Markup={Markup}, Grand={Grand}",
                vm.Subtotal,
                vm.TaxAmount,
                vm.MarkupAmount,
                vm.GrandTotal
            );

            _logger.LogInformation("===== 🧾 FULL ESTIMATE VIEWMODEL DUMP =====");
            _logger.LogInformation("QuotationId   : {QuotationId}", vm.QuotationId);
            _logger.LogInformation("ProjectId     : {ProjectId}", vm.ProjectId);
            _logger.LogInformation("ClientId      : {ClientId}", vm.ClientId);
            _logger.LogInformation("ContractorId  : {ContractorId}", vm.ContractorId);
            _logger.LogInformation("Description   : {Description}", vm.Description);
            _logger.LogInformation(
                "ValidUntil    : {ValidUntil} (Kind={Kind})",
                vm.ValidUntil,
                vm.ValidUntil.Kind
            );
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
                        "   #{Index} → ItemId={ItemId}, Name={Name}, Desc={Desc}, Qty={Qty}, Unit={Unit}, "
                            + "Category={Category}, UnitPrice={UnitPrice}, LineTotal={LineTotal}, "
                            + "IsAI={IsAiGenerated}, Confidence={AiConfidence}, DBRef={MaterialDatabaseId}, Notes={Notes}",
                        index++,
                        item.ItemId,
                        item.Name,
                        item.Description,
                        item.Quantity,
                        item.Unit,
                        item.Category,
                        item.UnitPrice,
                        item.LineTotal,
                        item.IsAiGenerated,
                        item.AiConfidence,
                        item.MaterialDatabaseId,
                        item.Notes
                    );
                }
            }
            _logger.LogInformation("===== END OF ESTIMATE VIEWMODEL DUMP =====");

            _logger.LogWarning(
                "🚨 DEBUG: Passing VM to view with Contractor ID ={ContractorId}",
                vm.ContractorId ?? "<null>"
            );

            return View("Estimate", vm);
        }

        [HttpPost("process-blueprint")]
        public async Task<IActionResult> ProcessBlueprint([FromBody] BlueprintRequest request)
        {
            _logger.LogInformation(
                "Processing blueprint for Project {ProjectId}",
                request.ProjectId
            );

            // Call API with the current user token
            var result = await _apiClient.PostAsync<EstimateViewModel>(
                "/api/estimates/process-blueprint",
                new { blueprintUrl = request.BlueprintUrl, projectId = request.ProjectId },
                User
            );

            if (result == null)
            {
                _logger.LogError(
                    "Blueprint processing failed for Project {ProjectId}",
                    request.ProjectId
                );
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
            var allQuotes =
                await _apiClient.GetAsync<List<QuotationDto>>("/api/quotations", User)
                ?? new List<QuotationDto>();
            _logger.LogInformation("✅ Retrieved {Q} base quotations", allQuotes.Count);

            // === 2. Fetch Projects + Clients for enrichment lookups ===
            _logger.LogInformation("Fetching related Projects and Clients...");
            var allProjects =
                await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                ?? new List<ProjectDto>();
            var allClients =
                await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                ?? new List<UserDto>();
            _logger.LogInformation(
                "✅ Retrieved {P} projects and {C} clients for enrichment",
                allProjects.Count,
                allClients.Count
            );

            // === 3. Enrich each quotation individually (get full doc by QuotationId) ===
            var enrichedQuotes = new List<QuotationDto>();

            foreach (var q in allQuotes)
            {
                _logger.LogInformation("🔍 Enriching Quote {Id}...", q.QuotationId);

                try
                {
                    var fullQuote = await _apiClient.GetAsync<QuotationDto>(
                        $"/api/quotations/{q.QuotationId}",
                        User
                    );

                    if (fullQuote == null)
                    {
                        _logger.LogWarning(
                            "⚠️ Could not enrich Quote {Id} (API returned null)",
                            q.QuotationId
                        );
                        enrichedQuotes.Add(q); // fallback to base quote
                    }
                    else
                    {
                        _logger.LogInformation(
                            "✅ Enriched Quote {Id}: ProjectId={ProjectId}, ClientId={ClientId}",
                            fullQuote.QuotationId ?? "<null>",
                            fullQuote.ProjectId ?? "<null>",
                            fullQuote.ClientId ?? "<null>"
                        );
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
            var vm = enrichedQuotes
                .Select(q =>
                {
                    var project = allProjects.FirstOrDefault(p => p.ProjectId == q.ProjectId);
                    var client = allClients.FirstOrDefault(c => c.UserId == q.ClientId);

                    _logger.LogInformation(
                        "Quote {Id} → ClientId={ClientId}, FoundClient={FoundClient}, ProjectId={ProjectId}, FoundProject={FoundProject}",
                        q.QuotationId,
                        q.ClientId ?? "<null>",
                        client?.FullName ?? "❌ not found",
                        q.ProjectId ?? "<null>",
                        project?.Name ?? "❌ not found"
                    );

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
                        ClientPhone = client?.Phone ?? "",
                    };
                })
                .ToList();

            // === 5. Debug dump of final mapped ViewModel ===
            _logger.LogInformation("===== 🕵️ DEBUG: FULL INDEX VIEWMODEL DUMP =====");
            int idx = 1;
            foreach (var quote in vm)
            {
                _logger.LogInformation(
                    "   #{Index} QuoteId={Id}, Status={Status}, GrandTotal={GrandTotal}, "
                        + "Project={ProjectName} ({ProjectId}), Client={ClientName} ({ClientId}), "
                        + "Email={Email}, Phone={Phone}, CreatedAt={CreatedAt}, UpdatedAt={UpdatedAt}",
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
            _logger.LogInformation(
                "🔄 Preparing duplication of quotation {Id} (no save yet)...",
                id
            );

            try
            {
                // 1. Load the existing quotation
                var original = await _apiClient.GetAsync<QuotationDto>(
                    $"/api/quotations/{id}",
                    User
                );
                if (original == null)
                {
                    _logger.LogWarning("⚠️ Quotation {Id} not found for duplication", id);
                    return NotFound();
                }

                // 2. Fetch project + client lists so we can resolve names
                var projects =
                    await _apiClient.GetAsync<List<ProjectDto>>(
                        "/api/projectmanager/projects",
                        User
                    ) ?? new List<ProjectDto>();
                var clients =
                    await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                    ?? new List<UserDto>();

                var project = projects.FirstOrDefault(p => p.ProjectId == original.ProjectId);
                var client = clients.FirstOrDefault(c => c.UserId == original.ClientId);

                _logger.LogInformation(
                    "✅ Loaded quotation {Id}: ProjectId={ProjectId}, ClientId={ClientId}, Items={ItemCount}",
                    id,
                    original.ProjectId ?? "<null>",
                    original.ClientId ?? "<null>",
                    original.Items?.Count ?? 0
                );

                // 3. Build prefilled EstimateViewModel
                var vm = new EstimateViewModel
                {
                    // 🚨 Force this as a NEW quote (blank QuotationId means POST branch will fire)
                    QuotationId = string.Empty,

                    ProjectId = original.ProjectId ?? string.Empty,
                    ClientId = original.ClientId ?? string.Empty,
                    ContractorId = original.ContractorId ?? string.Empty,
                    Description = $"[DUPLICATE of {id}] {original.Description ?? string.Empty}",

                    Items =
                        original
                            .Items?.Select(i => new EstimateLineItemDto
                            {
                                ItemId = null, // 🚨 Force new IDs on save
                                Name = i.Name,
                                Description = i.Description,
                                Quantity = i.Quantity,
                                Unit = i.Unit,
                                Category = i.Category,
                                UnitPrice = i.UnitPrice,
                                LineTotal = i.LineTotal,
                                IsAiGenerated = i.IsAiGenerated,
                                AiConfidence = i.AiConfidence ?? 0.0,
                                MaterialDatabaseId = i.MaterialDatabaseId,
                                Notes = i.Notes,
                            })
                            .ToList() ?? new List<EstimateLineItemDto>(),

                    // ✅ Default to Draft setup
                    TaxRate = original.Items?.FirstOrDefault()?.TaxRate ?? 15,
                    MarkupRate = 20,
                    ValidUntil = DateTime.SpecifyKind(
                        DateTime.UtcNow.AddDays(30),
                        DateTimeKind.Utc
                    ),

                    // ✅ Real names pulled from associations
                    ProjectName = project?.Name ?? "Unknown Project",
                    ClientName = client?.FullName ?? "Unknown Client",
                };

                // Force recalculation for duplicate
                vm.RecalculateTotals();

                _logger.LogInformation(
                    "📦 Prefilled EstimateViewModel ready for DUPLICATE: {ProjectName}, {ClientName}, {Count} items",
                    vm.ProjectName,
                    vm.ClientName,
                    vm.Items.Count
                );

                _logger.LogInformation("===== 🧾 DUPLICATE VIEWMODEL DUMP =====");
                _logger.LogInformation("QuotationId   : {QuotationId}", vm.QuotationId);
                _logger.LogInformation("ProjectId     : {ProjectId}", vm.ProjectId);
                _logger.LogInformation("ClientId      : {ClientId}", vm.ClientId);
                _logger.LogInformation("ContractorId  : {ContractorId}", vm.ContractorId);
                _logger.LogInformation("Description   : {Description}", vm.Description);
                _logger.LogInformation(
                    "ValidUntil    : {ValidUntil} (Kind={Kind})",
                    vm.ValidUntil,
                    vm.ValidUntil.Kind
                );
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
                            "   #{Index} → Name={Name}, Desc={Desc}, Qty={Qty}, Unit={Unit}, Category={Category}, "
                                + "UnitPrice={UnitPrice}, LineTotal={LineTotal}, Notes={Notes}, TaxRate={TaxRate}",
                            idx++,
                            item.Name,
                            item.Description,
                            item.Quantity,
                            item.Unit,
                            item.Category,
                            item.UnitPrice,
                            item.LineTotal,
                            item.Notes,
                            vm.TaxRate
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

        // ============================================
        // PROJECT MANAGER: Download Quotation (SA Format)
        // ============================================
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadQuote(string id)
        {
            _logger.LogInformation("🧾 [PM-DownloadQuote] Generating PDF for quotation {Id}", id);

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // === FETCH QUOTATION ===
            var quote = await _apiClient.GetAsync<QuotationDto>($"/api/quotations/{id}", User);
            if (quote == null)
                return NotFound("Quotation not found");

            // === FETCH PROJECT FROM PROJECT MANAGER SCOPE ===
            var allProjects =
                await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                ?? new List<ProjectDto>();
            var project = allProjects.FirstOrDefault(p => p.ProjectId == quote.ProjectId);
            if (project == null)
                return NotFound("Associated project not found");

            // === FETCH CLIENT INFO ===
            var client =
                await _apiClient.GetAsync<UserDto>($"/api/users/{project.ClientId}", User)
                ?? new UserDto { FullName = "Unknown Client" };

            // === BUILD PDF ===
            var fileBytes = Document
                .Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.PageColor("#FFFFFF");
                        page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(11));

                        // ===== HEADER =====
                        page.Header()
                            .BorderBottom(1)
                            .BorderColor("#F7EC59")
                            .PaddingBottom(6)
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Column(col =>
                                    {
                                        col.Item()
                                            .Text("TASKIT")
                                            .FontSize(22)
                                            .Bold()
                                            .FontColor("#1A1B25");
                                        col.Item()
                                            .Text(
                                                "Integrated Construction & Maintenance Management System"
                                            )
                                            .FontSize(10)
                                            .FontColor("#666");
                                        col.Item()
                                            .Text("support@taskit.co.za | +27 87 123 4567")
                                            .FontSize(9)
                                            .FontColor("#888");
                                    });

                                row.ConstantItem(90)
                                    .AlignRight()
                                    .Height(40)
                                    .Element(e => e.Image("wwwroot/images/TaskIt3.png").FitWidth());
                            });

                        // ===== CONTENT =====
                        page.Content()
                            .PaddingVertical(20)
                            .Column(stack =>
                            {
                                stack.Spacing(15);

                                // --- Title ---
                                stack
                                    .Item()
                                    .AlignCenter()
                                    .Text($"QUOTATION #{quote.QuotationId}")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor("#1A1B25");

                                // --- Summary Section ---
                                stack
                                    .Item()
                                    .Border(1)
                                    .BorderColor("#F7EC59")
                                    .Background("#FFFDE7")
                                    .Padding(10)
                                    .Column(info =>
                                    {
                                        info.Spacing(3);
                                        info.Item()
                                            .Text($"Client: {client.FullName}")
                                            .FontColor("#111");
                                        info.Item()
                                            .Text($"Project: {project.Name}")
                                            .FontColor("#111");
                                        if (!string.IsNullOrWhiteSpace(project.Description))
                                            info.Item()
                                                .Text($"Project Description: {project.Description}")
                                                .FontSize(10)
                                                .FontColor("#444");
                                        info.Item()
                                            .Text($"Issued: {quote.CreatedAt:dd MMM yyyy}")
                                            .FontColor("#333");
                                        info.Item()
                                            .Text($"Valid Until: {quote.ValidUntil:dd MMM yyyy}")
                                            .FontColor("#333");
                                        info.Item()
                                            .Text($"Planned Budget: R {project.BudgetPlanned:N2}")
                                            .FontColor("#000")
                                            .Bold();
                                    });

                                // --- Line Items Table ---
                                stack
                                    .Item()
                                    .PaddingTop(10)
                                    .Element(e =>
                                    {
                                        e.Table(table =>
                                        {
                                            table.ColumnsDefinition(cols =>
                                            {
                                                cols.RelativeColumn(5);
                                                cols.RelativeColumn(1);
                                                cols.RelativeColumn(2);
                                                cols.RelativeColumn(2);
                                            });

                                            table.Header(h =>
                                            {
                                                AddHeader(h, "Item");
                                                AddHeader(h, "Qty");
                                                AddHeader(h, "Unit Price (R)");
                                                AddHeader(h, "Total (R)");
                                            });

                                            if (quote.Items?.Any() == true)
                                            {
                                                foreach (var item in quote.Items)
                                                {
                                                    AddCell(table, item.Name);
                                                    AddCell(
                                                        table,
                                                        item.Quantity % 1 == 0
                                                            ? ((int)item.Quantity).ToString()
                                                            : item.Quantity.ToString("0.##")
                                                    );
                                                    AddCell(table, item.UnitPrice.ToString("N2"));
                                                    AddCell(table, item.LineTotal.ToString("N2"));
                                                }
                                            }
                                            else
                                            {
                                                table
                                                    .Cell()
                                                    .ColumnSpan(4)
                                                    .AlignCenter()
                                                    .Text("No line items available")
                                                    .FontColor("#777")
                                                    .Italic();
                                            }
                                        });
                                    });

                                // --- Totals Section ---
                                stack
                                    .Item()
                                    .PaddingTop(15)
                                    .AlignRight()
                                    .Column(tot =>
                                    {
                                        tot.Item()
                                            .Text($"Subtotal: R {quote.Subtotal:N2}")
                                            .FontSize(11);
                                        tot.Item()
                                            .Text($"Tax (15%): R {quote.TaxTotal:N2}")
                                            .FontSize(11);
                                        tot.Item()
                                            .Text($"Grand Total: R {quote.GrandTotal:N2}")
                                            .Bold()
                                            .FontSize(13)
                                            .FontColor("#000");
                                    });

                                // --- Banking Details ---
                                stack
                                    .Item()
                                    .PaddingTop(25)
                                    .BorderTop(1)
                                    .BorderColor("#EEE")
                                    .PaddingTop(10)
                                    .Column(bank =>
                                    {
                                        bank.Item()
                                            .Text("BANKING DETAILS")
                                            .Bold()
                                            .FontSize(12)
                                            .FontColor("#1A1B25");
                                        bank.Item()
                                            .Text("Bank: FNB | Acc No: 0000000000 | Branch: 250655")
                                            .FontSize(10)
                                            .FontColor("#333");
                                        bank.Item()
                                            .Text($"Reference: {project.Name}")
                                            .FontSize(10)
                                            .FontColor("#333");
                                        bank.Item()
                                            .Text("Email proof of payment to accounts@taskit.co.za")
                                            .FontSize(9)
                                            .FontColor("#555");
                                    });
                            });

                        // ===== FOOTER =====
                        page.Footer()
                            .BorderTop(1)
                            .BorderColor("#F7EC59")
                            .PaddingTop(8)
                            .AlignCenter()
                            .Text("Thank you for choosing TaskIt — powered by ICCMS")
                            .FontSize(9)
                            .FontColor("#666");
                    });

                    // ===== LOCAL HELPERS =====
                    static void AddHeader(TableCellDescriptor h, string text)
                    {
                        h.Cell()
                            .Background("#F7EC59")
                            .Padding(5)
                            .AlignCenter()
                            .Text(text)
                            .Bold()
                            .FontColor("#1A1B25");
                    }

                    static void AddCell(TableDescriptor table, string? text)
                    {
                        table
                            .Cell()
                            .BorderBottom(0.5f)
                            .BorderColor("#EEE")
                            .PaddingVertical(4)
                            .PaddingHorizontal(3)
                            .Text(text ?? "—")
                            .FontColor("#111");
                    }
                })
                .GeneratePdf();

            _logger.LogInformation("✅ [PM-DownloadQuote] PDF generated for quotation {Id}", id);
            return File(
                fileBytes,
                "application/pdf",
                $"Quotation_{project.Name.Replace(" ", "_")}.pdf"
            );
        }

        // ============================================
        // PROJECT MANAGER: Download Invoice (Professional SA Format)
        // ============================================
        [HttpGet("download-invoice/{id}")]
        public async Task<IActionResult> DownloadInvoice(string id)
        {
            _logger.LogInformation(
                "📄 [PM-DownloadInvoice] Generating professional PDF for invoice {Id}",
                id
            );

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // === FETCH INVOICE ===
            var invoice = await _apiClient.GetAsync<InvoiceDto>($"/api/invoices/{id}", User);
            if (invoice == null)
            {
                _logger.LogWarning("❌ Invoice not found for ID {Id}", id);
                return NotFound("Invoice not found");
            }

            // === FETCH PROJECT ===
            var allProjects =
                await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                ?? new List<ProjectDto>();
            var project = allProjects.FirstOrDefault(p => p.ProjectId == invoice.ProjectId);
            if (project == null)
            {
                _logger.LogWarning("⚠️ No project found for invoice {Id}", id);
                return NotFound("Associated project not found");
            }

            // === FETCH CLIENT ===
            var client =
                await _apiClient.GetAsync<UserDto>($"/api/users/{invoice.ClientId}", User)
                ?? new UserDto { FullName = "Unknown Client" };

            // === FETCH RELATED QUOTATION ===
            QuotationDto? quote = null;
            if (!string.IsNullOrWhiteSpace(invoice.QuotationId))
            {
                quote = await _apiClient.GetAsync<QuotationDto>(
                    $"/api/quotations/{invoice.QuotationId}",
                    User
                );
            }

            // === DETERMINE STATUS COLOR ===
            string statusColor = invoice.Status.ToLower() switch
            {
                "paid" => "#4CAF50",
                "overdue" => "#E53935",
                "issued" => "#FBC02D",
                _ => "#757575",
            };

            // === BUILD PDF ===
            var fileBytes = Document
                .Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.PageColor("#FFFFFF");
                        page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(11));

                        // ===== HEADER =====
                        page.Header()
                            .BorderBottom(1)
                            .BorderColor("#F7EC59")
                            .PaddingBottom(6)
                            .Row(row =>
                            {
                                row.RelativeItem()
                                    .Column(col =>
                                    {
                                        col.Item()
                                            .Text("TASKIT")
                                            .FontSize(22)
                                            .Bold()
                                            .FontColor("#1A1B25");
                                        col.Item()
                                            .Text(
                                                "Integrated Construction & Maintenance Management System"
                                            )
                                            .FontSize(10)
                                            .FontColor("#666");
                                        col.Item()
                                            .Text("support@taskit.co.za | +27 87 123 4567")
                                            .FontSize(9)
                                            .FontColor("#888");
                                    });

                                row.ConstantItem(90)
                                    .AlignRight()
                                    .Height(40)
                                    .Element(e => e.Image("wwwroot/images/TaskIt3.png").FitWidth());
                            });

                        // ===== CONTENT =====
                        page.Content()
                            .PaddingVertical(25)
                            .Column(stack =>
                            {
                                stack.Spacing(15);

                                // --- TITLE & STATUS ---
                                stack
                                    .Item()
                                    .AlignCenter()
                                    .Text($"INVOICE #{invoice.InvoiceNumber}")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor("#1A1B25");
                                stack
                                    .Item()
                                    .AlignCenter()
                                    .Text($"Status: {invoice.Status}")
                                    .FontSize(11)
                                    .Bold()
                                    .FontColor(statusColor);

                                // --- CLIENT & PROJECT SUMMARY ---
                                stack
                                    .Item()
                                    .Border(1)
                                    .BorderColor("#F7EC59")
                                    .Background("#FFFDE7")
                                    .Padding(12)
                                    .Column(info =>
                                    {
                                        info.Spacing(3);
                                        info.Item()
                                            .Text($"Client: {client.FullName}")
                                            .FontColor("#111");
                                        info.Item()
                                            .Text($"Email: {client.Email}")
                                            .FontColor("#111");
                                        if (!string.IsNullOrWhiteSpace(client.Phone))
                                            info.Item()
                                                .Text($"Phone: {client.Phone}")
                                                .FontColor("#111");
                                        info.Item()
                                            .Text($"Project: {project.Name}")
                                            .FontColor("#111");
                                        if (!string.IsNullOrWhiteSpace(project.Description))
                                            info.Item()
                                                .Text($"Project Description: {project.Description}")
                                                .FontSize(10)
                                                .FontColor("#444");
                                        info.Item()
                                            .Text($"Budget: R {project.BudgetPlanned:N2}")
                                            .FontColor("#000");
                                    });

                                // --- QUOTATION REFERENCE ---
                                if (quote != null)
                                {
                                    stack
                                        .Item()
                                        .Border(1)
                                        .BorderColor("#EEE")
                                        .Padding(10)
                                        .Background("#FAFAFA")
                                        .Column(q =>
                                        {
                                            q.Spacing(2);
                                            q.Item()
                                                .Text($"Linked Quote: {quote.QuotationId}")
                                                .FontSize(10)
                                                .FontColor("#444");
                                            q.Item()
                                                .Text(
                                                    $"Quote Accepted On: {quote.ApprovedAt?.ToString("dd MMM yyyy") ?? "Pending"}"
                                                )
                                                .FontSize(10)
                                                .FontColor("#444");
                                            q.Item()
                                                .Text($"Quote Description: {quote.Description}")
                                                .FontSize(10)
                                                .FontColor("#444");
                                        });
                                }

                                // --- DATES SECTION ---
                                stack
                                    .Item()
                                    .PaddingTop(10)
                                    .Column(dates =>
                                    {
                                        dates.Spacing(2);
                                        dates
                                            .Item()
                                            .Text(
                                                $"Invoice Created: {invoice.CreatedAt:dd MMM yyyy}"
                                            )
                                            .FontColor("#333");
                                        dates
                                            .Item()
                                            .Text($"Issued: {invoice.IssuedDate:dd MMM yyyy}")
                                            .FontColor("#333");
                                        dates
                                            .Item()
                                            .Text($"Due Date: {invoice.DueDate:dd MMM yyyy}")
                                            .FontColor("#333");
                                        if (invoice.PaidDate.HasValue)
                                            dates
                                                .Item()
                                                .Text(
                                                    $"Paid On: {invoice.PaidDate.Value:dd MMM yyyy}"
                                                )
                                                .FontColor("#4CAF50");
                                    });

                                // --- DESCRIPTION OF WORK ---
                                if (!string.IsNullOrWhiteSpace(invoice.Description))
                                {
                                    stack
                                        .Item()
                                        .PaddingTop(10)
                                        .Text($"Description of Work: {invoice.Description}")
                                        .FontSize(10)
                                        .FontColor("#444");
                                }

                                // --- FINANCIAL SUMMARY ---
                                stack
                                    .Item()
                                    .PaddingTop(15)
                                    .AlignRight()
                                    .Column(fin =>
                                    {
                                        fin.Item()
                                            .Text($"Subtotal: R {invoice.Subtotal:N2}")
                                            .FontSize(11);
                                        fin.Item()
                                            .Text($"Tax (15%): R {invoice.TaxTotal:N2}")
                                            .FontSize(11);
                                        fin.Item()
                                            .Text($"Total Amount: R {invoice.TotalAmount:N2}")
                                            .Bold()
                                            .FontSize(13)
                                            .FontColor("#000");
                                        fin.Item()
                                            .Text($"Currency: {invoice.Currency}")
                                            .FontSize(10)
                                            .FontColor("#555");
                                    });

                                // --- PAYMENT DETAILS ---
                                stack
                                    .Item()
                                    .PaddingTop(25)
                                    .BorderTop(1)
                                    .BorderColor("#EEE")
                                    .PaddingTop(10)
                                    .Column(bank =>
                                    {
                                        bank.Item()
                                            .Text("PAYMENT DETAILS")
                                            .Bold()
                                            .FontSize(12)
                                            .FontColor("#1A1B25");
                                        bank.Item()
                                            .Text("Bank: FNB | Acc No: 0000000000 | Branch: 250655")
                                            .FontSize(10)
                                            .FontColor("#333");
                                        bank.Item()
                                            .Text($"Reference: {invoice.InvoiceNumber}")
                                            .FontSize(10)
                                            .FontColor("#333");
                                        bank.Item()
                                            .Text("Email proof of payment to accounts@taskit.co.za")
                                            .FontSize(9)
                                            .FontColor("#555");
                                    });
                            });

                        // ===== FOOTER =====
                        page.Footer()
                            .BorderTop(1)
                            .BorderColor("#F7EC59")
                            .PaddingTop(8)
                            .AlignCenter()
                            .Text("Internal document — managed by Project Manager via ICCMS")
                            .FontSize(9)
                            .FontColor("#666");
                    });
                })
                .GeneratePdf();

            _logger.LogInformation("✅ [PM-DownloadInvoice] PDF generated for invoice {Id}", id);
            return File(
                fileBytes,
                "application/pdf",
                $"Invoice_{invoice.InvoiceNumber.Replace(" ", "_")}.pdf"
            );
        }

        // ================================================
        // STEP X: CREATE QUOTE FROM ESTIMATE (AUTO-PREFILL)
        // ================================================
        [HttpPost("create-from-estimate")]
        public async Task<IActionResult> CreateFromEstimate(
            [FromBody] CreateFromEstimateRequest request
        )
        {
            _logger.LogInformation(
                "🧠 [CreateFromEstimate] Start → ProjectId={ProjectId}",
                request.ProjectId
            );

            try
            {
                // 1️⃣ Fetch the latest estimate for the project
                var estimates = await _apiClient.GetAsync<List<EstimateDto>>(
                    $"/api/estimates/project/{request.ProjectId}",
                    User
                );

                if (estimates == null || !estimates.Any())
                {
                    _logger.LogWarning(
                        "⚠️ No estimates found for ProjectId={ProjectId}",
                        request.ProjectId
                    );
                    return BadRequest(new { error = "No estimates found for this project." });
                }

                var latestEstimate = estimates.OrderByDescending(e => e.CreatedAt).First();

                _logger.LogInformation(
                    "📄 Latest Estimate found: Id={EstimateId}, Desc={Desc}, Total={Total}",
                    latestEstimate.EstimateId,
                    latestEstimate.Description,
                    latestEstimate.TotalAmount
                );

                // 2️⃣ Create quotation from estimate via API
                var apiRoute = $"/api/quotations/from-estimate/{latestEstimate.EstimateId}";
                var body = new { clientId = request.ClientId ?? string.Empty };

                var quotationId = await _apiClient.PostAsync<string>(apiRoute, body, User);

                if (string.IsNullOrEmpty(quotationId))
                {
                    _logger.LogError(
                        "❌ API failed to create quotation from EstimateId={EstimateId}",
                        latestEstimate.EstimateId
                    );
                    return StatusCode(500, new { error = "Failed to create quotation." });
                }

                _logger.LogInformation(
                    "✅ Quotation created successfully → Id={QuotationId}",
                    quotationId
                );

                // 3️⃣ Return success response (for AJAX or redirect)
                return Ok(
                    new
                    {
                        message = "Quotation created successfully.",
                        quotationId,
                        estimateId = latestEstimate.EstimateId,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "💥 Exception during CreateFromEstimate for ProjectId={ProjectId}",
                    request.ProjectId
                );
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Helper request model
        public class CreateFromEstimateRequest
        {
            public string ProjectId { get; set; } = string.Empty;
            public string? ClientId { get; set; }
        }

        // ================================================
        // STEP X: SUBMIT QUOTATION (Create + Approve + Send)
        // ================================================
        [HttpPost("submit-quotation")]
        public async Task<IActionResult> SubmitQuotation([FromBody] QuotationDto model)
        {
            _logger.LogInformation(
                "📨 [SubmitQuotation] Start | ProjectId={ProjectId} | ClientId={ClientId}",
                model.ProjectId,
                model.ClientId
            );

            try
            {
                // === VALIDATION ===
                if (
                    string.IsNullOrWhiteSpace(model.ProjectId)
                    || string.IsNullOrWhiteSpace(model.ClientId)
                )
                    return BadRequest(
                        new { error = "Missing required project or client information." }
                    );

                if (model.Items == null || !model.Items.Any())
                    return BadRequest(new { error = "Quotation must contain at least one item." });

                // === SANITIZATION ===
                model.Description ??= "AI-generated estimate from blueprint";
                model.ContractorId ??= User.FindFirst("UserId")?.Value ?? "unknown";
                model.IsAiGenerated = true;
                model.ValidUntil = DateTime.UtcNow.AddDays(30);

                // === NORMALIZE RATES ===
                double markupRate = model.MarkupRate / 100.0;

                // === CALCULATIONS ===
                double subtotal = 0,
                    taxTotal = 0;
                foreach (var item in model.Items)
                {
                    item.LineTotal = item.Quantity * item.UnitPrice;
                    subtotal += item.LineTotal;
                    double itemTaxRate = item.TaxRate / 100.0;
                    taxTotal += (item.LineTotal * markupRate) * itemTaxRate;
                }

                double markupTotal = subtotal * markupRate;
                double grandTotal = subtotal + markupTotal + taxTotal;

                model.Subtotal = subtotal;
                model.TaxTotal = taxTotal;
                model.GrandTotal = grandTotal;
                model.Total = grandTotal;
                model.Status = "Draft";
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "💰 Totals Calculated | Subtotal={Subtotal}, Markup={MarkupTotal}, Tax={TaxTotal}, Grand={GrandTotal}",
                    subtotal,
                    markupTotal,
                    taxTotal,
                    grandTotal
                );

                // === CREATE IN FIRESTORE ===
                string? quotationId = await _apiClient.PostAsync<string>(
                    "/api/quotations",
                    model,
                    User
                );
                if (string.IsNullOrWhiteSpace(quotationId))
                    return StatusCode(
                        500,
                        new { error = "Quotation creation failed. No ID returned." }
                    );

                _logger.LogInformation("✅ Created quotation ID={QuotationId}", quotationId);

                // === STEP 1: SUBMIT FOR APPROVAL ===
                _logger.LogInformation("📤 Submitting quotation {Id} for approval...", quotationId);
                await _apiClient.PostAsync<object>(
                    $"/api/quotations/{quotationId}/submit-for-approval",
                    null,
                    User
                );

                // === STEP 2: PM APPROVAL ===
                _logger.LogInformation("🧾 Approving quotation {Id}...", quotationId);
                await _apiClient.PostAsync<object>(
                    $"/api/quotations/{quotationId}/pm-approve",
                    null,
                    User
                );

                // === STEP 3: SEND TO CLIENT ===
                _logger.LogInformation("📩 Sending quotation {Id} to client...", quotationId);
                await _apiClient.PostAsync<object>(
                    $"/api/quotations/{quotationId}/send-to-client",
                    null,
                    User
                );
                model.Status = "SentToClient";

                _logger.LogInformation(
                    "✅ Quotation {Id} fully processed: Draft → Approval → SentToClient",
                    quotationId
                );

                return Ok(
                    new
                    {
                        message = "Quotation created, approved, and sent to client successfully.",
                        quotationId,
                        status = model.Status,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "🔥 Exception during SubmitQuotation for ProjectId={ProjectId}",
                    model.ProjectId
                );
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ================================================
        // STEP: GetEstimateItems (for Quotation Prefill)
        // ================================================
        [HttpGet]
        public async Task<IActionResult> GetEstimateItems(string projectId)
        {
            _logger.LogInformation(
                "📡 Fetching Estimate items for ProjectId={ProjectId}",
                projectId
            );

            try
            {
                var estimates = await _apiClient.GetAsync<List<EstimateDto>>(
                    $"/api/estimates/project/{projectId}",
                    User
                );

                if (estimates == null || !estimates.Any())
                {
                    _logger.LogWarning(
                        "⚠️ No estimates found for ProjectId={ProjectId}",
                        projectId
                    );
                    return Ok(new List<EstimateLineItemDto>());
                }

                // ✅ Get most recent estimate and its line items
                var latest = estimates.OrderByDescending(e => e.CreatedAt).FirstOrDefault();
                var items = latest?.LineItems ?? new List<EstimateLineItemDto>();

                _logger.LogInformation(
                    "📦 Returning {Count} items from latest estimate",
                    items.Count
                );
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "🔥 Error fetching estimate items for ProjectId={ProjectId}",
                    projectId
                );
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<string> RenderViewAsync<T>(
            string viewName,
            T model,
            bool partial = false
        )
        {
            ViewData.Model = model;
            using var sw = new StringWriter();
            var viewResult = _viewEngine.FindView(ControllerContext, viewName, !partial);
            if (viewResult.View == null)
                throw new ArgumentNullException($"{viewName} not found");
            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                ViewData,
                TempData,
                sw,
                new HtmlHelperOptions()
            );
            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        [HttpPost("simulate-client-approval/{id}")]
        [Authorize(Roles = "Project Manager,Tester")]
        public async Task<IActionResult> SimulateClientApproval(string id)
        {
            try
            {
                var body = new { accept = true, note = "Simulated approval by tester" };
                await _apiClient.PostAsync<object>(
                    $"/api/quotations/{id}/client-decision",
                    body,
                    User
                );
                return Ok(new { message = "Simulated approval successful" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
