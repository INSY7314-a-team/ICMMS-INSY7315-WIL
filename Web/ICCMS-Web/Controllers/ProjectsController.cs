using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    // ============================================================
    //  PROJECTS CONTROLLER (WEB SIDE)
    //  Role: Project Manager, Tester
    //  Purpose: Acts as a front-end bridge between Razor views and
    //  the API's ProjectManagerController (Firestore backend).
    // ============================================================

    [Authorize(Roles = "ProjectManager,Tester")]
    public class ProjectsController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IApiClient apiClient, ILogger<ProjectsController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // ============================================================
        //  INDEX
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("📁 [ProjectsController] Loading Projects index...");

            var projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projectmanager/projects", User) ?? new();
            var clients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User) ?? new();
            var quotes = await _apiClient.GetAsync<List<QuotationDto>>("/api/quotations", User) ?? new();
            var estimates = await _apiClient.GetAsync<List<EstimateDto>>("/api/estimates", User) ?? new();

            var vm = new DashboardViewModel
            {
                RecentProjects = projects.OrderByDescending(p => p.StartDate).Take(50).ToList(),
                Clients = clients,
                AllQuotes = quotes,
                ProjectEstimates = estimates
                    .Where(e => !string.IsNullOrEmpty(e.ProjectId))
                    .GroupBy(e => e.ProjectId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedAt).First())
            };

            _logger.LogInformation("✅ [ProjectsController] Projects page loaded with {Count} projects", vm.RecentProjects.Count);
            return View(vm);
        }

        // ============================================================
        //  DETAILS
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            _logger.LogInformation("🧱 Loading project details for ID: {Id}", id);

            var project = await _apiClient.GetAsync<ProjectDto>($"/api/projectmanager/project/{id}", User);
            if (project == null)
                return NotFound();

            var phases = await _apiClient.GetAsync<List<PhaseDto>>($"/api/projectmanager/project/{id}/phases", User) ?? new();
            var tasks = await _apiClient.GetAsync<List<ProjectTaskDto>>($"/api/projectmanager/project/{id}/tasks", User) ?? new();
            var quotes = await _apiClient.GetAsync<List<QuotationDto>>($"/api/quotations", User) ?? new();

            var vm = new ProjectDetailsViewModel
            {
                Project = project,
                Phases = phases,
                Tasks = tasks,
                Quotes = quotes.Where(q => q.ProjectId == id).ToList()
            };

            // ✅ Return partial if AJAX (used by refreshPhases())
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                _logger.LogInformation("📦 Returning partial view for AJAX refresh.");
                return PartialView("_PhasesPartial", vm);
            }

            _logger.LogInformation("✅ Loaded project {Name} with {Phases} phases and {Tasks} tasks",
                project.Name, phases.Count, tasks.Count);

            return View(vm);
        }


        // ============================================================
        //  ADD PHASE
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> AddPhase([FromBody] PhaseDto phase)
        {
            _logger.LogInformation("➕ [ProjectsController] Adding new Phase to Project {ProjectId}", phase.ProjectId);

            var result = await _apiClient.PostAsync<PhaseDto>($"/api/projectmanager/create/project/{phase.ProjectId}/phase", phase, User);
            if (result == null)
            {
                _logger.LogError("❌ Failed to add Phase {Name}. API returned null or error.", phase.Name);
                return StatusCode(500, "Failed to add phase");
            }

            _logger.LogInformation("✅ Successfully added Phase {Name}", phase.Name);
            return Ok(result);
        }

        // ============================================================
        //  ADD TASK
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> AddTask([FromBody] ProjectTaskDto task)
        {
            _logger.LogInformation("➕ [ProjectsController] Adding new Task {Name} to Project {ProjectId}", task.Name, task.ProjectId);

            var result = await _apiClient.PostAsync<ProjectTaskDto>($"/api/projectmanager/create/project/{task.ProjectId}/task", task, User);
            if (result == null)
            {
                _logger.LogError("❌ Failed to add Task {Name}. API returned null or error.", task.Name);
                return StatusCode(500, "Failed to add task");
            }

            _logger.LogInformation("✅ Successfully added Task {Name}", task.Name);
            return Ok(result);
        }

        // ============================================================
        //  UPDATE PHASE
        // ============================================================
        [HttpPut]
        public async Task<IActionResult> UpdatePhase([FromBody] PhaseDto phase)
        {
            _logger.LogInformation("🛠️ [ProjectsController] Updating Phase {Id}", phase.PhaseId);

            var result = await _apiClient.PutAsync<PhaseDto>($"/api/projectmanager/update/phase/{phase.PhaseId}", phase, User);
            if (result == null)
            {
                _logger.LogError("❌ Failed to update Phase {Name}. API returned null or error.", phase.Name);
                return StatusCode(500, "Failed to update phase");
            }

            _logger.LogInformation("✅ Phase {Name} updated successfully", phase.Name);
            return Ok(result);
        }

        // ============================================================
        //  UPDATE TASK
        // ============================================================
        [HttpPut]
        public async Task<IActionResult> UpdateTask([FromBody] ProjectTaskDto task)
        {
            _logger.LogInformation("🛠️ [ProjectsController] Updating Task {Id}", task.TaskId);

            var result = await _apiClient.PutAsync<ProjectTaskDto>($"/api/projectmanager/update/task/{task.TaskId}", task, User);
            if (result == null)
            {
                _logger.LogError("❌ Failed to update Task {Name}. API returned null or error.", task.Name);
                return StatusCode(500, "Failed to update task");
            }

            _logger.LogInformation("✅ Task {Name} updated successfully", task.Name);
            return Ok(result);
        }

        // ============================================================
        //  DELETE PHASE
        // ============================================================
        [HttpDelete]
        public async Task<IActionResult> DeletePhase(string id)
        {
            _logger.LogInformation("🗑️ [ProjectsController] Deleting Phase {Id}", id);

            var success = await _apiClient.DeleteAsync($"/api/projectmanager/delete/phase/{id}", User);
            if (!success)
            {
                _logger.LogError("❌ Failed to delete Phase {Id}", id);
                return StatusCode(500, "Failed to delete phase");
            }

            _logger.LogInformation("✅ Phase {Id} deleted successfully", id);
            return Ok();
        }

        // ============================================================
        //  DELETE TASK
        // ============================================================
        [HttpDelete]
        public async Task<IActionResult> DeleteTask(string id)
        {
            _logger.LogInformation("🗑️ [ProjectsController] Deleting Task {Id}", id);

            var success = await _apiClient.DeleteAsync($"/api/projectmanager/delete/task/{id}", User);
            if (!success)
            {
                _logger.LogError("❌ Failed to delete Task {Id}", id);
                return StatusCode(500, "Failed to delete task");
            }

            _logger.LogInformation("✅ Task {Id} deleted successfully", id);
            return Ok();
        }

        // ============================================================
        //  GET CONTRACTORS (for dropdown assignment)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetContractors()
        {
            _logger.LogInformation("👷 [ProjectsController] Fetching contractor list...");

            var contractors = await _apiClient.GetAsync<List<UserDto>>("/api/users/contractors", User) ?? new();
            _logger.LogInformation("✅ Loaded {Count} contractors", contractors.Count);


            return Json(contractors);
        }
    }
}
