using System.Security.Claims;
using System.Threading.Tasks;

namespace ICCMS_Web.Services
{
    public interface IApiClient
    {
        // Generic GET helper
        Task<T?> GetAsync<T>(string endpoint, ClaimsPrincipal user);
        Task<T?> PostAsync<T>(string endpoint, object data, ClaimsPrincipal user);
        Task<T?> PutAsync<T>(string endpoint, object data, ClaimsPrincipal user);

        // Circuit breaker management
        void ResetCircuitBreaker(string endpoint);
        void ResetAllCircuitBreakers();
    }
}
