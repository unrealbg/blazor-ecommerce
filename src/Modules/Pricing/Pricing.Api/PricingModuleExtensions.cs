using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Application.DependencyInjection;
using Pricing.Application.Pricing;

namespace Pricing.Api;

public static class PricingModuleExtensions
{
    public static IServiceCollection AddPricingModule(this IServiceCollection services)
    {
        services.AddPricingApplication();
        return services;
    }

    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/pricing").WithTags("Pricing");

        group.MapGet("/variants/{variantId:guid}", async (
            Guid variantId,
            IVariantPricingService pricingService,
            CancellationToken cancellationToken) =>
        {
            var pricing = await pricingService.GetVariantPricingAsync(variantId, cancellationToken);
            return pricing is null ? Results.NotFound() : Results.Ok(pricing);
        }).AllowAnonymous();

        group.MapPost("/cart/price", async (
            CartPricingRequest request,
            ICartPricingService pricingService,
            CancellationToken cancellationToken) =>
        {
            var result = await pricingService.PriceAsync(request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : BusinessError(result.Error);
        }).AllowAnonymous();

        group.MapPost("/coupons/validate", async (
            CouponValidationRequest request,
            ICartPricingService pricingService,
            CancellationToken cancellationToken) =>
        {
            var result = await pricingService.ValidateCouponAsync(request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : BusinessError(result.Error);
        }).AllowAnonymous();

        group.MapGet("/price-lists", async (
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var priceLists = await managementService.GetPriceListsAsync(cancellationToken);
            return Results.Ok(priceLists);
        }).RequireAuthorization();

        group.MapPost("/price-lists", async (
            CreatePriceListRequest request,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.CreatePriceListAsync(request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/pricing/price-lists/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapPut("/price-lists/{priceListId:guid}", async (
            Guid priceListId,
            UpdatePriceListRequest request,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.UpdatePriceListAsync(priceListId, request, cancellationToken);
            return result.IsSuccess ? Results.Ok(new { id = priceListId }) : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapPost("/variant-prices", async (
            CreateVariantPriceRequest request,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.CreateVariantPriceAsync(request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/pricing/variant-prices/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapGet("/variant-prices/by-variant/{variantId:guid}", async (
            Guid variantId,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var variantPrice = await managementService.GetVariantPriceAsync(variantId, cancellationToken);
            return variantPrice is null ? Results.NotFound() : Results.Ok(variantPrice);
        }).RequireAuthorization();

        group.MapPut("/variant-prices/{variantPriceId:guid}", async (
            Guid variantPriceId,
            UpdateVariantPriceRequest request,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.UpdateVariantPriceAsync(variantPriceId, request, cancellationToken);
            return result.IsSuccess ? Results.Ok(new { id = variantPriceId }) : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapGet("/promotions", async (
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var promotions = await managementService.GetPromotionsAsync(cancellationToken);
            return Results.Ok(promotions);
        }).RequireAuthorization();

        group.MapGet("/promotions/{promotionId:guid}", async (
            Guid promotionId,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var promotion = await managementService.GetPromotionAsync(promotionId, cancellationToken);
            return promotion is null ? Results.NotFound() : Results.Ok(promotion);
        }).RequireAuthorization();

        group.MapPost("/promotions", async (
            CreatePromotionRequest request,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.CreatePromotionAsync(request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/pricing/promotions/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapPut("/promotions/{promotionId:guid}", async (
            Guid promotionId,
            UpdatePromotionRequest request,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.UpdatePromotionAsync(promotionId, request, cancellationToken);
            return result.IsSuccess ? Results.Ok(new { id = promotionId }) : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapPost("/promotions/{promotionId:guid}/activate", async (
            Guid promotionId,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.ActivatePromotionAsync(promotionId, cancellationToken);
            return result.IsSuccess ? Results.Ok(new { id = promotionId }) : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapPost("/promotions/{promotionId:guid}/archive", async (
            Guid promotionId,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.ArchivePromotionAsync(promotionId, cancellationToken);
            return result.IsSuccess ? Results.Ok(new { id = promotionId }) : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapGet("/coupons", async (
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var coupons = await managementService.GetCouponsAsync(cancellationToken);
            return Results.Ok(coupons);
        }).RequireAuthorization();

        group.MapPost("/coupons", async (
            CreateCouponRequest request,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.CreateCouponAsync(request, cancellationToken);
            return result.IsSuccess
                ? Results.Created($"/api/v1/pricing/coupons/{result.Value:D}", new { id = result.Value })
                : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapPut("/coupons/{couponId:guid}", async (
            Guid couponId,
            UpdateCouponRequest request,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.UpdateCouponAsync(couponId, request, cancellationToken);
            return result.IsSuccess ? Results.Ok(new { id = couponId }) : BusinessError(result.Error);
        }).RequireAuthorization();

        group.MapPost("/coupons/{couponId:guid}/disable", async (
            Guid couponId,
            IPricingManagementService managementService,
            CancellationToken cancellationToken) =>
        {
            var result = await managementService.DisableCouponAsync(couponId, cancellationToken);
            return result.IsSuccess ? Results.Ok(new { id = couponId }) : BusinessError(result.Error);
        }).RequireAuthorization();

        return endpoints;
    }

    private static IResult BusinessError(Error error)
    {
        var statusCode = error.Code switch
        {
            "pricing.coupon.not_found" => StatusCodes.Status404NotFound,
            "pricing.price_list.not_found" => StatusCodes.Status404NotFound,
            "pricing.variant_price.not_found" => StatusCodes.Status404NotFound,
            "pricing.promotion.not_found" => StatusCodes.Status404NotFound,
            _ when error.Code.Contains("duplicate", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ when error.Code.Contains("usage_limit_reached", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
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
}
