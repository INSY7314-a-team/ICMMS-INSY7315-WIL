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
        private readonly ILogger<ClientsController> _logger;
        private readonly string _apiBaseUrl;

        public ClientsController(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ClientsController> logger
        )
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    _logger.LogWarning(
                        "No FirebaseToken found for user {User}",
                        User.Identity?.Name
                    );
                    TempData["ErrorMessage"] =
                        "Authentication token not found. Please login again.";
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var projects = new List<ProjectDto>();
                var quotations = new List<QuotationDto>();

                // Get client's projects with timeout
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var projectsResponse = await _httpClient.GetAsync(
                        $"{_apiBaseUrl}/api/clients/projects",
                        cts.Token
                    );

                    if (projectsResponse.IsSuccessStatusCode)
                    {
                        var projectsBody = await projectsResponse.Content.ReadAsStringAsync();
                        projects =
                            JsonSerializer.Deserialize<List<ProjectDto>>(
                                projectsBody,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            ) ?? new List<ProjectDto>();
                    }
                    else
                    {
                        // Fallback data when API fails
                        projects = new List<ProjectDto>
                        {
                            new ProjectDto
                            {
                                Name = "Sample Project",
                                Description = "API service unavailable",
                            },
                        };
                    }
                }
                catch (Exception ex)
                {
                    // Fallback data when API fails
                    projects = new List<ProjectDto>
                    {
                        new ProjectDto
                        {
                            Name = "Sample Project",
                            Description = "API service unavailable",
                        },
                    };
                }

                // Get client's quotations with timeout
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    var quotationsResponse = await _httpClient.GetAsync(
                        $"{_apiBaseUrl}/api/clients/quotations",
                        cts.Token
                    );

                    if (quotationsResponse.IsSuccessStatusCode)
                    {
                        var quotationsBody = await quotationsResponse.Content.ReadAsStringAsync();
                        quotations =
                            JsonSerializer.Deserialize<List<QuotationDto>>(
                                quotationsBody,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            ) ?? new List<QuotationDto>();
                    }
                    else
                    {
                        // Fallback data when API fails
                        quotations = new List<QuotationDto>
                        {
                            new QuotationDto { QuotationId = "demo-1", Status = "API Unavailable" },
                        };
                    }
                }
                catch (Exception ex)
                {
                    // Fallback data when API fails
                    quotations = new List<QuotationDto>
                    {
                        new QuotationDto { QuotationId = "demo-1", Status = "API Unavailable" },
                    };
                }

                var viewModel = new ClientDashboardViewModel
                {
                    Projects = projects,
                    Quotations = quotations,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClientsController.Index");

                // Complete fallback when everything fails
                var fallbackViewModel = new ClientDashboardViewModel
                {
                    Projects = new List<ProjectDto>
                    {
                        new ProjectDto
                        {
                            Name = "Service Unavailable",
                            Description = "API connection failed",
                        },
                    },
                    Quotations = new List<QuotationDto>
                    {
                        new QuotationDto { QuotationId = "demo-1", Status = "Service Unavailable" },
                    },
                };

                TempData["ErrorMessage"] =
                    "API service is currently unavailable. Showing demo data.";
                return View(fallbackViewModel);
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

                var response = await _httpClient.GetAsync(
                    $"{_apiBaseUrl}/api/clients/project/{id}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var project = JsonSerializer.Deserialize<ProjectDto>(
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

                var response = await _httpClient.GetAsync(
                    $"{_apiBaseUrl}/api/clients/quotation/{id}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var quotation = JsonSerializer.Deserialize<QuotationDto>(
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

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/api/clients/quotation/{quotationId}/approve",
                    null
                );

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

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/api/clients/quotation/{quotationId}/reject",
                    null
                );

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
        public List<ProjectDto> Projects { get; set; } = new List<ProjectDto>();
        public List<QuotationDto> Quotations { get; set; } = new List<QuotationDto>();
    }
}
