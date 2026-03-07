using System.Security.Claims;
using BuildingBlocks.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Customers.Infrastructure.Identity;

internal sealed class BackofficeIdentitySeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<BackofficeSeedOptions> options,
    IHostEnvironment hostEnvironment,
    ILogger<BackofficeIdentitySeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var roleDefinition in BackofficePermissionCatalog.DefaultRolePermissions)
        {
            await EnsureRoleAsync(roleDefinition.Key, roleDefinition.Value, cancellationToken);
        }

        await EnsureDefaultAdminAsync(cancellationToken);
    }

    private async Task EnsureRoleAsync(
        string roleName,
        IReadOnlyCollection<string> expectedPermissions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new IdentityRole<Guid>(roleName)
            {
                NormalizedName = roleName.ToUpperInvariant(),
            };

            var createResult = await roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"Unable to seed role '{roleName}'.");
            }
        }

        var currentPermissionClaims = (await roleManager.GetClaimsAsync(role))
            .Where(claim => string.Equals(
                claim.Type,
                BackofficeClaimTypes.Permission,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var currentPermissions = currentPermissionClaims
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in currentPermissionClaims
                     .Where(claim => !expectedPermissions.Contains(claim.Value, StringComparer.OrdinalIgnoreCase)))
        {
            await roleManager.RemoveClaimAsync(role, permission);
        }

        foreach (var permission in expectedPermissions.Where(permission => !currentPermissions.Contains(permission)))
        {
            await roleManager.AddClaimAsync(role, new Claim(BackofficeClaimTypes.Permission, permission));
        }
    }

    private async Task EnsureDefaultAdminAsync(CancellationToken cancellationToken)
    {
        var seedOptions = options.Value;
        var shouldSeedDefaultAdmin = seedOptions.SeedDefaultAdmin || hostEnvironment.IsDevelopment();
        if (!shouldSeedDefaultAdmin)
        {
            return;
        }

        var email = string.IsNullOrWhiteSpace(seedOptions.Email)
            ? "admin@local.test"
            : seedOptions.Email.Trim().ToLowerInvariant();
        var password = string.IsNullOrWhiteSpace(seedOptions.Password)
            ? "Admin123!"
            : seedOptions.Password.Trim();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow,
                DisplayName = seedOptions.DisplayName,
                Department = seedOptions.Department,
                IsStaff = true,
                IsActive = true,
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to seed default admin user '{email}'.");
            }

            logger.LogInformation("Seeded default backoffice admin user {Email}.", email);
        }
        else
        {
            user.DisplayName = string.IsNullOrWhiteSpace(user.DisplayName)
                ? seedOptions.DisplayName
                : user.DisplayName;
            user.Department = string.IsNullOrWhiteSpace(user.Department)
                ? seedOptions.Department
                : user.Department;
            user.IsStaff = true;
            user.IsActive = true;
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);

            if (!await userManager.HasPasswordAsync(user))
            {
                await userManager.AddPasswordAsync(user, password);
            }
        }

        if (!await userManager.IsInRoleAsync(user, BackofficeRoles.Admin))
        {
            await userManager.AddToRoleAsync(user, BackofficeRoles.Admin);
        }
    }
}
