using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    /*
    Key CRUD Tasks:
        Create: User accounts [Done], role permissions [Done], system configurations.
        Read: All communication logs [Done], documents [Done], project statuses [Done].
        Update: User permissions [Done], notifications [Done], system settings.
        Delete: Remove outdated records [Done], deactivated users [Done], or invalid documents [Done].
    */
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Only admins can access this controller
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
                var validRoles = new[] { "Admin", "Project Manager", "Contractor", "Client" };
                if (!validRoles.Contains(request.Role))
                {
                    return BadRequest(
                        new
                        {
                            error = "Invalid role specified \n Valid roles are: Admin, Project Manager, Contractor, Client \n You chose: "
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
                var validRoles = new[] { "Admin", "ProjectManager", "Contractor", "Client" };
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
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }
}
