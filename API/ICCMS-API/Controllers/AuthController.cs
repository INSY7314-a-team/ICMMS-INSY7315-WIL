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

        public AuthController(IAuthService authService, IFirebaseService firebaseService, IAuditLogService auditLogService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
            _auditLogService = auditLogService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            Console.WriteLine($"[AuthController] LOGIN METHOD CALLED");
            try
            {
                Console.WriteLine($"[AuthController] ===== LOGIN REQUEST RECEIVED for {request.Email} =====");
                
                // Authenticate with Firebase using email and password
                var firebaseUser = await _authService.SignInWithEmailAndPasswordAsync(
                    request.Email,
                    request.Password
                );
                
                Console.WriteLine($"[AuthController] Firebase authentication completed. User: {(firebaseUser?.Uid ?? "NULL")}");

                if (firebaseUser == null)
                {
                    await _auditLogService.LogAsync("Login Attempt", "Login Failed", $"Failed login for {request.Email} - Invalid credentials", "unknown", request.Email);
                    return BadRequest(
                        new LoginResponse { Success = false, Message = "Invalid email or password" }
                    );
                }

                // Get user data from Firestore
                var user = await _firebaseService.GetDocumentAsync<Models.User>(
                    "users",
                    firebaseUser.Uid
                );

                if (user == null || !user.IsActive)
                {
                    var failureReason = user == null ? "Account not found" : "Account deactivated";
                    await _auditLogService.LogAsync("Login Attempt", "Login Failed", $"Failed login for {request.Email} - {failureReason}", firebaseUser?.Uid ?? "unknown", request.Email);
                    return BadRequest(
                        new LoginResponse
                        {
                            Success = false,
                            Message = "User not found or account deactivated",
                        }
                    );
                }

                // Generate a Firebase ID token for the user
                var idToken = await _authService.CreateIdTokenAsync(firebaseUser.Uid);

                Console.WriteLine($"[AuthController] About to log successful login for {user.Email}");
                Console.WriteLine($"[AuthController] _auditLogService is null: {_auditLogService == null}");
                if (_auditLogService != null)
                {
                    try
                    {
                        Console.WriteLine($"[AuthController] CALLING LogAsync NOW");
                        await _auditLogService.LogAsync("Login Attempt", "Login Successful", $"Successful login for {user.Email} (Role: {user.Role})", firebaseUser.Uid, firebaseUser.Uid);
                        Console.WriteLine($"[AuthController] Login audit log call completed");
                    }
                    catch (Exception auditEx)
                    {
                        Console.WriteLine($"[AuthController] EXCEPTION during audit log: {auditEx.Message}");
                        Console.WriteLine($"[AuthController] Audit exception stack: {auditEx.StackTrace}");
                    }
                }
                else
                {
                    Console.WriteLine($"[AuthController] ERROR: _auditLogService is NULL!");
                }

                return Ok(
                    new LoginResponse
                    {
                        Success = true,
                        Token = idToken,
                        Message = "Login successful",
                        User = new UserInfo
                        {
                            UserId = firebaseUser.Uid, // Use Firebase UID directly instead of user.UserId
                            Email = user.Email,
                            FullName = user.FullName,
                            Role = user.Role,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error details: {ex}");
                return StatusCode(
                    500,
                    new LoginResponse { Success = false, Message = $"Login error: {ex.Message}" }
                );
            }
        }

        [HttpPost("verify-token")]
        public async Task<ActionResult<TokenVerificationResponse>> VerifyToken(
            [FromBody] TokenVerificationRequest request
        )
        {
            try
            {
                // Verify the Firebase ID token
                var firebaseToken = await _authService.VerifyTokenAsync(request.IdToken);

                // Get user data from Firestore
                var user = await _firebaseService.GetDocumentAsync<Models.User>(
                    "users",
                    firebaseToken.Uid
                );

                if (user == null || !user.IsActive)
                {
                    return BadRequest(
                        new TokenVerificationResponse
                        {
                            Success = false,
                            Message = "User not found or account deactivated",
                        }
                    );
                }

                return Ok(
                    new TokenVerificationResponse
                    {
                        Success = true,
                        Message = "Token verified successfully",
                        User = new UserInfo
                        {
                            UserId = firebaseToken.Uid, // Use Firebase UID directly instead of user.UserId
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
