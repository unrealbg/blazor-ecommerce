using System.Security.Claims;

namespace BuildingBlocks.Application.Authorization;

public interface IBackofficePermissionService
{
    Task<BackofficePermissionSnapshot?> GetCurrentStaffSnapshotAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string permission,
        CancellationToken cancellationToken);
}
