using System.IO;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;
using ICCMS_Web.Models;
using ICCMS_Web.Models;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Client")] // Only clients can access this controller
    public class ClientsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IApiClient _apiClient;
        private readonly ILogger<ClientsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly IMessagingService _messagingService;

        public ClientsController(
            IApiClient apiClient,
            ILogger<ClientsController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IMessagingService messagingService
        )
        {
            _apiClient = apiClient;
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _messagingService = messagingService;
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

                var projects = new List<ProjectDto>();

                // === 🏗 Get Client Projects ===
                try
                {
                    _logger.LogInformation("🚀 [Index] Fetching Projects via API...");
                    projects =
                        await _apiClient.GetAsync<List<ProjectDto>>("/api/clients/projects", User)
                        ?? new List<ProjectDto>();

                    if (projects.Any())
                    {
                        projects = projects.Where(p => !p.IsDraft).ToList();
                        _logger.LogInformation(
                            "✅ [Index] Loaded {Count} non-draft projects",
                            projects.Count
                        );
                    }
                    else
                        _logger.LogWarning("⚠️ [Index] No projects found for this client.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "🔥 [Index] Failed to retrieve client projects via API.");
                    projects = new List<ProjectDto>
                    {
                        new ProjectDto
                        {
                            Name = "Sample Project",
                            Description = "API service unavailable",
                        },
                    };
                }

                // ✅ Build the ViewModel
                var viewModel = new ClientDashboardViewModel
                {
                    Projects = projects,
                    TotalProjects = projects.Count,
                    ActiveProjects = projects.Count(p => p.Status == "Active"),
                    CompletedProjects = projects.Count(p => p.Status == "Completed"),
                };

                // Calculate progress and status badge classes for each project
                foreach (var project in projects)
                {
                    // For now, use status-based progress. In the future, this could be enhanced
                    // to fetch actual task progress for more accurate calculations
                    var progress = viewModel.GetProjectProgress(project);
                    var statusBadgeClass = viewModel.GetStatusBadgeClass(project.Status);

                    viewModel.ProjectProgress[project.ProjectId] = progress;
                    viewModel.ProjectStatusBadgeClasses[project.ProjectId] = statusBadgeClass;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClientsController.Index");

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
                };

                TempData["ErrorMessage"] =
                    "API service is currently unavailable. Showing demo data.";
                return View(fallbackViewModel);
            }
        }

        [HttpGet("Clients/ProjectDetail/{id}")]
        public async Task<IActionResult> ProjectDetail(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid project ID.";
                    return RedirectToAction("Index");
                }

                var project = await _apiClient.GetAsync<ProjectDto>(
                    $"/api/clients/project/{id}",
                    User
                );
                if (project == null)
                {
                    TempData["ErrorMessage"] = "Project not found.";
                    return RedirectToAction("Index");
                }

                var phases =
                    await _apiClient.GetAsync<List<PhaseDto>>(
                        $"/api/clients/project/{id}/phases",
                        User
                    ) ?? new List<PhaseDto>();
                var tasks =
                    await _apiClient.GetAsync<List<ProjectTaskDto>>(
                        $"/api/clients/project/{id}/tasks",
                        User
                    ) ?? new List<ProjectTaskDto>();
                var progressReports =
                    await _apiClient.GetAsync<List<ProgressReportDto>>(
                        $"/api/clients/project/{id}/progress-reports",
                        User
                    ) ?? new List<ProgressReportDto>();
                var maintenanceRequests =
                    await _apiClient.GetAsync<List<MaintenanceRequestDto>>(
                        $"/api/clients/project/{id}/maintenance-requests",
                        User
                    ) ?? new List<MaintenanceRequestDto>();
                var quotations =
                    await _apiClient.GetAsync<List<QuotationDto>>(
                        $"/api/clients/project/{id}/quotations",
                        User
                    ) ?? new List<QuotationDto>();
                var invoices =
                    await _apiClient.GetAsync<List<InvoiceDto>>(
                        $"/api/clients/project/{id}/invoices",
                        User
                    ) ?? new List<InvoiceDto>();

                // Load contractor information for task assignments
                var contractors = new List<UserDto>();
                var uniqueContractorIds = tasks
                    .Where(t => !string.IsNullOrEmpty(t.AssignedTo))
                    .Select(t => t.AssignedTo)
                    .Distinct()
                    .ToList();

                foreach (var contractorId in uniqueContractorIds)
                {
                    try
                    {
                        var contractor = await _apiClient.GetAsync<UserDto>(
                            $"/api/users/{contractorId}",
                            User
                        );
                        if (contractor != null)
                        {
                            contractors.Add(contractor);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to load contractor {ContractorId}",
                            contractorId
                        );
                        // Continue loading other contractors even if one fails
                    }
                }

                // Calculate overall progress
                int overallProgress = 0;
                if (tasks.Any())
                {
                    overallProgress = (int)tasks.Average(t => t.Progress);
                }

                var viewModel = new ClientProjectDetailViewModel
                {
                    Project = project,
                    Phases = phases,
                    Tasks = tasks,
                    ProgressReports = progressReports,
                    MaintenanceRequests = maintenanceRequests,
                    Quotations = quotations,
                    Invoices = invoices,
                    Contractors = contractors,
                    OverallProgress = overallProgress,
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching project details for ID {Id}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> MaintenanceRequestDetails(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid maintenance request ID.";
                    return RedirectToAction("Index");
                }

                var request = await _apiClient.GetAsync<MaintenanceRequestDto>(
                    $"/api/maintenanceRequest/{id}",
                    User
                );

                if (request != null)
                {
                    return View(request);
                }

                TempData["ErrorMessage"] = "Failed to fetch maintenance request details.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching maintenance request details for ID {Id}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> QuotationDetails(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid quotation ID.";
                    return RedirectToAction("Index");
                }

                var quotation = await _apiClient.GetAsync<QuotationDto>(
                    $"/api/clients/quotation/{id}",
                    User
                );

                if (quotation != null)
                {
                    return View(quotation);
                }

                TempData["ErrorMessage"] = "Failed to fetch quotation details.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quotation details for ID {Id}", id);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public class RejectQuotationRequest
        {
            public string QuotationId { get; set; }
            public string Reason { get; set; }
        }

        public class ApproveQuotationRequest
        {
            public string QuotationId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> RejectQuotation([FromBody] RejectQuotationRequest request)
        {
            _logger.LogInformation(
                "🟠 [RejectQuotation] Triggered with quotationId={QuotationId}",
                request.QuotationId
            );

            try
            {
                if (string.IsNullOrEmpty(request.QuotationId))
                {
                    return Json(new { success = false, error = "Invalid quotation ID." });
                }

                var endpoint = $"/api/clients/reject/quotation/{request.QuotationId}";
                _logger.LogInformation(
                    "🌐 [RejectQuotation] Sending PUT request to {Endpoint}",
                    endpoint
                );

                var payload = new { reason = request.Reason };
                var result = await _apiClient.PutAsync<object>(endpoint, payload, User);

                if (result != null)
                {
                    _logger.LogInformation(
                        "✅ [RejectQuotation] Quotation rejected successfully for {QuotationId}",
                        request.QuotationId
                    );
                    return Json(
                        new { success = true, message = "Quotation rejected successfully." }
                    );
                }
                else
                {
                    _logger.LogError(
                        "❌ [RejectQuotation] API returned null for quotation {QuotationId}",
                        request.QuotationId
                    );
                    return Json(new { success = false, error = "Failed to reject quotation." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "🔥 [RejectQuotation] Unexpected error while rejecting quotation {QuotationId}",
                    request.QuotationId
                );
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQuotationDetails(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { success = false, error = "Quotation ID is required." });
                }

                // Call the API to get quotation details with line items
                var response = await _apiClient.GetAsync<object>(
                    $"/api/clients/quotation/{id}",
                    User
                );

                if (response != null)
                {
                    return Json(new { success = true, data = response });
                }
                else
                {
                    return Json(new { success = false, error = "Quotation not found." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotation details {QuotationId}", id);
                return Json(
                    new
                    {
                        success = false,
                        error = "An error occurred while loading quotation details.",
                    }
                );
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveQuotation(
            [FromBody] ApproveQuotationRequest request
        )
        {
            try
            {
                if (string.IsNullOrEmpty(request.QuotationId))
                {
                    return Json(new { success = false, error = "Quotation ID is required." });
                }

                // Call the API to approve the quotation using PUT method
                var response = await _apiClient.PutAsync<object>(
                    $"/api/clients/approve/quotation/{request.QuotationId}",
                    null,
                    User
                );

                if (response != null)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, error = "Failed to approve quotation." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error approving quotation {QuotationId}",
                    request.QuotationId
                );
                return Json(
                    new
                    {
                        success = false,
                        error = "An error occurred while approving the quotation.",
                    }
                );
            }
        }

        // ============================================
        // CLIENT: Download Quotation (Stable SA Format)
        // ============================================
        [HttpGet]
        [Route("Clients/DownloadQuotation/{id}")]
        public async Task<IActionResult> DownloadQuotation(string id)
        {
            _logger.LogInformation(
                "📄 [Client-DownloadQuotation] Generating PDF for quotation {Id}",
                id
            );

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var quote = await _apiClient.GetAsync<QuotationDto>(
                $"/api/clients/quotation/{id}",
                User
            );
            if (quote == null)
                return NotFound("Quotation not found");

            var project = await _apiClient.GetAsync<ProjectDto>(
                $"/api/clients/project/{quote.ProjectId}",
                User
            );
            if (project == null)
                return NotFound("Associated project not found");

            var client =
                await _apiClient.GetAsync<UserDto>($"/api/users/{project.ClientId}", User)
                ?? new UserDto { FullName = "Unknown Client" };

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
                                                cols.RelativeColumn(5); // Item
                                                cols.RelativeColumn(1); // Qty
                                                cols.RelativeColumn(2); // Unit Price
                                                cols.RelativeColumn(2); // Total
                                            });

                                            // Header Row
                                            table.Header(h =>
                                            {
                                                AddHeader(h, "Item");
                                                AddHeader(h, "Qty");
                                                AddHeader(h, "Unit Price (R)");
                                                AddHeader(h, "Total (R)");
                                            });

                                            // Data Rows
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

            _logger.LogInformation(
                "✅ [Client-DownloadQuotation] PDF generated for quotation {Id}",
                id
            );
            return File(
                fileBytes,
                "application/pdf",
                $"Quotation_{project.Name.Replace(" ", "_")}.pdf"
            );
        }

        // ============================================
        // CLIENT: Download Invoice (Professional SA Format)
        // ============================================
        [HttpGet]
        [Route("Clients/DownloadInvoice/{id}")]
        public async Task<IActionResult> DownloadInvoice(string id)
        {
            _logger.LogInformation(
                "📄 [Client-DownloadInvoice] Generating professional PDF for invoice {Id}",
                id
            );

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // === FETCH INVOICE ===
            var invoice = await _apiClient.GetAsync<InvoiceDto>($"/api/clients/invoice/{id}", User);
            if (invoice == null)
                return NotFound("Invoice not found");

            // === FETCH PROJECT ===
            var project = await _apiClient.GetAsync<ProjectDto>(
                $"/api/clients/project/{invoice.ProjectId}",
                User
            );
            if (project == null)
                return NotFound("Associated project not found");

            // === FETCH CLIENT ===
            var client =
                await _apiClient.GetAsync<UserDto>($"/api/users/{project.ClientId}", User)
                ?? new UserDto { FullName = "Unknown Client" };

            // === FETCH QUOTATION (if exists) ===
            QuotationDto? quote = null;
            if (!string.IsNullOrWhiteSpace(invoice.QuotationId))
            {
                quote = await _apiClient.GetAsync<QuotationDto>(
                    $"/api/clients/quotation/{invoice.QuotationId}",
                    User
                );
            }

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

                                // --- Title ---
                                stack
                                    .Item()
                                    .AlignCenter()
                                    .Text($"INVOICE #{invoice.InvoiceNumber}")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor("#1A1B25");

                                // --- Client & Project Section ---
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
                                        info.Item()
                                            .Text($"Status: {invoice.Status}")
                                            .FontColor("#000");
                                    });

                                // --- Quote Reference Section ---
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
                                                .Text($"Quote Reference: {quote.QuotationId}")
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

                                // --- Dates Section ---
                                stack
                                    .Item()
                                    .PaddingTop(10)
                                    .Column(dates =>
                                    {
                                        dates.Spacing(2);
                                        dates
                                            .Item()
                                            .Text(
                                                $"Invoice Issued: {invoice.IssuedDate:dd MMM yyyy}"
                                            )
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
                                                .FontColor("#28a745");
                                    });

                                // --- Financial Overview ---
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
                                            .Text($"Total Due: R {invoice.TotalAmount:N2}")
                                            .Bold()
                                            .FontSize(13)
                                            .FontColor("#000");
                                        fin.Item()
                                            .Text($"Currency: {invoice.Currency}")
                                            .FontSize(10)
                                            .FontColor("#555");
                                    });

                                // --- Payment Details ---
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
                            .Text("Thank you for your business — powered by ICCMS")
                            .FontSize(9)
                            .FontColor("#666");
                    });
                })
                .GeneratePdf();

            _logger.LogInformation("✅ [Client-DownloadInvoice] PDF generated for invoice {Id}", id);

            // Create audit log entry for invoice download
            try
            {
                var userId = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var auditLogEntry = new
                    {
                        LogType = "Document Download",
                        Title = "Invoice Downloaded",
                        Description = $"Client downloaded invoice {invoice.InvoiceNumber} for project {project.Name}",
                        UserId = userId,
                        EntityId = id,
                    };

                    await _apiClient.PostAsync<object>("/api/auditlogs", auditLogEntry, User);
                    _logger.LogInformation(
                        "📝 [Client-DownloadInvoice] Audit log created for invoice download {Id}",
                        id
                    );
                }
            }
            catch (Exception auditEx)
            {
                _logger.LogWarning(
                    auditEx,
                    "⚠️ [Client-DownloadInvoice] Failed to create audit log for invoice {Id}",
                    id
                );
                // Don't fail the download if audit logging fails
            }

            return File(
                fileBytes,
                "application/pdf",
                $"Invoice_{project.Name.Replace(" ", "_")}.pdf"
            );
        }

        [HttpPost]
        public async Task<IActionResult> PayInvoice([FromBody] PayInvoiceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.InvoiceId))
                {
                    return Json(new { success = false, error = "Invoice ID is required." });
                }

                // Create payment object for the API
                var payment = new
                {
                    Method = request.PaymentMethod ?? "Online",
                    TransactionId = request.TransactionId ?? Guid.NewGuid().ToString(),
                    Notes = request.Notes ?? "Payment made via client portal",
                };

                // Call the API to process payment
                var result = await _apiClient.PostAsync<object>(
                    $"/api/clients/pay/invoice/{request.InvoiceId}",
                    payment,
                    User
                );

                if (result != null)
                {
                    _logger.LogInformation(
                        "✅ [PayInvoice] Payment processed successfully for invoice {InvoiceId}",
                        request.InvoiceId
                    );
                    return Json(
                        new { success = true, message = "Payment processed successfully." }
                    );
                }
                else
                {
                    _logger.LogError(
                        "❌ [PayInvoice] API returned null for invoice {InvoiceId}",
                        request.InvoiceId
                    );
                    return Json(new { success = false, error = "Failed to process payment." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "🔥 [PayInvoice] Unexpected error while processing payment for invoice {InvoiceId}",
                    request.InvoiceId
                );
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMaintenanceRequest(
            [FromBody] MaintenanceRequestDto request
        )
        {
            _logger.LogInformation(
                "🧱 [CreateMaintenanceRequest] Triggered for project {ProjectId}",
                request.ProjectId
            );

            try
            {
                // ✅ Validate input
                if (
                    string.IsNullOrWhiteSpace(request.ProjectId)
                    || string.IsNullOrWhiteSpace(request.Description)
                )
                    return Json(
                        new { success = false, error = "Project and Description are required." }
                    );

                // 🧩 Fill required fields
                request.MaintenanceRequestId = Guid.NewGuid().ToString("N");
                request.Status = "Pending";
                request.CreatedAt = DateTime.UtcNow;
                request.RequestedBy = User.Identity?.Name ?? "Unknown Client";
                request.MediaUrl ??= string.Empty;

                // 🌐 Send to API (expects JSON response with maintenanceRequestId)
                var created = await _apiClient.PostAsync<MaintenanceRequestDto>(
                    "/api/clients/create/maintenanceRequest",
                    request,
                    User
                );

                if (created == null || string.IsNullOrEmpty(created.MaintenanceRequestId))
                {
                    _logger.LogWarning(
                        "❌ [CreateMaintenanceRequest] API returned null or invalid response for {ProjectId}",
                        request.ProjectId
                    );
                    return Json(
                        new { success = false, error = "Failed to create maintenance request." }
                    );
                }

                _logger.LogInformation(
                    "✅ [CreateMaintenanceRequest] Created successfully with ID {Id}",
                    created.MaintenanceRequestId
                );
                return Json(new { success = true, requestId = created.MaintenanceRequestId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 [CreateMaintenanceRequest] Unexpected error");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMaintenanceRequests()
        {
            _logger.LogInformation(
                "📡 [GetMaintenanceRequests] Fetching all maintenance requests for current client..."
            );

            try
            {
                var requests = await _apiClient.GetAsync<List<MaintenanceRequestDto>>(
                    "/api/clients/maintenanceRequests",
                    User
                );

                if (requests == null || !requests.Any())
                {
                    _logger.LogWarning("⚠️ [GetMaintenanceRequests] No maintenance requests found");
                    return Json(new List<MaintenanceRequestDto>());
                }

                _logger.LogInformation(
                    "✅ [GetMaintenanceRequests] {Count} requests loaded",
                    requests.Count
                );
                return Json(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 [GetMaintenanceRequests] Failed to fetch requests");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(
            IFormFile file,
            string projectId,
            string description
        )
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.FileName);
            content.Add(new StringContent(projectId ?? ""), "projectId");
            content.Add(new StringContent(description ?? ""), "description");

            var apiUrl = $"{_apiBaseUrl}/api/Documents/upload";
            _logger.LogInformation("📤 Forwarding document upload to {ApiUrl}", apiUrl);

            using var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(apiUrl, content);

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "❌ Upload failed with status {Status}: {Body}",
                    response.StatusCode,
                    body
                );
                return StatusCode((int)response.StatusCode, body);
            }

            _logger.LogInformation("✅ Upload succeeded: {Body}", body);
            return Ok(body);
        }

        [HttpGet]
        public async Task<IActionResult> MaintenanceRequestDetailsPartial(string id)
        {
            _logger.LogInformation(
                "🟡 [ClientsController] Entered MaintenanceRequestDetailsPartial() with ID: {Id}",
                id
            );

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning(
                    "⚠️ [ClientsController] No ID provided to MaintenanceRequestDetailsPartial()"
                );
                return BadRequest("Missing request ID");
            }

            try
            {
                _logger.LogInformation(
                    "📡 [ClientsController] Calling API endpoint for maintenance request..."
                );
                var endpoint = $"/api/clients/maintenanceRequest/{id}";
                _logger.LogInformation(
                    "➡️ [ClientsController] Full API path: {Endpoint}",
                    endpoint
                );

                var request = await _apiClient.GetAsync<MaintenanceRequestDto>(endpoint, User);
                _logger.LogInformation(
                    "🧾 [ClientsController] Retrieved model: {@Request}",
                    request
                );

                if (request == null)
                {
                    _logger.LogWarning("❌ [ClientsController] API returned NULL for ID: {Id}", id);
                    return NotFound($"Maintenance request not found for ID {id}");
                }

                _logger.LogInformation(
                    "✅ [ClientsController] Maintenance request retrieved successfully for ID: {Id}",
                    id
                );
                return PartialView("_MaintenanceRequestDetailsPartial", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "🔥 [ClientsController] Exception in MaintenanceRequestDetailsPartial() for ID: {Id}",
                    id
                );
                return StatusCode(500, "Error fetching maintenance details");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProjectDetailsPartial(string id)
        {
            _logger.LogInformation(
                "🟡 [ClientsController] Entered ProjectDetailsPartial() with ID: {Id}",
                id
            );

            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Missing project ID");

            try
            {
                var endpoint = $"/api/clients/project/{id}";
                _logger.LogInformation(
                    "➡️ [ClientsController] Full API path: {Endpoint}",
                    endpoint
                );

                var project = await _apiClient.GetAsync<ProjectDto>(endpoint, User);

                if (project == null)
                {
                    _logger.LogWarning(
                        "❌ [ClientsController] API returned NULL for project ID {Id}",
                        id
                    );
                    return NotFound($"Project not found for ID {id}");
                }

                _logger.LogInformation(
                    "✅ [ClientsController] Project retrieved successfully for ID: {Id}",
                    id
                );
                return PartialView("_ProjectDetailsPartial", project);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "🔥 [ClientsController] Exception in ProjectDetailsPartial() for ID: {Id}",
                    id
                );
                return StatusCode(500, "Error fetching project details");
            }
        }

        [HttpGet]
        public async Task<IActionResult> QuotationDetailsPartial(string id)
        {
            _logger.LogInformation(
                "🧾 [ClientsController] Fetching quotation partial for ID: {Id}",
                id
            );

            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Missing quotation ID");

            try
            {
                var endpoint = $"/api/clients/quotation/{id}";
                var quotation = await _apiClient.GetAsync<QuotationDto>(endpoint, User);

                if (quotation == null)
                {
                    _logger.LogWarning("❌ No quotation found for ID: {Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("✅ Quotation retrieved successfully: {Id}", id);
                return PartialView("_QuotationDetailsPartial", quotation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error retrieving quotation {Id}", id);
                return StatusCode(500, "Failed to load quotation details");
            }
        }

        [HttpGet]
        public async Task<IActionResult> InvoiceDetailsPartial(string id)
        {
            _logger.LogInformation(
                "🧾 [ClientsController] Fetching invoice partial for ID: {Id}",
                id
            );

            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Missing invoice ID");

            try
            {
                var endpoint = $"/api/clients/invoice/{id}";
                var invoice = await _apiClient.GetAsync<InvoiceDto>(endpoint, User);

                if (invoice == null)
                {
                    _logger.LogWarning("❌ No invoice found for ID: {Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("✅ Invoice retrieved successfully: {Id}", id);
                return PartialView("_InvoiceDetailsPartial", invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Error retrieving invoice {Id}", id);
                return StatusCode(500, "Failed to load invoice details");
            }
        }
    }

    public class ClientDashboardViewModel
    {
        public List<ProjectDto> Projects { get; set; } = new List<ProjectDto>();
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }

        public Dictionary<string, int> ProjectProgress { get; set; } =
            new Dictionary<string, int>();
        public Dictionary<string, string> ProjectStatusBadgeClasses { get; set; } =
            new Dictionary<string, string>();

        public int GetProjectProgress(ProjectDto project)
        {
            if (ProjectProgress.ContainsKey(project.ProjectId))
            {
                return ProjectProgress[project.ProjectId];
            }

            // Default progress based on status
            return project.Status?.ToLowerInvariant() switch
            {
                "completed" => 100,
                "active" => 50,
                "planning" => 25,
                "maintenance" => 90,
                "cancelled" => 0,
                _ => 0,
            };
        }

        public string GetStatusBadgeClass(string status)
        {
            return status?.ToLowerInvariant() switch
            {
                "draft" => "badge-secondary",
                "planning" => "badge-info",
                "active" => "badge-primary",
                "completed" => "badge-success",
                "maintenance" => "badge-warning",
                "cancelled" => "badge-danger",
                _ => "badge-light",
            };
        }
    }

    public class PayInvoiceRequest
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
    }
}
