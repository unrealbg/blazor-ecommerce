using System.Security.Claims;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Results;
using Customers.Application.Auth;
using Customers.Application.Compliance;
using Customers.Application.Customers;
using Customers.Application.DependencyInjection;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Customers.Api;

public static class CustomersModuleExtensions
{
    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddCustomersApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapCustomersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var customersGroup = endpoints.MapGroup("/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        customersGroup.MapGet("/me", async (HttpContext context, ISender sender, CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var customer = await sender.Send(new GetCurrentCustomerQuery(userId.Value), cancellationToken);
            return customer is not null ? Results.Ok(customer) : Results.NotFound();
        });

        customersGroup.MapPut("/me", async (
            HttpContext context,
            UpdateProfileRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await sender.Send(
                new UpdateProfileCommand(userId.Value, request.FirstName, request.LastName, request.PhoneNumber),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : BusinessError(result.Error);
        });

        customersGroup.MapGet("/me/addresses", async (
            HttpContext context,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var addresses = await sender.Send(new ListAddressesQuery(userId.Value), cancellationToken);
            return Results.Ok(addresses);
        });

        customersGroup.MapPost("/me/addresses", async (
            HttpContext context,
            AddressRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await sender.Send(
                new AddAddressCommand(userId.Value, ToAddressInput(request)),
                cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/v1/customers/me/addresses/{result.Value}", new { id = result.Value })
                : BusinessError(result.Error);
        });

        customersGroup.MapPut("/me/addresses/{addressId:guid}", async (
            HttpContext context,
            Guid addressId,
            AddressRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await sender.Send(
                new UpdateAddressCommand(userId.Value, addressId, ToAddressInput(request)),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : BusinessError(result.Error);
        });

        customersGroup.MapDelete("/me/addresses/{addressId:guid}", async (
            HttpContext context,
            Guid addressId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await sender.Send(new DeleteAddressCommand(userId.Value, addressId), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : BusinessError(result.Error);
        });

        customersGroup.MapGet("/me/export", async (
            HttpContext context,
            ICustomerDataExportService exportService,
            CancellationToken cancellationToken) =>
        {
            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var export = await exportService.ExportByUserIdAsync(userId.Value, cancellationToken);
            return export is null ? Results.NotFound() : Results.Ok(export);
        });

        customersGroup.MapPost("/me/erase", async (
            HttpContext context,
            ConfirmErasureRequest request,
            ICustomerDataErasureService erasureService,
            CancellationToken cancellationToken) =>
        {
            if (!string.Equals(request.ConfirmationText, "ERASE", StringComparison.Ordinal))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid request",
                    detail: "ConfirmationText must equal ERASE.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "customers.erasure.confirmation.invalid",
                    });
            }

            var userId = GetUserId(context.User);
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await erasureService.EraseByUserIdAsync(userId.Value, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        var authGroup = endpoints.MapGroup("/auth").WithTags("Auth");
        authGroup.AllowAnonymous();

        authGroup.MapPost("/register", async (
            RegisterRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new RegisterCommand(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.PhoneNumber),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(result.Value) : BusinessError(result.Error);
        }).RequireRateLimiting(RateLimitingPolicyNames.Auth);

        authGroup.MapPost("/login", async (
            LoginRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new LoginCommand(request.Email, request.Password, request.RememberMe),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(result.Value) : BusinessError(result.Error);
        }).RequireRateLimiting(RateLimitingPolicyNames.Auth);

        authGroup.MapPost("/logout", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new LogoutCommand(), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : BusinessError(result.Error);
        }).RequireRateLimiting(RateLimitingPolicyNames.Auth);

        authGroup.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ForgotPasswordCommand(request.Email), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : BusinessError(result.Error);
        }).RequireRateLimiting(RateLimitingPolicyNames.Auth);

        authGroup.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : BusinessError(result.Error);
        }).RequireRateLimiting(RateLimitingPolicyNames.Auth);

        authGroup.MapGet("/verify-email", async (
            [FromQuery(Name = "userId")] Guid userId,
            [FromQuery(Name = "token")] string token,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new VerifyEmailCommand(userId, token), cancellationToken);
            return result.IsSuccess ? Results.Ok(new { verified = true }) : BusinessError(result.Error);
        });

        return endpoints;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static AddressInput ToAddressInput(AddressRequest request)
    {
        return new AddressInput(
            request.Label,
            request.FirstName,
            request.LastName,
            request.Company,
            request.Street1,
            request.Street2,
            request.City,
            request.PostalCode,
            request.CountryCode,
            request.Phone,
            request.IsDefaultShipping,
            request.IsDefaultBilling);
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code.EndsWith(".not_found", StringComparison.Ordinal)
            ? StatusCodes.Status404NotFound
            : error.Code.EndsWith(".conflict", StringComparison.Ordinal)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

        return Results.Problem(
            statusCode: statusCode,
            title: "Business rule violation",
            detail: error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
            });
    }

    public sealed record UpdateProfileRequest(string? FirstName, string? LastName, string? PhoneNumber);

    public sealed record RegisterRequest(
        string Email,
        string Password,
        string? FirstName,
        string? LastName,
        string? PhoneNumber);

    public sealed record LoginRequest(string Email, string Password, bool RememberMe);

    public sealed record ForgotPasswordRequest(string Email);

    public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

    public sealed record ConfirmErasureRequest(string ConfirmationText);

    public sealed record AddressRequest(
        string Label,
        string FirstName,
        string LastName,
        string? Company,
        string Street1,
        string? Street2,
        string City,
        string PostalCode,
        string CountryCode,
        string? Phone,
        bool IsDefaultShipping,
        bool IsDefaultBilling);
}
