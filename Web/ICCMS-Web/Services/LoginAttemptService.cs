using System.Collections.Concurrent;

namespace ICCMS_Web.Services
{
    public interface ILoginAttemptService
    {
        bool IsAccountLocked(string email);
        void RecordFailedAttempt(string email);
        void RecordSuccessfulAttempt(string email);
        DateTime? GetLockoutEndTime(string email);
        int GetRemainingAttempts(string email);
    }

    public class LoginAttemptService : ILoginAttemptService
    {
        private readonly ConcurrentDictionary<string, LoginAttemptInfo> _attempts = new();
        private const int MaxAttempts = 4;
        private const int LockoutMinutes = 5;

        public bool IsAccountLocked(string email)
        {
            if (!_attempts.TryGetValue(email, out var info))
                return false;

            if (info.LockoutEndTime.HasValue && DateTime.UtcNow < info.LockoutEndTime.Value)
                return true;

            // Clear lockout if expired
            if (info.LockoutEndTime.HasValue && DateTime.UtcNow >= info.LockoutEndTime.Value)
            {
                _attempts.TryRemove(email, out _);
                return false;
            }

            return false;
        }

        public void RecordFailedAttempt(string email)
        {
            var info = _attempts.GetOrAdd(email, _ => new LoginAttemptInfo());
            info.FailedAttempts++;
            info.LastAttemptTime = DateTime.UtcNow;

            if (info.FailedAttempts >= MaxAttempts)
            {
                info.LockoutEndTime = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            }
        }

        public void RecordSuccessfulAttempt(string email)
        {
            _attempts.TryRemove(email, out _);
        }

        public DateTime? GetLockoutEndTime(string email)
        {
            return _attempts.TryGetValue(email, out var info) ? info.LockoutEndTime : null;
        }

        public int GetRemainingAttempts(string email)
        {
            if (!_attempts.TryGetValue(email, out var info))
                return MaxAttempts;

            return Math.Max(0, MaxAttempts - info.FailedAttempts);
        }

        private class LoginAttemptInfo
        {
            public int FailedAttempts { get; set; }
            public DateTime LastAttemptTime { get; set; }
            public DateTime? LockoutEndTime { get; set; }
        }
    }
}
