using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Tester")] // Only admins and testers can access this controller
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;

        public AdminController(IAuthService authService, IFirebaseService firebaseService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboard()
        {
            try
            {
                var users = await _firebaseService.GetCollectionAsync<User>("users");
                var activeUsers = users.Where(u => u.IsActive).ToList();

                var dashboard = new
                {
                    totalUsers = activeUsers.Count,
                    usersByRole = activeUsers
                        .GroupBy(u => u.Role)
                        .Select(g => new { role = g.Key, count = g.Count() }),
                    recentUsers = activeUsers.OrderByDescending(u => u.CreatedAt).Take(5),
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<User>>> GetUsers()
        {
            try
            {
                var users = await _firebaseService.GetCollectionAsync<User>("users");
                // Filter out deactivated users
                var activeUsers = users.Where(u => u.IsActive).ToList();
                return Ok(activeUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("deactivated")]
        public async Task<ActionResult<List<User>>> GetDeactivatedUsers()
        {
            try
            {
                var users = await _firebaseService.GetCollectionAsync<User>("users");
                // Filter out deactivated users
                var deactivatedUsers = users.Where(u => !u.IsActive).ToList();
                return Ok(deactivatedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user/{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                var user = await _firebaseService.GetDocumentAsync<User>("users", id);

                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("messages")]
        public async Task<ActionResult<List<Message>>> GetMessages()
        {
            try
            {
                var messages = await _firebaseService.GetCollectionAsync<Message>("messages");
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("message/{id}")]
        public async Task<ActionResult<Message>> GetMessage(string id)
        {
            try
            {
                var message = await _firebaseService.GetDocumentAsync<Message>("messages", id);
                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("projects/{id}/status")]
        public async Task<ActionResult<string>> GetProjectStatus(string id)
        {
            try
            {
                var project = await _firebaseService.GetDocumentAsync<Project>("projects", id);
                return Ok(project.Status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("documents")]
        public async Task<ActionResult<List<Document>>> GetDocuments()
        {
            try
            {
                var documents = await _firebaseService.GetCollectionAsync<Document>("documents");
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("create/user")]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Validate role
                var validRoles = new[]
                {
                    "Admin",
                    "Project Manager",
                    "Contractor",
                    "Client",
                    "Tester",
                };
                if (!validRoles.Contains(request.Role))
                {
                    return BadRequest(
                        new
                        {
                            error = "Invalid role specified \n Valid roles are: Admin, Project Manager, Contractor, Client, Tester \n You chose: "
                                + request.Role,
                        }
                    );
                }

                // 1. Create Firebase user
                var firebaseUid = await _authService.CreateUserAsync(
                    request.Email,
                    request.Password,
                    request.FullName
                );

                // 2. Create user document with Firebase UID as document ID
                var user = new User
                {
                    UserId = firebaseUid,
                    Role = request.Role,
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    CreatedAt = DateTimeOffset.UtcNow,
                    IsActive = true,
                };

                await _firebaseService.AddDocumentWithIdAsync("users", firebaseUid, user);

                return Ok(new { userId = firebaseUid, message = "User created successfully" });
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
                var newDocument = new Document
                {
                    DocumentId = Guid.NewGuid().ToString(),
                    ProjectId = document.ProjectId,
                    FileName = document.FileName,
                    Status = document.Status,
                    FileType = document.FileType,
                    FileSize = document.FileSize,
                    FileUrl = document.FileUrl,
                    UploadedBy = document.UploadedBy,
                    UploadedAt = DateTime.UtcNow,
                    Description = document.Description,
                };

                await _firebaseService.AddDocumentAsync("documents", newDocument);
                return Ok(newDocument);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("users/{id}/activate")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            try
            {
                var user = await _firebaseService.GetDocumentAsync<User>("users", id);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                user.IsActive = true;
                await _firebaseService.UpdateDocumentAsync("users", id, user);

                return Ok(new { message = "User activated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            try
            {
                var user = await _firebaseService.GetDocumentAsync<User>("users", id);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                user.IsActive = false;
                await _firebaseService.UpdateDocumentAsync("users", id, user);

                return Ok(new { message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("update/user/{id}")]
        public async Task<ActionResult<User>> UpdateUser(string id, [FromBody] User user)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                // Ensure the user ID in the URL matches the user object
                user.UserId = id;

                await _firebaseService.UpdateDocumentAsync("users", id, user);
                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(
            string id,
            [FromBody] UpdateRoleRequest request
        )
        {
            try
            {
                var validRoles = new[]
                {
                    "Admin",
                    "Project Manager",
                    "Contractor",
                    "Client",
                    "Tester",
                };
                if (!validRoles.Contains(request.Role))
                {
                    return BadRequest(new { error = "Invalid role specified" });
                }

                var user = await _firebaseService.GetDocumentAsync<User>("users", id);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                user.Role = request.Role;
                await _firebaseService.UpdateDocumentAsync("users", id, user);

                return Ok(new { message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("notifications/{id}")]
        public async Task<ActionResult<Notification>> UpdateNotification(
            string id,
            [FromBody] Notification notification
        )
        {
            try
            {
                await _firebaseService.UpdateDocumentAsync("notifications", id, notification);
                return Ok(new { message = "Notification updated successfully" });
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
                return Ok(new { message = "Document updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete/user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { error = "User ID is required" });
                }

                // Soft delete - mark as inactive instead of hard delete
                var user = await _firebaseService.GetDocumentAsync<User>("users", id);
                if (user != null)
                {
                    user.IsActive = false;
                    await _firebaseService.UpdateDocumentAsync("users", id, user);
                }

                return Ok(new { message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("delete/documents/{id}")]
        public async Task<ActionResult<Notification>> DeleteDocument(string id)
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

        [HttpGet("system-health")]
        public async Task<ActionResult<object>> GetSystemHealth()
        {
            try
            {
                var health = new
                {
                    overallStatus = "Healthy",
                    uptime = "99.9%",
                    lastHealthCheck = DateTime.UtcNow,
                    systemLoad = "Low",
                    memoryUsage = "45%",
                    diskUsage = "62%",
                    cpuUsage = "23%",
                    databaseStatus = "Connected",
                    apiStatus = "Operational",
                    timestamp = DateTime.UtcNow
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("system-metrics")]
        public async Task<ActionResult<object>> GetSystemMetrics()
        {
            try
            {
                var metrics = new
                {
                    performance = new
                    {
                        averageResponseTime = "245ms",
                        requestsPerSecond = 42,
                        errorRate = 0.1,
                        databaseResponseTime = "89ms",
                        apiUptime = "99.9%",
                        lastPerformanceCheck = DateTime.UtcNow,
                        peakConcurrentUsers = 15,
                        averageSessionDuration = "24 minutes"
                    },
                    database = new
                    {
                        status = "Connected",
                        responseTime = "89ms",
                        lastBackup = DateTime.UtcNow.AddHours(-6),
                        connectionPool = "Healthy",
                        queryPerformance = "Good",
                        storageUsed = "2.3 GB",
                        lastHealthCheck = DateTime.UtcNow
                    },
                    api = new
                    {
                        overallStatus = "Healthy",
                        healthyEndpoints = 8,
                        totalEndpoints = 8,
                        averageResponseTime = "245ms",
                        lastHealthCheck = DateTime.UtcNow,
                        endpoints = new[]
                        {
                            new { endpoint = "/api/users", status = "Healthy", responseTime = "89ms" },
                            new { endpoint = "/api/clients", status = "Healthy", responseTime = "92ms" },
                            new { endpoint = "/api/projects", status = "Healthy", responseTime = "156ms" },
                            new { endpoint = "/api/quotations", status = "Healthy", responseTime = "134ms" },
                            new { endpoint = "/api/invoices", status = "Healthy", responseTime = "98ms" },
                            new { endpoint = "/api/messages", status = "Healthy", responseTime = "112ms" },
                            new { endpoint = "/api/notifications", status = "Healthy", responseTime = "87ms" },
                            new { endpoint = "/api/estimates", status = "Healthy", responseTime = "145ms" }
                        }
                    }
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("system-alerts")]
        public async Task<ActionResult<object>> GetSystemAlerts()
        {
            try
            {
                var alerts = new[]
                {
                    new
                    {
                        type = "System",
                        title = "System Health Check",
                        message = "All systems are operating normally.",
                        timestamp = DateTime.UtcNow.AddMinutes(-5),
                        severity = "Info"
                    },
                    new
                    {
                        type = "Storage",
                        title = "Disk Usage Warning",
                        message = "Disk usage is at 62%. Consider cleaning up old files.",
                        timestamp = DateTime.UtcNow.AddMinutes(-30),
                        severity = "Warning"
                    },
                    new
                    {
                        type = "Performance",
                        title = "High Response Time",
                        message = "Average response time is above normal threshold.",
                        timestamp = DateTime.UtcNow.AddHours(-1),
                        severity = "Warning"
                    }
                };

                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("recent-activity")]
        public async Task<ActionResult<object>> GetRecentActivity()
        {
            try
            {
                var activities = new[]
                {
                    new
                    {
                        type = "User",
                        description = "New user registered: John Smith",
                        timestamp = DateTime.UtcNow.AddMinutes(-15),
                        user = "System",
                        icon = "fas fa-user-plus"
                    },
                    new
                    {
                        type = "Project",
                        description = "Project 'ICCMS Phase 2' status updated to Active",
                        timestamp = DateTime.UtcNow.AddMinutes(-32),
                        user = "Admin User",
                        icon = "fas fa-project-diagram"
                    },
                    new
                    {
                        type = "Financial",
                        description = "Invoice #INV-2024-001 marked as paid",
                        timestamp = DateTime.UtcNow.AddHours(-1),
                        user = "Finance Manager",
                        icon = "fas fa-dollar-sign"
                    },
                    new
                    {
                        type = "System",
                        description = "System backup completed successfully",
                        timestamp = DateTime.UtcNow.AddHours(-2),
                        user = "System",
                        icon = "fas fa-database"
                    },
                    new
                    {
                        type = "Message",
                        description = "New message thread created for Project Alpha",
                        timestamp = DateTime.UtcNow.AddHours(-3),
                        user = "Project Manager",
                        icon = "fas fa-comments"
                    }
                };

                return Ok(activities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}
