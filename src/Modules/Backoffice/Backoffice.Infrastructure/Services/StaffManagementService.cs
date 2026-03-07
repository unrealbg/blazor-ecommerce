using Backoffice.Application.Backoffice;
using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Domain.Results;
using Customers.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backoffice.Infrastructure.Services;

internal sealed class StaffManagementService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager)
    : IStaffManagementService
{
    public async Task<IReadOnlyCollection<BackofficeStaffRoleCatalogItemDto>> GetRoleCatalogAsync(
        CancellationToken cancellationToken)
    {
        var roles = await roleManager.Roles
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var items = new List<BackofficeStaffRoleCatalogItemDto>(roles.Count);
        foreach (var role in roles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var permissions = await ResolveRolePermissionsAsync(role);
            items.Add(new BackofficeStaffRoleCatalogItemDto(role.Name!, permissions));
        }

        return items;
    }

    public async Task<BackofficeStaffPage> GetStaffAsync(
        string? query,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        var usersQuery = userManager.Users.Where(user => user.IsStaff);

        if (isActive is not null)
        {
            usersQuery = usersQuery.Where(user => user.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim().ToLowerInvariant();
            usersQuery = usersQuery.Where(user =>
                (user.Email != null && user.Email.ToLower().Contains(normalizedQuery)) ||
                (user.DisplayName != null && user.DisplayName.ToLower().Contains(normalizedQuery)) ||
                (user.Department != null && user.Department.ToLower().Contains(normalizedQuery)));
        }

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);

        var users = await usersQuery
            .OrderByDescending(user => user.LastLoginAtUtc ?? user.CreatedAtUtc)
            .ThenBy(user => user.Email)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = new List<BackofficeStaffUserSummaryDto>(users.Count);
        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roles = await userManager.GetRolesAsync(user);
            items.Add(new BackofficeStaffUserSummaryDto(
                user.Id,
                user.Email ?? user.UserName ?? string.Empty,
                user.DisplayName,
                user.Department,
                user.IsStaff,
                user.IsActive,
                user.CreatedAtUtc,
                user.LastLoginAtUtc,
                roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        return new BackofficeStaffPage(
            normalizedPage,
            normalizedPageSize,
            totalCount,
            totalPages,
            items);
    }

    public async Task<BackofficeStaffUserDetailDto?> GetStaffUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsStaff, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await ResolvePermissionsAsync(roles);

        return new BackofficeStaffUserDetailDto(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            user.DisplayName,
            user.Department,
            user.IsStaff,
            user.IsActive,
            user.CreatedAtUtc,
            user.LastLoginAtUtc,
            roles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToArray(),
            permissions);
    }

    public async Task<Result<Guid>> CreateStaffUserAsync(
        string email,
        string password,
        string? displayName,
        string? department,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Result<Guid>.Failure(new Error(
                "backoffice.staff.email.required",
                "Email is required."));
        }

        var roleValidation = await ValidateRolesAsync(roles, cancellationToken);
        if (roleValidation.IsFailure)
        {
            return Result<Guid>.Failure(roleValidation.Error);
        }

        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return Result<Guid>.Failure(new Error(
                "backoffice.staff.email.conflict",
                "A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            LockoutEnabled = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow,
            DisplayName = Normalize(displayName),
            Department = Normalize(department),
            IsStaff = true,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return Result<Guid>.Failure(new Error(
                "backoffice.staff.create.failed",
                string.Join("; ", createResult.Errors.Select(error => error.Description))));
        }

        if (roles.Count > 0)
        {
            var addToRolesResult = await userManager.AddToRolesAsync(user, roles);
            if (!addToRolesResult.Succeeded)
            {
                return Result<Guid>.Failure(new Error(
                    "backoffice.staff.roles.assign_failed",
                    string.Join("; ", addToRolesResult.Errors.Select(error => error.Description))));
            }
        }

        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result> SetStaffActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsStaff, cancellationToken);
        if (user is null)
        {
            return Result.Failure(new Error(
                "backoffice.staff.not_found",
                "Staff user was not found."));
        }

        if (!isActive && await IsProtectedAdminChangeAsync(user, BackofficeRoles.Admin))
        {
            return Result.Failure(new Error(
                "backoffice.protected_role_modification.denied",
                "The last active admin cannot be deactivated."));
        }

        user.IsActive = isActive;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result.Failure(new Error(
                "backoffice.staff.update.failed",
                string.Join("; ", updateResult.Errors.Select(error => error.Description))));
        }

        return Result.Success();
    }

    public async Task<Result> AssignRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        var roleValidation = await ValidateRolesAsync([roleName], cancellationToken);
        if (roleValidation.IsFailure)
        {
            return roleValidation;
        }

        var user = await userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsStaff, cancellationToken);
        if (user is null)
        {
            return Result.Failure(new Error(
                "backoffice.staff.not_found",
                "Staff user was not found."));
        }

        if (await userManager.IsInRoleAsync(user, roleName))
        {
            return Result.Success();
        }

        var result = await userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            return Result.Failure(new Error(
                "backoffice.staff.roles.assign_failed",
                string.Join("; ", result.Errors.Select(error => error.Description))));
        }

        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsStaff, cancellationToken);
        if (user is null)
        {
            return Result.Failure(new Error(
                "backoffice.staff.not_found",
                "Staff user was not found."));
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            return Result.Success();
        }

        if (string.Equals(roleName, BackofficeRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
            await IsProtectedAdminChangeAsync(user, roleName))
        {
            return Result.Failure(new Error(
                "backoffice.protected_role_modification.denied",
                "The last active admin role cannot be removed."));
        }

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            return Result.Failure(new Error(
                "backoffice.staff.roles.remove_failed",
                string.Join("; ", result.Errors.Select(error => error.Description))));
        }

        return Result.Success();
    }

    private async Task<Result> ValidateRolesAsync(
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        foreach (var roleName in roles.Where(roleName => !string.IsNullOrWhiteSpace(roleName)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await roleManager.FindByNameAsync(roleName) is null)
            {
                return Result.Failure(new Error(
                    "backoffice.role_assignment.not_allowed",
                    $"Role '{roleName}' is not defined."));
            }
        }

        return Result.Success();
    }

    private async Task<IReadOnlyCollection<string>> ResolvePermissionsAsync(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            foreach (var permission in await ResolveRolePermissionsAsync(role))
            {
                permissions.Add(permission);
            }
        }

        return permissions
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<string>> ResolveRolePermissionsAsync(IdentityRole<Guid> role)
    {
        var claims = await roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(claim => string.Equals(
                claim.Type,
                BackofficeClaimTypes.Permission,
                StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (permissions.Length == 0 &&
            role.Name is not null &&
            BackofficePermissionCatalog.DefaultRolePermissions.TryGetValue(role.Name, out var defaultPermissions))
        {
            return defaultPermissions
                .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return permissions;
    }

    private async Task<bool> IsProtectedAdminChangeAsync(ApplicationUser targetUser, string roleName)
    {
        if (!string.Equals(roleName, BackofficeRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!await userManager.IsInRoleAsync(targetUser, BackofficeRoles.Admin))
        {
            return false;
        }

        var activeAdmins = await userManager.GetUsersInRoleAsync(BackofficeRoles.Admin);
        var protectedCount = activeAdmins.Count(user => user.IsStaff && user.IsActive);

        return protectedCount <= 1 && activeAdmins.Any(user => user.Id == targetUser.Id);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
