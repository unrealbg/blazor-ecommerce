using System.Security.Claims;
using BuildingBlocks.Application.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Customers.Infrastructure.Identity;

internal sealed class BackofficePermissionService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
    : IBackofficePermissionService
{
    public async Task<BackofficePermissionSnapshot?> GetCurrentStaffSnapshotAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var userId = TryGetUserId(principal);
        if (userId is null)
        {
            return null;
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null || !user.HasStaffAccess())
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await ResolvePermissionsAsync(roles, cancellationToken);

        return new BackofficePermissionSnapshot(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            user.DisplayName,
            user.Department,
            user.IsStaff,
            user.IsActive(),
            user.LastLoginAtUtc,
            roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToArray(),
            permissions);
    }

    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string permission,
        CancellationToken cancellationToken)
    {
        if (!BackofficePermissionCatalog.IsKnownPermission(permission))
        {
            return false;
        }

        var snapshot = await GetCurrentStaffSnapshotAsync(principal, cancellationToken);
        return snapshot is not null &&
               snapshot.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyCollection<string>> ResolvePermissionsAsync(
        IEnumerable<string> roles,
        CancellationToken cancellationToken)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await roleManager.GetClaimsAsync(role);
            var permissionClaims = claims
                .Where(claim => string.Equals(
                    claim.Type,
                    BackofficeClaimTypes.Permission,
                    StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            if (permissionClaims.Length == 0 &&
                BackofficePermissionCatalog.DefaultRolePermissions.TryGetValue(roleName, out var defaultPermissions))
            {
                permissionClaims = defaultPermissions.ToArray();
            }

            foreach (var permission in permissionClaims)
            {
                permissions.Add(permission);
            }
        }

        return permissions
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
