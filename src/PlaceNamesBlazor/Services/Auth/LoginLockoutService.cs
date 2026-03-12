using Microsoft.Extensions.Caching.Memory;

namespace PlaceNamesBlazor.Services.Auth;

public class LoginLockoutService : ILoginLockoutService
{
    private const string KeyPrefix = "lockout:";
    private readonly IMemoryCache _cache;
    private readonly int _maxFailedAttempts;
    private readonly int _lockoutDurationMinutes;

    public LoginLockoutService(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _maxFailedAttempts = configuration.GetValue("Lockout:MaxFailedAttempts", 5);
        _lockoutDurationMinutes = configuration.GetValue("Lockout:LockoutDurationMinutes", 15);
    }

    public bool IsLockedOut(string email, out DateTime lockedUntilUtc)
    {
        lockedUntilUtc = default;
        var key = KeyPrefix + email.Trim().ToLowerInvariant();
        if (_cache.TryGetValue(key, out LockoutEntry? entry) && entry != null && entry.LockedUntilUtc > DateTime.UtcNow)
        {
            lockedUntilUtc = entry.LockedUntilUtc;
            return true;
        }
        return false;
    }

    public void RecordFailedAttempt(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var key = KeyPrefix + normalized;
        var now = DateTime.UtcNow;
        var entry = _cache.GetOrCreate(key, ce =>
        {
            ce.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_lockoutDurationMinutes + 1);
            return new LockoutEntry { FailedCount = 0, FirstAttemptUtc = now };
        });
        if (entry == null) return;
        lock (entry)
        {
            if (entry.LockedUntilUtc > now)
                return;
            entry.FailedCount++;
            if (entry.FailedCount >= _maxFailedAttempts)
            {
                entry.LockedUntilUtc = now.AddMinutes(_lockoutDurationMinutes);
                _cache.Set(key, entry, TimeSpan.FromMinutes(_lockoutDurationMinutes + 1));
            }
            else
            {
                _cache.Set(key, entry, TimeSpan.FromMinutes(_lockoutDurationMinutes + 1));
            }
        }
    }

    public void Reset(string email)
    {
        _cache.Remove(KeyPrefix + email.Trim().ToLowerInvariant());
    }

    private sealed class LockoutEntry
    {
        public int FailedCount { get; set; }
        public DateTime FirstAttemptUtc { get; set; }
        public DateTime LockedUntilUtc { get; set; }
    }
}
