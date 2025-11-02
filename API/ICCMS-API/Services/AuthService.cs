using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;

namespace ICCMS_API.Services
{
    public interface IAuthService
    {
        Task<FirebaseToken> VerifyTokenAsync(string idToken);
        Task<UserRecord> GetUserAsync(string uid);
        Task<string> CreateUserAsync(string email, string password, string displayName);
        
        // WARNING: This method cannot verify passwords. Firebase Admin SDK does not support password verification.
        // Password verification must be done using Firebase Client SDK (JavaScript/web, Android, iOS).
        // This method will throw InvalidOperationException to prevent misuse.
        [Obsolete("Cannot verify passwords with Firebase Admin SDK. Use Firebase Client SDK for authentication.")]
        Task<UserRecord> SignInWithEmailAndPasswordAsync(string email, string password);
        
        Task<bool> DeleteUserAsync(string uid);
        Task<bool> UpdateUserAsync(string uid, string displayName, string email);

        // Add the missing method to the interface
        Task<UserRecord> GetUserByEmailAsync(string email);
        Task<string> CreateCustomTokenAsync(string uid);
        Task<string> CreateIdTokenAsync(string uid);
    }

    public class AuthService : IAuthService
    {
        private readonly FirebaseAuth _auth;

        public AuthService()
        {
            _auth =
                FirebaseAuth.DefaultInstance
                ?? throw new InvalidOperationException("Firebase Admin SDK not initialized");
        }

        public async Task<FirebaseToken> VerifyTokenAsync(string idToken)
        {
            if (_auth == null)
            {
                throw new InvalidOperationException("Firebase Auth not initialized");
            }
            return await _auth.VerifyIdTokenAsync(idToken);
        }

        public async Task<UserRecord> GetUserAsync(string uid)
        {
            if (_auth == null)
            {
                throw new InvalidOperationException("Firebase Auth not initialized");
            }
            return await _auth.GetUserAsync(uid);
        }

        public async Task<string> CreateUserAsync(string email, string password, string displayName)
        {
            if (_auth == null)
            {
                throw new InvalidOperationException("Firebase Auth not initialized");
            }

            var userArgs = new UserRecordArgs
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
            };
            var userRecord = await _auth.CreateUserAsync(userArgs);
            return userRecord.Uid;
        }

        public async Task<UserRecord> GetUserByEmailAsync(string email)
        {
            if (_auth == null)
            {
                throw new InvalidOperationException("Firebase Auth not initialized");
            }

            try
            {
                return await _auth.GetUserByEmailAsync(email);
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
            {
                return null;
            }
        }

        public async Task<UserRecord> SignInWithEmailAndPasswordAsync(string email, string password)
        {
            // Firebase Admin SDK cannot verify passwords directly
            // This method should not be used for password verification
            // Password verification must be done using Firebase Client SDK
            // This implementation is kept for backward compatibility but should not be relied upon
            throw new InvalidOperationException("SignInWithEmailAndPasswordAsync cannot verify passwords. Use Firebase Client SDK for authentication.");
        }

        public async Task<bool> DeleteUserAsync(string uid)
        {
            if (_auth == null)
            {
                throw new InvalidOperationException("Firebase Auth not initialized");
            }

            try
            {
                await _auth.DeleteUserAsync(uid);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(string uid, string displayName, string email)
        {
            if (_auth == null)
            {
                throw new InvalidOperationException("Firebase Auth not initialized");
            }

            try
            {
                var args = new UserRecordArgs
                {
                    Uid = uid,
                    DisplayName = displayName,
                    Email = email,
                };
                await _auth.UpdateUserAsync(args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> CreateCustomTokenAsync(string uid)
        {
            try
            {
                var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid);
                return customToken;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create custom token: {ex.Message}");
            }
        }

        public async Task<string> CreateIdTokenAsync(string uid)
        {
            try
            {
                // Firebase Admin SDK cannot directly create ID tokens for users
                // Instead, we create a custom token that the client should exchange for an ID token
                // However, for the Web app to use it, we need to provide the custom token
                // which the Web app will exchange client-side
                
                var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid);
                
                // Return the custom token - the client should exchange this for an ID token
                // using the Firebase client SDK before sending it back to verify-token
                return customToken;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create token: {ex.Message}");
            }
        }
    }
}
