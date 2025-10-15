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

        public ProjectManagerController(IApiClient apiClient, ILogger<ProjectManagerController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        /// <summary>
        /// Loads the Project Manager Dashboard with full project lifecycle data:
        /// Projects → Phases → Tasks, plus summary cards for Clients, Contractors, and Quotes.
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            // ===============================================================
            // 🧭 1. INITIALIZATION + USER CONTEXT
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] ENTERED ProjectManagerController.Dashboard ===");
            _logger.LogInformation("Active User: {UserName} | Role: ProjectManager", User.Identity?.Name);

            var phaseDict = new Dictionary<string, List<PhaseDto>>();     // key = ProjectId
            var taskDict = new Dictionary<string, List<ProjectTaskDto>>(); // key = PhaseId

            // ===============================================================
            // 📦 2. FETCH PROJECTS
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] Fetching Projects from API endpoint /api/projectmanager/projects ===");

            var allProjects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User);
            if (allProjects == null)
            {
                _logger.LogWarning("⚠️ API returned null for project list — initializing empty list.");
                allProjects = new List<ProjectDto>();
            }

            var recentProjects = allProjects
                .OrderByDescending(p => p.StartDate)
                .Take(50)
                .ToList();

            _logger.LogInformation("✅ [Dashboard] Projects loaded successfully. Total: {CountTotal}, Recent: {CountRecent}",
                allProjects.Count, recentProjects.Count);

            // ===============================================================
            // 🧩 3. FETCH PHASES AND TASKS FOR EACH PROJECT
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] Fetching phases and tasks for each project ===");

            foreach (var project in allProjects)
            {
                _logger.LogInformation("➡️ Processing Project: {ProjectName} ({ProjectId})", project.Name, project.ProjectId);

                var phases = await _apiClient.GetAsync<List<PhaseDto>>(
                    $"/api/projectmanager/project/{project.ProjectId}/phases", User) ?? new List<PhaseDto>();
                phaseDict[project.ProjectId] = phases;

                _logger.LogInformation("✅ Retrieved {Count} phases for Project {ProjectId}", phases.Count, project.ProjectId);

                foreach (var phase in phases)
                {
                    _logger.LogInformation("🔍 Fetching tasks for Phase: {PhaseName} ({PhaseId})", phase.Name, phase.PhaseId);

                    var allProjectTasks = await _apiClient.GetAsync<List<ProjectTaskDto>>(
                        $"/api/projectmanager/project/{project.ProjectId}/tasks", User) ?? new List<ProjectTaskDto>();

                    var phaseTasks = allProjectTasks.Where(t => t.PhaseId == phase.PhaseId).ToList();
                    taskDict[phase.PhaseId] = phaseTasks;

                    _logger.LogInformation("✅ Phase {PhaseName} ({PhaseId}) has {CountTasks} tasks linked",
                        phase.Name, phase.PhaseId, phaseTasks.Count);
                }
            }

            _logger.LogInformation("=== [Dashboard] Completed fetching lifecycle data ===");
            _logger.LogInformation("Summary → Projects: {ProjCount}, Total Phases: {PhaseCount}, Total Tasks: {TaskCount}",
                allProjects.Count,
                phaseDict.Values.Sum(list => list.Count),
                taskDict.Values.Sum(list => list.Count));

            // ===============================================================
            // 💬 4. FETCH QUOTATIONS
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] Fetching Quotations from /api/quotations ===");
            var allQuotes = await _apiClient.GetAsync<List<QuotationDto>>("/api/quotations", User) ?? new List<QuotationDto>();

            var acceptedQuotes = allQuotes
                .Where(q => q.Status.Equals("ClientAccepted", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(q => q.ApprovedAt ?? q.CreatedAt)
                .Take(5)
                .ToList();

            _logger.LogInformation("✅ Quotations loaded. Total: {All}, Accepted: {Accepted}",
                allQuotes.Count, acceptedQuotes.Count);

            // ===============================================================
            // 👥 5. FETCH CLIENTS
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] Fetching Clients from /api/users/clients ===");
            var allClients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User) ?? new List<UserDto>();
            var recentClients = allClients.Take(5).ToList();
            _logger.LogInformation("✅ Clients loaded. Total: {Count}", allClients.Count);

            // ===============================================================
            // 🧰 6. FETCH CONTRACTORS
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] Fetching Contractors from /api/users/contractors ===");
            var allContractors = await _apiClient.GetAsync<List<UserDto>>("/api/users/contractors", User) ?? new List<UserDto>();
            var recentContractors = allContractors.Take(5).ToList();
            _logger.LogInformation("✅ Contractors loaded. Total: {Count}", allContractors.Count);

            // ===============================================================
            // 🧠 7. BUILD FINAL VIEW MODEL
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] Constructing DashboardViewModel ===");

            var vm = new DashboardViewModel
            {
                TotalProjects = allProjects.Count,
                RecentProjects = recentProjects,

                TotalQuotes = allQuotes.Count,
                RecentAcceptedQuotes = acceptedQuotes,
                AllQuotes = allQuotes,

                TotalClients = allClients.Count,
                RecentClients = recentClients,

                TotalContractors = allContractors.Count,
                RecentContractors = recentContractors,

                ProjectPhases = phaseDict,
                PhaseTasks = taskDict
            };

            _logger.LogInformation("✅ DashboardViewModel ready: Projects={P}, Phases={Ph}, Tasks={T}, Clients={C}, Contractors={Co}",
                vm.TotalProjects, vm.ProjectPhases.Values.Sum(x => x.Count), vm.PhaseTasks.Values.Sum(x => x.Count),
                vm.TotalClients, vm.TotalContractors);

            // ===============================================================
            // 🧮 8. FETCH ESTIMATES + MAP TO VIEWMODEL
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] Fetching all estimates for mapping ===");
            var allEstimates = await _apiClient.GetAsync<List<EstimateDto>>("/api/estimates", User) ?? new List<EstimateDto>();
            var estimateDict = allEstimates
                .Where(e => !string.IsNullOrEmpty(e.ProjectId))
                .GroupBy(e => e.ProjectId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedAt).First());

            vm.ProjectEstimates = estimateDict;
            _logger.LogInformation("✅ Mapped {Count} projects with estimates", estimateDict.Count);

            // ===============================================================
            // 🚀 9. RETURN FINALIZED DASHBOARD VIEW
            // ===============================================================
            _logger.LogInformation("=== [Dashboard] Rendering Project Manager Dashboard View ===");
            _logger.LogInformation("=== [Dashboard] EXIT ===");
            return View(vm);
        }

        /// <summary>
        /// Displays form for creating a new project.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateProject()
        {
            _logger.LogInformation("=== Displaying Create Project form ===");

            // Fetch Clients via API (the ProjectManager only needs clients)
            var clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User)
                        ?? new List<UserDto>();

            // Optional: fetch projects if you want to show references or validations
            var projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User)
                        ?? new List<ProjectDto>();

            // Build ViewModel
            var vm = new CreateProjectViewModel
            {
                Project = new ProjectDto(),
                Clients = clients
            };

            _logger.LogInformation("Loaded {C} clients and {P} existing projects for CreateProject form",
                clients.Count, projects.Count);

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

            _logger.LogInformation("StartDate: {StartDate} | Kind: {Kind}", 
                project.StartDate, project.StartDate.Kind);
            _logger.LogInformation("EndDatePlanned: {EndDatePlanned} | Kind: {Kind}", 
                project.EndDatePlanned, project.EndDatePlanned.Kind);
            _logger.LogInformation("EndDateActual: {EndDateActual} | Kind: {Kind}", 
                project.EndDateActual, project.EndDateActual?.Kind.ToString() ?? "null");

            _logger.LogInformation("########## END OF PROJECT INPUT DATA ##########");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ Invalid project model submitted by {User}", User.Identity?.Name);
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
                project.EndDatePlanned = DateTime.SpecifyKind(project.EndDatePlanned, DateTimeKind.Utc);
                if (project.EndDateActual.HasValue)
                    project.EndDateActual = DateTime.SpecifyKind(project.EndDateActual.Value, DateTimeKind.Utc);

                _logger.LogInformation("========= NORMALIZED DATES TO UTC =========");
                _logger.LogInformation("StartDate.Kind: {Kind}", project.StartDate.Kind);
                _logger.LogInformation("EndDatePlanned.Kind: {Kind}", project.EndDatePlanned.Kind);
                _logger.LogInformation("EndDateActual.Kind: {Kind}", project.EndDateActual?.Kind.ToString() ?? "null");

                _logger.LogInformation("========= SERIALIZING PAYLOAD FOR API =========");
                var tempOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var jsonPreview = JsonSerializer.Serialize(project, tempOptions);
                _logger.LogInformation("++++++++++ RAW JSON PAYLOAD ++++++++++\n{Json}\n++++++++++ END OF PAYLOAD ++++++++++", jsonPreview);

                // === API Call ===
                _logger.LogInformation("Sending new project {Name} to API...", project.Name);
                var created = await _apiClient.PostAsync<ProjectDto>(
                    "/api/projectmanager/create/project", project, User);

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

    
    }
}
