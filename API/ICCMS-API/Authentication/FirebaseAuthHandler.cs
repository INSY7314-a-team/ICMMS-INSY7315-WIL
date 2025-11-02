using System.Security.Claims;
using System.Text.Encodings.Web;
using ICCMS_API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ICCMS_API.Authentication
{
    public class FirebaseAuthHandler : AuthenticationHandler<FirebaseAuthSchemeOptions>
    {
        public FirebaseAuthHandler(
            IOptionsMonitor<FirebaseAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder
        )
            : base(options, logger, encoder) { }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Authorization header was not found.");

            string? bearerToken = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(bearerToken) || !bearerToken.StartsWith("Bearer "))
                return AuthenticateResult.Fail("Invalid token format.");

            var token = bearerToken.Substring("Bearer ".Length);

            try
            {
                // Get services from the request scope
                var authService = Context.RequestServices.GetRequiredService<IAuthService>();
                var firebaseService =
                    Context.RequestServices.GetRequiredService<IFirebaseService>();

                Console.WriteLine("Attempting to verify Firebase token...");
                // Verify Firebase token
                var firebaseToken = await authService.VerifyTokenAsync(token);
                Console.WriteLine($"Token verified successfully for UID: {firebaseToken.Uid}");

                // Get user data from Firestore
                var user = await firebaseService.GetDocumentAsync<Models.User>(
                    "users",
                    firebaseToken.Uid
                );

                if (user != null)
                {
                    // Create claims for authorization
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role),
                    };

                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }
                else
                {
                    return AuthenticateResult.Fail("User not found in database");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication failed: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return AuthenticateResult.Fail($"Authentication failed: {ex.Message}");
            }
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            return Task.CompletedTask;
        }
    }
}
