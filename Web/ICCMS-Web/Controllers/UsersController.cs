using System.Security.Claims;
using System.Text.Json;
using ICCMS_Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ICCMS_Web.Controllers
{
    [Authorize(Roles = "Admin")] // Only admins can access this controller
    public class UsersController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiBaseUrl;

        public UsersController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7136";
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                Console.WriteLine($"Firebase Token: {firebaseToken}");

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/admin/users");
                var responseDeactivated = await _httpClient.GetAsync(
                    $"{_apiBaseUrl}/api/admin/deactivated"
                );
                if (response.IsSuccessStatusCode && responseDeactivated.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<UserDto>>(
                        responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    var responseBodyDeactivated =
                        await responseDeactivated.Content.ReadAsStringAsync();
                    var usersDeactivated = JsonSerializer.Deserialize<List<UserDto>>(
                        responseBodyDeactivated,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    var allUsers = users.Concat(usersDeactivated).ToList();
                    return View(allUsers);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to fetch users from the server.";
                    return View(new List<UserDto>());
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return View(new List<UserDto>());
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            Console.WriteLine("ModelState.IsValid:");

            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var createUserRequest = new
                {
                    Email = model.Email,
                    Password = model.Password,
                    FullName = model.FullName,
                    Role = model.Role,
                    Phone = model.Phone ?? string.Empty,
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/admin/create/user",
                    createUserRequest
                );

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Failed to create user: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/admin/user/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<UserDto>(
                        responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    // Convert to User
                    var editModel = new UserDto
                    {
                        UserId = user.UserId,
                        Email = user.Email,
                        FullName = user.FullName,
                        Phone = user.Phone,
                        Role = user.Role,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                    };

                    return View(editModel);
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to fetch user details.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost("update/user/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConfirmed(string id, UserDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var updateUserRequest = new
                {
                    UserId = model.UserId,
                    Email = model.Email,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    Role = model.Role,
                    IsActive = model.IsActive,
                    CreatedAt = model.CreatedAt.ToUniversalTime(),
                };

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiBaseUrl}/api/admin/update/user/{id}",
                    updateUserRequest
                );

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Failed to update user: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.DeleteAsync(
                    $"{_apiBaseUrl}/api/admin/delete/user/{userId}"
                );

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User deleted successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Failed to delete user: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string userId, string newRole)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var updateRoleRequest = new { Role = newRole };

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiBaseUrl}/api/admin//user/{userId}/role",
                    updateRoleRequest
                );

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User role updated successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Failed to update role: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string userId)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/api/admin/users/{userId}/activate",
                    null
                );

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User activated successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Failed to activate user: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string userId)
        {
            try
            {
                var firebaseToken = User.FindFirst("FirebaseToken")?.Value;
                if (string.IsNullOrEmpty(firebaseToken))
                {
                    return RedirectToAction("Login", "Auth");
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firebaseToken);

                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/api/admin/users/{userId}/deactivate",
                    null
                );

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User deactivated successfully!";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Failed to deactivate user: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
