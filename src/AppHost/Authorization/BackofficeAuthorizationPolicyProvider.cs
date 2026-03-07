using BuildingBlocks.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AppHost.Authorization;

public sealed class BackofficeAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await base.GetPolicyAsync(policyName);
        if (existing is not null)
        {
            return existing;
        }

        if (string.Equals(policyName, BackofficePolicyNames.StaffAccess, StringComparison.Ordinal))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new BackofficePermissionRequirement(null))
                .Build();
        }

        if (BackofficePolicyNames.TryParsePermission(policyName, out var permission))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new BackofficePermissionRequirement(permission))
                .Build();
        }

        return null;
    }
}
