using System.Linq;
using System.Security.Claims;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Contractor")]
    public class ContractorsController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;

        public ContractorsController(IAuthService authService, IFirebaseService firebaseService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
        }

        [HttpGet("project/tasks")]
        public async Task<ActionResult<List<ProjectTask>>> GetProjectTasks()
        {
            try
            {
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var contractorTasks = tasks
                    .Where(t => t.AssignedTo == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(contractorTasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/phases")]
        public async Task<ActionResult<List<Phase>>> GetProjectPhases()
        {
            try
            {
                var phases = await _firebaseService.GetCollectionAsync<Phase>("phases");
                var contractorPhases = phases
                    .Where(p => p.AssignedTo == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(contractorPhases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/documents")]
        public async Task<ActionResult<List<Document>>> GetProjectDocuments()
        {
            try
            {
                var documents = await _firebaseService.GetCollectionAsync<Document>("documents");
                var contractorDocuments = documents
                    .Where(d => d.UploadedBy == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                    .ToList();
                return Ok(contractorDocuments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("upload/project/{projectId}/document")]
        public async Task<ActionResult<Document>> UploadDocument(
            string projectId,
            [FromBody] Document document
        )
        {
            try
            {
                document.ProjectId = projectId;
                document.UploadedBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                document.UploadedAt = DateTime.UtcNow;
                document.Status = "Pending";
                await _firebaseService.AddDocumentWithIdAsync(
                    "documents",
                    document.DocumentId,
                    document
                );
                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update/project/task/{id}")]
        public async Task<ActionResult<ProjectTask>> UpdateProjectTask(
            string id,
            [FromBody] ProjectTask task
        )
        {
            try
            {
                var existingTask = await _firebaseService.GetDocumentAsync<ProjectTask>(
                    "tasks",
                    id
                );
                if (existingTask == null)
                {
                    return NotFound(new { error = "Task not found" });
                }
                if (existingTask.AssignedTo != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to update this task" }
                    );
                }

                await _firebaseService.UpdateDocumentAsync("tasks", id, task);
                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update/document/{id}")]
        public async Task<ActionResult<Document>> UpdateDocument(
            string id,
            [FromBody] Document document
        )
        {
            try
            {
                var existingDocument = await _firebaseService.GetDocumentAsync<Document>(
                    "documents",
                    id
                );
                if (existingDocument == null)
                {
                    return NotFound(new { error = "Document not found" });
                }
                if (existingDocument.UploadedBy != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to update this document" }
                    );
                }
                await _firebaseService.UpdateDocumentAsync("documents", id, document);
                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete/document/{id}")]
        public async Task<ActionResult<Document>> DeleteDocument(string id)
        {
            try
            {
                var existingDocument = await _firebaseService.GetDocumentAsync<Document>(
                    "documents",
                    id
                );
                if (existingDocument == null)
                {
                    return NotFound(new { error = "Document not found" });
                }
                if (existingDocument.UploadedBy != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to delete this document" }
                    );
                }
                await _firebaseService.DeleteDocumentAsync("documents", id);
                return Ok(new { message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
