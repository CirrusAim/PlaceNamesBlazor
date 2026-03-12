namespace PlaceNamesBlazor.Services.Auth;

public interface ILoginLockoutService
{
    bool IsLockedOut(string email, out DateTime lockedUntilUtc);
    void RecordFailedAttempt(string email);
    void Reset(string email);
}
