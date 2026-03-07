using Microsoft.AspNetCore.Authorization;

namespace AppHost.Authorization;

public sealed class BackofficePermissionRequirement(string? permission) : IAuthorizationRequirement
{
    public string? Permission { get; } = permission;
}
