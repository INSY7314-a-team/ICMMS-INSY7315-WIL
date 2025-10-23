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
    [Authorize(Roles = "Contractor,Tester")]
    public class ContractorsController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;

        public ContractorsController(IAuthService authService, IFirebaseService firebaseService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
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
        public async Task<ActionResult<PaginatedResponse<ProjectTask>>> GetAssignedTasks(
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

                var response = new PaginatedResponse<ProjectTask>
                {
                    Data = tasks,
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

                // Filter progress reports at database level
                var filters = new Dictionary<string, object> { { "taskid", taskId } };
                var taskReports =
                    await _firebaseService.GetCollectionWithFiltersAsync<ProgressReport>(
                        "progressReports",
                        filters,
                        1,
                        1000, // Get all reports for this task
                        "SubmittedAt",
                        true // Order by SubmittedAt descending
                    );

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

                // Set report properties
                report.ProgressReportId = Guid.NewGuid().ToString();
                report.TaskId = taskId;
                report.ProjectId = task.ProjectId;
                report.SubmittedBy = contractorId;
                report.SubmittedAt = DateTime.UtcNow;
                report.Status = "Submitted";

                await _firebaseService.AddDocumentWithIdAsync(
                    "progressReports",
                    report.ProgressReportId,
                    report
                );

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
            [FromBody] object requestData
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

                // Update task status to "Awaiting Approval"
                task.Status = "Awaiting Approval";
                await _firebaseService.UpdateDocumentAsync("tasks", taskId, task);

                return Ok(new { message = "Completion request submitted successfully", taskId });
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
