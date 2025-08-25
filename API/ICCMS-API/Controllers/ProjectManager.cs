using System.Linq;
using System.Security.Claims;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    /*
    Key CRUD Tasks:
        • Create: New construction projects with budgets, phases, deadlines.
        • Read: Project status, budget vs actual reports, contractor updates.
        • Update: Resource allocations, task assignments, document approvals, progress reports.
        • Delete: Cancel projects or remove incorrect data.
    */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Project Manager")] // Only project managers can access this controller
    public class ProjectManagerController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;

        public ProjectManagerController(IAuthService authService, IFirebaseService firebaseService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
        }

        [HttpGet("projects")]
        public async Task<ActionResult<List<Project>>> GetProjects()
        {
            try
            {
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var projectManagerProjects = projects
                    .Where(p =>
                        p.ProjectManagerId == User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    )
                    .ToList();
                return Ok(projectManagerProjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}")]
        public async Task<ActionResult<Project>> GetProject(string id)
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (project == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
                else if (
                    project.ProjectManagerId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                )
                {
                    return NotFound(
                        new { error = "You are not authorized to access this project" }
                    );
                }
                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/phases")]
        public async Task<ActionResult<List<Phase>>> GetProjectPhases(string id)
        {
            try
            {
                var phases = await _firebaseService.GetCollectionAsync<Phase>("phases");
                var projectPhases = phases.Where(p => p.ProjectId == id).ToList();
                return Ok(projectPhases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/tasks")]
        public async Task<ActionResult<List<ProjectTask>>> GetProjectTasks(string id)
        {
            try
            {
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var projectTasks = tasks.Where(t => t.ProjectId == id).ToList();
                return Ok(projectTasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("project/{id}/documents")]
        public async Task<ActionResult<List<Document>>> GetProjectDocuments(string id)
        {
            try
            {
                var documents = await _firebaseService.GetCollectionAsync<Document>("documents");
                var projectDocuments = documents.Where(d => d.ProjectId == id).ToList();
                return Ok(projectDocuments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/project")]
        public async Task<ActionResult<Project>> CreateProject([FromBody] Project project)
        {
            try
            {
                project.ProjectManagerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _firebaseService.AddDocumentWithIdAsync(
                    "projects",
                    project.ProjectId,
                    project
                );
                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/phase")]
        public async Task<ActionResult<Phase>> CreatePhase([FromBody] Phase phase)
        {
            try
            {
                phase.ProjectId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _firebaseService.AddDocumentWithIdAsync("phases", phase.PhaseId, phase);
                return Ok(phase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/task")]
        public async Task<ActionResult<ProjectTask>> CreateTask([FromBody] ProjectTask task)
        {
            try
            {
                task.ProjectId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await _firebaseService.AddDocumentWithIdAsync("tasks", task.TaskId, task);
                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/document")]
        public async Task<ActionResult<Document>> CreateDocument([FromBody] Document document)
        {
            try
            {
                document.ProjectId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

        [HttpPut("update/project/{id}")]
        public async Task<ActionResult<Project>> UpdateProject(
            string id,
            [FromBody] Project project
        )
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("projects", id, project);
                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update/phase/{id}")]
        public async Task<ActionResult<Phase>> UpdatePhase(string id, [FromBody] Phase phase)
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("phases", id, phase);
                return Ok(phase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update/task/{id}")]
        public async Task<ActionResult<ProjectTask>> UpdateTask(
            string id,
            [FromBody] ProjectTask task
        )
        {
            try
            {
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
                await _firebaseService.UpdateDocumentAsync("documents", id, document);
                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete/project/{id}")]
        public async Task<ActionResult<Project>> DeleteProject(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("projects", id);
                return Ok(new { message = "Project deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete/phase/{id}")]
        public async Task<ActionResult<Phase>> DeletePhase(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("phases", id);
                return Ok(new { message = "Phase deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete/task/{id}")]
        public async Task<ActionResult<ProjectTask>> DeleteTask(string id)
        {
            try
            {
                await _firebaseService.DeleteDocumentAsync("tasks", id);
                return Ok(new { message = "Task deleted successfully" });
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
