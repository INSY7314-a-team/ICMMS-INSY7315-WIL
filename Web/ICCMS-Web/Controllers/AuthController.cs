using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ICCMS_Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public AuthController(
            ILogger<AuthController> logger,
            HttpClient httpClient,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;

            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
            _logger.LogInformation("AuthController initialized. API base URL: {Url}", _apiBaseUrl);
        }

        // Show login form
        [HttpGet]
        public IActionResult Login()
        {
            _logger.LogInformation("Rendering login page.");
            return View(new LoginViewModel());
        }

        // Proxy login request to API
        [HttpPost("api/auth/login")]
        public async Task<IActionResult> LoginProxy([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                _logger.LogWarning("LoginProxy: Missing credentials");
                return Json(new { success = false, message = "Email and password are required" });
            }

            try
            {
                _logger.LogInformation("LoginProxy: Forwarding login request for {Email} to API", request.Email);

                // Call API /api/auth/login
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/auth/login",
                    request
                );

                var content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("LoginProxy: API returned {Status}. Body: {Body}", response.StatusCode, content);
                    return StatusCode((int)response.StatusCode, content);
                }

                _logger.LogInformation("LoginProxy: API login successful for {Email}", request.Email);
                
                // Return the API response as-is
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoginProxy: Exception forwarding login for {Email}", request.Email);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Called by JS (Login.cshtml) after Firebase sign-in
        [HttpPost]
        public async Task<IActionResult> VerifyToken([FromBody] VerifyTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
            {
                _logger.LogWarning("VerifyToken: Missing token for {Email}", request.Email);
                return Json(new { success = false, message = "Token missing." });
            }

            try
            {
                _logger.LogInformation(
                    "VerifyToken: Sending token to API for {Email}",
                    request.Email
                );

                // Call API /api/auth/verify-token
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/auth/verify-token",
                    new { IdToken = request.IdToken }
                );

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "VerifyToken: API returned {Status}. Body: {Body}",
                        response.StatusCode,
                        body
                    );
                    return Json(new { success = false, message = "API verification failed" });
                }

                var content = await response.Content.ReadAsStringAsync();
                var verificationResponse = JsonSerializer.Deserialize<TokenVerificationResponse>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (verificationResponse == null || !verificationResponse.Success)
                {
                    _logger.LogWarning(
                        "VerifyToken: API rejected token for {Email}",
                        request.Email
                    );
                    return Json(
                        new
                        {
                            success = false,
                            message = verificationResponse?.Message ?? "Verification failed",
                        }
                    );
                }

                var user = verificationResponse.User;
                _logger.LogInformation(
                    "VerifyToken: API verified {Email} as {Role}",
                    user.Email,
                    user.Role
                );

                // Build claims (now includes role!)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId ?? request.Email ?? "unknown"),
                    new Claim(ClaimTypes.Name, user.FullName ?? user.Email ?? "User"),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "User"),
                    new Claim("FirebaseToken", request.IdToken), // Save raw token for ApiClient
                };

                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    "Cookies",
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = request.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                    }
                );

                return Json(
                    new
                    {
                        success = true,
                        user = new
                        {
                            user.FullName,
                            user.Email,
                            user.Role,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "VerifyToken: Exception while verifying {Email}",
                    request.Email
                );
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            _logger.LogInformation("User logged out.");
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning(
                "Access denied for user {User} trying to access {ReturnUrl}",
                User.Identity?.Name,
                Request.Query["ReturnUrl"]
            );

            ViewBag.ReturnUrl = Request.Query["ReturnUrl"];
            return View();
        }
    }

    // Request from Login.cshtml JS
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class VerifyTokenRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    // Mirror of API response
    public class TokenVerificationResponse
    {
        public bool Success { get; set; }
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
}
