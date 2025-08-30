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

                // Get client's projects
                var projectsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/projects");
                var quotationsResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/api/clients/quotations");

                var projects = new List<Project>();
                var quotations = new List<Quotation>();

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

                var viewModel = new ClientDashboardViewModel
                {
                    Projects = projects,
                    Quotations = quotations
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
    }

    public class ClientDashboardViewModel
    {
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<Quotation> Quotations { get; set; } = new List<Quotation>();
    }
}
