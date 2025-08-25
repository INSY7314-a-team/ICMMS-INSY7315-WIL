using Microsoft.AspNetCore.Mvc;
using ICCMS_API.Models;
using ICCMS_API.Services;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IFirebaseService _firebaseService;

        public DocumentsController(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Document>>> GetDocuments()
        {
            try
            {
                var documents = await _firebaseService.GetCollectionAsync<Document>("documents");
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Document>> GetDocument(string id)
        {
            try
            {
                var document = await _firebaseService.GetDocumentAsync<Document>("documents", id);
                if (document == null)
                    return NotFound();
                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<List<Document>>> GetDocumentsByProject(string projectId)
        {
            try
            {
                var documents = await _firebaseService.GetCollectionAsync<Document>("documents");
                var projectDocuments = documents.Where(d => d.ProjectId == projectId).ToList();
                return Ok(projectDocuments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateDocument([FromBody] Document document)
        {
            try
            {
                document.UploadedAt = DateTime.UtcNow;
                var documentId = await _firebaseService.AddDocumentAsync("documents", document);
                return Ok(documentId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(string id, [FromBody] Document document)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("documents", id, document);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("documents", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
