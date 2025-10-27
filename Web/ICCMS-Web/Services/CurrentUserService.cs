using System.Security.Claims;

namespace ICCMS_Web.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CurrentUserService> _logger;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CurrentUserService> logger
        )
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public ClaimsPrincipal GetCurrentUser()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null)
            {
                _logger.LogWarning("HttpContext or User is null - user is not authenticated");
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            if (!user.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("User is not authenticated - Identity.IsAuthenticated is false");
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            return user;
        }

        public string GetCurrentUserId()
        {
            var user = GetCurrentUser();
            var userId =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("user_id")?.Value
                ?? user.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("No user ID found in claims");
                throw new UnauthorizedAccessException("User ID not found in claims");
            }

            return userId;
        }

        public string GetFirebaseToken()
        {
            var user = GetCurrentUser();
            var token = user.FindFirst("FirebaseToken")?.Value;

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No FirebaseToken found in claims");
                throw new UnauthorizedAccessException("Firebase token not found in claims");
            }

            return token;
        }
    }
}
