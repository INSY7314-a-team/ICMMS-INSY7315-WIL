using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using ICCMS_API.Auth;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Contractor,Tester")]
    public class ContractorsController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IAuditLogService _auditLogService;

        public ContractorsController(
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

        [HttpGet("Project/Tasks")]
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

        [HttpGet("task/{taskId}")]
        public async Task<ActionResult<ProjectTask>> GetTaskDetails(string taskId)
        {
            Console.WriteLine($"[ContractorsController] GetTaskDetails called for task {taskId}");
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Get the specific task
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }

                // Verify the task is assigned to this contractor
                if (task.AssignedTo != contractorId)
                {
                    return Forbid("You are not authorized to view this task");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[ContractorsController] Error getting task details: {ex.Message}"
                );
                return StatusCode(500, new { error = "Internal server error" });
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

                var userId = User.UserId();
                _auditLogService.LogAsync(
                    "Contractor Update",
                    "Task Updated",
                    $"Task {task.Name} ({id}) updated by contractor",
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

        // ===================== CONTRACTOR DASHBOARD ENDPOINTS =====================

        [HttpGet("health")]
        public ActionResult<object> GetHealth()
        {
            return Ok(
                new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    message = "Contractor API is running",
                }
            );
        }

        [HttpGet("messaging/available-users")]
        public async Task<ActionResult<List<object>>> GetAvailableUsersForMessaging()
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized("Contractor ID not found");
                }

                // Get all projects where this contractor has tasks
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var contractorTasks = tasks.Where(t => t.AssignedTo == contractorId).ToList();
                var projectIds = contractorTasks.Select(t => t.ProjectId).Distinct().ToList();

                if (!projectIds.Any())
                {
                    return Ok(new List<object>());
                }

                // Get all projects to find their Project Managers
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var contractorProjects = projects
                    .Where(p => projectIds.Contains(p.ProjectId))
                    .ToList();

                // Get unique Project Manager IDs
                var pmIds = contractorProjects
                    .Where(p => !string.IsNullOrEmpty(p.ProjectManagerId))
                    .Select(p => p.ProjectManagerId)
                    .Distinct()
                    .ToList();

                // Get all users to find Project Managers, other contractors, and clients
                var users = await _firebaseService.GetCollectionAsync<User>("users");

                // Get Project Managers and Admins
                var projectManagers = users
                    .Where(u =>
                        pmIds.Contains(u.UserId)
                        && (u.Role == "Project Manager" || u.Role == "Admin")
                    )
                    .ToList();

                // Get other contractors working on the same projects
                var otherContractorTasks = tasks
                    .Where(t => projectIds.Contains(t.ProjectId) && t.AssignedTo != contractorId)
                    .ToList();
                var otherContractorIds = otherContractorTasks
                    .Select(t => t.AssignedTo)
                    .Distinct()
                    .ToList();

                var otherContractors = users
                    .Where(u => otherContractorIds.Contains(u.UserId) && u.Role == "Contractor")
                    .ToList();

                // Get clients of the projects
                var clientIds = contractorProjects
                    .Where(p => !string.IsNullOrEmpty(p.ClientId))
                    .Select(p => p.ClientId)
                    .Distinct()
                    .ToList();

                var clients = users
                    .Where(u => clientIds.Contains(u.UserId) && u.Role == "Client")
                    .ToList();

                // Combine all available users
                var allAvailableUsers = projectManagers
                    .Concat(otherContractors)
                    .Concat(clients)
                    .Select(u => new
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Role = u.Role,
                        Email = u.Email,
                    })
                    .ToList();

                return Ok(allAvailableUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("messaging/available-projects")]
        public async Task<ActionResult<List<object>>> GetAvailableProjectsForMessaging()
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized("Contractor ID not found");
                }

                // Get all projects where this contractor has tasks
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var contractorTasks = tasks.Where(t => t.AssignedTo == contractorId).ToList();
                var projectIds = contractorTasks.Select(t => t.ProjectId).Distinct().ToList();

                if (!projectIds.Any())
                {
                    return Ok(new List<object>());
                }

                // Get the projects
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var contractorProjects = projects
                    .Where(p => projectIds.Contains(p.ProjectId))
                    .Select(p => new
                    {
                        ProjectId = p.ProjectId,
                        Name = p.Name,
                        Description = p.Description,
                        Status = p.Status,
                    })
                    .ToList();

                return Ok(contractorProjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("projects")]
        public async Task<ActionResult<List<Project>>> GetProjects()
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized("Contractor ID not found");
                }

                // Get all projects where this contractor has tasks
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var contractorTasks = tasks.Where(t => t.AssignedTo == contractorId).ToList();
                var projectIds = contractorTasks.Select(t => t.ProjectId).Distinct().ToList();

                if (!projectIds.Any())
                {
                    return Ok(new List<Project>());
                }

                // Get the projects
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var contractorProjects = projects
                    .Where(p => projectIds.Contains(p.ProjectId))
                    .ToList();

                // Return the projects

                return Ok(contractorProjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("debug/tasks")]
        public async Task<ActionResult<object>> DebugTasks()
        {
            try
            {
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");

                var taskAssignments = tasks
                    .Select(t => new
                    {
                        TaskId = t.TaskId,
                        AssignedTo = t.AssignedTo,
                        ProjectId = t.ProjectId,
                        Name = t.Name,
                        Status = t.Status,
                    })
                    .ToList();

                var uniqueAssignments = tasks
                    .Select(t => t.AssignedTo)
                    .Distinct()
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();

                return Ok(
                    new
                    {
                        totalTasks = tasks.Count,
                        totalProjects = projects.Count,
                        uniqueAssignments = uniqueAssignments,
                        sampleTasks = taskAssignments.Take(10).ToList(),
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboard()
        {
            try
            {
                Console.WriteLine("[ContractorsController] Dashboard endpoint called");
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"[ContractorsController] Contractor ID: {contractorId}");

                if (string.IsNullOrEmpty(contractorId))
                {
                    Console.WriteLine(
                        "[ContractorsController] No contractor ID found, returning Unauthorized"
                    );
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Get all tasks assigned to this contractor
                Console.WriteLine("[ContractorsController] Fetching tasks from Firestore...");
                var tasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                Console.WriteLine($"[ContractorsController] Found {tasks.Count} total tasks");

                // Debug: Log all task assignments to see what's in the database
                Console.WriteLine("[ContractorsController] All task assignments:");
                foreach (var task in tasks.Take(10)) // Log first 10 tasks
                {
                    Console.WriteLine(
                        $"[ContractorsController] Task {task.TaskId}: AssignedTo='{task.AssignedTo}', ProjectId='{task.ProjectId}', Name='{task.Name}'"
                    );
                }

                var contractorTasks = tasks.Where(t => t.AssignedTo == contractorId).ToList();
                Console.WriteLine(
                    $"[ContractorsController] Found {contractorTasks.Count} tasks for contractor {contractorId}"
                );

                // Debug: Log all unique AssignedTo values to see what contractors exist
                var uniqueAssignments = tasks
                    .Select(t => t.AssignedTo)
                    .Distinct()
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();
                Console.WriteLine(
                    $"[ContractorsController] Unique AssignedTo values: {string.Join(", ", uniqueAssignments)}"
                );

                // Get projects for these tasks
                var projectIds = contractorTasks.Select(t => t.ProjectId).Distinct().ToList();
                var projects = await _firebaseService.GetCollectionAsync<Project>("projects");
                var taskProjects = projects.Where(p => projectIds.Contains(p.ProjectId)).ToList();

                // Get progress reports for these tasks
                var progressReports = await _firebaseService.GetCollectionAsync<ProgressReport>(
                    "progressReports"
                );
                var taskProgressReports = progressReports
                    .Where(pr => contractorTasks.Any(t => t.TaskId == pr.TaskId))
                    .ToList();

                // Calculate stats
                var totalTasks = contractorTasks.Count;
                var completedTasks = contractorTasks.Count(t => t.Status == "Completed");
                var inProgressTasks = contractorTasks.Count(t =>
                    t.Status == "InProgress" || t.Status == "In Progress"
                );
                var overdueTasks = contractorTasks.Count(t =>
                    t.DueDate < DateTime.UtcNow && t.Status != "Completed"
                );

                var response = new
                {
                    totalTasks,
                    completedTasks,
                    inProgressTasks,
                    overdueTasks,
                    tasks = contractorTasks,
                    projects = taskProjects,
                    progressReports = taskProgressReports,
                };

                Console.WriteLine(
                    $"[ContractorsController] Returning dashboard data: {totalTasks} total tasks, {completedTasks} completed, {inProgressTasks} in progress, {overdueTasks} overdue"
                );
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("tasks/assigned")]
        public async Task<ActionResult<PaginatedResponse<ContractorTaskDto>>> GetAssignedTasks(
            int page = 1,
            int pageSize = 20
        )
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Validate pagination parameters
                if (page < 1)
                    page = 1;
                if (pageSize < 1 || pageSize > 100)
                    pageSize = 20;

                Console.WriteLine(
                    $"[ContractorsController] Looking for tasks assigned to contractor: {contractorId}"
                );

                var filters = new Dictionary<string, object> { { "assignedto", contractorId } };

                // Debug: First get all tasks to see what's available
                var allTasks = await _firebaseService.GetCollectionAsync<ProjectTask>("tasks");
                Console.WriteLine(
                    $"[ContractorsController] Total tasks in database: {allTasks.Count}"
                );

                // Debug: Show some sample tasks and their assignments
                var sampleTasks = allTasks.Take(5).ToList();
                foreach (var task in sampleTasks)
                {
                    Console.WriteLine(
                        $"[ContractorsController] Sample task: {task.TaskId}, AssignedTo: '{task.AssignedTo}', ProjectId: '{task.ProjectId}'"
                    );
                }

                var tasks = await _firebaseService.GetCollectionWithFiltersAsync<ProjectTask>(
                    "tasks",
                    filters,
                    page,
                    pageSize,
                    "dueDate",
                    false // Order by dueDate ascending (earliest first)
                );

                var totalCount = await _firebaseService.GetCollectionCountAsync<ProjectTask>(
                    "tasks",
                    filters
                );

                // Convert ProjectTask objects to ContractorTaskDto objects with computed properties
                var contractorTasks = tasks
                    .Select(task => new ContractorTaskDto
                    {
                        TaskId = task.TaskId,
                        ProjectId = task.ProjectId,
                        Name = task.Name,
                        Description = task.Description,
                        AssignedTo = task.AssignedTo,
                        Priority = task.Priority,
                        Status = task.Status,
                        StartDate = task.StartDate,
                        DueDate = task.DueDate,
                        CompletedDate = task.CompletedDate,
                        Progress = task.Progress,
                        EstimatedHours = task.EstimatedHours,
                        ActualHours = task.ActualHours,
                        IsOverdue = task.DueDate < DateTime.UtcNow && task.Status != "Completed",
                        DaysUntilDue = Math.Max(0, (task.DueDate - DateTime.UtcNow).Days),
                        CanSubmitProgress =
                            task.Status?.Equals("In Progress", StringComparison.OrdinalIgnoreCase)
                                == true
                            || task.Status?.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                                == true,
                        CanRequestCompletion =
                            task.Status?.Equals("In Progress", StringComparison.OrdinalIgnoreCase)
                                == true
                            || task.Status?.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                                == true,
                    })
                    .ToList();

                var response = new PaginatedResponse<ContractorTaskDto>
                {
                    Data = contractorTasks,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("task/{taskId}/progress-reports")]
        public async Task<ActionResult<List<ProgressReport>>> GetTaskProgressReports(string taskId)
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Verify contractor is assigned to this task
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }
                if (task.AssignedTo != contractorId)
                {
                    return Forbid();
                }

                // Get all progress reports and filter in memory (avoids index requirement)
                var allProgressReports = await _firebaseService.GetCollectionAsync<ProgressReport>(
                    "progressReports"
                );
                var taskReports = allProgressReports
                    .Where(r => r.TaskId == taskId)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToList();

                return Ok(taskReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("task/{taskId}/progress-report")]
        public async Task<ActionResult<ProgressReport>> SubmitProgressReport(
            string taskId,
            [FromBody] ProgressReport report
        )
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Verify contractor is assigned to this task
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }
                if (task.AssignedTo != contractorId)
                {
                    return Unauthorized(new { error = "You are not assigned to this task" });
                }

                // Log task details for debugging
                Console.WriteLine(
                    $"[SubmitProgressReport] Task details - TaskId: {task.TaskId}, ProjectId: {task.ProjectId}, AssignedTo: {task.AssignedTo}"
                );

                // Set report properties
                report.ProgressReportId = Guid.NewGuid().ToString();
                report.TaskId = taskId;
                report.ProjectId = task.ProjectId;
                report.SubmittedBy = contractorId;
                report.SubmittedAt = DateTime.UtcNow;
                report.Status = "Approved"; // Auto-approve progress reports
                report.ReviewedBy = "System"; // Mark as auto-approved by system
                report.ReviewedAt = DateTime.UtcNow;

                await _firebaseService.AddDocumentWithIdAsync(
                    "progressReports",
                    report.ProgressReportId,
                    report
                );

                // Send workflow notification to project manager
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    await _workflowMessageService.SendProgressReportNotificationAsync(
                        report.ProgressReportId,
                        currentUserId
                    );
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("task/{taskId}/request-completion")]
        public async Task<ActionResult<object>> RequestCompletion(
            string taskId,
            [FromBody] CompletionReport completionReport
        )
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Verify contractor is assigned to this task
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }
                if (task.AssignedTo != contractorId)
                {
                    return Unauthorized(new { error = "You are not assigned to this task" });
                }

                // Log task details for debugging
                Console.WriteLine(
                    $"[RequestCompletion] Task details - TaskId: {task.TaskId}, ProjectId: {task.ProjectId}, AssignedTo: {task.AssignedTo}"
                );

                // Set completion report properties
                completionReport.CompletionReportId = Guid.NewGuid().ToString();
                completionReport.TaskId = taskId;
                completionReport.ProjectId = task.ProjectId;
                Console.WriteLine(
                    $"[RequestCompletion] Creating completion report for task {taskId} with project {task.ProjectId}"
                );
                completionReport.SubmittedBy = contractorId;
                completionReport.SubmittedAt = DateTime.UtcNow;
                completionReport.CompletionDate = completionReport.CompletionDate.ToUniversalTime();
                completionReport.Status = "Submitted";

                // Save the completion report
                await _firebaseService.AddDocumentWithIdAsync(
                    "completionReports",
                    completionReport.CompletionReportId,
                    completionReport
                );

                // Update task status to "Awaiting Approval"
                task.Status = "Awaiting Approval";
                await _firebaseService.UpdateDocumentAsync("tasks", taskId, task);

                // Send workflow notification to project manager
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    await _workflowMessageService.SendCompletionRequestNotificationAsync(
                        taskId,
                        currentUserId
                    );
                }

                return Ok(
                    new
                    {
                        message = "Completion request submitted successfully",
                        taskId,
                        completionReportId = completionReport.CompletionReportId,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        error = "An error occurred while submitting completion request",
                        details = ex.Message,
                    }
                );
            }
        }

        [HttpGet("task/{taskId}/completion-reports")]
        public async Task<ActionResult<List<CompletionReport>>> GetTaskCompletionReports(
            string taskId
        )
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Verify contractor is assigned to this task
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }
                if (task.AssignedTo != contractorId)
                {
                    return Forbid();
                }

                // Get all completion reports and filter in memory (avoids index requirement)
                Console.WriteLine(
                    $"[ContractorsController] Getting completion reports for task {taskId}"
                );
                var allCompletionReports =
                    await _firebaseService.GetCollectionAsync<CompletionReport>(
                        "completionReports"
                    );
                Console.WriteLine(
                    $"[ContractorsController] Found {allCompletionReports.Count} total completion reports"
                );

                var taskReports = allCompletionReports
                    .Where(r => r.TaskId == taskId)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToList();

                Console.WriteLine(
                    $"[ContractorsController] Found {taskReports.Count} completion reports for task {taskId}"
                );

                return Ok(taskReports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("task/{taskId}/project-budget")]
        public async Task<ActionResult<object>> GetTaskProjectBudget(string taskId)
        {
            try
            {
                var contractorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(contractorId))
                {
                    return Unauthorized(new { error = "Contractor ID not found" });
                }

                // Verify contractor is assigned to this task
                var task = await _firebaseService.GetDocumentAsync<ProjectTask>("tasks", taskId);
                if (task == null)
                {
                    return NotFound(new { error = "Task not found" });
                }
                if (task.AssignedTo != contractorId)
                {
                    return Forbid();
                }

                // Get project budget
                var project = await _firebaseService.GetDocumentAsync<Project>(
                    "projects",
                    task.ProjectId
                );
                if (project == null)
                {
                    return NotFound(new { error = "Project not found" });
                }

                return Ok(
                    new
                    {
                        projectId = project.ProjectId,
                        projectName = project.Name,
                        budgetPlanned = project.BudgetPlanned,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
