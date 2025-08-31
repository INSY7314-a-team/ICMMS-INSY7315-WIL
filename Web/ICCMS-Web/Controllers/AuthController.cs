using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public AuthController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyToken([FromBody] TokenVerificationRequest request)
        {
            try
            {
                // Send the Firebase ID token to your API for verification
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/auth/verify-token",
                    new { idToken = request.IdToken }
                );

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TokenVerificationResponse>(
                        responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (result?.Success == true)
                    {
                        // Create ASP.NET Identity claims
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, result.User.UserId),
                            new Claim(ClaimTypes.Name, result.User.FullName),
                            new Claim(ClaimTypes.Email, result.User.Email),
                            new Claim(ClaimTypes.Role, result.User.Role),
                            new Claim("FirebaseToken", request.IdToken), // Store the actual ID token
                        };

                        var identity = new ClaimsIdentity(claims, "Cookies");
                        var principal = new ClaimsPrincipal(identity);

                        // Sign in the user
                        await HttpContext.SignInAsync(
                            "Cookies",
                            principal,
                            new AuthenticationProperties
                            {
                                IsPersistent = request.RememberMe,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                            }
                        );

                        // Log successful login
                        await LogSuccessfulLoginAsync(result.User.Email, result.User.UserId, request.IdToken);

                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(
                            new
                            {
                                success = false,
                                message = result?.Message ?? "Verification failed",
                            }
                        );
                    }
                }
                else
                {
                    return Json(new { success = false, message = "API verification failed" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        private async Task LogSuccessfulLoginAsync(string email, string userId, string firebaseToken)
        {
            try
            {
                var auditLogRequest = new
                {
                    LogType = "Login Attempt",
                    Title = "Login Successful",
                    Description = "Login successful",
                    UserId = userId,
                    EntityId = email
                };

                // Clear any existing headers to avoid conflicts
                _httpClient.DefaultRequestHeaders.Clear();
                
                // Add authentication header
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);
                
                var json = JsonSerializer.Serialize(auditLogRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Debug logging
                Console.WriteLine($"Audit Log Request: {json}");
                Console.WriteLine($"Audit Log URL: {_apiBaseUrl}/api/auditlogs");
                Console.WriteLine($"Auth Header: Bearer {firebaseToken.Substring(0, Math.Min(20, firebaseToken.Length))}...");

                // Send to audit logs API
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/auditlogs", content);
                
                // Debug logging
                Console.WriteLine($"Audit Log Response: {response.StatusCode}");
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Audit Log Error: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Audit Log Exception: {ex.Message}");
                Console.WriteLine($"Audit Log Stack Trace: {ex.StackTrace}");
            }
        }
    }

    public class TokenVerificationRequest
    {
        public string IdToken { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

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
