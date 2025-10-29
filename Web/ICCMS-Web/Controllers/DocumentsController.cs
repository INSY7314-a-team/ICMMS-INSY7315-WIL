using System.Diagnostics;
using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Admin,Tester")]
    public class DocumentsController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<DocumentsController> logger
        )
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 25,
            string projectFilter = "all",
            string typeFilter = "all",
            string searchTerm = ""
        )
        {
            try
            {
                _logger.LogInformation(
                    "Loading documents management dashboard for user: {User}",
                    User.Identity?.Name
                );

                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
                // Debug: Log all available claims
                _logger.LogInformation("Available user claims:");
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation(
                        "Claim: {Type} = {Value}",
                        claim.Type,
                        claim.Value?.Substring(0, Math.Min(50, claim.Value?.Length ?? 0)) + "..."
                    );
                }

                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                _logger.LogInformation(
                    "Firebase token found: {HasToken}",
                    !string.IsNullOrEmpty(firebaseToken)
                );

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    _logger.LogWarning("Firebase token not found for user");
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {firebaseToken}");

                // Get documents with filtering
                var documentsResponse = await GetDocuments(apiBaseUrl, projectFilter);
                _logger.LogInformation(
                    "Retrieved {Count} documents from API",
                    documentsResponse.Count
                );

                // Get projects for filter dropdown
                var projectsResponse = await GetProjects(apiBaseUrl);
                _logger.LogInformation(
                    "Projects response: {ProjectsResponse}",
                    JsonSerializer.Serialize(projectsResponse)
                );
                _logger.LogInformation(
                    "Retrieved {Count} projects from API",
                    projectsResponse.Count
                );

                // Get users for display
                var usersResponse = await GetUsers(apiBaseUrl);
                _logger.LogInformation("Retrieved {Count} users from API", usersResponse.Count);

                // Populate project names in documents
                _logger.LogInformation(
                    "Available projects: {ProjectCount}",
                    projectsResponse.Count
                );

                // Debug: Log all project IDs and names
                foreach (var proj in projectsResponse)
                {
                    _logger.LogInformation(
                        "Available Project - ID: '{ProjectId}', Name: '{ProjectName}'",
                        proj.Id,
                        proj.Name
                    );
                }

                foreach (var doc in documentsResponse)
                {
                    _logger.LogInformation(
                        "Document - FileName: '{FileName}', ProjectId: '{ProjectId}'",
                        doc.FileName,
                        doc.ProjectId
                    );

                    var project = projectsResponse.FirstOrDefault(p => p.Id == doc.ProjectId);
                    if (project != null)
                    {
                        doc.ProjectName = project.Name;
                        _logger.LogInformation(
                            "✅ MATCH FOUND - Document {FileName} -> Project: {ProjectName}",
                            doc.FileName,
                            project.Name
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "❌ NO MATCH - Document {FileName} has ProjectId '{ProjectId}' but no matching project found",
                            doc.FileName,
                            doc.ProjectId
                        );
                    }
                }

                // Apply search term filtering after all data is populated
                var filteredDocuments = documentsResponse
                    .Where(doc =>
                        string.IsNullOrEmpty(searchTerm)
                        || doc.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                        || (
                            doc.Description != null
                            && doc.Description.Contains(
                                searchTerm,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                        || doc.ProjectName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                var viewModel = new DocumentsViewModel
                {
                    Documents = filteredDocuments, // Use filtered list
                    AvailableProjects = projectsResponse,
                    AvailableUsers = usersResponse,
                    CurrentPage = page,
                    PageSize = pageSize,
                    CurrentProjectFilter = projectFilter,
                    CurrentTypeFilter = typeFilter,
                    CurrentSearchTerm = searchTerm,
                    TotalDocuments = filteredDocuments.Count, // Use filtered count
                    TotalPages = (int)Math.Ceiling((double)filteredDocuments.Count / pageSize),
                };

                // Apply pagination
                viewModel.Documents = filteredDocuments
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation(
                    "Documents dashboard loaded successfully with {Count} documents",
                    viewModel.Documents.Count
                );

                // Debug: Log first document details
                if (viewModel.Documents.Count > 0)
                {
                    var firstDoc = viewModel.Documents[0];
                    _logger.LogInformation(
                        "First document in view model: FileName={FileName}, ProjectId={ProjectId}, ProjectName={ProjectName}",
                        firstDoc.FileName,
                        firstDoc.ProjectId,
                        firstDoc.ProjectName
                    );

                    // Debug: Log all properties of the first document
                    _logger.LogInformation("First document all properties:");
                    _logger.LogInformation("  FileName: '{FileName}'", firstDoc.FileName);
                    _logger.LogInformation("  ProjectId: '{ProjectId}'", firstDoc.ProjectId);
                    _logger.LogInformation("  ProjectName: '{ProjectName}'", firstDoc.ProjectName);
                    _logger.LogInformation("  Description: '{Description}'", firstDoc.Description);
                    _logger.LogInformation("  UploadedBy: '{UploadedBy}'", firstDoc.UploadedBy);
                    _logger.LogInformation("  FileSize: {FileSize}", firstDoc.FileSize);
                    _logger.LogInformation("  ContentType: '{ContentType}'", firstDoc.ContentType);
                    _logger.LogInformation("  IsApproved: {IsApproved}", firstDoc.IsApproved);
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading documents dashboard");
                return View(new DocumentsViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> Upload(
            IFormFile file,
            string projectId,
            string description
        )
        {
            try
            {
                _logger.LogInformation("Uploading document: {FileName}", file?.FileName);

                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(new { success = false, message = "Authentication required" });
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {firebaseToken}");

                using var content = new MultipartFormDataContent();
                content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
                content.Add(new StringContent(projectId), "projectId");
                if (!string.IsNullOrEmpty(description))
                {
                    content.Add(new StringContent(description), "description");
                }

                var response = await _httpClient.PostAsync(
                    $"{apiBaseUrl}/api/documents/upload",
                    content
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Document uploaded successfully: {FileName}",
                        file.FileName
                    );
                    return Json(new { success = true, message = "Document uploaded successfully" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Document upload failed: {Error}", errorContent);
                    return Json(new { success = false, message = "Upload failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return Json(new { success = false, message = "Upload error occurred" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] DeleteDocumentRequest request)
        {
            try
            {
                _logger.LogInformation("Deleting document: {FileName}", request.FileName);

                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return Json(new { success = false, message = "Authentication required" });
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {firebaseToken}");

                var response = await _httpClient.DeleteAsync(
                    $"{apiBaseUrl}/api/admin/delete/documents/{request.FileName}"
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Document deleted successfully: {FileName}",
                        request.FileName
                    );
                    return Json(new { success = true, message = "Document deleted successfully" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Document deletion failed: {Error}", errorContent);
                    return Json(new { success = false, message = "Deletion failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                return Json(new { success = false, message = "Deletion error occurred" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            try
            {
                _logger.LogInformation("Downloading document: {FileName}", fileName);

                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {firebaseToken}");

                var response = await _httpClient.GetAsync($"{apiBaseUrl}/api/documents/{fileName}");

                _logger.LogInformation(
                    "Download API response status: {StatusCode}",
                    response.StatusCode
                );

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    var contentType =
                        response.Content.Headers.ContentType?.ToString()
                        ?? "application/octet-stream";

                    _logger.LogInformation(
                        "Document downloaded successfully: {FileName}, Size: {Size} bytes, ContentType: {ContentType}",
                        fileName,
                        content.Length,
                        contentType
                    );

                    // Create audit log for document download
                    await CreateDownloadAuditLog(fileName, content.Length, contentType);

                    return File(content, contentType, fileName);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Document download failed: {StatusCode}, Error: {Error}",
                        response.StatusCode,
                        errorContent
                    );
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document");
                return StatusCode(500);
            }
        }

        private async Task<List<DocumentViewModel>> GetDocuments(
            string apiBaseUrl,
            string projectFilter
        )
        {
            try
            {
                var url = $"{apiBaseUrl}/api/documents/metadata";
                if (!string.IsNullOrEmpty(projectFilter) && projectFilter != "all")
                {
                    url += $"?projectId={projectFilter}";
                }

                var response = await _httpClient.GetAsync(url);

                _logger.LogInformation(
                    "API call to {Url} returned status {StatusCode}",
                    url,
                    response.StatusCode
                );

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Received {Length} characters from API", content.Length);

                    var documents = JsonSerializer.Deserialize<List<JsonElement>>(content);
                    _logger.LogInformation("Deserialized {Count} documents", documents?.Count ?? 0);

                    // Debug: Log the first document structure
                    if (documents?.Count > 0)
                    {
                        _logger.LogInformation(
                            "First document structure: {DocumentJson}",
                            documents[0].GetRawText()
                        );

                        // Debug: Log individual field values
                        var firstDoc = documents[0];
                        _logger.LogInformation("First document fields:");
                        _logger.LogInformation(
                            "  fileName: {FileName}",
                            firstDoc.TryGetProperty("fileName", out var fn)
                                ? fn.GetString()
                                : "NOT_FOUND"
                        );
                        _logger.LogInformation(
                            "  projectId: {ProjectId}",
                            firstDoc.TryGetProperty("projectId", out var pid)
                                ? pid.GetString()
                                : "NOT_FOUND"
                        );
                        _logger.LogInformation(
                            "  fileSize: {FileSize}",
                            firstDoc.TryGetProperty("fileSize", out var fs) ? fs.GetInt64() : -1
                        );
                        _logger.LogInformation(
                            "  status: {Status}",
                            firstDoc.TryGetProperty("status", out var st)
                                ? st.GetString()
                                : "NOT_FOUND"
                        );
                        _logger.LogInformation(
                            "  uploadedBy: {UploadedBy}",
                            firstDoc.TryGetProperty("uploadedBy", out var ub)
                                ? ub.GetString()
                                : "NOT_FOUND"
                        );

                        // Debug: Log all available properties
                        _logger.LogInformation("All available properties in first document:");
                        foreach (var prop in firstDoc.EnumerateObject())
                        {
                            _logger.LogInformation(
                                "  {PropertyName}: {PropertyValue}",
                                prop.Name,
                                prop.Value
                            );
                        }
                    }

                    var documentViewModels = new List<DocumentViewModel>();

                    foreach (var doc in documents)
                    {
                        var documentViewModel = new DocumentViewModel
                        {
                            FileName = doc.TryGetProperty("fileName", out var fileName)
                                ? fileName.GetString()
                                : "Unknown",
                            ProjectId = doc.TryGetProperty("projectId", out var projectId)
                                ? projectId.GetString()
                                : "",
                            ProjectName = "Unknown Project", // Will be populated later
                            Description = doc.TryGetProperty("description", out var description)
                                ? description.GetString()
                                : "",
                            UploadedBy = doc.TryGetProperty("uploadedBy", out var uploadedBy)
                                ? (
                                    !string.IsNullOrEmpty(uploadedBy.GetString())
                                        ? uploadedBy.GetString()
                                        : "System"
                                )
                                : "Unknown",
                            UploadedAt = doc.TryGetProperty("uploadedAt", out var uploadedAt)
                                ? uploadedAt.GetDateTime()
                                : DateTime.UtcNow,
                            FileSize = doc.TryGetProperty("fileSize", out var fileSize)
                                ? fileSize.GetInt64()
                                : 0,
                            ContentType = doc.TryGetProperty("fileType", out var fileType)
                                ? fileType.GetString()
                                : "application/octet-stream",
                            Category = "General", // Default category
                            IsApproved = doc.TryGetProperty("status", out var status)
                                ? status.GetString() == "Active"
                                : false,
                            ApprovedBy = "",
                            ApprovedAt = null,
                        };

                        documentViewModels.Add(documentViewModel);
                    }

                    return documentViewModels.OrderByDescending(d => d.UploadedAt).ToList();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "API call failed with status {StatusCode}: {Error}",
                        response.StatusCode,
                        errorContent
                    );
                }

                return new List<DocumentViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching documents");
                return new List<DocumentViewModel>();
            }
        }

        private async Task<List<Models.ProjectSummary>> GetProjects(string apiBaseUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{apiBaseUrl}/api/documents/projects-with-documents"
                );

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var projects = JsonSerializer.Deserialize<List<JsonElement>>(content);

                    _logger.LogInformation(
                        "Projects API returned {Count} projects",
                        projects?.Count ?? 0
                    );

                    // Debug: Log the first project structure to see field names
                    if (projects?.Count > 0)
                    {
                        var firstProject = projects[0];
                        _logger.LogInformation(
                            "First project structure: {ProjectJson}",
                            firstProject.GetRawText()
                        );

                        // Log all available properties
                        _logger.LogInformation("Available properties in first project:");
                        foreach (var prop in firstProject.EnumerateObject())
                        {
                            _logger.LogInformation(
                                "  {PropertyName}: {PropertyValue}",
                                prop.Name,
                                prop.Value
                            );
                        }
                    }

                    return projects
                        .Select(p => new Models.ProjectSummary
                        {
                            Id = p.TryGetProperty("projectId", out var id) ? id.GetString() : "",
                            Name = p.TryGetProperty("name", out var name)
                                ? name.GetString()
                                : "Unknown Project",
                        })
                        .ToList();
                }

                return new List<Models.ProjectSummary>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching projects");
                return new List<Models.ProjectSummary>();
            }
        }

        private async Task<List<Models.UserSummary>> GetUsers(string apiBaseUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{apiBaseUrl}/api/admin/users");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<JsonElement>>(content);

                    return users
                        .Select(u => new Models.UserSummary
                        {
                            Id = u.TryGetProperty("userId", out var id) ? id.GetString() : "",
                            FullName = u.TryGetProperty("fullName", out var fullName)
                                ? fullName.GetString()
                                : "Unknown User",
                            Email = u.TryGetProperty("email", out var email)
                                ? email.GetString()
                                : "",
                        })
                        .ToList();
                }

                return new List<Models.UserSummary>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return new List<Models.UserSummary>();
            }
        }

        private async Task CreateDownloadAuditLog(
            string fileName,
            long fileSize,
            string contentType
        )
        {
            try
            {
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                var userId = User.FindFirst("UserId")?.Value ?? User.Identity?.Name ?? "Unknown";

                if (string.IsNullOrEmpty(firebaseToken))
                {
                    _logger.LogWarning("Cannot create audit log: Firebase token not found");
                    return;
                }

                var auditLogData = new
                {
                    LogType = "DocumentDownload",
                    Title = "Document Downloaded",
                    Description = $"User downloaded document: {fileName} (Size: {FormatFileSize(fileSize)}, Type: {contentType})",
                    UserId = userId,
                    EntityId = fileName,
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {firebaseToken}");

                var jsonContent = JsonSerializer.Serialize(auditLogData);
                var content = new StringContent(
                    jsonContent,
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{apiBaseUrl}/api/auditlogs", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Audit log created for document download: {FileName}",
                        fileName
                    );
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "Failed to create audit log for document download: {StatusCode}, Error: {Error}",
                        response.StatusCode,
                        errorContent
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating audit log for document download: {FileName}",
                    fileName
                );
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0)
                return "0 Bytes";
            string[] sizes = { "Bytes", "KB", "MB", "GB" };
            int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
            return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
        }
    }

    public class DeleteDocumentRequest
    {
        public string FileName { get; set; } = string.Empty;
    }
}
