using System.Security.Claims;

namespace ICCMS_Web.Services
{
    public interface ICurrentUserService
    {
        ClaimsPrincipal GetCurrentUser();
        string GetCurrentUserId();
        string GetFirebaseToken();
    }
}
