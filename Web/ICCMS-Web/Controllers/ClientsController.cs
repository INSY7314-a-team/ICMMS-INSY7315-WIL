using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Client")] // Only clients can access this controller
    public class ClientsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public ClientsController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Get client's projects, quotations, and maintenance requests
                var projectsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/projects");
                var quotationsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/quotations");
                var maintenanceRequestsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/maintenanceRequests");

                var projects = new List<Project>();
                var quotations = new List<Quotation>();
                var maintenanceRequests = new List<MaintenanceRequest>();

                if (projectsResponse.IsSuccessStatusCode)
                {
                    var projectsBody = await projectsResponse.Content.ReadAsStringAsync();
                    projects = JsonSerializer.Deserialize<List<Project>>(
                        projectsBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new List<Project>();
                }

                if (quotationsResponse.IsSuccessStatusCode)
                {
                    var quotationsBody = await quotationsResponse.Content.ReadAsStringAsync();
                    quotations = JsonSerializer.Deserialize<List<Quotation>>(
                        quotationsBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new List<Quotation>();
                }

                if (maintenanceRequestsResponse.IsSuccessStatusCode)
                {
                    var maintenanceRequestsBody = await maintenanceRequestsResponse.Content.ReadAsStringAsync();
                    maintenanceRequests = JsonSerializer.Deserialize<List<MaintenanceRequest>>(
                        maintenanceRequestsBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new List<MaintenanceRequest>();
                }

                var viewModel = new ClientDashboardViewModel
                {
                    Projects = projects,
                    Quotations = quotations,
                    MaintenanceRequests = maintenanceRequests
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View(new ClientDashboardViewModel());
            }
        }

        public async Task<IActionResult> ProjectDetails(string id)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/project/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var project = JsonSerializer.Deserialize<Project>(
                        responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return View(project);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to fetch project details.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> QuotationDetails(string id)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/quotation/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var quotation = JsonSerializer.Deserialize<Quotation>(
                        responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return View(quotation);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to fetch quotation details.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveQuotation(string quotationId)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/clients/quotation/{quotationId}/approve", null);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Quotation approved successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve quotation.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectQuotation(string quotationId)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/clients/quotation/{quotationId}/reject", null);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Quotation rejected successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject quotation.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> CreateMaintenanceRequest()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                // Get client's projects for the dropdown
                var projectsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/projects");
                
                if (projectsResponse.IsSuccessStatusCode)
                {
                    var projectsBody = await projectsResponse.Content.ReadAsStringAsync();
                    var projects = JsonSerializer.Deserialize<List<Project>>(
                        projectsBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new List<Project>();
                    
                    ViewBag.Projects = projects;
                }

                return View(new CreateMaintenanceRequestViewModel());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaintenanceRequest(CreateMaintenanceRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-populate projects dropdown
                try
                {
                    var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                    if (!string.IsNullOrEmpty(firebaseToken))
                    {
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                        var projectsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/projects");
                        
                        if (projectsResponse.IsSuccessStatusCode)
                        {
                            var projectsBody = await projectsResponse.Content.ReadAsStringAsync();
                            var projects = JsonSerializer.Deserialize<List<Project>>(
                                projectsBody,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            ) ?? new List<Project>();
                            
                            ViewBag.Projects = projects;
                        }
                    }
                }
                catch
                {
                    // Ignore errors when re-populating dropdown
                }
                
                return View(model);
            }

            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var maintenanceRequest = new MaintenanceRequest
                {
                    ProjectId = model.ProjectId,
                    Description = model.Description,
                    Priority = model.Priority,
                    MediaUrl = model.MediaUrl ?? string.Empty,
                    RequestedBy = User.Identity?.Name ?? string.Empty
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/clients/create/maintenanceRequest",
                    maintenanceRequest
                );

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Maintenance request created successfully.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create maintenance request.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("CreateMaintenanceRequest");
        }
    }

    public class ClientDashboardViewModel
    {
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Quotation> Quotations { get; set; } = new List<Quotation>();
        public List<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
