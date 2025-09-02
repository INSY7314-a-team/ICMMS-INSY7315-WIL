using System.Text.Json;
using ICCMS_API.Models;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFirebaseService _firebaseService;

        public AuthController(IAuthService authService, IFirebaseService firebaseService)
        {
            _authService = authService;
            _firebaseService = firebaseService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Authenticate with Firebase using email and password
                var firebaseUser = await _authService.SignInWithEmailAndPasswordAsync(
                    request.Email,
                    request.Password
                );

                if (firebaseUser == null)
                {
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

                return Ok(
                    new LoginResponse
                    {
                        Success = true,
                        Token = idToken,
                        Message = "Login successful",
                        User = new UserInfo
                        {
                            UserId = user.UserId,
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

                Console.WriteLine($"DEBUG: User retrieved from Firestore - Role: '{user.Role}', Length: {user.Role?.Length}");
                Console.WriteLine($"DEBUG: User data: UserId={user.UserId}, Email={user.Email}, FullName={user.FullName}, Role={user.Role}");

                return Ok(
                    new TokenVerificationResponse
                    {
                        Success = true,
                        Message = "Token verified successfully",
                        User = new UserInfo
                        {
                            UserId = user.UserId,
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
                        UserId = user.UserId,
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
