using System.Text.Json;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // No authorization required - this is for login/authentication
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;
        private readonly IAuditLogService _auditLogService;

        public AuthController(
            IAuthService authService,
            IFirebaseService firebaseService,
            IAuditLogService auditLogService
        )
        {
            _authService = authService;
            _firebaseService = firebaseService;
            _auditLogService = auditLogService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            Console.WriteLine($"[AuthController] LOGIN METHOD CALLED - DEPRECATED");
            Console.WriteLine(
                $"[AuthController] This endpoint does not verify passwords and should not be used"
            );
            Console.WriteLine(
                $"[AuthController] Use Firebase Client SDK for authentication instead"
            );

            return BadRequest(
                new LoginResponse
                {
                    Success = false,
                    Message =
                        "This endpoint cannot verify passwords. Please use Firebase Client SDK for authentication.",
                }
            );
        }

        [HttpPost("verify-token")]
        public async Task<ActionResult<TokenVerificationResponse>> VerifyToken(
            [FromBody] TokenVerificationRequest request
        )
        {
            string? userId = null;
            string? userEmail = null;

            try
            {
                // Verify the Firebase ID token
                var firebaseToken = await _authService.VerifyTokenAsync(request.IdToken);
                userId = firebaseToken.Uid;

                // Get user data from Firestore
                var user = await _firebaseService.GetDocumentAsync<Models.User>(
                    "users",
                    firebaseToken.Uid
                );

                if (user == null || !user.IsActive)
                {
                    var failureReason = user == null ? "Account not found" : "Account deactivated";
                    await _auditLogService.LogAsync(
                        "Login Attempt",
                        "Login Failed",
                        $"Failed login for {userEmail ?? "unknown user"} - {failureReason}",
                        userId ?? "unknown",
                        userId ?? "unknown"
                    );

                    return BadRequest(
                        new TokenVerificationResponse
                        {
                            Success = false,
                            Message = "User not found or account deactivated",
                        }
                    );
                }

                userEmail = user.Email;

                // Log successful login
                await _auditLogService.LogAsync(
                    "Login Attempt",
                    "Login Successful",
                    $"Successful login for {user.Email} (Role: {user.Role})",
                    userId,
                    userId
                );

                return Ok(
                    new TokenVerificationResponse
                    {
                        Success = true,
                        Message = "Token verified successfully",
                        User = new UserInfo
                        {
                            UserId = firebaseToken.Uid,
                            Email = user.Email,
                            FullName = user.FullName,
                            Role = user.Role,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token verification error: {ex}");

                // Log failed login attempt
                await _auditLogService.LogAsync(
                    "Login Attempt",
                    "Login Failed",
                    $"Failed login for {userEmail ?? "unknown user"} - Token verification error: {ex.Message}",
                    userId ?? "unknown",
                    userId ?? "unknown"
                );

                return BadRequest(
                    new TokenVerificationResponse
                    {
                        Success = false,
                        Message = $"Token verification failed: {ex.Message}",
                    }
                );
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetProfile()
        {
            try
            {
                var userId = HttpContext.Items["UserId"] as string;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _firebaseService.GetDocumentAsync<Models.User>("users", userId);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(
                    new UserInfo
                    {
                        UserId = userId, // Use the userId from HttpContext instead of user.UserId
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role,
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public UserInfo User { get; set; } = new();
    }

    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class TokenVerificationRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }

    public class TokenVerificationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserInfo User { get; set; } = new();
    }
}
