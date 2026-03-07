using BuildingBlocks.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AppHost.Authorization;

public sealed class BackofficeAuthorizationHandler(IBackofficePermissionService permissionService)
    : AuthorizationHandler<BackofficePermissionRequirement>,
        IAuthorizationHandler
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BackofficePermissionRequirement requirement)
    {
        var cancellationToken = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;

        if (string.IsNullOrWhiteSpace(requirement.Permission))
        {
            var snapshot = await permissionService.GetCurrentStaffSnapshotAsync(context.User, cancellationToken);
            if (snapshot is not null)
            {
                context.Succeed(requirement);
            }

            return;
        }

        if (await permissionService.HasPermissionAsync(context.User, requirement.Permission, cancellationToken))
        {
            context.Succeed(requirement);
        }
    }
}
