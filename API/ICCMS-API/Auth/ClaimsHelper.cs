using System.Security.Claims;

namespace ICCMS_API.Auth
{
    public static class ClaimsHelper
    {
        public static string? UserId(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("user_id")?.Value
            ?? user.FindFirst("uid")?.Value
            ?? user.FindFirst("sub")?.Value;

        public static string? Role(this ClaimsPrincipal user) =>
            user.FindFirst(ClaimTypes.Role)?.Value
            ?? user.FindFirst("role")?.Value;
    }
}
