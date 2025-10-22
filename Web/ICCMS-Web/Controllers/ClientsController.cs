using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using ICCMS_Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Text;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ICCMS_Web.Models;
using System.IO;
using System.Threading.Tasks;

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

        public ClientsController(
            IApiClient apiClient,
            ILogger<ClientsController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory
        )
        {
            _apiClient = apiClient;
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }


        public async Task<IActionResult> Index()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    _logger.LogWarning("No FirebaseToken found for user {User}", User.Identity?.Name);
                    TempData["ErrorMessage"] = "Authentication token not found. Please login again.";
                    return RedirectToAction("Login", "Auth");
                }

                var projects = new List<ProjectDto>();
                var quotations = new List<QuotationDto>();

                // === Get Client Projects ===
                try
                {
                    projects = await _apiClient.GetAsync<List<ProjectDto>>("/api/clients/projects", User)
                            ?? new List<ProjectDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve client projects via API.");
                    projects = new List<ProjectDto>
                    {
                        new ProjectDto
                        {
                            Name = "Sample Project",
                            Description = "API service unavailable"
                        }
                    };
                }

                // === Get Client Quotations ===
                try
                {
                    quotations = await _apiClient.GetAsync<List<QuotationDto>>("/api/clients/quotations", User)
                                ?? new List<QuotationDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve client quotations via API.");
                    quotations = new List<QuotationDto>
                    {
                        new QuotationDto
                        {
                            QuotationId = "demo-1",
                            Status = "API Unavailable"
                        }
                    };
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
                _logger.LogError(ex, "Error in ClientsController.Index");

                var fallbackViewModel = new ClientDashboardViewModel
                {
                    Projects = new List<ProjectDto>
                    {
                        new ProjectDto
                        {
                            Name = "Service Unavailable",
                            Description = "API connection failed"
                        }
                    },
                    Quotations = new List<QuotationDto>
                    {
                        new QuotationDto
                        {
                            QuotationId = "demo-1",
                            Status = "Service Unavailable"
                        }
                    }
                };

                TempData["ErrorMessage"] = "API service is currently unavailable. Showing demo data.";
                return View(fallbackViewModel);
            }
        }


            public async Task<IActionResult> ProjectDetails(string id)
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                    {
                        TempData["ErrorMessage"] = "Invalid project ID.";
                        return RedirectToAction("Index");
                    }

                    var project = await _apiClient.GetAsync<ProjectDto>($"/api/clients/project/{id}", User);

                    if (project != null)
                    {
                        return View(project);
                    }

                    TempData["ErrorMessage"] = "Failed to fetch project details.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching project details for ID {Id}", id);
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

                var quotation = await _apiClient.GetAsync<QuotationDto>($"/api/clients/quotation/{id}", User);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveQuotation(string quotationId)
        {
            _logger.LogInformation("🟢 [ApproveQuotation] Triggered with quotationId={QuotationId}", quotationId);

            try
            {
                if (string.IsNullOrEmpty(quotationId))
                {
                    TempData["ErrorMessage"] = "Invalid quotation ID.";
                    return RedirectToAction("Index");
                }

                var endpoint = $"/api/clients/approve/quotation/{quotationId}";
                _logger.LogInformation("🌐 [ApproveQuotation] Sending PUT request to {Endpoint}", endpoint);

                var result = await _apiClient.PutAsync<object>(endpoint, null, User);

                if (result != null)
                {
                    _logger.LogInformation("✅ [ApproveQuotation] Quotation approved successfully for {QuotationId}", quotationId);
                    TempData["SuccessMessage"] = "Quotation approved successfully.";
                }
                else
                {
                    _logger.LogError("❌ [ApproveQuotation] API returned null for quotation {QuotationId}", quotationId);
                    TempData["ErrorMessage"] = "Failed to approve quotation.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 [ApproveQuotation] Unexpected error while approving quotation {QuotationId}", quotationId);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectQuotation(string quotationId)
        {
            _logger.LogInformation("🟠 [RejectQuotation] Triggered with quotationId={QuotationId}", quotationId);

            try
            {
                if (string.IsNullOrEmpty(quotationId))
                {
                    TempData["ErrorMessage"] = "Invalid quotation ID.";
                    return RedirectToAction("Index");
                }

                var endpoint = $"/api/clients/reject/quotation/{quotationId}";
                _logger.LogInformation("🌐 [RejectQuotation] Sending PUT request to {Endpoint}", endpoint);

                var result = await _apiClient.PutAsync<object>(endpoint, null, User);

                if (result != null)
                {
                    _logger.LogInformation("✅ [RejectQuotation] Quotation rejected successfully for {QuotationId}", quotationId);
                    TempData["SuccessMessage"] = "Quotation rejected successfully.";
                }
                else
                {
                    _logger.LogError("❌ [RejectQuotation] API returned null for quotation {QuotationId}", quotationId);
                    TempData["ErrorMessage"] = "Failed to reject quotation.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 [RejectQuotation] Unexpected error while rejecting quotation {QuotationId}", quotationId);
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // ============================
        // CLIENT: Download Quote (GET)
        // ============================
        [HttpGet]
        [Route("Clients/DownloadQuotation/{id}")]
        public async Task<IActionResult> DownloadQuotation(string id)
        {
            _logger.LogInformation("📄 [Client-DownloadQuotation] Generating PDF for quotation {Id}", id);

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            QuestPDF.Settings.EnableDebugging = false;

            var quote = await _apiClient.GetAsync<QuotationDto>($"/api/clients/quotation/{id}", User);
            if (quote == null)
            {
                _logger.LogWarning("❌ Quotation not found for ID {Id}", id);
                return NotFound("Quotation not found");
            }

            var fileBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));
                    page.Background("#FFFFFF");

                    // ===== HEADER =====
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(t => t.Span("TASKIT").FontSize(22).Bold().FontColor("#222"));
                            col.Item().Text(t => t.Span("Integrated Construction & Maintenance Management System")
                                                .FontSize(10).FontColor("#666"));
                            col.Item().Text(t => t.Span("support@taskit.co.za | +27 87 123 4567")
                                                .FontSize(9).FontColor("#666"));
                        });

                        row.ConstantItem(110).AlignRight().Height(50).Width(100).Element(e =>
                        {
                            e.Image("wwwroot/images/TaskIt2.png").FitHeight();
                        });
                    });

                    // ===== CONTENT =====
                    page.Content().PaddingVertical(15).Column(stack =>
                    {
                        stack.Spacing(10);

                        // --- Summary box ---
                        stack.Item().Border(1).BorderColor("#FFD54F")
                            .Padding(10).Background("#FFFDE7")
                            .Column(summary =>
                            {
                                summary.Spacing(2);
                                summary.Item().Text(t => t.Span($"Quotation ID: {quote.QuotationId}").Bold());
                                summary.Item().Text(t => t.Span($"Project ID: {quote.ProjectId}"));
                                summary.Item().Text(t => t.Span($"Client ID: {quote.ClientId}"));
                                summary.Item().Text(t => t.Span($"Issued: {quote.CreatedAt:dd MMM yyyy}"));
                                summary.Item().Text(t => t.Span($"Valid Until: {quote.ValidUntil:dd MMM yyyy}"));
                            });

                        if (!string.IsNullOrWhiteSpace(quote.Description))
                            stack.Item().PaddingTop(10)
                                .Text(t => t.Span($"Description: {quote.Description}").FontColor("#333"));

                        // ===== TABLE =====
                        stack.Item().PaddingTop(10).Element(e =>
                        {
                            e.Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(4);
                                    cols.RelativeColumn(1);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                });

                                // header row
                                table.Header(h =>
                                {
                                    AddHeader(h, "Item");
                                    AddHeader(h, "Description");
                                    AddHeader(h, "Qty");
                                    AddHeader(h, "Unit Price (R)");
                                    AddHeader(h, "Total (R)");
                                });

                                if (quote.Items != null && quote.Items.Any())
                                {
                                    foreach (var item in quote.Items)
                                    {
                                        AddCell(table, item.Name);
                                        AddCell(table, item.Description);
                                        AddCell(table, item.Quantity.ToString());
                                        AddCell(table, item.UnitPrice.ToString("N2"));
                                        AddCell(table, item.LineTotal.ToString("N2"));
                                    }
                                }
                                else
                                {
                                    table.Cell().ColumnSpan(5)
                                        .BorderBottom(0.5f).BorderColor("#EEE")
                                        .AlignCenter()
                                        .Text(t => t.Span("No line items available"));
                                }
                            });
                        });

                        // ===== TOTALS =====
                        stack.Item().PaddingTop(20).AlignRight().Column(tot =>
                        {
                            tot.Item().Text(t => t.Span($"Subtotal: R {quote.Subtotal:N2}"));
                            tot.Item().Text(t => t.Span($"Tax (15%): R {quote.TaxTotal:N2}"));
                            tot.Item().Text(t => t.Span($"Grand Total: R {quote.GrandTotal:N2}")
                                                    .Bold().FontSize(13).FontColor("#000"));
                        });

                        // ===== BANKING =====
                        stack.Item().PaddingTop(25).BorderTop(1).BorderColor("#DDD").PaddingTop(10).Column(bank =>
                        {
                            bank.Item().Text(t => t.Span("Banking Details").Bold().FontSize(12).FontColor("#222"));
                            bank.Item().Text(t => t.Span("Bank: FNB | Acc No: 0000000000 | Branch: 250655 | Ref: Project ID"));
                            bank.Item().Text(t => t.Span("Email proof of payment to accounts@taskit.co.za"));
                        });
                    });

                    // ===== FOOTER =====
                    page.Footer().AlignCenter().PaddingTop(10)
                        .Text(t => t.Span("Thank you for choosing TaskIt — powered by ICCMS")
                                    .FontSize(9).FontColor("#777"));
                });

                // --- local helpers ---
                static void AddHeader(TableCellDescriptor h, string text)
                {
                    h.Cell().BorderBottom(0.5f).BorderColor("#ccc")
                        .Background("#FFD54F").Padding(5)
                        .Text(t => t.Span(text).Bold().FontColor("#000"));
                }

                static void AddCell(TableDescriptor table, string? text)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor("#EEE")
                        .PaddingVertical(4).PaddingHorizontal(3)
                        .Text(t => t.Span(text ?? "—"));
                }
            }).GeneratePdf();

            _logger.LogInformation("✅ [Client-DownloadQuotation] PDF generated for quotation {Id}", id);
            return File(fileBytes, "application/pdf", $"Quotation_{id}.pdf");
        }


    }

    public class ClientDashboardViewModel
    {
        public List<ProjectDto> Projects { get; set; } = new List<ProjectDto>();
        public List<QuotationDto> Quotations { get; set; } = new List<QuotationDto>();
    }
}
