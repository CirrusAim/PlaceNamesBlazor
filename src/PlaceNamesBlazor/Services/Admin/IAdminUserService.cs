using PlaceNamesBlazor.Contracts.Admin;

namespace PlaceNamesBlazor.Services.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserListDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingReporterCountAsync(CancellationToken cancellationToken = default);
    Task<UserListDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserAsync(int userId, UserUpdateRequest request, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(int userId, int actorUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserRoleAsync(int userId, string role, int actorUserId, CancellationToken cancellationToken = default);
}
