using BuildingBlocks.Application.Authorization;
using Microsoft.AspNetCore.Builder;

namespace AppHost.Authorization;

public static class HttpContextAuthorizationExtensions
{
    public static RouteHandlerBuilder RequireBackofficePermission(
        this RouteHandlerBuilder builder,
        string permission)
    {
        return builder.RequireAuthorization(BackofficePolicyNames.Permission(permission));
    }

    public static RouteGroupBuilder RequireBackofficePermission(
        this RouteGroupBuilder builder,
        string permission)
    {
        return builder.RequireAuthorization(BackofficePolicyNames.Permission(permission));
    }
}
