using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PlaceNamesBlazor.Contracts.Auth;
using PlaceNamesBlazor.Data;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<PlaceNamesDbContext> _factory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILoginLockoutService _lockout;

    public AuthService(IDbContextFactory<PlaceNamesDbContext> factory, IHttpContextAccessor httpContextAccessor, ILoginLockoutService lockout)
    {
        _factory = factory;
        _httpContextAccessor = httpContextAccessor;
        _lockout = lockout;
    }

    public async Task<AuthResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var (user, _, _) = await ValidateLoginAsync(email, password, cancellationToken);
        if (user == null) return null;
        await SignInAsync(user);
        return ToResult(user);
    }

    public async Task<(User? User, string? Error, DateTime? LockedUntilUtc)> ValidateLoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (_lockout.IsLockedOut(normalized, out var lockedUntilUtc))
            return (null, $"Account temporarily locked. Try again after {lockedUntilUtc:HH:mm} UTC.", lockedUntilUtc);

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == normalized && u.IsActive, cancellationToken);
        if (user == null)
        {
            _lockout.RecordFailedAttempt(normalized);
            return (null, null, null);
        }
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _lockout.RecordFailedAttempt(normalized);
            return (null, null, null);
        }
        _lockout.Reset(normalized);
        user.LastLogin = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (user, null, null);
    }

    public async Task<AuthResultDto?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = await RegisterAndGetUserAsync(request, cancellationToken);
        if (user == null) return null;
        await SignInAsync(user);
        return ToResult(user);
    }

    public async Task<User?> RegisterAndGetUserAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var (pwdValid, pwdError) = PasswordPolicy.Validate(request.Password);
        if (!pwdValid)
            return null;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim();
        if (await db.Users.AnyAsync(u => u.Email == email || (u.Username != null && u.Username == username), cancellationToken))
            return null;
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            Email = email,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10),
            Role = "registered",
            Telephone = string.IsNullOrWhiteSpace(request.Telephone) ? null : request.Telephone.Trim(),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public ClaimsPrincipal CreatePrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Username ?? user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public Task SignOutAsync()
    {
        return _httpContextAccessor.HttpContext?.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme) ?? Task.CompletedTask;
    }

    public async Task<AuthResultDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive, cancellationToken);
        return user == null ? null : ToResult(user);
    }

    private async Task SignInAsync(User user)
    {
        var principal = CreatePrincipal(user);
        var props = new AuthenticationProperties { IsPersistent = true };
        if (_httpContextAccessor.HttpContext != null)
            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
    }

    private static AuthResultDto ToResult(User u) => new()
    {
        UserId = u.UserId,
        Email = u.Email,
        Role = u.Role,
        FullName = $"{u.FirstName} {u.LastName}".Trim(),
        Username = u.Username,
        Telephone = u.Telephone
    };

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, string? email, string? telephone, string? currentPassword, string? newPassword, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive, cancellationToken);
        if (user == null) return (false, "User not found");

        if (!string.IsNullOrWhiteSpace(email))
        {
            var newEmail = email.Trim().ToLowerInvariant();
            if (newEmail != user.Email)
            {
                if (await db.Users.AnyAsync(u => u.Email == newEmail && u.UserId != userId, cancellationToken))
                    return (false, "Email already taken");
                user.Email = newEmail;
            }
        }

        if (telephone != null)
            user.Telephone = string.IsNullOrWhiteSpace(telephone) ? null : telephone.Trim();

        if (!string.IsNullOrEmpty(currentPassword) || !string.IsNullOrEmpty(newPassword))
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
                return (false, "Current password and new password are required to change password");
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return (false, "Current password is incorrect");
            var (pwdValid, pwdError) = PasswordPolicy.Validate(newPassword);
            if (!pwdValid)
                return (false, pwdError ?? "New password does not meet policy.");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 10);
        }

        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
