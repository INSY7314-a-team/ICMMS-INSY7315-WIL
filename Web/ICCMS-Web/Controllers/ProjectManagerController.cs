using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace ICCMS_Web.Controllers
{
    // Restrict this controller to Project Managers and Testers
    [Authorize(Roles = "ProjectManager,Tester")]
    public class ProjectManagerController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ProjectManagerController> _logger;
        private readonly ProjectSetupService _projectSetupService;

        // ✅ Inject the new service via constructor
        public ProjectManagerController(
            IApiClient apiClient,
            ILogger<ProjectManagerController> logger,
            ProjectSetupService projectSetupService)
        {
            _apiClient = apiClient;
            _logger = logger;
            _projectSetupService = projectSetupService;
        }

        /// <summary>
        /// Loads the Project Manager Dashboard with minimal API reads.
        /// Uses in-memory batching to avoid duplicate / redundant Firestore calls.
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            _logger.LogInformation("=== [Dashboard] ENTERED ProjectManagerController.Dashboard ===");

            var vm = new DashboardViewModel();

            try
            {
                // ===============================================================
                // 🧭 1. FETCH ALL CORE DATA IN PARALLEL
                // ===============================================================
                _logger.LogInformation("📦 Fetching all core collections (Projects, Quotes, Clients, Contractors)");

                var projectsTask = _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User);
                var quotesTask = _apiClient.GetAsync<List<QuotationDto>>("/api/quotations", User);
                var clientsTask = _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User);
                var contractorsTask = _apiClient.GetAsync<List<UserDto>>("/api/users/contractors", User);
                var estimatesTask = _apiClient.GetAsync<List<EstimateDto>>("/api/estimates", User);

                await Task.WhenAll(projectsTask, quotesTask, clientsTask, contractorsTask, estimatesTask);

                var allProjects = projectsTask.Result ?? new();
                var allQuotes = quotesTask.Result ?? new();
                var allClients = clientsTask.Result ?? new();
                var allContractors = contractorsTask.Result ?? new();
                var allEstimates = estimatesTask.Result ?? new();

                _logger.LogInformation("✅ Core data loaded: {P} projects | {Q} quotes | {C} clients | {Co} contractors",
                    allProjects.Count, allQuotes.Count, allClients.Count, allContractors.Count);

                // ===============================================================
                // 🧠 2. MAP QUOTES + CLIENTS FOR DASHBOARD USE
                // ===============================================================
                var recentProjects = allProjects.OrderByDescending(p => p.StartDate).Take(50).ToList();

                // Group quotes per project
                var quotesByProject = allQuotes
                    .Where(q => !string.IsNullOrEmpty(q.ProjectId))
                    .GroupBy(q => q.ProjectId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Client lookup dictionary
                var clientDict = allClients.ToDictionary(c => c.UserId, c => c.FullName ?? "Unknown Client");

                // ===============================================================
                // 🧩 3. PHASES + TASKS — LAZY LOAD STRATEGY
                // ===============================================================
                // Instead of fetching phases/tasks for every project upfront (huge Firestore cost),
                // we defer those requests until the user opens the ProjectDetails or SetupProject views.
                // This drops Firestore reads by ~80–90% per dashboard load.

                _logger.LogInformation("🧩 Skipping per-project Phase/Task fetching to reduce Firestore reads.");
                _logger.LogInformation("   Phases and Tasks will load only when a ProjectDetails or SetupProject page is opened.");

                var phaseDict = new Dictionary<string, List<PhaseDto>>();
                var taskDict  = new Dictionary<string, List<ProjectTaskDto>>();

                // ===============================================================
                // 📊 4. ESTIMATES (CACHED PER PROJECT)
                // ===============================================================
                var estimateDict = allEstimates
                    .Where(e => !string.IsNullOrEmpty(e.ProjectId))
                    .GroupBy(e => e.ProjectId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedAt).First());

                // ===============================================================
                // 🚀 5. POPULATE VIEWMODEL
                // ===============================================================
                vm.TotalProjects = allProjects.Count;
                vm.RecentProjects = recentProjects;
                vm.TotalQuotes = allQuotes.Count;
                vm.AllQuotes = allQuotes;
                vm.RecentAcceptedQuotes = allQuotes
                    .Where(q => q.Status.Equals("ClientAccepted", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(q => q.ApprovedAt ?? q.CreatedAt)
                    .Take(5)
                    .ToList();
                vm.TotalClients = allClients.Count;
                vm.RecentClients = allClients.Take(5).ToList();
                vm.TotalContractors = allContractors.Count;
                vm.RecentContractors = allContractors.Take(5).ToList();
                vm.ProjectPhases = phaseDict;
                vm.PhaseTasks = taskDict;
                vm.ProjectEstimates = estimateDict;
                vm.Clients = allClients;

                _logger.LogInformation("✅ Dashboard populated successfully.");

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error loading dashboard data.");
                TempData["ErrorMessage"] = $"Error loading dashboard: {ex.Message}";
                return RedirectToAction("Error", "Home");
            }
        }


        /// <summary>
        /// Displays form for creating a new project.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateProject()
        {
            _logger.LogInformation("=== Displaying Create Project form ===");

            // === Fetch required data ===
            // Fetch Clients via API (ProjectManager only needs clients)
            var clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                        ?? new List<UserDto>();

            // Optional: fetch projects if you want to show references or validations
            var projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                        ?? new List<ProjectDto>();

            // === Initialize defaults ===
            var nowUtc = DateTime.UtcNow;
            var defaultEnd = nowUtc.AddMonths(1);

            // === Build ViewModel with safe defaults ===
            var vm = new CreateProjectViewModel
            {
                Project = new ProjectDto
                {
                    StartDate = nowUtc,                     // Prevent 0001-01-01 UTC issue
                    EndDatePlanned = defaultEnd,            // Give at least 1-month buffer
                    Status = "Draft"                        // Default project status
                },
                Clients = clients
            };

            _logger.LogInformation("📦 Loaded {C} clients and {P} existing projects for CreateProject form",
                clients.Count, projects.Count);
            _logger.LogInformation("🕓 Default StartDate={Start}, EndDatePlanned={End}",
                vm.Project.StartDate, vm.Project.EndDatePlanned);

            return View(vm);
        }


        /// <summary>
        /// Submits a new project to the API.
        /// Ensures all dates are valid and UTC-normalized before sending to Firestore.
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

            _logger.LogInformation("StartDate: {StartDate} | Kind: {Kind}", project.StartDate, project.StartDate.Kind);
            _logger.LogInformation("EndDatePlanned: {EndDatePlanned} | Kind: {Kind}", project.EndDatePlanned, project.EndDatePlanned.Kind);
            _logger.LogInformation("EndDateActual: {EndDateActual} | Kind: {Kind}", project.EndDateActual, project.EndDateActual?.Kind.ToString() ?? "null");
            _logger.LogInformation("########## END OF PROJECT INPUT DATA ##########");

            // === Model validation ===
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ Invalid project model submitted by {User}", User.Identity?.Name);
                return View(project);
            }

            // === Ensure ID exists ===
            if (string.IsNullOrEmpty(project.ProjectId))
            {
                project.ProjectId = Guid.NewGuid().ToString();
                _logger.LogDebug("Generated new ProjectId: {Id}", project.ProjectId);
            }

            try
            {
                // === STEP 1: Sanitize invalid or MinValue dates ===
                if (project.StartDate == DateTime.MinValue || project.StartDate.Year < 1900)
                {
                    _logger.LogWarning("⚠️ StartDate invalid (MinValue/<1900) — auto-setting to UtcNow");
                    project.StartDate = DateTime.UtcNow;
                }

                if (project.EndDatePlanned == DateTime.MinValue || project.EndDatePlanned.Year < 1900)
                {
                    _logger.LogWarning("⚠️ EndDatePlanned invalid — auto-setting to +1 month from now");
                    project.EndDatePlanned = DateTime.UtcNow.AddMonths(1);
                }

                // === STEP 2: Explicitly mark all dates as UTC ===
                // SpecifyKind does not shift time, only tags it with Kind = Utc
                project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
                project.EndDatePlanned = DateTime.SpecifyKind(project.EndDatePlanned, DateTimeKind.Utc);

                if (project.EndDateActual.HasValue)
                    project.EndDateActual = DateTime.SpecifyKind(project.EndDateActual.Value, DateTimeKind.Utc);

                _logger.LogInformation("🕒 Dates sanitized + normalized to UTC");
                _logger.LogInformation("StartDate={StartDate} (Kind={Kind})", project.StartDate, project.StartDate.Kind);
                _logger.LogInformation("EndDatePlanned={EndDatePlanned} (Kind={Kind})", project.EndDatePlanned, project.EndDatePlanned.Kind);

                // === STEP 3: Preview JSON payload for verification ===
                var jsonPreview = JsonSerializer.Serialize(project, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation("++++++++++ RAW JSON PAYLOAD ++++++++++\n{Json}\n++++++++++ END OF PAYLOAD ++++++++++", jsonPreview);

                // === STEP 4: API Call ===
                _logger.LogInformation("📤 Sending new project {Name} to API...", project.Name);
                var created = await _apiClient.PostAsync<ProjectDto>("/api/projectmanager/create/project", project, User);

                if (created == null)
                {
                    _logger.LogError("❌ API returned null when creating project {Name}", project.Name);
                    TempData["ErrorMessage"] = "Failed to create project.";
                    return View(project);
                }

                _logger.LogInformation("✅ Project {Name} ({Id}) created successfully.", created.Name, created.ProjectId);
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
                var quote = await _apiClient.GetAsync<QuotationDto>($"/api/quotations/{quotationId}", User);
                if (quote == null)
                {
                    TempData["ErrorMessage"] = "Quotation not found.";
                    return RedirectToAction("Dashboard");
                }

                if (!quote.Status.Equals("ClientAccepted", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Only accepted quotations can be synced to projects.";
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
                    EndDatePlanned = DateTime.UtcNow.AddMonths(1)
                };

                _logger.LogInformation("Built ProjectDto from quotation {Q}", quotationId);

                // Send to API
                var created = await _apiClient.PostAsync<ProjectDto>(
                    "/api/projectmanager/create/project", project, User);

                if (created == null)
                {
                    TempData["ErrorMessage"] = "Failed to sync project from quotation.";
                    return RedirectToAction("Dashboard");
                }

                TempData["SuccessMessage"] = $"Project '{created.Name}' created from quotation {quotationId}.";
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
            _logger.LogInformation("=== [RequestEstimate] ENTERED for ProjectId={ProjectId} ===", projectId);

            if (string.IsNullOrEmpty(projectId))
            {
                _logger.LogWarning("⚠️ Missing ProjectId parameter.");
                TempData["ErrorMessage"] = "Invalid project ID.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                // Fetch project details for context
                _logger.LogInformation("Fetching project details for ProjectId={ProjectId}", projectId);
                var project = await _apiClient.GetAsync<ProjectDto>($"/api/projectmanager/project/{projectId}", User);

                if (project == null)
                {
                    _logger.LogWarning("⚠️ Project {ProjectId} not found via API.", projectId);
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction("Dashboard");
                }

                // Here you would call an API to create an "Estimate" entity or show form
                _logger.LogInformation("✅ Preparing to redirect to Estimate creation for project {Name}", project.Name);

                TempData["SuccessMessage"] = $"Ready to create estimate for '{project.Name}'.";
                return RedirectToAction("Estimate", "Quotes", new { projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error during RequestEstimate for ProjectId={ProjectId}", projectId);
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
            _logger.LogInformation("=== [ReviewEstimate] ENTERED for ProjectId={ProjectId} ===", projectId);

            try
            {
                var estimate = await _apiClient.GetAsync<EstimateDto>($"/api/estimates/project/{projectId}", User);
                if (estimate == null)
                {
                    _logger.LogWarning("⚠️ No estimate found for Project {ProjectId}", projectId);
                    TempData["ErrorMessage"] = "No estimate available for review.";
                    return RedirectToAction("Dashboard");
                }

                _logger.LogInformation("✅ Found estimate {EstimateId} for Project {ProjectId}", estimate.EstimateId, projectId);
                TempData["SuccessMessage"] = $"Estimate {estimate.EstimateId} ready for review.";
                return RedirectToAction("EstimateReview", "Quotes", new { id = estimate.EstimateId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error reviewing estimate for ProjectId={ProjectId}", projectId);
                TempData["ErrorMessage"] = $"Error reviewing estimate: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }


        /// <summary>
        /// Step 3: Assign tasks to a project (after quote approval).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AssignTasks(string projectId)
        {
            _logger.LogInformation("=== [AssignTasks] ENTERED for ProjectId={ProjectId} ===", projectId);

            try
            {
                var project = await _apiClient.GetAsync<ProjectDto>($"/api/projectmanager/project/{projectId}", User);
                if (project == null)
                {
                    _logger.LogWarning("⚠️ Project not found for task assignment (ProjectId={ProjectId})", projectId);
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction("Dashboard");
                }

                // Placeholder — redirect to a future AssignTasks view
                _logger.LogInformation("✅ Preparing AssignTasks page for project {Name}", project.Name);
                return RedirectToAction("Tasks", "ProjectTasks", new { projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error assigning tasks for ProjectId={ProjectId}", projectId);
                TempData["ErrorMessage"] = $"Error assigning tasks: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }


        /// <summary>
        /// Step 4: Activate maintenance mode for a completed project.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateMaintenance(string projectId)
        {
            
            _logger.LogInformation("=== [ActivateMaintenance] ENTERED for ProjectId={ProjectId} ===", projectId);

            try
            {
                // Call API endpoint to update project status
                var updatedProject = await _apiClient.PutAsync<ProjectDto>(
                    $"/api/projectmanager/project/{projectId}/status",
                    new { Status = "Maintenance" },
                    User);

                if (updatedProject == null)
                {
                    _logger.LogWarning("⚠️ Failed to update project {ProjectId} to Maintenance mode.", projectId);
                    TempData["ErrorMessage"] = "Could not activate maintenance mode.";
                    return RedirectToAction("Dashboard");
                }

                _logger.LogInformation("✅ Project {Name} transitioned to Maintenance mode.", updatedProject.Name);
                TempData["SuccessMessage"] = $"Project '{updatedProject.Name}' is now in Maintenance mode.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error activating maintenance for ProjectId={ProjectId}", projectId);
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
            _logger.LogInformation("=== [CreateEstimate] ENTERED === ProjectId={ProjectId}", request.ProjectId);

            try
            {
                if (string.IsNullOrEmpty(request.ProjectId) || string.IsNullOrEmpty(request.BlueprintUrl))
                {
                    _logger.LogWarning("⚠️ Invalid payload received in CreateEstimate");
                    return BadRequest(new { error = "Invalid project or blueprint." });
                }

                var apiResponse = await _apiClient.PostAsync<EstimateDto>(
                    "/api/estimates/process-blueprint", request, User);

                if (apiResponse == null)
                {
                    _logger.LogError("❌ API returned null for AI estimate generation.");
                    return StatusCode(500, new { error = "AI processing failed." });
                }

                _logger.LogInformation("✅ AI Estimate created successfully for ProjectId={ProjectId}", request.ProjectId);
                return Json(apiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception during CreateEstimate");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        
        /// <summary>
        /// Step 5: Opens the Project Setup page for a given project.
        /// Allows adding Phases & Tasks before activation.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SetupProject(string id)
        {
            _logger.LogInformation("=== [SetupProject] ENTERED for ProjectId={ProjectId} ===", id);

            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid project ID.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                var project = await _apiClient.GetAsync<ProjectDto>($"/api/projectmanager/project/{id}", User);
                if (project == null)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction("Dashboard");
                }

                var phases = await _apiClient.GetAsync<List<PhaseDto>>($"/api/projectmanager/project/{id}/phases", User) ?? new();
                var tasks  = await _apiClient.GetAsync<List<ProjectTaskDto>>($"/api/projectmanager/project/{id}/tasks", User) ?? new();

                ViewBag.Project = project;
                ViewBag.Phases  = phases;
                ViewBag.Tasks   = tasks;

                return View("SetupProject");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error loading project setup for {ProjectId}", id);
                TempData["ErrorMessage"] = $"Error loading setup: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }


            // ================================================================
        // 🧩 ADD PHASE
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPhase(string id, PhaseDto phase)
        {
            _logger.LogInformation("🧩 [AddPhase] Adding Phase '{Name}' to Project {ProjectId}", phase.Name, id);

            if (string.IsNullOrEmpty(phase.Name))
            {
                TempData["ErrorMessage"] = "Phase name is required.";
                return RedirectToAction("SetupProject", new { id });
            }

            var result = await _projectSetupService.CreatePhaseAsync(id, phase, User);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            return RedirectToAction("SetupProject", new { id });
        }

        // ================================================================
        // 🧱 ADD TASK
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(string id, ProjectTaskDto task)
        {
            _logger.LogInformation("🧱 [AddTask] Adding Task '{Name}' to Project {ProjectId}", task.Name, id);

            if (string.IsNullOrEmpty(task.Name))
            {
                TempData["ErrorMessage"] = "Task name is required.";
                return RedirectToAction("SetupProject", new { id });
            }

            var result = await _projectSetupService.CreateTaskAsync(id, task, User);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            return RedirectToAction("SetupProject", new { id });
        }

        // ================================================================
        // 🚀 FINALIZE PROJECT (Status must be 'SetUp')
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeProject(string id)
        {
            _logger.LogInformation("🚀 [FinalizeProject] Triggered for ProjectId={ProjectId}", id);

            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid project ID." });

            try
            {
                var project = await _apiClient.GetAsync<ProjectDto>($"/api/projectmanager/project/{id}", User);
                if (project == null)
                    return Json(new { success = false, message = "Project not found." });

                if (!project.Status.Equals("SetUp", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = $"Project must be in 'SetUp' state before finalizing. (Current: {project.Status})" });

                var result = await _projectSetupService.FinalizeProjectAsync(project, User);

                if (!result.Success)
                    return Json(new { success = false, message = result.Message });

                _logger.LogInformation("✅ Project {Id} successfully finalized (status → Active)", id);
                return Json(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Exception during FinalizeProject for {Id}", id);
                return Json(new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }

        // ================================================================
        // 🏁 COMPLETE PROJECT (Status must be 'Active')
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProject(string id)
        {
            _logger.LogInformation("🏁 [CompleteProject] Triggered for ProjectId={ProjectId}", id);

            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid project ID." });

            try
            {
                var project = await _apiClient.GetAsync<ProjectDto>($"/api/projectmanager/project/{id}", User);
                if (project == null)
                    return Json(new { success = false, message = "Project not found." });

                if (!project.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = $"Project must be 'Active' before marking as completed. (Current: {project.Status})" });

                var result = await _projectSetupService.CompleteProjectAsync(project, User);

                if (!result.Success)
                    return Json(new { success = false, message = result.Message });

                _logger.LogInformation("✅ Project {Id} successfully marked as Completed.", id);
                return Json(new { success = true, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Exception during CompleteProject for {Id}", id);
                return Json(new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Centralized Project Details Hub (replaces SetupProject)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProjectDetails(string id)
        {
            _logger.LogInformation("📁 [ProjectDetails] Loading details for ProjectId={ProjectId}", id);

            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Invalid project ID.";
                return RedirectToAction("Dashboard");
            }

            try
            {
                // 🔹 Fetch main project
                var project = await _apiClient.GetAsync<ProjectDto>($"/api/projectmanager/project/{id}", User);
                if (project == null)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction("Dashboard");
                }

                // 🔹 Fetch related data
                var phases  = await _apiClient.GetAsync<List<PhaseDto>>($"/api/projectmanager/project/{id}/phases", User) ?? new();
                var tasks   = await _apiClient.GetAsync<List<ProjectTaskDto>>($"/api/projectmanager/project/{id}/tasks", User) ?? new();
                var quotes  = await _apiClient.GetAsync<List<QuotationDto>>($"/api/quotations", User) ?? new();

                // 🔹 Group data for view
                var projectQuotes = quotes.Where(q => q.ProjectId == id).ToList();

                // 🔹 Build ViewModel
                var vm = new ProjectDetailsViewModel
                {
                    Project = project,
                    Phases = phases,
                    Tasks = tasks,
                    Quotes = projectQuotes
                };

                _logger.LogInformation("✅ [ProjectDetails] Data prepared successfully for {ProjectName}", project.Name);
                return View("ProjectDetails", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error loading ProjectDetails for {ProjectId}", id);
                TempData["ErrorMessage"] = $"Error loading project details: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }



    
    }
}
