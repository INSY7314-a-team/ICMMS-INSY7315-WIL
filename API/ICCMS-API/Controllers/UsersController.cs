using System.Text.Json;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Project Manager,Client,Contractor,Tester")] // All authenticated users can access their profile
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;

        public UsersController(IAuthService authService, IFirebaseService firebaseService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
        }

        // Add endpoint for users to get their own profile
        [HttpGet("profile")]
        public async Task<ActionResult<User>> GetProfile()
        {
            try
            {
                var userId = HttpContext.Items["UserId"] as string;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _firebaseService.GetDocumentAsync<User>("users", userId);
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

        // Add endpoint for users to update their device token for push notifications
        [HttpPut("device-token")]
        public async Task<IActionResult> UpdateDeviceToken(
            [FromBody] UpdateDeviceTokenRequest request
        )
        {
            try
            {
                var userId = HttpContext.Items["UserId"] as string;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                if (string.IsNullOrEmpty(request.DeviceToken))
                {
                    return BadRequest(new { error = "Device token is required" });
                }

                var user = await _firebaseService.GetDocumentAsync<User>("users", userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Update the device token
                user.DeviceToken = request.DeviceToken;
                await _firebaseService.UpdateDocumentAsync("users", userId, user);

                return Ok(new { message = "Device token updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("clients")]
        public async Task<ActionResult<List<User>>> GetClients()
        {
            try
            {
                var clients = await _firebaseService.GetCollectionAsync<User>("users");
                var activeClients = clients.Where(u => u.IsActive && u.Role == "Client").ToList();
                return Ok(activeClients);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        
        // ✅ Get a single user by ID (works for any role)
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                    return BadRequest(new { error = "Missing user ID" });

                var user = await _firebaseService.GetDocumentAsync<User>("users", id);
                if (user == null)
                    return NotFound(new { error = $"User not found for ID {id}" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpGet("contractors")]
        public async Task<ActionResult<List<User>>> GetContractors()
        {
            try
            {
                var contractors = await _firebaseService.GetCollectionAsync<User>("users");
                var activeContractors = contractors
                    .Where(u => u.IsActive && u.Role == "Contractor")
                    .ToList();
                return Ok(activeContractors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}

public class UpdateDeviceTokenRequest
{
    public string DeviceToken { get; set; } = string.Empty;
}
