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
    [Authorize(Roles = "Project Manager,Tester")] // Only project managers and testers can access this controller
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
                Console.WriteLine("[GetProjects] Fetching all projects from Firestore");
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                Console.WriteLine(
                    $"[GetProjects] Retrieved {projects.Count} projects from Firestore"
                );

                // For testing purposes, return all projects instead of filtering by ProjectManagerId
                // TODO: Re-enable filtering once user authentication is properly configured
                var projectManagerProjects = projects.ToList();
                return Ok(projectManagerProjects);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetProjects] Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("projects/paginated")]
        public async Task<ActionResult<object>> GetProjectsPaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8
        )
        {
            try
            {
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var filtered = projects
                    .Where(p =>
                        p.ProjectManagerId == User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    )
                    .OrderByDescending(p => p.StartDate)
                    .ToList();

                var paginated = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return Ok(
                    new
                    {
                        projects = paginated,
                        currentPage = page,
                        pageSize = pageSize,
                        totalProjects = filtered.Count,
                        hasMore = (page * pageSize) < filtered.Count,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("projects/simple")]
        public async Task<ActionResult<object>> GetProjectsSimple(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 24
        )
        {
            try
            {
                Console.WriteLine(
                    $"[GetProjectsSimple] Called with page={page}, pageSize={pageSize}"
                );

                // Scope to currently logged-in Project Manager
                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[GetProjectsSimple] Current PM ID: '{currentPmId}'");

                if (string.IsNullOrWhiteSpace(currentPmId))
                {
                    Console.WriteLine("[GetProjectsSimple] No current PM ID found in claims");
                    return Ok(
                        new
                        {
                            projects = new List<Project>(),
                            currentPage = page,
                            pageSize = pageSize,
                            totalProjects = 0,
                            totalPages = 0,
                            hasMore = false,
                        }
                    );
                }

                // Get ALL projects for this PM (no pagination at database level)
                var allProjects = await _firebaseService.GetCollectionAsync<Project>("projects");

                // Filter by ProjectManagerId in memory (no Firestore index needed)
                var pmProjects = allProjects.Where(p => p.ProjectManagerId == currentPmId).ToList();

                // Apply pagination in memory
                var projects = pmProjects.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var totalProjects = pmProjects.Count;

                var totalPages = (int)Math.Ceiling((double)totalProjects / pageSize);
                var hasMore = page < totalPages;

                Console.WriteLine(
                    $"[GetProjectsSimple] Results: {projects.Count} projects, total: {totalProjects}, hasMore: {hasMore}"
                );

                return Ok(
                    new
                    {
                        projects = projects,
                        currentPage = page,
                        pageSize = pageSize,
                        totalProjects = totalProjects,
                        totalPages = totalPages,
                        hasMore = hasMore,
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetProjectsSimple] Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("projects/all")]
        public async Task<ActionResult<List<Project>>> GetAllProjectsForManager()
        {
            try
            {
                Console.WriteLine(
                    "[GetAllProjectsForManager] Fetching all projects for current PM"
                );

                // Scope to currently logged-in Project Manager
                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[GetAllProjectsForManager] Current PM ID: '{currentPmId}'");

                if (string.IsNullOrWhiteSpace(currentPmId))
                {
                    Console.WriteLine(
                        "[GetAllProjectsForManager] No current PM ID found in claims"
                    );
                    return Ok(new List<Project>());
                }

                // Get ALL projects from Firestore
                var allProjects = await _firebaseService.GetCollectionAsync<Project>("projects");
                Console.WriteLine(
                    $"[GetAllProjectsForManager] Retrieved {allProjects.Count} total projects from Firestore"
                );

                // Filter by ProjectManagerId in memory (no Firestore index needed)
                var pmProjects = allProjects.Where(p => p.ProjectManagerId == currentPmId).ToList();
                Console.WriteLine(
                    $"[GetAllProjectsForManager] Filtered to {pmProjects.Count} projects for PM {currentPmId}"
                );

                return Ok(pmProjects);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetAllProjectsForManager] Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("projects/draft")]
        public async Task<ActionResult<List<Project>>> GetDraftProjects(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8
        )
        {
            try
            {
                Console.WriteLine(
                    $"[GetDraftProjects] Called with page={page}, pageSize={pageSize}"
                );

                var filters = new Dictionary<string, object> { ["status"] = "Draft" };

                // Scope to currently logged-in Project Manager
                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[GetDraftProjects] Current PM ID: '{currentPmId}'");
                if (!string.IsNullOrWhiteSpace(currentPmId))
                {
                    filters["projectmanagerid"] = currentPmId;
                    Console.WriteLine(
                        $"[GetDraftProjects] Added projectmanagerid filter: '{currentPmId}'"
                    );
                }
                else
                {
                    Console.WriteLine("[GetDraftProjects] No current PM ID found in claims");
                }

                // Get ALL projects and filter in memory (no Firestore index needed)
                var allProjects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var pmProjects = allProjects.Where(p => p.ProjectManagerId == currentPmId).ToList();
                var draftProjects = pmProjects.Where(p => p.Status == "Draft").ToList();

                // Apply pagination in memory
                var projects = draftProjects.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                Console.WriteLine($"[GetDraftProjects] Results: {projects.Count} draft projects");
                return Ok(projects);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetDraftProjects] Error: {ex.Message}");
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
                // 🔐 Assign the Project Manager ID from the logged-in user
                project.ProjectManagerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // 🕒 Normalize all DateTime fields to UTC
                project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
                project.EndDatePlanned = DateTime.SpecifyKind(
                    project.EndDatePlanned,
                    DateTimeKind.Utc
                );

                if (project.EndDateActual.HasValue)
                    project.EndDateActual = DateTime.SpecifyKind(
                        project.EndDateActual.Value,
                        DateTimeKind.Utc
                    );

                // 🧾 Log the normalized values (optional)
                Console.WriteLine($"[CreateProject] StartDate.Kind = {project.StartDate.Kind}");
                Console.WriteLine(
                    $"[CreateProject] EndDatePlanned.Kind = {project.EndDatePlanned.Kind}"
                );
                Console.WriteLine(
                    $"[CreateProject] EndDateActual.Kind = {project.EndDateActual?.Kind.ToString() ?? "null"}"
                );

                // 💾 Add to Firestore
                await _firebaseService.AddDocumentWithIdAsync(
                    "projects",
                    project.ProjectId,
                    project
                );

                Console.WriteLine($"[CreateProject] Added project {project.Name} successfully.");
                return Ok(project);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateProject] Error: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/project/{projectId}/phase")]
        public async Task<ActionResult<Phase>> CreatePhase(string projectId, [FromBody] Phase phase)
        {
            try
            {
                phase.ProjectId = projectId;
                await _firebaseService.AddDocumentWithIdAsync("phases", phase.PhaseId, phase);
                return Ok(phase);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/project/{projectId}/task")]
        public async Task<ActionResult<ProjectTask>> CreateTask(
            string projectId,
            [FromBody] ProjectTask task
        )
        {
            try
            {
                task.ProjectId = projectId;
                await _firebaseService.AddDocumentWithIdAsync("tasks", task.TaskId, task);
                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/project/{projectId}/document")]
        public async Task<ActionResult<Document>> CreateDocument(
            string projectId,
            [FromBody] Document document
        )
        {
            try
            {
                document.ProjectId = projectId;
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
                var existingProject = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    id
                );
                if (existingProject == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
                if (
                    existingProject.ProjectManagerId
                    != User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                )
                {
                    return Unauthorized(
                        new { error = "You are not authorized to update this project" }
                    );
                }
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
                var existingPhase = await _firebaseService.GetDocumentAsync<Phase>("phases", id);
                if (existingPhase == null)
                {
                    return NotFound(new { error = "Phase not found" });
                }
                if (existingPhase.ProjectId != phase.ProjectId)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to update this phase" }
                    );
                }
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
                var existingTask = await _firebaseService.GetDocumentAsync<ProjectTask>(
                    "tasks",
                    id
                );
                if (existingTask == null)
                {
                    return NotFound(new { error = "Task not found" });
                }
                if (existingTask.ProjectId != task.ProjectId)
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
                var documentFromDb = await _firebaseService.GetDocumentAsync<Document>(
                    "documents",
                    id
                );
                if (documentFromDb == null)
                {
                    return NotFound(new { error = "Document not found" });
                }
                if (documentFromDb.ProjectId != document.ProjectId)
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

        [HttpPut("approve/document/{id}")]
        public async Task<ActionResult<Document>> ApproveDocument(string id)
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
                if (existingDocument.ProjectId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to approve this document" }
                    );
                }
                existingDocument.Status = "Approved";
                await _firebaseService.UpdateDocumentAsync("documents", id, existingDocument);
                return Ok(existingDocument);
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
                var existingProject = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    id
                );
                if (existingProject == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
                if (
                    existingProject.ProjectManagerId
                    != User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                )
                {
                    return Unauthorized(
                        new { error = "You are not authorized to delete this project" }
                    );
                }
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
                var existingPhase = await _firebaseService.GetDocumentAsync<Phase>("phases", id);
                if (existingPhase == null)
                {
                    return NotFound(new { error = "Phase not found" });
                }
                if (existingPhase.ProjectId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to delete this phase" }
                    );
                }
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
                var existingTask = await _firebaseService.GetDocumentAsync<ProjectTask>(
                    "tasks",
                    id
                );
                if (existingTask == null)
                {
                    return NotFound(new { error = "Task not found" });
                }
                if (existingTask.ProjectId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to delete this task" }
                    );
                }
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
                var existingDocument = await _firebaseService.GetDocumentAsync<Document>(
                    "documents",
                    id
                );
                if (existingDocument == null)
                {
                    return NotFound(new { error = "Document not found" });
                }
                if (existingDocument.ProjectId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
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
