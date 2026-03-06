using BuildingBlocks.Domain.Results;
using Inventory.Application.DependencyInjection;
using Inventory.Application.Stock.AdjustStock;
using Inventory.Application.Stock.GetProductInventory;
using Inventory.Application.Stock.ListActiveReservations;
using Inventory.Application.Stock.ListStockMovements;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Api;

public static class InventoryModuleExtensions
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddInventoryApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/inventory").WithTags("Inventory");

        group.MapGet("/products/{productId:guid}", async (
            Guid productId,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var details = await sender.Send(new GetProductInventoryQuery(productId), cancellationToken);
            return details is not null ? Results.Ok(details) : Results.NotFound();
        }).AllowAnonymous();

        var adminGroup = group.MapGroup(string.Empty).RequireAuthorization();

        adminGroup.MapPost("/products/{productId:guid}/adjust", async (
            Guid productId,
            AdjustStockRequest request,
            HttpContext context,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var createdBy = context.User.Identity?.Name ?? "admin";
            var result = await sender.Send(
                new AdjustStockCommand(productId, request.QuantityDelta, request.Reason, createdBy),
                cancellationToken);

            return result.IsSuccess ? Results.Ok(new { adjusted = true }) : BusinessError(result.Error);
        });

        adminGroup.MapGet("/products/{productId:guid}/movements", async (
            Guid productId,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var response = await sender.Send(
                new ListStockMovementsQuery(productId, page, pageSize),
                cancellationToken);

            return Results.Ok(response);
        });

        adminGroup.MapGet("/reservations/active", async (
            Guid? productId,
            int page,
            int pageSize,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var response = await sender.Send(
                new ListActiveReservationsQuery(productId, page, pageSize),
                cancellationToken);

            return Results.Ok(response);
        });

        return endpoints;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code switch
        {
            "inventory.stock_item.not_found" => StatusCodes.Status404NotFound,
            "inventory.stock.insufficient" => StatusCodes.Status409Conflict,
            "inventory.stock.concurrency_conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            statusCode: statusCode,
            title: "Business rule violation",
            detail: error.Message,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
            });
    }

    public sealed record AdjustStockRequest(int QuantityDelta, string? Reason);
}
