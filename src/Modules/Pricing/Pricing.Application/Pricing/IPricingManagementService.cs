using BuildingBlocks.Domain.Results;

namespace Pricing.Application.Pricing;

public interface IPricingManagementService
{
    Task<IReadOnlyCollection<PriceListDto>> GetPriceListsAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> CreatePriceListAsync(CreatePriceListRequest request, CancellationToken cancellationToken);

    Task<Result> UpdatePriceListAsync(Guid priceListId, UpdatePriceListRequest request, CancellationToken cancellationToken);

    Task<VariantPriceDto?> GetVariantPriceAsync(Guid variantId, CancellationToken cancellationToken);

    Task<Result<Guid>> CreateVariantPriceAsync(CreateVariantPriceRequest request, CancellationToken cancellationToken);

    Task<Result> UpdateVariantPriceAsync(Guid variantPriceId, UpdateVariantPriceRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PromotionDto>> GetPromotionsAsync(CancellationToken cancellationToken);

    Task<PromotionDto?> GetPromotionAsync(Guid promotionId, CancellationToken cancellationToken);

    Task<Result<Guid>> CreatePromotionAsync(CreatePromotionRequest request, CancellationToken cancellationToken);

    Task<Result> UpdatePromotionAsync(Guid promotionId, UpdatePromotionRequest request, CancellationToken cancellationToken);

    Task<Result> ActivatePromotionAsync(Guid promotionId, CancellationToken cancellationToken);

    Task<Result> ArchivePromotionAsync(Guid promotionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CouponDto>> GetCouponsAsync(CancellationToken cancellationToken);

    Task<Result<Guid>> CreateCouponAsync(CreateCouponRequest request, CancellationToken cancellationToken);

    Task<Result> UpdateCouponAsync(Guid couponId, UpdateCouponRequest request, CancellationToken cancellationToken);

    Task<Result> DisableCouponAsync(Guid couponId, CancellationToken cancellationToken);
}
