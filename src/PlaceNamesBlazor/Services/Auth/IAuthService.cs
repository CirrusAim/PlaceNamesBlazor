using System.Security.Claims;
using PlaceNamesBlazor.Contracts.Auth;
using PlaceNamesBlazor.Data.Entities;

namespace PlaceNamesBlazor.Services.Auth;

public interface IAuthService
{
    Task<AuthResultDto?> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthResultDto?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task SignOutAsync();
    Task<AuthResultDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<(User? User, string? Error, DateTime? LockedUntilUtc)> ValidateLoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<User?> RegisterAndGetUserAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    ClaimsPrincipal CreatePrincipal(User user);
    Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, string? email, string? telephone, string? currentPassword, string? newPassword, CancellationToken cancellationToken = default);
}
