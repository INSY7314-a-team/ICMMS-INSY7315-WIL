using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")] // All authenticated users can access documents
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
        public async Task<ActionResult<List<string>>> GetDocumentsByProject(string projectId)
        {
            try
            {
                var allDocuments = await _firebaseService.GetCollectionAsync<Document>("documents");
                var documents = allDocuments.Where(d => d.ProjectId == projectId).ToList();
                Console.WriteLine($"Documents: {documents}");
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
                    "documents",
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
                    "documents",
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
                await _supabaseService.DeleteFileAsync("documents", fileName);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
