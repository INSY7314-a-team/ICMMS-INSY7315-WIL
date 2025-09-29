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
        /// Loads the Project Manager Dashboard with recent quotations snapshot.
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            _logger.LogInformation("Loading Project Manager Dashboard for {User}", User.Identity?.Name);
            _logger.LogInformation("User claims:");
            foreach (var c in User.Claims)
            {
                _logger.LogInformation(" - {Type}: {Value}", c.Type, c.Value);
            }

            var allQuotes = await _apiClient.GetAsync<List<QuotationDto>>(
                "/api/quotations", User);

            if (allQuotes == null)
            {
                _logger.LogWarning("API returned no quotations for Dashboard.");
                return View(new DashboardViewModel());
            }

            var recentAccepted = allQuotes
                .Where(q => q.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(q => q.ApprovedAt ?? q.CreatedAt)
                .Take(5)
                .ToList();

            var vm = new DashboardViewModel
            {
                TotalQuotes = allQuotes.Count,
                RecentAcceptedQuotes = recentAccepted
            };

            _logger.LogInformation("Dashboard loaded with {Total} total quotes, {Recent} recent accepted",
                vm.TotalQuotes, vm.RecentAcceptedQuotes.Count);

            return View(vm);
        }

        /// <summary>
        /// Show popup form for creating a new project.
        /// </summary>
        [HttpGet]
        public IActionResult CreateProject()
        {
            _logger.LogInformation("Displaying Create Project form.");
            return View(new ProjectDto());
        }

        /// <summary>
        /// Submits a new project to the API and assigns it to an existing client.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(ProjectDto project)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid project model submitted by {User}", User.Identity?.Name);
                return View(project);
            }

            // Generate ProjectId if missing
            if (string.IsNullOrEmpty(project.ProjectId))
            {
                project.ProjectId = Guid.NewGuid().ToString();
                _logger.LogDebug("Generated new ProjectId: {Id}", project.ProjectId);
            }

            try
            {
                // ✅ Correct API call
                var created = await _apiClient.PostAsync<ProjectDto>(
                    "/api/projectmanager/create/project",
                    project,
                    User);

                if (created == null)
                {
                    _logger.LogError("API returned null when creating project {Name}", project.Name);
                    TempData["ErrorMessage"] = "Failed to create project.";
                    return View(project);
                }

                _logger.LogInformation("Project {Name} ({Id}) created successfully via API.",
                    created.Name, created.ProjectId);

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
