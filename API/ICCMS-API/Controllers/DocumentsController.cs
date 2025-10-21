using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")] // All authenticated users can access documents
    public class DocumentsController : ControllerBase
    {
        private readonly ISupabaseService _supabaseService;
        private readonly IFirebaseService _firebaseService;

        public DocumentsController(
            ISupabaseService supabaseService,
            IFirebaseService firebaseService
        )
        {
            _supabaseService = supabaseService;
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetDocuments()
        {
            try
            {
                var documents = await _supabaseService.ListFilesAsync("upload");
                Console.WriteLine($"Documents: {documents}");
                return Ok(documents);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting documents: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{fileName}")]
        public async Task<ActionResult<byte[]>> GetDocument(string fileName)
        {
            try
            {
                Console.WriteLine($"Getting document: {fileName}");
                var document = await _supabaseService.DownloadFileAsync("upload", fileName);
                if (document == null || document.Length == 0)
                    return NotFound();
                Console.WriteLine($"Document found: {fileName}");
                return Ok(document);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting document: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<Document>>> GetDocumentsByProject(string projectId)
        {
            try
            {
                var allDocuments = await _firebaseService.GetCollectionAsync<Document>("documents");
                var documents = allDocuments.Where(d => d.ProjectId == projectId).ToList();
                // Normalize URL for consumers (older records might have empty FileUrl or wrong bucket)
                foreach (var d in documents)
                {
                    if (
                        string.IsNullOrWhiteSpace(d.FileUrl)
                        && !string.IsNullOrWhiteSpace(d.FileName)
                    )
                    {
                        // Build public URL from bucket name used in Upload ("upload")
                        var bucket = _supabaseService.SupabaseClient.Storage.From("upload");
                        d.FileUrl = bucket.GetPublicUrl(d.FileName);
                    }
                    else if (
                        !string.IsNullOrWhiteSpace(d.FileUrl) && d.FileUrl.Contains("icmms-storage")
                    )
                    {
                        // Fix URLs that still have the old bucket name
                        var bucket = _supabaseService.SupabaseClient.Storage.From("upload");
                        d.FileUrl = bucket.GetPublicUrl(d.FileName);
                    }
                }
                return Ok(documents);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting documents: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<ActionResult<Document>> UploadDocument(
            [FromForm] IFormFile file,
            [FromForm] string projectId,
            [FromForm] string description = ""
        )
        {
            try
            {
                Console.WriteLine(
                    $"UploadDocument called with file: {file?.FileName}, projectId: {projectId}, description: {description}"
                );

                if (file == null || file.Length == 0)
                {
                    Console.WriteLine("No file uploaded");
                    return BadRequest("No file uploaded");
                }

                Console.WriteLine(
                    $"File details: {file.FileName}, Size: {file.Length}, ContentType: {file.ContentType}"
                );

                // Generate a unique filename
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                Console.WriteLine($"Generated filename: {fileName}");

                // Upload file to Supabase
                using var stream = file.OpenReadStream();
                Console.WriteLine("Starting Supabase upload...");

                var fileUrl = await _supabaseService.UploadFileAsync(
                    "upload",
                    fileName,
                    stream,
                    file.ContentType
                );

                Console.WriteLine($"Supabase upload successful. File URL: {fileUrl}");

                // Create document metadata
                var document = new Document
                {
                    DocumentId = Guid.NewGuid().ToString(),
                    ProjectId = projectId,
                    FileName = fileName,
                    FileType = file.ContentType,
                    FileSize = file.Length,
                    FileUrl = fileUrl,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy =
                        User?.Identity?.Name ?? User?.FindFirst("user_id")?.Value ?? string.Empty,
                    Description = description,
                    Status = "Active",
                };

                await _firebaseService.AddDocumentAsync("documents", document);

                Console.WriteLine("Document metadata created successfully");
                return Ok(document);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UploadDocument: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("update/{fileName}")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> UpdateDocument(
            string fileName,
            [FromForm] IFormFile file,
            [FromForm] string description = ""
        )
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                // Upload updated file to Supabase
                using var stream = file.OpenReadStream();
                await _supabaseService.UploadFileAsync(
                    "upload",
                    fileName,
                    stream,
                    file.ContentType
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{fileName}")]
        public async Task<IActionResult> DeleteDocument(string fileName)
        {
            try
            {
                await _supabaseService.DeleteFileAsync("upload", fileName);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("document/{documentId}")]
        public async Task<IActionResult> DeleteDocumentById(string documentId)
        {
            try
            {
                // Get the document from Firestore first to get the fileName
                var document = await _firebaseService.GetDocumentAsync<Document>(
                    "documents",
                    documentId
                );
                if (document == null)
                {
                    return NotFound("Document not found");
                }

                // Delete from Supabase storage
                if (!string.IsNullOrWhiteSpace(document.FileName))
                {
                    await _supabaseService.DeleteFileAsync("upload", document.FileName);
                }

                // Delete from Firestore
                await _firebaseService.DeleteDocumentAsync("documents", documentId);

                return Ok(
                    new { message = "Document deleted successfully from both storage and database" }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting document {documentId}: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("fix-bucket-urls")]
        public async Task<IActionResult> FixBucketUrls()
        {
            try
            {
                // Get all documents from Firestore
                var allDocuments = await _firebaseService.GetCollectionAsync<Document>("documents");
                var documentsToFix = allDocuments
                    .Where(d =>
                        !string.IsNullOrWhiteSpace(d.FileUrl) && d.FileUrl.Contains("icmms-storage")
                    )
                    .ToList();

                Console.WriteLine($"Found {documentsToFix.Count} documents with wrong bucket URLs");

                var bucket = _supabaseService.SupabaseClient.Storage.From("upload");
                int fixedCount = 0;

                foreach (var doc in documentsToFix)
                {
                    // Generate correct URL with "upload" bucket
                    var correctUrl = bucket.GetPublicUrl(doc.FileName);

                    // Update the document in Firestore
                    doc.FileUrl = correctUrl;
                    await _firebaseService.UpdateDocumentAsync("documents", doc.DocumentId, doc);

                    Console.WriteLine($"Fixed URL for document {doc.DocumentId}: {correctUrl}");
                    fixedCount++;
                }

                return Ok(
                    new
                    {
                        message = $"Fixed {fixedCount} document URLs",
                        fixedCount = fixedCount,
                        totalDocuments = allDocuments.Count,
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fixing bucket URLs: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("check-bucket")]
        public async Task<IActionResult> CheckBucket()
        {
            try
            {
                Console.WriteLine("Checking Supabase bucket status...");

                // Check if "upload" bucket exists
                var uploadExists = await _supabaseService.EnsureBucketExistsAsync("upload");

                // Check if "icmms-storage" bucket exists (for comparison)
                var icmmsStorageExists = await _supabaseService.EnsureBucketExistsAsync(
                    "icmms-storage"
                );

                // Get Supabase configuration from appsettings
                var configuration =
                    HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                var supabaseUrl = configuration["Supabase:Url"];
                var supabaseKey = configuration["Supabase:AnonKey"];

                return Ok(
                    new
                    {
                        message = "Bucket status check completed",
                        buckets = new
                        {
                            upload = new { exists = uploadExists, name = "upload" },
                            icmmsStorage = new
                            {
                                exists = icmmsStorageExists,
                                name = "icmms-storage",
                            },
                        },
                        supabaseConfig = new
                        {
                            url = supabaseUrl,
                            keyPrefix = supabaseKey?.Substring(
                                0,
                                Math.Min(20, supabaseKey?.Length ?? 0)
                            ) + "...",
                        },
                        recommendations = new[]
                        {
                            uploadExists
                                ? "✅ 'upload' bucket exists and is accessible"
                                : "❌ 'upload' bucket does not exist - please create it in Supabase dashboard",
                            icmmsStorageExists
                                ? "✅ 'icmms-storage' bucket exists"
                                : "❌ 'icmms-storage' bucket does not exist",
                            "If 'upload' bucket doesn't exist, create it in your Supabase dashboard with public access",
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking bucket status: {ex.Message}");
                return StatusCode(
                    500,
                    new { error = ex.Message, message = "Failed to check bucket status" }
                );
            }
        }

        [HttpGet("test-file-access/{fileName}")]
        public async Task<IActionResult> TestFileAccess(string fileName)
        {
            try
            {
                Console.WriteLine($"Testing file access for: {fileName}");

                // Try to download the file from upload bucket
                var fileBytes = await _supabaseService.DownloadFileAsync("upload", fileName);

                if (fileBytes != null && fileBytes.Length > 0)
                {
                    return Ok(
                        new
                        {
                            message = "File access successful",
                            fileName = fileName,
                            fileSize = fileBytes.Length,
                            bucket = "upload",
                        }
                    );
                }
                else
                {
                    return NotFound(
                        new
                        {
                            message = "File not found or empty",
                            fileName = fileName,
                            bucket = "upload",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing file access: {ex.Message}");
                return StatusCode(
                    500,
                    new
                    {
                        error = ex.Message,
                        message = "Failed to access file",
                        fileName = fileName,
                        bucket = "upload",
                    }
                );
            }
        }
    }
}
