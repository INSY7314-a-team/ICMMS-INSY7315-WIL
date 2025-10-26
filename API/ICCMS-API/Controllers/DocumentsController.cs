using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")]
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

        [HttpGet("metadata")]
        public async Task<ActionResult<List<Document>>> GetDocumentsMetadata([FromQuery] string? projectId = null)
        {
            try
            {
                var documents = await _firebaseService.GetCollectionAsync<Document>("documents");
                Console.WriteLine($"Found {documents.Count} documents in Firebase");
                
                // Debug: Log the first document to see what we're getting
                if (documents.Count > 0)
                {
                    var firstDoc = documents[0];
                    Console.WriteLine($"First document details:");
                    Console.WriteLine($"  DocumentId: '{firstDoc.DocumentId}'");
                    Console.WriteLine($"  ProjectId: '{firstDoc.ProjectId}'");
                    Console.WriteLine($"  FileName: '{firstDoc.FileName}'");
                    Console.WriteLine($"  Status: '{firstDoc.Status}'");
                    Console.WriteLine($"  FileSize: {firstDoc.FileSize}");
                    Console.WriteLine($"  UploadedBy: '{firstDoc.UploadedBy}'");
                    Console.WriteLine($"  UploadedAt: {firstDoc.UploadedAt}");
                    Console.WriteLine($"  Description: '{firstDoc.Description}'");
                    
                    // Debug: Check if the values are actually populated
                    Console.WriteLine($"Field population check:");
                    Console.WriteLine($"  FileName is null or empty: {string.IsNullOrEmpty(firstDoc.FileName)}");
                    Console.WriteLine($"  ProjectId is null or empty: {string.IsNullOrEmpty(firstDoc.ProjectId)}");
                    Console.WriteLine($"  Status is null or empty: {string.IsNullOrEmpty(firstDoc.Status)}");
                }
                
                // Filter by project if specified
                if (!string.IsNullOrEmpty(projectId))
                {
                    documents = documents.Where(d => d.ProjectId == projectId).ToList();
                    Console.WriteLine($"Filtered to {documents.Count} documents for project {projectId}");
                }
                
                // Normalize URLs for all documents
                foreach (var doc in documents)
                {
                    if (string.IsNullOrWhiteSpace(doc.FileUrl) && !string.IsNullOrWhiteSpace(doc.FileName))
                    {
                        var bucket = _supabaseService.SupabaseClient.Storage.From("upload");
                        doc.FileUrl = bucket.GetPublicUrl(doc.FileName);
                    }
                    else if (!string.IsNullOrWhiteSpace(doc.FileUrl) && doc.FileUrl.Contains("icmms-storage"))
                    {
                        var bucket = _supabaseService.SupabaseClient.Storage.From("upload");
                        doc.FileUrl = bucket.GetPublicUrl(doc.FileName);
                    }
                }
                
                return Ok(documents);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting documents metadata: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{fileName}")]
        public async Task<IActionResult> GetDocument(string fileName)
        {
            try
            {
                Console.WriteLine($"Getting document: {fileName}");
                var document = await _supabaseService.DownloadFileAsync("upload", fileName);
                if (document == null || document.Length == 0)
                    return NotFound();
                
                Console.WriteLine($"Document found: {fileName}");
                
                // Determine content type based on file extension
                var contentType = GetContentType(fileName);
                
                return File(document, contentType, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting document: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
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

        [HttpGet("debug-fields")]
        public async Task<IActionResult> DebugDocumentFields()
        {
            try
            {
                Console.WriteLine("Debug fields endpoint called");
                
                // Get raw Firestore documents
                var snapshot = await _firebaseService.GetRawCollectionAsync("documents");
                Console.WriteLine($"Found {snapshot.Count} raw documents");
                
                if (snapshot.Count > 0)
                {
                    var firstDoc = snapshot.First();
                    var fields = firstDoc.ToDictionary();
                    
                    Console.WriteLine($"First document ID: {firstDoc.Id}");
                    Console.WriteLine($"Available fields: {string.Join(", ", fields.Keys)}");
                    
                    // Show each field and its value
                    foreach (var field in fields)
                    {
                        Console.WriteLine($"Field '{field.Key}': {field.Value} (Type: {field.Value?.GetType().Name})");
                    }
                    
                    return Ok(new
                    {
                        message = "Field debug successful",
                        documentId = firstDoc.Id,
                        availableFields = fields.Keys.ToList(),
                        fieldDetails = fields.Select(f => new { 
                            name = f.Key, 
                            value = f.Value?.ToString(), 
                            type = f.Value?.GetType().Name 
                        }).ToList()
                    });
                }
                else
                {
                    return Ok(new
                    {
                        message = "No documents found",
                        documentCount = 0
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in debug fields endpoint: {ex.Message}");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("test-metadata")]
        public async Task<IActionResult> TestMetadata()
        {
            try
            {
                Console.WriteLine("Testing metadata endpoint...");
                
                var documents = await _firebaseService.GetCollectionAsync<Document>("documents");
                Console.WriteLine($"Found {documents.Count} documents in Firebase");
                
                return Ok(new
                {
                    message = "Metadata endpoint working",
                    documentCount = documents.Count,
                    sampleDocument = documents.FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing metadata: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("debug-bucket-files")]
        public async Task<IActionResult> DebugBucketFiles()
        {
            try
            {
                Console.WriteLine("Debug bucket files endpoint called");
                
                var allFiles = await _supabaseService.ListFilesAsync("upload");
                Console.WriteLine($"Found {allFiles.Count} files in upload bucket");
                
                return Ok(new
                {
                    message = "Bucket files debug successful",
                    bucket = "upload",
                    fileCount = allFiles.Count,
                    files = allFiles
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in debug bucket files endpoint: {ex.Message}");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("test-file-access/{fileName}")]
        public async Task<IActionResult> TestFileAccess(string fileName)
        {
            try
            {
                Console.WriteLine($"Testing file access for: {fileName}");

                // First, let's list all files in the bucket to see what's actually there
                var allFiles = await _supabaseService.ListFilesAsync("upload");
                Console.WriteLine($"All files in upload bucket: {string.Join(", ", allFiles)}");

                // Check if our specific file exists in the list
                var fileExists = allFiles.Contains(fileName);
                Console.WriteLine($"File '{fileName}' exists in bucket: {fileExists}");

                if (!fileExists)
                {
                    // Try to find similar files
                    var similarFiles = allFiles.Where(f => f.Contains(fileName.Split('_').Last())).ToList();
                    Console.WriteLine($"Similar files found: {string.Join(", ", similarFiles)}");
                    
                    return NotFound(
                        new
                        {
                            message = "File not found in bucket",
                            fileName = fileName,
                            bucket = "upload",
                            allFiles = allFiles,
                            similarFiles = similarFiles
                        }
                    );
                }

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
                            message = "File found but empty or download failed",
                            fileName = fileName,
                            bucket = "upload",
                            allFiles = allFiles
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing file access: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(
                    500,
                    new
                    {
                        error = ex.Message,
                        message = "Failed to access file",
                        fileName = fileName,
                        bucket = "upload",
                        stackTrace = ex.StackTrace
                    }
                );
            }
        }
    }
}
