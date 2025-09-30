using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        /// Loads the Project Manager Dashboard with projects, clients, contractors, and accepted quotes.
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            _logger.LogInformation("=== Loading Project Manager Dashboard for {User} ===", User.Identity?.Name);

            // ================= FETCH PROJECTS =================
            _logger.LogInformation("Fetching projects from API...");
            var allProjects = await _apiClient.GetAsync<List<ProjectDto>>("/api/projects", User);
            if (allProjects == null) allProjects = new List<ProjectDto>();

            var recentProjects = allProjects
                .OrderByDescending(p => p.StartDate) // valid property on ProjectDto
                .Take(5)
                .ToList();
            _logger.LogInformation("Fetched {Count} total projects.", allProjects.Count);

            // ================= FETCH QUOTES =================
            _logger.LogInformation("Fetching quotations from API...");
            var allQuotes = await _apiClient.GetAsync<List<QuotationDto>>("/api/quotations", User);
            if (allQuotes == null) allQuotes = new List<QuotationDto>();

            var acceptedQuotes = allQuotes
                .Where(q => q.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(q => q.ApprovedAt ?? q.CreatedAt)
                .Take(5)
                .ToList();

            _logger.LogInformation("Fetched {Count} total quotes, {Accepted} accepted.",
                allQuotes.Count, acceptedQuotes.Count);

            // ================= FETCH CLIENTS =================
            _logger.LogInformation("Fetching clients from /api/users/clients...");
            var allClients = await _apiClient.GetAsync<List<UserDto>>("/api/users/clients", User) ?? new List<UserDto>();
            var recentClients = allClients.Take(5).ToList();
            _logger.LogInformation("Fetched {Count} active clients.", allClients.Count);

            // ================= FETCH CONTRACTORS =================
            _logger.LogInformation("Fetching contractors from /api/users/contractors...");
            var allContractors = await _apiClient.GetAsync<List<UserDto>>("/api/users/contractors", User) ?? new List<UserDto>();
            var recentContractors = allContractors.Take(5).ToList();
            _logger.LogInformation("Fetched {Count} active contractors.", allContractors.Count);

            // ================= BUILD VIEW MODEL =================
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
                RecentContractors = recentContractors
            };

            _logger.LogInformation(
                "DashboardViewModel prepared. Projects={P}, Quotes={Q}, Clients={C}, Contractors={Co}",
                vm.TotalProjects, vm.TotalQuotes, vm.TotalClients, vm.TotalContractors);

            return View(vm);
        }

        /// <summary>
        /// Displays form for creating a new project.
        /// </summary>
        [HttpGet]
        public IActionResult CreateProject()
        {
            _logger.LogInformation("Displaying Create Project form.");
            return View(new ProjectDto());
        }

        /// <summary>
        /// Submits a new project to the API.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(ProjectDto project)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid project submitted by {User}", User.Identity?.Name);
                return View(project);
            }

            if (string.IsNullOrEmpty(project.ProjectId))
            {
                project.ProjectId = Guid.NewGuid().ToString();
                _logger.LogDebug("Generated new ProjectId: {Id}", project.ProjectId);
            }

            try
            {
                _logger.LogInformation("Sending new project {Name} to API...", project.Name);
                var created = await _apiClient.PostAsync<ProjectDto>(
                    "/api/projectmanager/create/project", project, User);

                if (created == null)
                {
                    _logger.LogError("API returned null when creating project {Name}", project.Name);
                    TempData["ErrorMessage"] = "Failed to create project.";
                    return View(project);
                }

                _logger.LogInformation("Project {Name} ({Id}) created successfully.", created.Name, created.ProjectId);
                TempData["SuccessMessage"] = $"Project '{created.Name}' created successfully.";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project {Name}", project.Name);
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                return View(project);
            }
        }
    }
}
