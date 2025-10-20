using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ICCMS_Web.Controllers
{
    // Restrict this controller to Project Managers and Testers
    [Authorize(Roles = "Project Manager,Tester")]
    public class ProjectManagerController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ProjectManagerController> _logger;
        private readonly IProjectIndexService _projectIndexService;
        private readonly IEstimatesService _estimatesService;
        private readonly IDocumentsService _documentsService;
        private readonly IQuotationsService _quotationsService;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public ProjectManagerController(
            IApiClient apiClient,
            ILogger<ProjectManagerController> logger,
            IProjectIndexService projectIndexService,
            IEstimatesService estimatesService,
            IDocumentsService documentsService,
            IQuotationsService quotationsService,
            IConfiguration config,
            IHttpClientFactory httpClientFactory
        )
        {
            _apiClient = apiClient;
            _logger = logger;
            _projectIndexService = projectIndexService;
            _estimatesService = estimatesService;
            _documentsService = documentsService;
            _quotationsService = quotationsService;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Simple dashboard that fetches and displays all projects for the current PM
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            _logger.LogInformation(
                "=== [Dashboard] Loading projects for {UserName} ===",
                User.Identity?.Name
            );

            try
            {
                // Fetch all projects for the current project manager
                var allProjects =
                    await _apiClient.GetAsync<List<ProjectDto>>(
                        "/api/projectmanager/projects/all",
                        User
                    ) ?? new();

                _logger.LogInformation("📊 Retrieved {Count} projects", allProjects.Count);

                // Build/update in-memory index for fast search/filter (per user)
                var userId = User.Identity?.Name ?? "anonymous";
                _projectIndexService.BuildOrUpdateIndex(userId, allProjects);

                // Create simple view model
                // Enrich projects with client names for UI convenience
                var clients =
                    await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User) ?? new();
                var clientMap = clients.ToDictionary(c => c.UserId, c => c.FullName);
                foreach (var p in allProjects)
                {
                    if (clientMap.TryGetValue(p.ClientId ?? string.Empty, out var fullName))
                    {
                        // Store on a dynamic bag via ViewBag mapping at render time
                        // or rely on ViewBag.ClientName when rendering each card
                    }
                }

                var vm = new DashboardViewModel
                {
                    DraftProjects = allProjects.Where(p => p.Status == "Draft").ToList(),
                    FilteredProjects = allProjects.Where(p => p.Status != "Draft").ToList(),
                    TotalProjects = allProjects.Count,
                    Clients = clients,
                };

                _logger.LogInformation("✅ Dashboard ready with {Total} projects", vm.TotalProjects);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading dashboard");
                ViewBag.ErrorMessage = "Failed to load projects. Please try again later.";
                return View("Error");
            }
        }

        /// <summary>
        /// JSON endpoint for fast in-memory search and filtering of projects (AJAX-friendly).
        /// Builds the index lazily if it doesn't exist for the current user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchProjects(
            string? q = null,
            string? status = null,
            string? clientId = null
        )
        {
            try
            {
                var userId = User.Identity?.Name ?? "anonymous";

                // Ensure index exists (lazy build) in case this endpoint is hit before Dashboard
                var index = _projectIndexService.GetIndex(userId);
                if (index == null)
                {
                    var allProjects =
                        await _apiClient.GetAsync<List<ProjectDto>>(
                            "/api/projectmanager/projects/all",
                            User
                        ) ?? new();
                    _projectIndexService.BuildOrUpdateIndex(userId, allProjects);
                }

                var normalized = (status ?? string.Empty).Trim();
                if (string.Equals(normalized, "All", StringComparison.OrdinalIgnoreCase))
                {
                    normalized = string.Empty; // treat All as no status filter
                }

                var results = _projectIndexService.Search(userId, q, normalized, clientId).ToList();

                var drafts = results
                    .Where(p =>
                        string.Equals(p.Status, "Draft", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
                var nonDrafts = results
                    .Where(p =>
                        !string.Equals(p.Status, "Draft", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                // Render server-side HTML using existing partial so the UI exactly matches initial render
                string projectsHtml = await this.RenderViewAsync(
                    "Views/ProjectManager/_ProjectsCards.cshtml",
                    nonDrafts,
                    true
                );
                string draftsHtml = await this.RenderViewAsync(
                    "Views/ProjectManager/_ProjectsCards.cshtml",
                    drafts,
                    true
                );

                return Json(
                    new
                    {
                        total = results.Count,
                        projectsHtml,
                        draftsHtml,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchProjects failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // // ===============================================================
        // // 🧭 1. INITIALIZATION + USER CONTEXT
        // // ===============================================================
        // _logger.LogInformation(
        //     "=== [Dashboard] ENTERED ProjectManagerController.Dashboard ==="
        // );
        // _logger.LogInformation(
        //     "Active User: {UserName} | Role: ProjectManager",
        //     User.Identity?.Name
        // );

        // // Debug: Check user claims
        // var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
        // _logger.LogInformation(
        //     "🔑 [Dashboard] FirebaseToken present: {HasToken}",
        //     !string.IsNullOrEmpty(firebaseToken)
        // );
        // _logger.LogInformation(
        //     "🔑 [Dashboard] User claims count: {Count}",
        //     User.Claims.Count()
        // );

        // var vm = new DashboardViewModel();

        // var phaseDict = new Dictionary<string, List<PhaseDto>>();
        // var taskDict = new Dictionary<string, List<ProjectTaskDto>>();

        // // ===============================================================
        // // 📦 2. FETCH ALL PROJECTS FOR PM
        // // ===============================================================

        // _logger.LogInformation("🔍 [Dashboard] Fetching ALL projects for current PM...");
        // var allProjects =
        //     await _apiClient.GetAsync<List<ProjectDto>>(
        //         "/api/projectmanager/projects/all",
        //         User
        //     ) ?? new();

        // _logger.LogInformation(
        //     "📊 [Dashboard] Retrieved {Count} total projects for PM",
        //     allProjects.Count
        // );

        // // Separate draft and non-draft projects
        // vm.DraftProjects = allProjects.Where(p => p.Status == "Draft").ToList();
        // vm.FilteredProjects = allProjects.Where(p => p.Status != "Draft").ToList();

        // _logger.LogInformation(
        //     "📊 [Dashboard] Project categorization - Draft: {DraftCount}, Active: {ActiveCount}",
        //     vm.DraftProjects.Count,
        //     vm.FilteredProjects.Count
        // );

        // vm.TotalProjects = allProjects.Count;

        // _logger.LogInformation(
        //     "✅ [Dashboard] All projects loaded: {TotalCount}",
        //     allProjects.Count
        // );

        // // ===============================================================
        // // 💬 3. FETCH QUOTATIONS
        // // ===============================================================
        // var allQuotes =
        //     await _apiClient.GetAsync<List<QuotationDto>>("/api/quotations", User) ?? new();
        // var acceptedQuotes = allQuotes
        //     .Where(q => q.Status.Equals("ClientAccepted", StringComparison.OrdinalIgnoreCase))
        //     .OrderByDescending(q => q.ApprovedAt ?? q.CreatedAt)
        //     .Take(5)
        //     .ToList();

        // _logger.LogInformation(
        //     "✅ Quotations loaded. Total: {All}, Accepted: {Accepted}",
        //     allQuotes.Count,
        //     acceptedQuotes.Count
        // );

        // // ===============================================================
        // // 👥 4. FETCH CLIENTS
        // // ===============================================================
        // _logger.LogInformation("=== [Dashboard] Fetching Clients from /api/users/clients ===");

        // var allClients =
        //     await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User) ?? new();
        // vm.Clients = allClients; // ✅ Now this works because vm exists
        // vm.TotalClients = allClients.Count;
        // vm.RecentClients = allClients.Take(5).ToList();

        // _logger.LogInformation("✅ Clients loaded. Total: {Count}", allClients.Count);

        // // ===============================================================
        // // 🧰 5. FETCH CONTRACTORS
        // // ===============================================================
        // var allContractors =
        //     await _apiClient.GetAsync<List<UserDto>>("/api/users/contractors", User) ?? new();
        // var recentContractors = allContractors.Take(5).ToList();

        // _logger.LogInformation("✅ Contractors loaded. Total: {Count}", allContractors.Count);

        // // ===============================================================
        // // 🧮 6. FETCH ESTIMATES (moved before project processing)
        // // ===============================================================
        // var allEstimates =
        //     await _apiClient.GetAsync<List<EstimateDto>>("/api/estimates", User) ?? new();
        // var estimateDict = allEstimates
        //     .Where(e => !string.IsNullOrEmpty(e.ProjectId))
        //     .GroupBy(e => e.ProjectId)
        //     .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedAt).First());
        // vm.ProjectEstimates = estimateDict;

        // // ===============================================================
        // // 🧩 7. FETCH PHASES + TASKS (Optimized - only for displayed projects)
        // // ===============================================================
        // var projectsToFetch = vm.FilteredProjects.Take(5).ToList(); // Only fetch for displayed projects

        // _logger.LogInformation(
        //     "Fetching phases/tasks for {Count} projects",
        //     projectsToFetch.Count
        // );

        // foreach (var project in projectsToFetch)
        // {
        //     try
        //     {
        //         var phases =
        //             await _apiClient.GetAsync<List<PhaseDto>>(
        //                 $"/api/projectmanager/project/{project.ProjectId}/phases",
        //                 User
        //             ) ?? new();

        //         phaseDict[project.ProjectId] = phases;

        //         // Only fetch tasks if there are phases
        //         if (phases.Any())
        //         {
        //             var allTasks =
        //                 await _apiClient.GetAsync<List<ProjectTaskDto>>(
        //                     $"/api/projectmanager/project/{project.ProjectId}/tasks",
        //                     User
        //                 ) ?? new();
        //             foreach (var phase in phases)
        //             {
        //                 var phaseTasks = allTasks
        //                     .Where(t => t.PhaseId == phase.PhaseId)
        //                     .ToList();
        //                 taskDict[phase.PhaseId] = phaseTasks;
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(
        //             "Failed to fetch phases/tasks for project {ProjectId}: {Error}",
        //             project.ProjectId,
        //             ex.Message
        //         );
        //         // Continue with other projects even if one fails
        //     }
        // }

        // // ===============================================================
        // // 🧠 7. POPULATE VM
        // // ===============================================================
        // vm.TotalQuotes = allQuotes.Count;
        // vm.RecentAcceptedQuotes = acceptedQuotes;
        // vm.AllQuotes = allQuotes;
        // vm.TotalContractors = allContractors.Count;
        // vm.RecentContractors = recentContractors;
        // vm.ProjectPhases = phaseDict;
        // vm.PhaseTasks = taskDict;

        // // Pre-process project data to avoid complex operations in the view
        // var allProjectsToProcess = vm
        //     .FilteredProjects.Concat(vm.DraftProjects)
        //     .Distinct()
        //     .ToList();
        // _logger.LogInformation(
        //     "Processing data for {Count} projects",
        //     allProjectsToProcess.Count
        // );

        // foreach (var project in allProjectsToProcess)
        // {
        //     try
        //     {
        //         var phases = phaseDict.ContainsKey(project.ProjectId)
        //             ? phaseDict[project.ProjectId]
        //             : new List<PhaseDto>();
        //         var tasks = phases
        //             .SelectMany(p =>
        //                 taskDict.ContainsKey(p.PhaseId)
        //                     ? taskDict[p.PhaseId]
        //                     : new List<ProjectTaskDto>()
        //             )
        //             .ToList();
        //         var estimate = estimateDict.ContainsKey(project.ProjectId)
        //             ? estimateDict[project.ProjectId]
        //             : null;
        //         var progress = vm.GetProjectProgress(project);
        //         var statusBadgeClass = vm.GetStatusBadgeClass(project.Status);

        //         // Store processed data in the ViewModel
        //         vm.ProjectDetails[project.ProjectId] = new ProjectDetails
        //         {
        //             Phases = phases,
        //             Tasks = tasks,
        //             Estimate = estimate,
        //             Progress = progress,
        //             StatusBadgeClass = statusBadgeClass,
        //         };
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning(
        //             "Failed to process project data for {ProjectId}: {Error}",
        //             project.ProjectId,
        //             ex.Message
        //         );
        //         // Set default values for failed projects
        //         vm.ProjectDetails[project.ProjectId] = new ProjectDetails
        //         {
        //             Phases = new List<PhaseDto>(),
        //             Tasks = new List<ProjectTaskDto>(),
        //             Estimate = null,
        //             Progress = 0,
        //             StatusBadgeClass = "badge-light",
        //         };
        //     }
        // }

        // // ===============================================================
        // // 🧮 8. ESTIMATES ALREADY PROCESSED ABOVE
        // // ===============================================================

        // // ===============================================================
        // // 🚀 9. RETURN
        // // ===============================================================
        // _logger.LogInformation(
        //     "✅ DashboardViewModel ready: Projects={P}, Clients={C}, Contractors={Co}",
        //     vm.TotalProjects,
        //     vm.TotalClients,
        //     vm.TotalContractors
        // );

        // try
        // {
        //     return View(vm);
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogError(ex, "❌ Error rendering dashboard view");

        //     // Return a simple error view if the main view fails
        //     ViewBag.ErrorMessage =
        //         "Dashboard is temporarily unavailable. Please try again later.";
        //     return View("Error");
        // }

        /// <summary>
        /// Displays form for creating a new project.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateProject()
        {
            _logger.LogInformation("=== Displaying Create Project form ===");

            // Fetch Clients via API (the ProjectManager only needs clients)
            var clients =
                await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                ?? new List<UserDto>();

            // Optional: fetch projects if you want to show references or validations
            var projects =
                await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                ?? new List<ProjectDto>();

            // Build ViewModel
            var vm = new CreateProjectViewModel { Project = new ProjectDto(), Clients = clients };

            _logger.LogInformation(
                "Loaded {C} clients and {P} existing projects for CreateProject form",
                clients.Count,
                projects.Count
            );

            return View(vm);
        }

        /// <summary>
        /// Submits a new project to the API.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(ProjectDto project)
        {
            _logger.LogInformation("========= ENTERING CREATE PROJECT =========");

            // === Log raw incoming form values ===
            _logger.LogInformation("########## CREATING PROJECT WITH DATA ##########");
            _logger.LogInformation("Name: {Name}", project.Name);
            _logger.LogInformation("Description: {Description}", project.Description);
            _logger.LogInformation("ClientId: {ClientId}", project.ClientId);
            _logger.LogInformation("BudgetPlanned: {BudgetPlanned}", project.BudgetPlanned);
            _logger.LogInformation("Status: {Status}", project.Status);

            _logger.LogInformation(
                "StartDate: {StartDate} | Kind: {Kind}",
                project.StartDate,
                project.StartDate.Kind
            );
            _logger.LogInformation(
                "EndDatePlanned: {EndDatePlanned} | Kind: {Kind}",
                project.EndDatePlanned,
                project.EndDatePlanned.Kind
            );
            _logger.LogInformation(
                "EndDateActual: {EndDateActual} | Kind: {Kind}",
                project.EndDateActual,
                project.EndDateActual?.Kind.ToString() ?? "null"
            );

            _logger.LogInformation("########## END OF PROJECT INPUT DATA ##########");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "❌ Invalid project model submitted by {User}",
                    User.Identity?.Name
                );
                return View(project);
            }

            if (string.IsNullOrEmpty(project.ProjectId))
            {
                project.ProjectId = Guid.NewGuid().ToString();
                _logger.LogDebug("Generated new ProjectId: {Id}", project.ProjectId);
            }

            try
            {
                // === Normalize to UTC ===
                project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
                project.EndDatePlanned = DateTime.SpecifyKind(
                    project.EndDatePlanned,
                    DateTimeKind.Utc
                );
                if (project.EndDateActual.HasValue)
                    project.EndDateActual = DateTime.SpecifyKind(
                        project.EndDateActual.Value,
                        DateTimeKind.Utc
                    );

                _logger.LogInformation("========= NORMALIZED DATES TO UTC =========");
                _logger.LogInformation("StartDate.Kind: {Kind}", project.StartDate.Kind);
                _logger.LogInformation("EndDatePlanned.Kind: {Kind}", project.EndDatePlanned.Kind);
                _logger.LogInformation(
                    "EndDateActual.Kind: {Kind}",
                    project.EndDateActual?.Kind.ToString() ?? "null"
                );

                _logger.LogInformation("========= SERIALIZING PAYLOAD FOR API =========");
                var tempOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
                var jsonPreview = JsonSerializer.Serialize(project, tempOptions);
                _logger.LogInformation(
                    "++++++++++ RAW JSON PAYLOAD ++++++++++\n{Json}\n++++++++++ END OF PAYLOAD ++++++++++",
                    jsonPreview
                );

                // === API Call ===
                _logger.LogInformation("Sending new project {Name} to API...", project.Name);
                var created = await _apiClient.PostAsync<ProjectDto>(
                    "/api/projectmanager/create/project",
                    project,
                    User
                );

                if (created == null)
                {
                    _logger.LogError(
                        "❌ API returned null when creating project {Name}",
                        project.Name
                    );
                    TempData["ErrorMessage"] = "Failed to create project.";
                    return View(project);
                }

                _logger.LogInformation(
                    "✅ Project {Name} ({Id}) created successfully.",
                    created.Name,
                    created.ProjectId
                );
                TempData["SuccessMessage"] = $"Project '{created.Name}' created successfully.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error creating project {Name}", project.Name);
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                return View(project);
            }
            finally
            {
                _logger.LogInformation("======= END OF CREATE PROJECT PROCESS =======");
            }
        }

        /// <summary>
        /// Sync a ClientAccepted Quotation into a Project (manual, since API doesn’t auto-update).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncProjectFromQuote(string quotationId)
        {
            _logger.LogInformation("Attempting to sync project from quotation {Q}", quotationId);

            try
            {
                // Get the quotation details
                var quote = await _apiClient.GetAsync<QuotationDto>(
                    $"/api/quotations/{quotationId}",
                    User
                );
                if (quote == null)
                {
                    TempData["ErrorMessage"] = "Quotation not found.";
                    return RedirectToAction("Dashboard");
                }

                if (!quote.Status.Equals("ClientAccepted", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] =
                        "Only accepted quotations can be synced to projects.";
                    return RedirectToAction("Dashboard");
                }

                // Build a new ProjectDto from the Quotation
                var project = new ProjectDto
                {
                    ProjectId = Guid.NewGuid().ToString(),
                    ClientId = quote.ClientId,
                    Name = quote.Description ?? $"Project from Quote {quote.QuotationId}",
                    Description = $"Auto-generated from accepted quotation {quote.QuotationId}",
                    BudgetPlanned = quote.GrandTotal,
                    Status = "Active",
                    StartDate = DateTime.UtcNow,
                    EndDatePlanned = DateTime.UtcNow.AddMonths(1),
                };

                _logger.LogInformation("Built ProjectDto from quotation {Q}", quotationId);

                // Send to API
                var created = await _apiClient.PostAsync<ProjectDto>(
                    "/api/projectmanager/create/project",
                    project,
                    User
                );

                if (created == null)
                {
                    TempData["ErrorMessage"] = "Failed to sync project from quotation.";
                    return RedirectToAction("Dashboard");
                }

                TempData["SuccessMessage"] =
                    $"Project '{created.Name}' created from quotation {quotationId}.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing project from quotation {Q}", quotationId);
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // ------------------------------------------------------------------
        //  EXISTING DASHBOARD + CREATE PROJECT METHODS UNCHANGED
        // ------------------------------------------------------------------
        // (All your existing code here — unchanged)
        // ------------------------------------------------------------------

        // ==================================================================
        //  🔧 NEW LIFECYCLE ACTIONS — CONNECTED TO DASHBOARD BUTTONS
        // ==================================================================

        /// <summary>
        /// Step 1: Request an Estimate for a project (creates initial Estimate draft).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RequestEstimate(string projectId)
        {
            _logger.LogInformation(
                "=== [RequestEstimate] ENTERED for ProjectId={ProjectId} ===",
                projectId
            );

            if (string.IsNullOrEmpty(projectId))
            {
                _logger.LogWarning("⚠️ Missing ProjectId parameter.");
                TempData["ErrorMessage"] = "Invalid project ID.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                // Fetch project details for context
                _logger.LogInformation(
                    "Fetching project details for ProjectId={ProjectId}",
                    projectId
                );
                var project = await _apiClient.GetAsync<ProjectDto>(
                    $"/api/projectmanager/project/{projectId}",
                    User
                );

                if (project == null)
                {
                    _logger.LogWarning("⚠️ Project {ProjectId} not found via API.", projectId);
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction("Dashboard");
                }

                // Here you would call an API to create an "Estimate" entity or show form
                _logger.LogInformation(
                    "✅ Preparing to redirect to Estimate creation for project {Name}",
                    project.Name
                );

                TempData["SuccessMessage"] = $"Ready to create estimate for '{project.Name}'.";
                return RedirectToAction("Estimate", "Quotes", new { projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "💥 Error during RequestEstimate for ProjectId={ProjectId}",
                    projectId
                );
                TempData["ErrorMessage"] = $"Error requesting estimate: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Step 2: Review an existing Estimate (after contractor submission).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ReviewEstimate(string projectId)
        {
            _logger.LogInformation(
                "=== [ReviewEstimate] ENTERED for ProjectId={ProjectId} ===",
                projectId
            );

            try
            {
                var estimate = await _estimatesService.GetByProjectAsync(projectId, User);
                if (estimate == null)
                {
                    _logger.LogWarning("⚠️ No estimate found for Project {ProjectId}", projectId);
                    TempData["ErrorMessage"] = "No estimate available for review.";
                    return RedirectToAction("Dashboard");
                }

                _logger.LogInformation(
                    "✅ Found estimate {EstimateId} for Project {ProjectId}",
                    estimate.EstimateId,
                    projectId
                );
                TempData["SuccessMessage"] = $"Estimate {estimate.EstimateId} ready for review.";
                return RedirectToAction(
                    "EstimateReview",
                    "Quotes",
                    new { id = estimate.EstimateId }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "💥 Error reviewing estimate for ProjectId={ProjectId}",
                    projectId
                );
                TempData["ErrorMessage"] = $"Error reviewing estimate: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // ===================== BLUEPRINT PICKER + PROCESS =====================
        [HttpGet]
        public async Task<IActionResult> ProjectBlueprints(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                return BadRequest(new { error = "Missing project id" });

            var docs =
                await _documentsService.GetProjectDocumentsAsync(projectId, User)
                ?? new List<DocumentDto>();
            return Json(docs);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessBlueprint(
            [FromBody] ProcessBlueprintRequest request
        )
        {
            _logger.LogInformation(
                "[ProcessBlueprint] Received request for ProjectId={ProjectId}, Url={Url}",
                request?.ProjectId,
                request?.BlueprintUrl
            );
            if (
                request == null
                || string.IsNullOrWhiteSpace(request.ProjectId)
                || string.IsNullOrWhiteSpace(request.BlueprintUrl)
            )
            {
                _logger.LogWarning("[ProcessBlueprint] Invalid request payload");
                return BadRequest(new { error = "Invalid request" });
            }

            // Normalize URL (some API docs store under different properties)
            request.BlueprintUrl = request.BlueprintUrl.Trim();
            if (request.BlueprintUrl.StartsWith("/"))
            {
                // best-effort absolute URL
                var origin = $"{Request.Scheme}://{Request.Host}";
                request.BlueprintUrl = origin + request.BlueprintUrl;
            }

            var estimate = await _estimatesService.ProcessBlueprintAsync(request, User);
            if (estimate == null)
            {
                _logger.LogError("[ProcessBlueprint] Estimate service returned null");
                return StatusCode(500, new { error = "Processing failed" });
            }

            return Json(new { estimateId = estimate.EstimateId, total = estimate.TotalAmount });
        }

        [HttpPost]
        public async Task<IActionResult> UploadBlueprint(
            string projectId,
            IFormFile file,
            string? description
        )
        {
            if (string.IsNullOrWhiteSpace(projectId) || file == null || file.Length == 0)
                return BadRequest(new { error = "Missing project or file" });

            var token = User.FindFirst("FirebaseToken")?.Value;
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized();

            var baseUrl = _config["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                file.ContentType ?? "application/octet-stream"
            );
            content.Add(fileContent, "file", file.FileName);
            content.Add(new StringContent(projectId), "projectId");
            if (!string.IsNullOrWhiteSpace(description))
                content.Add(new StringContent(description), "description");

            var response = await client.PostAsync($"{baseUrl}/api/documents/upload", content);
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Upload failed ({Code}): {Body}", response.StatusCode, text);
                return StatusCode((int)response.StatusCode, new { error = "Upload failed" });
            }
            return Content(text, "application/json");
        }

        // ===================== ESTIMATE CRUD (modal) =====================
        [HttpGet]
        public async Task<IActionResult> GetEstimate(string projectId)
        {
            var estimate = await _estimatesService.GetByProjectAsync(projectId, User);
            return Json(estimate ?? new EstimateDto { ProjectId = projectId });
        }

        [HttpPut]
        public async Task<IActionResult> SaveEstimate(
            string estimateId,
            [FromBody] EstimateDto estimate
        )
        {
            if (string.IsNullOrWhiteSpace(estimateId))
                return BadRequest(new { error = "Missing estimate id" });
            var saved = await _estimatesService.SaveAsync(estimateId, estimate, User);
            if (saved == null)
                return StatusCode(500, new { error = "Save failed" });
            return Json(saved);
        }

        // ===================== QUOTATION ACTIONS =====================
        [HttpPost]
        public async Task<IActionResult> CreateQuoteFromEstimate(string estimateId)
        {
            if (string.IsNullOrWhiteSpace(estimateId))
                return BadRequest(new { error = "Missing estimate id" });
            var quote = await _quotationsService.CreateFromEstimateAsync(estimateId, User);
            if (quote == null)
                return StatusCode(500, new { error = "Create quotation failed" });
            return Json(new { quotationId = quote.QuotationId });
        }

        [HttpPost]
        public async Task<IActionResult> SendQuoteToClient(string quotationId)
        {
            if (string.IsNullOrWhiteSpace(quotationId))
                return BadRequest(new { error = "Missing quotation id" });
            var sent = await _quotationsService.SendToClientAsync(quotationId, User);
            if (sent == null)
                return StatusCode(500, new { error = "Send to client failed" });
            return Json(new { success = true });
        }

        /// <summary>
        /// Step 3: Assign tasks to a project (after quote approval).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AssignTasks(string projectId)
        {
            _logger.LogInformation(
                "=== [AssignTasks] ENTERED for ProjectId={ProjectId} ===",
                projectId
            );

            try
            {
                var project = await _apiClient.GetAsync<ProjectDto>(
                    $"/api/projectmanager/project/{projectId}",
                    User
                );
                if (project == null)
                {
                    _logger.LogWarning(
                        "⚠️ Project not found for task assignment (ProjectId={ProjectId})",
                        projectId
                    );
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction("Dashboard");
                }

                // Placeholder — redirect to a future AssignTasks view
                _logger.LogInformation(
                    "✅ Preparing AssignTasks page for project {Name}",
                    project.Name
                );
                return RedirectToAction("Tasks", "ProjectTasks", new { projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "💥 Error assigning tasks for ProjectId={ProjectId}",
                    projectId
                );
                TempData["ErrorMessage"] = $"Error assigning tasks: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Create a new project via AJAX (for wizard)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProjectAjax([FromBody] ProjectDto project)
        {
            _logger.LogInformation(
                "=== [CreateProjectAjax] Creating project {Name} ===",
                project.Name
            );

            try
            {
                if (string.IsNullOrEmpty(project.ProjectId))
                {
                    project.ProjectId = Guid.NewGuid().ToString();
                }

                // Normalize dates to UTC only if they are valid dates (required for Firebase)
                if (project.StartDate.Year > 1900)
                    project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
                if (project.EndDatePlanned.Year > 1900)
                    project.EndDatePlanned = DateTime.SpecifyKind(
                        project.EndDatePlanned,
                        DateTimeKind.Utc
                    );
                if (project.EndDateActual.HasValue && project.EndDateActual.Value.Year > 1900)
                    project.EndDateActual = DateTime.SpecifyKind(
                        project.EndDateActual.Value,
                        DateTimeKind.Utc
                    );

                var created = await _apiClient.PostAsync<ProjectDto>(
                    "/api/projectmanager/create/project",
                    project,
                    User
                );

                if (created == null)
                {
                    _logger.LogError(
                        "❌ API returned null when creating project {Name}",
                        project.Name
                    );
                    return Json(new { success = false, error = "Failed to create project." });
                }

                _logger.LogInformation(
                    "✅ Project {Name} ({Id}) created successfully.",
                    created.Name,
                    created.ProjectId
                );
                return Json(
                    new
                    {
                        success = true,
                        projectId = created.ProjectId,
                        message = "Project created successfully.",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error creating project {Name}", project.Name);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ========================= DRAFT + AUTOSAVE + WIZARD SUPPORT =========================

        [HttpPost]
        public async Task<IActionResult> StartDraft()
        {
            var draft = new ProjectDto { ProjectId = Guid.NewGuid().ToString(), Status = "Draft" };
            var created = await _apiClient.PostAsync<ProjectDto>(
                "/api/projectmanager/save-draft",
                draft,
                User
            );
            if (created == null)
            {
                return Json(new { success = false, error = "Failed to start draft" });
            }
            return Json(new { success = true, projectId = created.ProjectId });
        }

        [HttpPut]
        public async Task<IActionResult> AutosaveProject(string id, [FromBody] ProjectDto project)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Missing project id" });

            project.ProjectId = id;
            project.Status = "Draft";
            var updated = await _apiClient.PutAsync<ProjectDto>(
                $"/api/projectmanager/projects/{id}/autosave",
                project,
                User
            );
            if (updated == null)
                return Json(new { success = false, error = "Autosave failed" });
            return Json(new { success = true, projectId = updated.ProjectId });
        }

        [HttpPost]
        public async Task<IActionResult> SavePhases(string id, [FromBody] List<PhaseDto> phases)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Missing project id" });
            var result = await _apiClient.PostAsync<object>(
                $"/api/projectmanager/projects/{id}/phases-bulk",
                phases,
                User
            );
            return Json(result ?? new { saved = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> SaveTasks(string id, [FromBody] List<ProjectTaskDto> tasks)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Missing project id" });
            var result = await _apiClient.PostAsync<object>(
                $"/api/projectmanager/projects/{id}/tasks-bulk",
                tasks,
                User
            );
            return Json(result ?? new { saved = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> FinalizeProject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { error = "Missing project id" });
            var finalized = await _apiClient.PostAsync<ProjectDto>(
                $"/api/projectmanager/projects/{id}/finalize",
                new { },
                User
            );
            if (finalized == null)
            {
                return Json(new { success = false, error = "Finalize failed" });
            }
            return Json(new { success = true, status = finalized.Status });
        }

        /// <summary>
        /// Step 4: Activate maintenance mode for a completed project.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateMaintenance(string projectId)
        {
            _logger.LogInformation(
                "=== [ActivateMaintenance] ENTERED for ProjectId={ProjectId} ===",
                projectId
            );

            try
            {
                // Call API endpoint to update project status
                var updatedProject = await _apiClient.PutAsync<ProjectDto>(
                    $"/api/projectmanager/project/{projectId}/status",
                    new { Status = "Maintenance" },
                    User
                );

                if (updatedProject == null)
                {
                    _logger.LogWarning(
                        "⚠️ Failed to update project {ProjectId} to Maintenance mode.",
                        projectId
                    );
                    TempData["ErrorMessage"] = "Could not activate maintenance mode.";
                    return RedirectToAction("Dashboard");
                }

                _logger.LogInformation(
                    "✅ Project {Name} transitioned to Maintenance mode.",
                    updatedProject.Name
                );
                TempData["SuccessMessage"] =
                    $"Project '{updatedProject.Name}' is now in Maintenance mode.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "💥 Error activating maintenance for ProjectId={ProjectId}",
                    projectId
                );
                TempData["ErrorMessage"] = $"Error activating maintenance: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Called via AJAX from estimate-popup.js to run AI processing.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateEstimate([FromBody] ProcessBlueprintRequest request)
        {
            _logger.LogInformation(
                "=== [CreateEstimate] ENTERED === ProjectId={ProjectId}",
                request.ProjectId
            );

            try
            {
                if (
                    string.IsNullOrEmpty(request.ProjectId)
                    || string.IsNullOrEmpty(request.BlueprintUrl)
                )
                {
                    _logger.LogWarning("⚠️ Invalid payload received in CreateEstimate");
                    return BadRequest(new { error = "Invalid project or blueprint." });
                }

                var apiResponse = await _apiClient.PostAsync<EstimateDto>(
                    "/api/estimates/process-blueprint",
                    request,
                    User
                );

                if (apiResponse == null)
                {
                    _logger.LogError("❌ API returned null for AI estimate generation.");
                    return StatusCode(500, new { error = "AI processing failed." });
                }

                _logger.LogInformation(
                    "✅ AI Estimate created successfully for ProjectId={ProjectId}",
                    request.ProjectId
                );
                return Json(apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception during CreateEstimate");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Save a draft project with partial data
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveDraftProject([FromBody] ProjectDto project)
        {
            _logger.LogInformation("=== [SaveDraftProject] Saving draft project ===");

            try
            {
                // Set required fields for draft
                project.Status = "Draft";
                project.ProjectId = project.ProjectId ?? Guid.NewGuid().ToString();

                var created = await _apiClient.PostAsync<ProjectDto>(
                    "/api/projectmanager/save-draft",
                    project,
                    User
                );

                if (created == null)
                {
                    _logger.LogError("❌ API returned null when saving draft project");
                    return Json(new { success = false, error = "Failed to save draft project." });
                }

                _logger.LogInformation(
                    "✅ Draft project {Name} ({Id}) saved successfully.",
                    created.Name,
                    created.ProjectId
                );
                return Json(
                    new
                    {
                        success = true,
                        projectId = created.ProjectId,
                        message = "Draft saved successfully.",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error saving draft project {Name}", project.Name);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing draft project
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> UpdateDraftProject(
            string id,
            [FromBody] ProjectDto project
        )
        {
            _logger.LogInformation("=== [UpdateDraftProject] Updating draft project {Id} ===", id);

            try
            {
                project.ProjectId = id;
                project.Status = "Draft";

                var updated = await _apiClient.PutAsync<ProjectDto>(
                    $"/api/projectmanager/update-draft/{id}",
                    project,
                    User
                );

                if (updated == null)
                {
                    _logger.LogError("❌ API returned null when updating draft project {Id}", id);
                    return Json(new { success = false, error = "Failed to update draft project." });
                }

                _logger.LogInformation(
                    "✅ Draft project {Name} ({Id}) updated successfully.",
                    updated.Name,
                    updated.ProjectId
                );
                return Json(
                    new
                    {
                        success = true,
                        projectId = updated.ProjectId,
                        message = "Draft updated successfully.",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error updating draft project {Id}", id);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get a draft project for resuming creation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDraftProject(string id)
        {
            _logger.LogInformation("=== [GetDraftProject] Retrieving draft project {Id} ===", id);

            try
            {
                // Use general project endpoint
                var project = await _apiClient.GetAsync<ProjectDto>(
                    $"/api/projectmanager/project/{id}",
                    User
                );

                if (project == null)
                {
                    _logger.LogWarning("⚠️ Draft project {Id} not found", id);
                    return Json(new { success = false, error = "Draft project not found." });
                }

                _logger.LogInformation(
                    "✅ Draft project {Name} ({Id}) retrieved successfully.",
                    project.Name,
                    project.ProjectId
                );
                return Json(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error retrieving draft project {Id}", id);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get project phases (proxy to API)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProjectPhases(string id)
        {
            try
            {
                var phases = await _apiClient.GetAsync<List<PhaseDto>>(
                    $"/api/projectmanager/project/{id}/phases",
                    User
                );
                return Json(phases ?? new List<PhaseDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving phases for project {Id}", id);
                return Json(new List<PhaseDto>());
            }
        }

        /// <summary>
        /// Get project tasks (proxy to API)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProjectTasks(string id)
        {
            try
            {
                var tasks = await _apiClient.GetAsync<List<ProjectTaskDto>>(
                    $"/api/projectmanager/project/{id}/tasks",
                    User
                );
                return Json(tasks ?? new List<ProjectTaskDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tasks for project {Id}", id);
                return Json(new List<ProjectTaskDto>());
            }
        }

        /// <summary>
        /// Complete a draft project (convert to active)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CompleteDraftProject(
            string id,
            [FromBody] ProjectDto project
        )
        {
            _logger.LogInformation(
                "=== [CompleteDraftProject] Completing draft project {Id} ===",
                id
            );

            try
            {
                project.ProjectId = id;
                project.Status = "Active"; // Set to Active for completed projects
                project.CompletionPhase = 3;

                var completed = await _apiClient.PutAsync<ProjectDto>(
                    $"/api/projectmanager/update-draft/{id}",
                    project,
                    User
                );

                if (completed == null)
                {
                    _logger.LogError("❌ API returned null when completing draft project {Id}", id);
                    return Json(
                        new { success = false, error = "Failed to complete draft project." }
                    );
                }

                _logger.LogInformation(
                    "✅ Draft project {Name} ({Id}) completed successfully.",
                    completed.Name,
                    completed.ProjectId
                );
                return Json(
                    new
                    {
                        success = true,
                        projectId = completed.ProjectId,
                        message = "Project created successfully.",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error completing draft project {Id}", id);
                return Json(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get clients for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            try
            {
                var clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User);
                return Json(clients ?? new List<UserDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clients");
                return Json(new List<UserDto>());
            }
        }

        /// <summary>
        /// Get contractors for dropdowns (task assignments)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetContractors()
        {
            try
            {
                var contractors = await _apiClient.GetAsync<List<UserDto>>(
                    "/api/users/contractors",
                    User
                );
                return Json(contractors ?? new List<UserDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contractors");
                return Json(new List<UserDto>());
            }
        }
    }
}
