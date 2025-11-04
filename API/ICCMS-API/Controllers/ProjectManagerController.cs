using System.Linq;
using System.Security.Claims;
using ICCMS_API.Auth;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Tester")]
    public class ProjectManagerController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IAuditLogService _auditLogService;

        public ProjectManagerController(
            IAuthService authService,
            IFirebaseService firebaseService,
            IWorkflowMessageService workflowMessageService,
            IAuditLogService auditLogService
        )
        {
            _authService = authService;
            _firebaseService = firebaseService;
            _workflowMessageService = workflowMessageService;
            _auditLogService = auditLogService;
        }

        [HttpGet("projects")]
        public async Task<IActionResult> GetProjects()
        {
            try
            {
                var pmId = User.UserId();
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized("User not identified.");
                }

                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var pmProjects = projects.Where(p => p.ProjectManagerId == pmId).ToList();

                return Ok(pmProjects);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while fetching projects.");
            }
        }

        [HttpGet("messaging/available-users")]
        public async Task<IActionResult> GetAvailableUsersForMessaging()
        {
            try
            {
                var pmId = User.UserId();
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized("Project Manager not identified.");
                }

                // Get all projects managed by this PM
                var allProjects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var pmProjects = allProjects.Where(p => p.ProjectManagerId == pmId).ToList();
                var pmClientIds = new HashSet<string>(pmProjects.Select(p => p.ClientId));

                // Get all users
                var allUsers = await _firebaseService.GetCollectionAsync<User>("users");

                // Filter users based on the rule
                var availableUsers = allUsers
                    .Where(u =>
                        u.UserId != pmId
                        && // Exclude self
                        u.Role != "Project Manager"
                        && // Exclude other Project Managers
                        (
                            u.Role != "Client"
                            || (u.Role == "Client" && pmClientIds.Contains(u.UserId))
                        )
                    )
                    .ToList();

                return Ok(availableUsers);
            }
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(
                    500,
                    "An error occurred while fetching available users for messaging."
                );
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

        // ===============================================
        // Draft lifecycle endpoints
        // ===============================================

        [HttpPost("save-draft")]
        public async Task<ActionResult<Project>> SaveDraft([FromBody] Project project)
        {
            try
            {
                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(currentPmId))
                {
                    Console.WriteLine(
                        "[SaveDraft] No current user ID found - Draft creation is not allowed"
                    );
                    return Unauthorized(
                        new
                        {
                            error = "Authentication error: No project manager ID found. Please log in again.",
                        }
                    );
                }

                // Initialize draft defaults
                project.ProjectId = string.IsNullOrWhiteSpace(project.ProjectId)
                    ? Guid.NewGuid().ToString()
                    : project.ProjectId;
                project.ProjectManagerId = currentPmId;
                project.Status = "Draft";
                project.UpdatedAt = DateTime.UtcNow;
                project.CreatedByUserId = currentPmId;
                project.IsDraft = true;

                // Normalize dates if present
                if (project.StartDate.Year > 1900)
                    project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
                if (project.EndDatePlanned.Year > 1900)
                    project.EndDatePlanned = DateTime.SpecifyKind(
                        project.EndDatePlanned,
                        DateTimeKind.Utc
                    );
                if (project.EndDateActual.HasValue && project.EndDateActual.Value.Year > 1900)
                    project.EndDateActual = DateTime.SpecifyKind(
                        project.EndDateActual.Value,
                        DateTimeKind.Utc
                    );

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

        [HttpPut("update-draft/{id}")]
        public async Task<ActionResult<Project>> UpdateDraft(string id, [FromBody] Project project)
        {
            try
            {
                var existing = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (existing == null)
                {
                    return NotFound(new { error = "Project not found" });
                }

                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (existing.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(new { error = "Not authorized to update this draft" });
                }

                // Preserve identifiers and draft status
                project.ProjectId = id;
                project.ProjectManagerId = existing.ProjectManagerId;
                project.CreatedByUserId = existing.CreatedByUserId;
                project.Status = "Draft";
                project.IsDraft = true;
                project.UpdatedAt = DateTime.UtcNow;

                // Normalize dates if present
                if (project.StartDate.Year > 1900)
                    project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
                if (project.EndDatePlanned.Year > 1900)
                    project.EndDatePlanned = DateTime.SpecifyKind(
                        project.EndDatePlanned,
                        DateTimeKind.Utc
                    );
                if (project.EndDateActual.HasValue && project.EndDateActual.Value.Year > 1900)
                    project.EndDateActual = DateTime.SpecifyKind(
                        project.EndDateActual.Value,
                        DateTimeKind.Utc
                    );

                await _firebaseService.UpdateDocumentAsync("projects", id, project);
                return Ok(project);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("projects/{id}/autosave")]
        public async Task<ActionResult<Project>> AutosaveProject(
            string id,
            [FromBody] Project partial
        )
        {
            try
            {
                var existing = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (existing == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (existing.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(new { error = "Not authorized to autosave" });
                }

                // Merge known basic info fields; keep as draft
                existing.Name = string.IsNullOrWhiteSpace(partial.Name)
                    ? existing.Name
                    : partial.Name;
                existing.Description = string.IsNullOrWhiteSpace(partial.Description)
                    ? existing.Description
                    : partial.Description;
                existing.ClientId = string.IsNullOrWhiteSpace(partial.ClientId)
                    ? existing.ClientId
                    : partial.ClientId;
                existing.BudgetPlanned =
                    partial.BudgetPlanned != 0 ? partial.BudgetPlanned : existing.BudgetPlanned;
                if (partial.StartDate.Year > 1900)
                    existing.StartDate = DateTime.SpecifyKind(partial.StartDate, DateTimeKind.Utc);
                if (partial.EndDatePlanned.Year > 1900)
                    existing.EndDatePlanned = DateTime.SpecifyKind(
                        partial.EndDatePlanned,
                        DateTimeKind.Utc
                    );

                existing.IsDraft = true;
                existing.Status = "Draft";
                existing.UpdatedAt = DateTime.UtcNow;

                await _firebaseService.UpdateDocumentAsync("projects", id, existing);
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("projects/{id}/phases-bulk")]
        public async Task<ActionResult<object>> SavePhasesBulk(
            string id,
            [FromBody] List<Phase> phases
        )
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (project == null)
                    return NotFound(new { error = "Project not found" });

                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (project.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(new { error = "Not authorized" });
                }

                foreach (var phase in phases)
                {
                    phase.ProjectId = id;
                    if (string.IsNullOrWhiteSpace(phase.PhaseId))
                    {
                        phase.PhaseId = Guid.NewGuid().ToString();
                    }
                    await _firebaseService.AddDocumentWithIdAsync("phases", phase.PhaseId, phase);
                }
                return Ok(new { saved = phases.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("projects/{id}/tasks-bulk")]
        public async Task<ActionResult<object>> SaveTasksBulk(
            string id,
            [FromBody] List<ProjectTask> tasks
        )
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (project == null)
                    return NotFound(new { error = "Project not found" });

                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (project.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(new { error = "Not authorized" });
                }

                var currentUserId = User.UserId();

                foreach (var task in tasks)
                {
                    task.ProjectId = id;
                    if (string.IsNullOrWhiteSpace(task.TaskId))
                    {
                        task.TaskId = Guid.NewGuid().ToString();
                    }

                    // Create a normalized task with proper DateTime handling
                    var normalizedTask = new ProjectTask
                    {
                        TaskId = task.TaskId,
                        ProjectId = task.ProjectId,
                        PhaseId = task.PhaseId,
                        Name = task.Name,
                        Description = task.Description,
                        AssignedTo = task.AssignedTo,
                        Priority = task.Priority,
                        Status = task.Status,
                        Progress = task.Progress,
                        EstimatedHours = task.EstimatedHours,
                        ActualHours = task.ActualHours,
                        // Normalize DateTime fields to UTC
                        StartDate = NormalizeDateTime(task.StartDate, DateTime.UtcNow),
                        DueDate = NormalizeDateTime(task.DueDate, DateTime.UtcNow.AddDays(7)),
                        CompletedDate =
                            task.CompletedDate.HasValue && task.CompletedDate.Value.Year > 1900
                                ? NormalizeDateTime(task.CompletedDate.Value, null)
                                : null,
                    };

                    await _firebaseService.AddDocumentWithIdAsync(
                        "tasks",
                        task.TaskId,
                        normalizedTask
                    );

                    if (
                        !string.IsNullOrEmpty(normalizedTask.AssignedTo)
                        && !string.IsNullOrEmpty(currentUserId)
                    )
                    {
                        var systemEvent = new SystemEvent
                        {
                            EventType = "task_assignment",
                            EntityId = normalizedTask.TaskId,
                            EntityType = "task",
                            Action = "assigned",
                            ProjectId = normalizedTask.ProjectId,
                            UserId = currentUserId,
                            Data = new Dictionary<string, object>
                            {
                                { "taskId", normalizedTask.TaskId },
                                { "taskName", normalizedTask.Name },
                                { "assignedToId", normalizedTask.AssignedTo },
                                { "assignedById", currentUserId },
                            },
                        };
                        await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                    }
                }
                return Ok(new { saved = tasks.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("projects/{id}/finalize")]
        public async Task<ActionResult<Project>> FinalizeProject(string id)
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (project == null)
                    return NotFound(new { error = "Project not found" });

                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (project.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(new { error = "Not authorized" });
                }

                // Validate completeness
                if (
                    string.IsNullOrWhiteSpace(project.Name)
                    || string.IsNullOrWhiteSpace(project.ClientId)
                )
                {
                    return BadRequest(new { error = "Basic information incomplete" });
                }

                var phases = await _firebaseService.GetCollectionAsync<Phase>("phases");
                var projectPhases = phases.Where(p => p.ProjectId == id).ToList();
                if (!projectPhases.Any())
                {
                    return BadRequest(new { error = "At least one phase is required" });
                }

                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var projectTasks = tasks.Where(t => t.ProjectId == id).ToList();
                if (!projectTasks.Any())
                {
                    return BadRequest(new { error = "At least one task is required" });
                }
                if (projectTasks.Any(t => string.IsNullOrWhiteSpace(t.AssignedTo)))
                {
                    return BadRequest(new { error = "All tasks must have a contractor assigned" });
                }

                // Transition to Planning
                project.IsDraft = false;
                project.Status = "Planning";
                project.UpdatedAt = DateTime.UtcNow;
                await _firebaseService.UpdateDocumentAsync("projects", id, project);

                return Ok(project);
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
                Console.WriteLine($"[GetProject] Requesting project {id}");
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[GetProject] Current user ID: {currentUserId}");

                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (project == null)
                {
                    Console.WriteLine($"[GetProject] Project {id} not found in database");
                    return NotFound(new { error = "Project not found" });
                }

                Console.WriteLine(
                    $"[GetProject] Project found - ProjectManagerId: {project.ProjectManagerId}"
                );
                if (project.ProjectManagerId != currentUserId)
                {
                    Console.WriteLine(
                        $"[GetProject] Access denied - ProjectManagerId ({project.ProjectManagerId}) != CurrentUserId ({currentUserId})"
                    );
                    return NotFound(
                        new { error = "You are not authorized to access this project" }
                    );
                }

                Console.WriteLine($"[GetProject] Access granted for project {id}");
                return Ok(project);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetProject] Error: {ex.Message}");
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

        [HttpGet("project/{id}/maintenance-requests")]
        public async Task<ActionResult<List<MaintenanceRequest>>> GetProjectMaintenanceRequests(string id)
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                if (project == null)
                {
                    return NotFound(new { error = "Project not found" });
                }

                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (project.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(new { error = "You are not authorized to access this project" });
                }

                var requests = await _firebaseService.GetCollectionAsync<MaintenanceRequest>(
                    "maintenanceRequests"
                );
                var projectRequests = requests.Where(r => r.ProjectId == id).ToList();
                return Ok(projectRequests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("maintenance-request/{id}")]
        public async Task<ActionResult<MaintenanceRequest>> GetMaintenanceRequest(string id)
        {
            try
            {
                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentPmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var maintenanceRequest = await _firebaseService.GetDocumentAsync<MaintenanceRequest>(
                    "maintenanceRequests",
                    id
                );
                if (maintenanceRequest == null)
                {
                    return NotFound(new { error = "Maintenance request not found" });
                }

                // Verify PM owns the project
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    maintenanceRequest.ProjectId
                );
                if (project == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
                if (project.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to access this maintenance request" }
                    );
                }

                return Ok(maintenanceRequest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting maintenance request: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("maintenance-request/{id}/approve")]
        public async Task<ActionResult> ApproveMaintenanceRequest(
            string id,
            [FromBody] ApproveMaintenanceRequestRequest request
        )
        {
            try
            {
                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentPmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                // Get maintenance request
                var maintenanceRequest = await _firebaseService.GetDocumentAsync<MaintenanceRequest>(
                    "maintenanceRequests",
                    id
                );
                if (maintenanceRequest == null)
                {
                    return NotFound(new { error = "Maintenance request not found" });
                }

                // Verify PM owns the project
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    maintenanceRequest.ProjectId
                );
                if (project == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
                if (project.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to approve this maintenance request" }
                    );
                }

                // Update maintenance request status
                maintenanceRequest.Status = "Approved";
                await _firebaseService.UpdateDocumentAsync(
                    "maintenanceRequests",
                    id,
                    maintenanceRequest
                );

                // Create phases if provided
                if (request.Phases != null && request.Phases.Any())
                {
                    foreach (var phase in request.Phases)
                    {
                        phase.ProjectId = maintenanceRequest.ProjectId;
                        if (string.IsNullOrWhiteSpace(phase.PhaseId))
                        {
                            phase.PhaseId = Guid.NewGuid().ToString();
                        }
                        phase.Status = "Pending";
                        phase.Progress = 0;

                        await _firebaseService.AddDocumentWithIdAsync(
                            "phases",
                            phase.PhaseId,
                            phase
                        );
                    }
                }

                // Create tasks if provided
                if (request.Tasks != null && request.Tasks.Any())
                {
                    foreach (var task in request.Tasks)
                    {
                        task.ProjectId = maintenanceRequest.ProjectId;
                        if (string.IsNullOrWhiteSpace(task.TaskId))
                        {
                            task.TaskId = Guid.NewGuid().ToString();
                        }

                        var normalizedTask = new ProjectTask
                        {
                            TaskId = task.TaskId,
                            ProjectId = task.ProjectId,
                            PhaseId = task.PhaseId,
                            Name = task.Name,
                            Description = task.Description,
                            AssignedTo = task.AssignedTo,
                            Priority = task.Priority,
                            Status = task.Status,
                            Progress = task.Progress,
                            EstimatedHours = task.EstimatedHours,
                            ActualHours = task.ActualHours,
                            StartDate = NormalizeDateTime(task.StartDate, DateTime.UtcNow),
                            DueDate = NormalizeDateTime(task.DueDate, DateTime.UtcNow.AddDays(7)),
                            CompletedDate = task.CompletedDate.HasValue
                                    && task.CompletedDate.Value.Year > 1900
                                ? NormalizeDateTime(task.CompletedDate.Value, null)
                                : null,
                        };

                        await _firebaseService.AddDocumentWithIdAsync(
                            "tasks",
                            task.TaskId,
                            normalizedTask
                        );
                    }
                }

                // Ensure project status is Maintenance
                if (project.Status != "Maintenance")
                {
                    project.Status = "Maintenance";
                    await _firebaseService.UpdateDocumentAsync(
                        "projects",
                        maintenanceRequest.ProjectId,
                        project
                    );
                }

                // Send workflow message to client
                if (!string.IsNullOrEmpty(project.ClientId))
                {
                    var systemEvent = new SystemEvent
                    {
                        EventType = "maintenance_request",
                        EntityId = maintenanceRequest.MaintenanceRequestId,
                        EntityType = "maintenance_request",
                        Action = "approved",
                        ProjectId = maintenanceRequest.ProjectId,
                        UserId = project.ClientId,
                        Data = new Dictionary<string, object>
                        {
                            { "requestId", maintenanceRequest.MaintenanceRequestId },
                            { "projectId", maintenanceRequest.ProjectId },
                            { "projectName", project.Name },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                }

                return Ok(new { message = "Maintenance request approved successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error approving maintenance request: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("maintenance-request/{id}/reject")]
        public async Task<ActionResult> RejectMaintenanceRequest(string id)
        {
            try
            {
                var currentPmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentPmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                // Get maintenance request
                var maintenanceRequest = await _firebaseService.GetDocumentAsync<MaintenanceRequest>(
                    "maintenanceRequests",
                    id
                );
                if (maintenanceRequest == null)
                {
                    return NotFound(new { error = "Maintenance request not found" });
                }

                // Verify PM owns the project
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    maintenanceRequest.ProjectId
                );
                if (project == null)
                {
                    return NotFound(new { error = "Project not found" });
                }
                if (project.ProjectManagerId != currentPmId)
                {
                    return Unauthorized(
                        new { error = "You are not authorized to reject this maintenance request" }
                    );
                }

                // Update maintenance request status
                maintenanceRequest.Status = "Rejected";
                await _firebaseService.UpdateDocumentAsync(
                    "maintenanceRequests",
                    id,
                    maintenanceRequest
                );

                // Send workflow message to client
                if (!string.IsNullOrEmpty(project.ClientId))
                {
                    var systemEvent = new SystemEvent
                    {
                        EventType = "maintenance_request",
                        EntityId = maintenanceRequest.MaintenanceRequestId,
                        EntityType = "maintenance_request",
                        Action = "rejected",
                        ProjectId = maintenanceRequest.ProjectId,
                        UserId = project.ClientId,
                        Data = new Dictionary<string, object>
                        {
                            { "requestId", maintenanceRequest.MaintenanceRequestId },
                            { "projectId", maintenanceRequest.ProjectId },
                            { "projectName", project.Name },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                }

                // Check if all maintenance requests for this project are rejected and nothing is open
                // If so, move project back to "Completed"
                if (project.Status == "Maintenance")
                {
                    var allMaintenanceRequests = await _firebaseService.GetCollectionAsync<MaintenanceRequest>(
                        "maintenanceRequests"
                    );
                    var projectMaintenanceRequests = allMaintenanceRequests
                        .Where(mr => mr.ProjectId == maintenanceRequest.ProjectId)
                        .ToList();

                    // Check if there are any open maintenance requests (not rejected, not completed)
                    var openRequests = projectMaintenanceRequests
                        .Where(mr => mr.Status != "Rejected" && mr.Status != "Completed" && mr.Status != "Resolved")
                        .ToList();

                    // If no open requests, move project back to Completed
                    if (!openRequests.Any())
                    {
                        project.Status = "Completed";
                        if (project.EndDateActual == null)
                        {
                            project.EndDateActual = DateTime.UtcNow;
                        }
                        await _firebaseService.UpdateDocumentAsync(
                            "projects",
                            maintenanceRequest.ProjectId,
                            project
                        );
                        Console.WriteLine(
                            $"✅ All maintenance requests rejected/completed. Project {maintenanceRequest.ProjectId} moved back to 'Completed'"
                        );
                    }
                }

                return Ok(new { message = "Maintenance request rejected successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rejecting maintenance request: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public class ApproveMaintenanceRequestRequest
        {
            public string RequestId { get; set; } = string.Empty;
            public string ProjectId { get; set; } = string.Empty;
            public List<Phase>? Phases { get; set; }
            public List<ProjectTask>? Tasks { get; set; }
        }

        [HttpPost("save-project")]
        public async Task<ActionResult<SaveProjectResponse>> SaveProject(
            [FromBody] SaveProjectRequest request
        )
        {
            try
            {
                // 1. Validate authentication
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    Console.WriteLine(
                        "SaveProject: No current user ID found - Project save is not allowed"
                    );
                    return BadRequest(
                        new
                        {
                            error = "Authentication error: No project manager ID found. Please log in again.",
                        }
                    );
                }

                // 2. Set ProjectManagerId (double-check)
                request.Project.ProjectManagerId = currentUserId;
                Console.WriteLine($"SaveProject: Set ProjectManagerId to: '{currentUserId}'");

                // 3. Normalize DateTime fields to UTC
                NormalizeDateTimeFields(request.Project);

                // 4. Check if project exists (update vs create)
                var existing = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    request.Project.ProjectId
                );

                if (existing != null)
                {
                    // Update existing project
                    UpdateProjectFields(existing, request.Project);
                    await _firebaseService.UpdateDocumentAsync(
                        "projects",
                        request.Project.ProjectId,
                        existing
                    );
                    Console.WriteLine(
                        $"SaveProject: Updated existing project {request.Project.ProjectId}"
                    );
                }
                else
                {
                    // Create new project
                    await _firebaseService.AddDocumentWithIdAsync(
                        "projects",
                        request.Project.ProjectId,
                        request.Project
                    );
                    Console.WriteLine(
                        $"SaveProject: Created new project {request.Project.ProjectId}"
                    );

                    // Log project creation
                    var userId = User.UserId();
                    _auditLogService.LogAsync(
                        "Project Creation",
                        "Project Created",
                        $"Project {request.Project.Name} ({request.Project.ProjectId}) created for client {request.Project.ClientId}",
                        userId ?? "system",
                        request.Project.ProjectId
                    );
                }

                // 5. Save phases if provided
                int phasesSaved = 0;
                int phasesFailed = 0;
                if (request.Phases?.Any() == true)
                {
                    foreach (var phase in request.Phases)
                    {
                        try
                        {
                            phase.ProjectId = request.Project.ProjectId;
                            await SavePhase(phase);
                            phasesSaved++;
                        }
                        catch (Exception ex)
                        {
                            phasesFailed++;
                            Console.WriteLine(
                                $"SaveProject: Failed to save phase {phase.PhaseId} - {ex.Message}"
                            );
                        }
                    }
                    Console.WriteLine(
                        $"SaveProject: Saved {phasesSaved} phases, {phasesFailed} failed"
                    );
                }

                // 6. Save tasks if provided
                int tasksSaved = 0;
                int tasksFailed = 0;
                if (request.Tasks?.Any() == true)
                {
                    foreach (var task in request.Tasks)
                    {
                        try
                        {
                            task.ProjectId = request.Project.ProjectId;
                            await SaveTask(task);
                            tasksSaved++;
                        }
                        catch (Exception ex)
                        {
                            tasksFailed++;
                            Console.WriteLine(
                                $"SaveProject: Failed to save task {task.TaskId} - {ex.Message}"
                            );
                        }
                    }
                    Console.WriteLine(
                        $"SaveProject: Saved {tasksSaved} tasks, {tasksFailed} failed"
                    );
                }

                // Build response message
                var message = $"Project saved successfully";
                if (phasesFailed > 0 || tasksFailed > 0)
                {
                    message +=
                        $" (Warning: {phasesFailed} phases and {tasksFailed} tasks failed to save)";
                }

                return Ok(
                    new SaveProjectResponse
                    {
                        ProjectId = request.Project.ProjectId,
                        Status = request.Project.Status,
                        Message = message,
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveProject: Error - {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private void NormalizeDateTimeFields(Project project)
        {
            project.StartDate = DateTime.SpecifyKind(project.StartDate, DateTimeKind.Utc);
            project.EndDatePlanned = DateTime.SpecifyKind(project.EndDatePlanned, DateTimeKind.Utc);
            if (project.EndDateActual.HasValue)
                project.EndDateActual = DateTime.SpecifyKind(
                    project.EndDateActual.Value,
                    DateTimeKind.Utc
                );
            project.UpdatedAt = DateTime.UtcNow;
        }

        private void UpdateProjectFields(Project existing, Project updated)
        {
            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.ClientId = updated.ClientId;
            existing.StartDate = updated.StartDate;
            existing.EndDatePlanned = updated.EndDatePlanned;
            existing.BudgetPlanned = updated.BudgetPlanned;
            existing.Status = updated.Status;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        private async Task SavePhase(Phase phase)
        {
            try
            {
                var phaseId = string.IsNullOrEmpty(phase.PhaseId)
                    ? Guid.NewGuid().ToString()
                    : phase.PhaseId;

                Console.WriteLine($"SavePhase: Processing phase {phaseId}");
                Console.WriteLine(
                    $"Original StartDate: {phase.StartDate} (Kind: {phase.StartDate.Kind})"
                );
                Console.WriteLine(
                    $"Original EndDate: {phase.EndDate} (Kind: {phase.EndDate.Kind})"
                );

                // Create a new phase object with properly normalized DateTime fields
                var normalizedPhase = new Phase
                {
                    PhaseId = phaseId,
                    ProjectId = phase.ProjectId,
                    Name = phase.Name,
                    Description = phase.Description,
                    Status = phase.Status,
                    Progress = phase.Progress,
                    Budget = phase.Budget,
                    AssignedTo = phase.AssignedTo,
                    // Normalize DateTime fields to UTC
                    StartDate = NormalizeDateTime(phase.StartDate, DateTime.UtcNow),
                    EndDate = NormalizeDateTime(phase.EndDate, DateTime.UtcNow.AddDays(30)),
                };

                Console.WriteLine(
                    $"Normalized StartDate: {normalizedPhase.StartDate} (Kind: {normalizedPhase.StartDate.Kind})"
                );
                Console.WriteLine(
                    $"Normalized EndDate: {normalizedPhase.EndDate} (Kind: {normalizedPhase.EndDate.Kind})"
                );

                var existing = await _firebaseService.GetDocumentAsync<Phase>("phases", phaseId);
                if (existing != null)
                {
                    await _firebaseService.UpdateDocumentAsync("phases", phaseId, normalizedPhase);
                    Console.WriteLine($"SavePhase: Updated existing phase {phaseId}");
                }
                else
                {
                    await _firebaseService.AddDocumentWithIdAsync(
                        "phases",
                        phaseId,
                        normalizedPhase
                    );
                    Console.WriteLine($"SavePhase: Created new phase {phaseId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SavePhase: Error saving phase {phase.PhaseId} - {ex.Message}");
                Console.WriteLine($"SavePhase: Stack trace: {ex.StackTrace}");
                throw; // Re-throw to be handled by the calling method
            }
        }

        private DateTime NormalizeDateTime(DateTime dateTime, DateTime? fallback = null)
        {
            // If the date is invalid (year <= 1900), use fallback or current time
            if (dateTime.Year <= 1900)
            {
                return fallback ?? DateTime.UtcNow;
            }

            // Ensure the DateTime is UTC
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            else if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }
            else
            {
                return dateTime; // Already UTC
            }
        }

        private async Task SaveTask(ProjectTask task)
        {
            try
            {
                var taskId = string.IsNullOrEmpty(task.TaskId)
                    ? Guid.NewGuid().ToString()
                    : task.TaskId;

                Console.WriteLine($"SaveTask: Processing task {taskId}");
                Console.WriteLine(
                    $"Original StartDate: {task.StartDate} (Kind: {task.StartDate.Kind})"
                );
                Console.WriteLine($"Original DueDate: {task.DueDate} (Kind: {task.DueDate.Kind})");
                Console.WriteLine(
                    $"Original CompletedDate: {task.CompletedDate} (Kind: {task.CompletedDate?.Kind})"
                );

                // Create a new task object with properly normalized DateTime fields
                var normalizedTask = new ProjectTask
                {
                    TaskId = taskId,
                    ProjectId = task.ProjectId,
                    PhaseId = task.PhaseId,
                    Name = task.Name,
                    Description = task.Description,
                    AssignedTo = task.AssignedTo,
                    Priority = task.Priority,
                    Status = task.Status,
                    Progress = task.Progress,
                    EstimatedHours = task.EstimatedHours,
                    ActualHours = task.ActualHours,
                    // Normalize DateTime fields to UTC
                    StartDate = NormalizeDateTime(task.StartDate, DateTime.UtcNow),
                    DueDate = NormalizeDateTime(task.DueDate, DateTime.UtcNow.AddDays(7)),
                    CompletedDate =
                        task.CompletedDate.HasValue && task.CompletedDate.Value.Year > 1900
                            ? NormalizeDateTime(task.CompletedDate.Value, null)
                            : null,
                };

                Console.WriteLine(
                    $"Normalized StartDate: {normalizedTask.StartDate} (Kind: {normalizedTask.StartDate.Kind})"
                );
                Console.WriteLine(
                    $"Normalized DueDate: {normalizedTask.DueDate} (Kind: {normalizedTask.DueDate.Kind})"
                );
                Console.WriteLine(
                    $"Normalized CompletedDate: {normalizedTask.CompletedDate} (Kind: {normalizedTask.CompletedDate?.Kind})"
                );

                var existing = await _firebaseService.GetDocumentAsync<ProjectTask>(
                    "tasks",
                    taskId
                );

                var currentUserId = User.UserId();
                var isNowAssigned = !string.IsNullOrEmpty(normalizedTask.AssignedTo);

                if (existing != null)
                {
                    var assignmentChanged = existing.AssignedTo != normalizedTask.AssignedTo;
                    await _firebaseService.UpdateDocumentAsync("tasks", taskId, normalizedTask);
                    Console.WriteLine($"SaveTask: Updated existing task {taskId}");

                    if (assignmentChanged && isNowAssigned && !string.IsNullOrEmpty(currentUserId))
                    {
                        var systemEvent = new SystemEvent
                        {
                            EventType = "task_assignment",
                            EntityId = normalizedTask.TaskId,
                            EntityType = "task",
                            Action = "assigned",
                            ProjectId = normalizedTask.ProjectId,
                            UserId = currentUserId,
                            Data = new Dictionary<string, object>
                            {
                                { "taskId", normalizedTask.TaskId },
                                { "taskName", normalizedTask.Name },
                                { "assignedToId", normalizedTask.AssignedTo },
                                { "assignedById", currentUserId },
                            },
                        };
                        await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                    }
                }
                else
                {
                    await _firebaseService.AddDocumentWithIdAsync("tasks", taskId, normalizedTask);
                    Console.WriteLine($"SaveTask: Created new task {taskId}");
                    if (isNowAssigned && !string.IsNullOrEmpty(currentUserId))
                    {
                        var systemEvent = new SystemEvent
                        {
                            EventType = "task_assignment",
                            EntityId = normalizedTask.TaskId,
                            EntityType = "task",
                            Action = "assigned",
                            ProjectId = normalizedTask.ProjectId,
                            UserId = currentUserId,
                            Data = new Dictionary<string, object>
                            {
                                { "taskId", normalizedTask.TaskId },
                                { "taskName", normalizedTask.Name },
                                { "assignedToId", normalizedTask.AssignedTo },
                                { "assignedById", currentUserId },
                            },
                        };
                        await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveTask: Error saving task {task.TaskId} - {ex.Message}");
                Console.WriteLine($"SaveTask: Stack trace: {ex.StackTrace}");
                throw; // Re-throw to be handled by the calling method
            }
        }

        [HttpPost("create/project")]
        public async Task<ActionResult<Project>> CreateProject([FromBody] Project project)
        {
            try
            {
                // Assign the Project Manager ID from the logged-in user
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    Console.WriteLine("No current user ID found - Project creation is not allowed");
                    return BadRequest(
                        new
                        {
                            error = "Authentication error: No project manager ID found. Please log in again.",
                        }
                    );
                }

                project.ProjectManagerId = currentUserId;
                Console.WriteLine(
                    $"[CreateProject] Set ProjectManagerId to: '{project.ProjectManagerId}'"
                );

                // Normalize all DateTime fields to UTC
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

                // Log the normalized values (optional)
                Console.WriteLine($"[CreateProject] StartDate.Kind = {project.StartDate.Kind}");
                Console.WriteLine(
                    $"[CreateProject] EndDatePlanned.Kind = {project.EndDatePlanned.Kind}"
                );
                Console.WriteLine(
                    $"[CreateProject] EndDateActual.Kind = {project.EndDateActual?.Kind.ToString() ?? "null"}"
                );

                // Add to Firestore
                await _firebaseService.AddDocumentWithIdAsync(
                    "projects",
                    project.ProjectId,
                    project
                );

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Project Creation",
                    "Project Created",
                    $"Project {project.Name} ({project.ProjectId}) created for client {project.ClientId}",
                    userId ?? "system",
                    project.ProjectId
                );

                Console.WriteLine($"Added project {project.Name} successfully.");
                return Ok(project);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
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

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Project Update",
                    "Phase Created",
                    $"Phase {phase.Name} ({phase.PhaseId}) created for project {projectId}",
                    userId ?? "system",
                    phase.PhaseId
                );

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

                // Send workflow notification if task is assigned to someone
                if (!string.IsNullOrEmpty(task.AssignedTo))
                {
                    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        var systemEvent = new SystemEvent
                        {
                            EventType = "task_assignment",
                            EntityId = task.TaskId,
                            EntityType = "task",
                            Action = "assigned",
                            ProjectId = task.ProjectId,
                            UserId = currentUserId,
                            Data = new Dictionary<string, object>
                            {
                                { "taskId", task.TaskId },
                                { "taskName", task.Name },
                                { "assignedToId", task.AssignedTo },
                                { "assignedById", currentUserId },
                            },
                        };
                        await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                    }
                }

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Task Update",
                    "Task Created",
                    $"Task {task.Name} ({task.TaskId}) created for project {projectId}",
                    userId ?? "system",
                    task.TaskId
                );

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

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Project Update",
                    "Project Updated",
                    $"Project {project.Name} ({id}) updated",
                    userId ?? "system",
                    id
                );

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

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Project Update",
                    "Phase Updated",
                    $"Phase {phase.Name} ({id}) updated for project {phase.ProjectId}",
                    userId ?? "system",
                    id
                );

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

                // Check if task assignment has changed
                var assignmentChanged = existingTask.AssignedTo != task.AssignedTo;
                var wasUnassigned = string.IsNullOrEmpty(existingTask.AssignedTo);
                var isNowAssigned = !string.IsNullOrEmpty(task.AssignedTo);

                var statusChanged = existingTask.Status != task.Status;
                var progressChanged = existingTask.Progress != task.Progress;

                await _firebaseService.UpdateDocumentAsync("tasks", id, task);

                // Send project update notification to client if status or progress changes
                if (statusChanged || progressChanged)
                {
                    var project = await _firebaseService.GetDocumentAsync<Project>(
                        "projects",
                        task.ProjectId
                    );
                    if (project != null && !string.IsNullOrEmpty(project.ClientId))
                    {
                        string updateMessage = "";
                        if (statusChanged && progressChanged)
                        {
                            updateMessage =
                                $"Task '{task.Name}' status is now '{task.Status}' and progress is {task.Progress}%.";
                        }
                        else if (statusChanged)
                        {
                            updateMessage =
                                $"Task '{task.Name}' status has been updated to '{task.Status}'.";
                        }
                        else // progressChanged
                        {
                            updateMessage = $"Task '{task.Name}' progress is now {task.Progress}%.";
                        }

                        var systemEvent = new SystemEvent
                        {
                            EventType = "project_update",
                            EntityId = task.ProjectId,
                            EntityType = "project",
                            Action = "task_update",
                            ProjectId = task.ProjectId,
                            UserId = project.ClientId,
                            Data = new Dictionary<string, object>
                            {
                                { "projectId", task.ProjectId },
                                { "updateType", updateMessage },
                                { "userId", project.ClientId },
                            },
                        };
                        await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                    }
                }

                // Send workflow notification if task assignment changed
                if (assignmentChanged && isNowAssigned)
                {
                    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        var systemEvent = new SystemEvent
                        {
                            EventType = "task_assignment",
                            EntityId = task.TaskId,
                            EntityType = "task",
                            Action = "assigned",
                            ProjectId = task.ProjectId,
                            UserId = currentUserId,
                            Data = new Dictionary<string, object>
                            {
                                { "taskId", task.TaskId },
                                { "taskName", task.Name },
                                { "assignedToId", task.AssignedTo },
                                { "assignedById", currentUserId },
                            },
                        };
                        await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                    }
                }

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Task Update",
                    "Task Updated",
                    $"Task {task.Name} ({id}) updated for project {task.ProjectId}",
                    userId ?? "system",
                    id
                );

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
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var existingPhase = await _firebaseService.GetDocumentAsync<Phase>("phases", id);
                if (existingPhase == null)
                {
                    return NotFound(new { error = "Phase not found" });
                }

                // Verify the phase belongs to a project managed by this PM
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    existingPhase.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
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
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var existingTask = await _firebaseService.GetDocumentAsync<ProjectTask>(
                    "tasks",
                    id
                );
                if (existingTask == null)
                {
                    return NotFound(new { error = "Task not found" });
                }

                // Verify the task belongs to a project managed by this PM
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    existingTask.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
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

        // ===================== PROGRESS REPORT & COMPLETION APPROVAL ENDPOINTS =====================

        [HttpGet("progress-reports/pending")]
        public async Task<ActionResult<List<ProgressReport>>> GetPendingProgressReports()
        {
            try
            {
                var progressReports = await _firebaseService.GetCollectionAsync<ProgressReport>(
                    "progressReports"
                );
                var pendingReports = progressReports
                    .Where(pr => pr.Status == "Approved")
                    .OrderBy(pr => pr.SubmittedAt)
                    .ToList();

                return Ok(pendingReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("progress-report/{id}")]
        public async Task<ActionResult<ProgressReport>> GetProgressReport(string id)
        {
            try
            {
                var progressReport = await _firebaseService.GetDocumentAsync<ProgressReport>(
                    "progressReports",
                    id
                );

                if (progressReport == null)
                {
                    return NotFound(new { error = "Progress report not found" });
                }

                return Ok(progressReport);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("progress-report/{id}/approve")] // DEPRECATED: Progress reports are now auto-approved
        public async Task<ActionResult<ProgressReport>> ApproveProgressReport(
            string id,
            [FromBody] object approvalData
        )
        {
            try
            {
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var report = await _firebaseService.GetDocumentAsync<ProgressReport>(
                    "progressReports",
                    id
                );
                if (report == null)
                {
                    return NotFound(new { error = "Progress report not found" });
                }

                // Ensure this report belongs to a project managed by the caller
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    report.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
                {
                    return Forbid();
                }

                // Idempotency and state preconditions
                if (report.Status == "Approved")
                {
                    return Ok(report);
                }

                // Update the report status
                report.Status = "Approved";
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewedBy = pmId;
                await _firebaseService.UpdateDocumentAsync("progressReports", id, report);

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        error = "An error occurred while approving progress report",
                        details = ex.Message,
                    }
                );
            }
        }

        [HttpPut("progress-report/{id}/reject")] // DEPRECATED: Progress reports are now auto-approved
        public async Task<ActionResult<ProgressReport>> RejectProgressReport(
            string id,
            [FromBody] object rejectionData
        )
        {
            try
            {
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var report = await _firebaseService.GetDocumentAsync<ProgressReport>(
                    "progressReports",
                    id
                );
                if (report == null)
                {
                    return NotFound(new { error = "Progress report not found" });
                }

                // ownership guard
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    report.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
                {
                    return Forbid();
                }
                // idempotent reject
                if (report.Status == "Rejected")
                {
                    return Ok(report);
                }
                // only allow rejecting from Submitted state
                if (report.Status != "Submitted")
                {
                    return Conflict(new { error = "Only 'Submitted' reports can be rejected." });
                }

                // Update report status
                report.Status = "Rejected";
                report.ReviewedBy = pmId;
                report.ReviewedAt = DateTime.UtcNow;
                // Note: ReviewNotes would be extracted from rejectionData in a real implementation

                await _firebaseService.UpdateDocumentAsync("progressReports", id, report);

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("task/{taskId}/approve-completion")]
        public async Task<ActionResult<ProjectTask>> ApproveTaskCompletion(string taskId)
        {
            try
            {
                Console.WriteLine($"[ApproveTaskCompletion] Starting approval for task {taskId}");

                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(pmId))
                {
                    Console.WriteLine($"[ApproveTaskCompletion] No PM ID found in claims");
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                Console.WriteLine($"[ApproveTaskCompletion] PM ID: {pmId}");

                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task == null)
                {
                    Console.WriteLine($"[ApproveTaskCompletion] Task {taskId} not found");
                    return NotFound(new { error = "Task not found" });
                }

                Console.WriteLine($"[ApproveTaskCompletion] Found task: {task.Name}");

                // Verify the task belongs to a project managed by this PM
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    task.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
                {
                    Console.WriteLine(
                        $"[ApproveTaskCompletion] Task {taskId} does not belong to PM {pmId}"
                    );
                    return Forbid();
                }

                Console.WriteLine(
                    $"[ApproveTaskCompletion] Task belongs to project: {project.Name}"
                );

                // Find and update the completion report for this task
                var allCompletionReports =
                    await _firebaseService.GetCollectionAsync<CompletionReport>(
                        "completionReports"
                    );
                var completionReport = allCompletionReports
                    .Where(r => r.TaskId == taskId && r.Status == "Submitted")
                    .OrderByDescending(r => r.SubmittedAt)
                    .FirstOrDefault();

                if (completionReport != null)
                {
                    Console.WriteLine(
                        $"[ApproveTaskCompletion] Found completion report {completionReport.CompletionReportId} for task {taskId}"
                    );

                    // Update the completion report status
                    completionReport.Status = "Approved";
                    completionReport.ReviewedAt = DateTime.UtcNow;
                    completionReport.ReviewedBy = pmId;

                    await _firebaseService.UpdateDocumentAsync(
                        "completionReports",
                        completionReport.CompletionReportId,
                        completionReport
                    );
                    Console.WriteLine(
                        $"[ApproveTaskCompletion] Updated completion report status to Approved"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"[ApproveTaskCompletion] No completion report found for task {taskId}"
                    );
                    return NotFound(new { error = "No completion request found for this task" });
                }

                // Update task status to completed
                task.Status = "Completed";
                task.CompletedDate = DateTime.UtcNow;
                
                // Update task actual hours and spent amount from completion report
                if (completionReport != null)
                {
                    task.ActualHours = completionReport.FinalHours;
                    task.SpentAmount = completionReport.SpentAmount;
                    Console.WriteLine(
                        $"[ApproveTaskCompletion] Updated task {taskId} actual hours to {completionReport.FinalHours} and spent amount to {completionReport.SpentAmount} from completion report"
                    );
                }

                await _firebaseService.UpdateDocumentAsync("tasks", taskId, task);
                Console.WriteLine(
                    $"[ApproveTaskCompletion] Updated task {taskId} status to Completed"
                );

                // Update task progress to 100% and phase progress
                await UpdateTaskProgressToComplete(taskId);
                await UpdatePhaseProgress(task.PhaseId);
                
                // Update phase and project spent amounts
                await UpdatePhaseSpentAmount(task.PhaseId);
                await UpdateProjectBudgetActual(task.ProjectId);

                // Check project completion (will check if all tasks/phases are done and update project status)
                await CheckProjectCompletion(task.ProjectId);

                // Notify client of task completion
                if (project != null && !string.IsNullOrEmpty(project.ClientId))
                {
                    var systemEvent = new SystemEvent
                    {
                        EventType = "project_update",
                        EntityId = project.ProjectId,
                        EntityType = "project",
                        Action = "task_completed",
                        ProjectId = project.ProjectId,
                        UserId = project.ClientId,
                        Data = new Dictionary<string, object>
                        {
                            { "projectId", project.ProjectId },
                            { "updateType", $"Task '{task.Name}' has been completed." },
                            { "userId", project.ClientId },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                }

                Console.WriteLine($"[ApproveTaskCompletion] Successfully approved task {taskId}");
                return Ok(task);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApproveTaskCompletion] Error: {ex.Message}");
                return StatusCode(
                    500,
                    new
                    {
                        error = "An error occurred while approving task completion",
                        details = ex.Message,
                    }
                );
            }
        }

        [HttpPut("task/{taskId}/reject-completion")]
        public async Task<ActionResult<ProjectTask>> RejectTaskCompletion(
            string taskId,
            [FromBody] object? rejectionData = null
        )
        {
            try
            {
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }

                // Verify that the calling PM actually owns the parent project
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    task.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
                {
                    return Forbid();
                }

                // Idempotency: if it’s already In Progress, just return it
                if (task.Status == "In Progress")
                {
                    return Ok(task);
                }

                // Revert task status back to In Progress and clear any completion timestamp
                task.Status = "In Progress";
                task.CompletedDate = null;

                await _firebaseService.UpdateDocumentAsync("tasks", taskId, task);

                // Recalculate phase progress since a task was rejected
                await UpdatePhaseProgress(task.PhaseId);

                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("completion-reports")]
        public async Task<ActionResult<List<CompletionReport>>> GetCompletionReports()
        {
            try
            {
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[GetCompletionReports] PM ID: {pmId}");
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                // Get all projects managed by this PM
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                Console.WriteLine($"[GetCompletionReports] Found {projects.Count} total projects");
                Console.WriteLine($"[GetCompletionReports] Current PM ID: {pmId}");

                var pmProjects = projects.Where(p => p.ProjectManagerId == pmId).ToList();
                Console.WriteLine(
                    $"[GetCompletionReports] Found {pmProjects.Count} projects for PM {pmId}"
                );

                var projectIds = pmProjects.Select(p => p.ProjectId).ToList();
                Console.WriteLine(
                    $"[GetCompletionReports] PM project IDs: {string.Join(", ", projectIds)}"
                );

                // Get all completion reports and filter by project
                var allCompletionReports =
                    await _firebaseService.GetCollectionAsync<CompletionReport>(
                        "completionReports"
                    );
                Console.WriteLine(
                    $"[GetCompletionReports] Found {allCompletionReports.Count} total completion reports"
                );

                // Log all completion report project IDs
                var allReportProjectIds = allCompletionReports
                    .Select(r => r.ProjectId)
                    .Distinct()
                    .ToList();
                Console.WriteLine(
                    $"[GetCompletionReports] Completion report project IDs: {string.Join(", ", allReportProjectIds)}"
                );

                var pmCompletionReports = allCompletionReports
                    .Where(r => projectIds.Contains(r.ProjectId))
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToList();

                Console.WriteLine(
                    $"[GetCompletionReports] Found {pmCompletionReports.Count} completion reports for PM projects"
                );

                // If no completion reports found, check if there are orphaned reports
                if (pmCompletionReports.Count == 0 && allCompletionReports.Count > 0)
                {
                    var orphanedReports = allCompletionReports
                        .Where(r => !projectIds.Contains(r.ProjectId))
                        .ToList();
                    Console.WriteLine(
                        $"[GetCompletionReports] Found {orphanedReports.Count} orphaned completion reports"
                    );

                    // Log details about orphaned reports for debugging
                    foreach (var report in orphanedReports)
                    {
                        Console.WriteLine(
                            $"[GetCompletionReports] Orphaned report: TaskId={report.TaskId}, ProjectId={report.ProjectId}, SubmittedBy={report.SubmittedBy}"
                        );
                    }
                }

                return Ok(pmCompletionReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("completion-report/{id}")]
        public async Task<ActionResult<CompletionReport>> GetCompletionReport(string id)
        {
            try
            {
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var report = await _firebaseService.GetDocumentAsync<CompletionReport>(
                    "completionReports",
                    id
                );
                if (report == null)
                {
                    return NotFound(new { error = "Completion report not found" });
                }

                // Ensure this report belongs to a project managed by the caller
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    report.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
                {
                    return Forbid();
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("completion-report/{id}/approve")]
        public async Task<ActionResult<CompletionReport>> ApproveCompletionReport(
            string id,
            [FromBody] object approvalData
        )
        {
            try
            {
                Console.WriteLine($"[ApproveCompletionReport] Approving completion report {id}");
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[ApproveCompletionReport] PM ID: {pmId}");
                if (string.IsNullOrEmpty(pmId))
                {
                    Console.WriteLine($"[ApproveCompletionReport] No PM ID found");
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var report = await _firebaseService.GetDocumentAsync<CompletionReport>(
                    "completionReports",
                    id
                );
                if (report == null)
                {
                    return NotFound(new { error = "Completion report not found" });
                }

                // Ensure this report belongs to a project managed by the caller
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    report.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
                {
                    return Forbid();
                }

                // Idempotency and state preconditions
                if (report.Status == "Approved")
                {
                    return Ok(report);
                }

                // Update the report status
                report.Status = "Approved";
                report.ReviewedAt = DateTime.UtcNow;
                report.ReviewedBy = pmId;
                await _firebaseService.UpdateDocumentAsync("completionReports", id, report);

                // Notify contractor about approval
                if (!string.IsNullOrEmpty(report.SubmittedBy))
                {
                    var systemEvent = new SystemEvent
                    {
                        EventType = "project_update",
                        EntityId = report.ProjectId,
                        EntityType = "project",
                        Action = "completion_request_approved",
                        ProjectId = report.ProjectId,
                        UserId = report.SubmittedBy,
                        Data = new Dictionary<string, object>
                        {
                            { "projectId", report.ProjectId },
                            { "updateType", "Your completion request for task has been approved." },
                            { "userId", report.SubmittedBy },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                }

                // Update the associated task status to "Completed"
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>(
                    "tasks",
                    report.TaskId
                );
                if (task != null)
                {
                    task.Status = "Completed";
                    task.CompletedDate = DateTime.UtcNow;
                    await _firebaseService.UpdateDocumentAsync("tasks", report.TaskId, task);
                    Console.WriteLine(
                        $"[ApproveCompletionReport] Updated task {report.TaskId} status to Completed"
                    );

                    // Update task progress to 100% and phase progress
                    await UpdateTaskProgressToComplete(report.TaskId);
                    await UpdatePhaseProgress(task.PhaseId);

                    // Send workflow notification about task completion
                    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        var systemEvent = new SystemEvent
                        {
                            EventType = "task_completion",
                            EntityId = report.TaskId,
                            EntityType = "task",
                            Action = "completed",
                            ProjectId = report.ProjectId,
                            UserId = report.SubmittedBy,
                            Data = new Dictionary<string, object>
                            {
                                { "taskId", report.TaskId },
                                { "completedById", report.SubmittedBy },
                            },
                        };
                        await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                    }
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("completion-report/{id}/reject")]
        public async Task<ActionResult<CompletionReport>> RejectCompletionReport(
            string id,
            [FromBody] object rejectionData
        )
        {
            try
            {
                var pmId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(pmId))
                {
                    return Unauthorized(new { error = "Project Manager ID not found" });
                }

                var report = await _firebaseService.GetDocumentAsync<CompletionReport>(
                    "completionReports",
                    id
                );
                if (report == null)
                {
                    return NotFound(new { error = "Completion report not found" });
                }

                // Ensure this report belongs to a project managed by the caller
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    report.ProjectId
                );
                if (project == null || project.ProjectManagerId != pmId)
                {
                    return Forbid();
                }

                // Update report status
                report.Status = "Rejected";
                report.ReviewedBy = pmId;
                report.ReviewedAt = DateTime.UtcNow;
                // Note: ReviewNotes would be extracted from rejectionData in a real implementation

                await _firebaseService.UpdateDocumentAsync("completionReports", id, report);

                // Notify contractor about rejection
                if (!string.IsNullOrEmpty(report.SubmittedBy))
                {
                    var systemEvent = new SystemEvent
                    {
                        EventType = "project_update",
                        EntityId = report.ProjectId,
                        EntityType = "project",
                        Action = "completion_request_rejected",
                        ProjectId = report.ProjectId,
                        UserId = report.SubmittedBy,
                        Data = new Dictionary<string, object>
                        {
                            { "projectId", report.ProjectId },
                            {
                                "updateType",
                                "Your completion request for task has been rejected. Please review and resubmit."
                            },
                            { "userId", report.SubmittedBy },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                }

                // Update the associated task status back to "In Progress"
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>(
                    "tasks",
                    report.TaskId
                );
                if (task != null)
                {
                    task.Status = "In Progress";
                    await _firebaseService.UpdateDocumentAsync("tasks", report.TaskId, task);
                    Console.WriteLine(
                        $"[RejectCompletionReport] Updated task {report.TaskId} status to In Progress"
                    );

                    // Recalculate phase progress since a task was rejected
                    await UpdatePhaseProgress(task.PhaseId);
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Helper method to update task progress to 100% when completed
        private async Task UpdateTaskProgressToComplete(string taskId)
        {
            try
            {
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task != null)
                {
                    task.Progress = 100;
                    await _firebaseService.UpdateDocumentAsync("tasks", taskId, task);
                    Console.WriteLine(
                        $"[UpdateTaskProgressToComplete] Set task {taskId} progress to 100%"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[UpdateTaskProgressToComplete] Error updating task progress: {ex.Message}"
                );
            }
        }

        // Helper method to calculate and update phase progress
        private async Task UpdatePhaseProgress(string phaseId)
        {
            try
            {
                // Get all tasks for this phase
                var allTasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var phaseTasks = allTasks.Where(t => t.PhaseId == phaseId).ToList();

                if (phaseTasks.Count == 0)
                {
                    Console.WriteLine($"[UpdatePhaseProgress] No tasks found for phase {phaseId}");
                    return;
                }

                // Calculate progress: (completed tasks / total tasks) * 100
                var completedTasks = phaseTasks.Count(t => t.Status == "Completed");
                var progressPercentage = (int)
                    Math.Round((double)completedTasks / phaseTasks.Count * 100);

                Console.WriteLine(
                    $"[UpdatePhaseProgress] Phase {phaseId}: {completedTasks}/{phaseTasks.Count} tasks completed = {progressPercentage}%"
                );

                // Update phase progress
                var phase = await _firebaseService.GetDocumentAsync<Phase>("phases", phaseId);
                if (phase != null)
                {
                    phase.Progress = progressPercentage;

                    // Check if all tasks are completed to mark phase as complete
                    if (completedTasks == phaseTasks.Count)
                    {
                        phase.Status = "Completed";
                        Console.WriteLine(
                            $"[UpdatePhaseProgress] Phase {phaseId} marked as Completed"
                        );
                    }
                    else if (phase.Status == "Completed" && completedTasks < phaseTasks.Count)
                    {
                        // If phase was marked complete but not all tasks are done, revert status
                        phase.Status = "In Progress";
                        Console.WriteLine(
                            $"[UpdatePhaseProgress] Phase {phaseId} reverted to In Progress"
                        );
                    }

                    await _firebaseService.UpdateDocumentAsync("phases", phaseId, phase);
                    Console.WriteLine(
                        $"[UpdatePhaseProgress] Updated phase {phaseId} progress to {progressPercentage}%"
                    );

                    // Check if project completion should be updated (checks all projects when phases complete)
                    if (phase.Status == "Completed")
                    {
                        await CheckProjectCompletion(phase.ProjectId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[UpdatePhaseProgress] Error updating phase progress: {ex.Message}"
                );
            }
        }

        // Helper method to calculate and update phase spent amount from tasks
        private async Task UpdatePhaseSpentAmount(string phaseId)
        {
            try
            {
                // Get all tasks for this phase
                var allTasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var phaseTasks = allTasks.Where(t => t.PhaseId == phaseId).ToList();

                if (phaseTasks.Count == 0)
                {
                    Console.WriteLine($"[UpdatePhaseSpentAmount] No tasks found for phase {phaseId}");
                    return;
                }

                // Calculate total spent amount: sum of all task spent amounts
                var totalSpentAmount = phaseTasks.Sum(t => t.SpentAmount);

                Console.WriteLine(
                    $"[UpdatePhaseSpentAmount] Phase {phaseId}: Total spent amount = {totalSpentAmount}"
                );

                // Update phase spent amount
                var phase = await _firebaseService.GetDocumentAsync<Phase>("phases", phaseId);
                if (phase != null)
                {
                    phase.SpentAmount = totalSpentAmount;
                    await _firebaseService.UpdateDocumentAsync("phases", phaseId, phase);
                    Console.WriteLine(
                        $"[UpdatePhaseSpentAmount] Updated phase {phaseId} spent amount to {totalSpentAmount}"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[UpdatePhaseSpentAmount] Error updating phase spent amount: {ex.Message}"
                );
            }
        }

        // Helper method to calculate and update project budget actual from phases/tasks
        private async Task UpdateProjectBudgetActual(string projectId)
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", projectId);
                if (project == null)
                {
                    Console.WriteLine($"[UpdateProjectBudgetActual] Project {projectId} not found");
                    return;
                }

                // Get all phases for this project
                var allPhases = await _firebaseService.GetCollectionAsync<Phase>("phases");
                var projectPhases = allPhases.Where(p => p.ProjectId == projectId).ToList();

                // Calculate total spent amount: sum of all phase spent amounts
                var totalSpentAmount = projectPhases.Sum(p => p.SpentAmount);

                Console.WriteLine(
                    $"[UpdateProjectBudgetActual] Project {projectId}: Total spent amount = {totalSpentAmount} from {projectPhases.Count} phases"
                );

                // Update project budget actual
                project.BudgetActual = totalSpentAmount;
                await _firebaseService.UpdateDocumentAsync("projects", projectId, project);
                Console.WriteLine(
                    $"[UpdateProjectBudgetActual] Updated project {projectId} budget actual to {totalSpentAmount}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[UpdateProjectBudgetActual] Error updating project budget actual: {ex.Message}"
                );
            }
        }

        // Helper method to check if all tasks/phases are complete and update project status to Completed
        // Works for all project types (Active, Maintenance, etc.), but skips projects already marked as Completed
        private async Task CheckProjectCompletion(string projectId)
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", projectId);
                if (project == null)
                {
                    Console.WriteLine($"[CheckProjectCompletion] Project {projectId} not found");
                    return;
                }

                // Skip if project is already completed or cancelled
                if (project.Status == "Completed" || project.Status == "Cancelled")
                {
                    Console.WriteLine($"[CheckProjectCompletion] Project {projectId} already in final state: {project.Status}");
                    return;
                }

                // Store original status for workflow message type determination
                var originalStatus = project.Status;
                var isMaintenanceProject = originalStatus == "Maintenance";

                // Get all phases for this project
                var allPhases = await _firebaseService.GetCollectionAsync<Phase>("phases");
                var projectPhases = allPhases.Where(p => p.ProjectId == projectId).ToList();

                if (projectPhases.Count == 0)
                {
                    Console.WriteLine($"[CheckProjectCompletion] No phases found for project {projectId}");
                    return;
                }

                // Check if all phases are completed
                var allPhasesComplete = projectPhases.All(p => p.Status == "Completed");
                if (!allPhasesComplete)
                {
                    Console.WriteLine(
                        $"[CheckProjectCompletion] Not all phases complete for project {projectId}. Completed: {projectPhases.Count(p => p.Status == "Completed")}/{projectPhases.Count}"
                    );
                    return;
                }

                // Get all tasks for this project
                var allTasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var projectTasks = allTasks.Where(t => t.ProjectId == projectId).ToList();

                if (projectTasks.Count == 0)
                {
                    Console.WriteLine($"[CheckProjectCompletion] No tasks found for project {projectId}");
                    return;
                }

                // Check if all tasks are completed
                var allTasksComplete = projectTasks.All(t => t.Status == "Completed");
                if (!allTasksComplete)
                {
                    Console.WriteLine(
                        $"[CheckProjectCompletion] Not all tasks complete for project {projectId}. Completed: {projectTasks.Count(t => t.Status == "Completed")}/{projectTasks.Count}"
                    );
                    return;
                }

                // All phases and tasks are complete - update project status to Completed
                project.Status = "Completed";
                project.EndDateActual = DateTime.UtcNow;
                await _firebaseService.UpdateDocumentAsync("projects", projectId, project);

                Console.WriteLine(
                    $"[CheckProjectCompletion] Project {projectId} ({project.Name}) marked as Completed - all {projectPhases.Count} phases and {projectTasks.Count} tasks are complete"
                );

                // Send workflow message to client
                if (!string.IsNullOrEmpty(project.ClientId))
                {
                    // For maintenance projects, send maintenance_request completion message
                    // For other projects, send project_update completion message
                    var systemEvent = new SystemEvent
                    {
                        EventType = isMaintenanceProject ? "maintenance_request" : "project_update",
                        EntityId = projectId,
                        EntityType = isMaintenanceProject ? "maintenance_request" : "project",
                        Action = "completed",
                        ProjectId = projectId,
                        UserId = project.ClientId,
                        Data = new Dictionary<string, object>
                        {
                            { "projectId", projectId },
                            { "projectName", project.Name },
                        },
                    };
                    await _workflowMessageService.CreateWorkflowMessageAsync(systemEvent);
                    Console.WriteLine(
                        $"[CheckProjectCompletion] Sent completion notification to client {project.ClientId} (project type: {originalStatus})"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[CheckProjectCompletion] Error checking project completion: {ex.Message}"
                );
            }
        }
    }
}
