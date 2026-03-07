using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Pricing.Application.Pricing;
using Pricing.Domain.Coupons;
using Pricing.Domain.PriceLists;
using Pricing.Domain.Promotions;
using Pricing.Domain.VariantPrices;

namespace Pricing.Infrastructure.Pricing;

internal sealed class PricingManagementService(
    IPriceListRepository priceListRepository,
    IVariantPriceRepository variantPriceRepository,
    IPromotionRepository promotionRepository,
    ICouponRepository couponRepository,
    IPricingUnitOfWork unitOfWork,
    IClock clock)
    : IPricingManagementService
{
    public async Task<IReadOnlyCollection<PriceListDto>> GetPriceListsAsync(CancellationToken cancellationToken)
    {
        var priceLists = await priceListRepository.ListAsync(cancellationToken);
        return priceLists.Select(MapPriceList).ToList();
    }

    public async Task<Result<Guid>> CreatePriceListAsync(CreatePriceListRequest request, CancellationToken cancellationToken)
    {
        var result = PriceList.Create(
            request.Name,
            request.Code,
            request.Currency,
            request.IsDefault,
            request.IsActive,
            request.Priority,
            clock.UtcNow);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Error);
        }

        if (await priceListRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(new Error("pricing.price_list.code.duplicate", "Price list code already exists."));
        }

        await NormalizeDefaultsAsync(request.IsDefault, request.Currency, cancellationToken);
        await priceListRepository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(result.Value.Id);
    }

    public async Task<Result> UpdatePriceListAsync(
        Guid priceListId,
        UpdatePriceListRequest request,
        CancellationToken cancellationToken)
    {
        var priceList = await priceListRepository.GetByIdAsync(priceListId, cancellationToken);
        if (priceList is null)
        {
            return Result.Failure(new Error("pricing.price_list.not_found", "Price list was not found."));
        }

        var updateResult = priceList.Update(
            request.Name,
            request.Currency,
            request.IsDefault,
            request.IsActive,
            request.Priority,
            clock.UtcNow);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await NormalizeDefaultsAsync(request.IsDefault, request.Currency, cancellationToken, priceList.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<VariantPriceDto?> GetVariantPriceAsync(Guid variantId, CancellationToken cancellationToken)
    {
        var priceList = await priceListRepository.GetDefaultAsync("EUR", cancellationToken);
        if (priceList is null)
        {
            return null;
        }

        var variantPrice = await variantPriceRepository.GetActiveForVariantAsync(
            priceList.Id,
            variantId,
            clock.UtcNow,
            cancellationToken);

        return variantPrice is null ? null : MapVariantPrice(variantPrice);
    }

    public async Task<Result<Guid>> CreateVariantPriceAsync(
        CreateVariantPriceRequest request,
        CancellationToken cancellationToken)
    {
        var result = VariantPrice.Create(
            request.PriceListId,
            request.VariantId,
            request.BasePriceAmount,
            request.CompareAtPriceAmount,
            request.Currency,
            request.IsActive,
            request.ValidFromUtc,
            request.ValidToUtc,
            clock.UtcNow);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Error);
        }

        await variantPriceRepository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(result.Value.Id);
    }

    public async Task<Result> UpdateVariantPriceAsync(
        Guid variantPriceId,
        UpdateVariantPriceRequest request,
        CancellationToken cancellationToken)
    {
        var variantPrice = await variantPriceRepository.GetByIdAsync(variantPriceId, cancellationToken);
        if (variantPrice is null)
        {
            return Result.Failure(new Error("pricing.variant_price.not_found", "Variant price was not found."));
        }

        var updateResult = variantPrice.Update(
            request.BasePriceAmount,
            request.CompareAtPriceAmount,
            request.Currency,
            request.IsActive,
            request.ValidFromUtc,
            request.ValidToUtc,
            clock.UtcNow);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyCollection<PromotionDto>> GetPromotionsAsync(CancellationToken cancellationToken)
    {
        var promotions = await promotionRepository.ListAsync(cancellationToken);
        return promotions.Select(MapPromotion).ToList();
    }

    public async Task<PromotionDto?> GetPromotionAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        return promotion is null ? null : MapPromotion(promotion);
    }

    public async Task<Result<Guid>> CreatePromotionAsync(CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var result = Promotion.Create(
            request.Name,
            request.Code,
            request.Type,
            request.Description,
            request.Priority,
            request.IsExclusive,
            request.AllowWithCoupons,
            request.StartAtUtc,
            request.EndAtUtc,
            request.UsageLimitTotal,
            request.UsageLimitPerCustomer,
            request.Scopes,
            request.Conditions,
            request.Benefits,
            clock.UtcNow);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Error);
        }

        await promotionRepository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(result.Value.Id);
    }

    public async Task<Result> UpdatePromotionAsync(
        Guid promotionId,
        UpdatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion is null)
        {
            return Result.Failure(new Error("pricing.promotion.not_found", "Promotion was not found."));
        }

        var updateResult = promotion.Update(
            request.Name,
            request.Code,
            request.Description,
            request.Priority,
            request.IsExclusive,
            request.AllowWithCoupons,
            request.StartAtUtc,
            request.EndAtUtc,
            request.UsageLimitTotal,
            request.UsageLimitPerCustomer,
            request.Scopes,
            request.Conditions,
            request.Benefits,
            clock.UtcNow);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ActivatePromotionAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion is null)
        {
            return Result.Failure(new Error("pricing.promotion.not_found", "Promotion was not found."));
        }

        var activateResult = promotion.Activate(clock.UtcNow);
        if (activateResult.IsFailure)
        {
            return activateResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ArchivePromotionAsync(Guid promotionId, CancellationToken cancellationToken)
    {
        var promotion = await promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion is null)
        {
            return Result.Failure(new Error("pricing.promotion.not_found", "Promotion was not found."));
        }

        var archiveResult = promotion.Archive(clock.UtcNow);
        if (archiveResult.IsFailure)
        {
            return archiveResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyCollection<CouponDto>> GetCouponsAsync(CancellationToken cancellationToken)
    {
        var coupons = await couponRepository.ListAsync(cancellationToken);
        return coupons.Select(MapCoupon).ToList();
    }

    public async Task<Result<Guid>> CreateCouponAsync(CreateCouponRequest request, CancellationToken cancellationToken)
    {
        var result = Coupon.Create(
            request.Code,
            request.Description,
            request.PromotionId,
            request.StartAtUtc,
            request.EndAtUtc,
            request.UsageLimitTotal,
            request.UsageLimitPerCustomer,
            clock.UtcNow);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Error);
        }

        if (await couponRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            return Result<Guid>.Failure(new Error("pricing.coupon.code.duplicate", "Coupon code already exists."));
        }

        await couponRepository.AddAsync(result.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(result.Value.Id);
    }

    public async Task<Result> UpdateCouponAsync(Guid couponId, UpdateCouponRequest request, CancellationToken cancellationToken)
    {
        var coupon = await couponRepository.GetByIdAsync(couponId, cancellationToken);
        if (coupon is null)
        {
            return Result.Failure(new Error("pricing.coupon.not_found", "Coupon was not found."));
        }

        var updateResult = coupon.Update(
            request.Description,
            request.StartAtUtc,
            request.EndAtUtc,
            request.UsageLimitTotal,
            request.UsageLimitPerCustomer,
            clock.UtcNow);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DisableCouponAsync(Guid couponId, CancellationToken cancellationToken)
    {
        var coupon = await couponRepository.GetByIdAsync(couponId, cancellationToken);
        if (coupon is null)
        {
            return Result.Failure(new Error("pricing.coupon.not_found", "Coupon was not found."));
        }

        coupon.Disable(clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task NormalizeDefaultsAsync(
        bool isDefault,
        string currency,
        CancellationToken cancellationToken,
        Guid? excludeId = null)
    {
        if (!isDefault)
        {
            return;
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        var priceLists = await priceListRepository.ListAsync(cancellationToken);
        foreach (var priceList in priceLists.Where(priceList =>
                     priceList.Currency == normalizedCurrency &&
                     priceList.IsDefault &&
                     priceList.Id != excludeId))
        {
            priceList.Update(priceList.Name, priceList.Currency, false, priceList.IsActive, priceList.Priority, clock.UtcNow);
        }
    }

    private static PriceListDto MapPriceList(PriceList priceList)
    {
        return new PriceListDto(
            priceList.Id,
            priceList.Name,
            priceList.Code,
            priceList.Currency,
            priceList.IsDefault,
            priceList.IsActive,
            priceList.Priority,
            priceList.CreatedAtUtc,
            priceList.UpdatedAtUtc);
    }

    private static VariantPriceDto MapVariantPrice(VariantPrice variantPrice)
    {
        return new VariantPriceDto(
            variantPrice.Id,
            variantPrice.PriceListId,
            variantPrice.VariantId,
            variantPrice.BasePriceAmount,
            variantPrice.CompareAtPriceAmount,
            variantPrice.Currency,
            variantPrice.IsActive,
            variantPrice.ValidFromUtc,
            variantPrice.ValidToUtc,
            variantPrice.CreatedAtUtc,
            variantPrice.UpdatedAtUtc);
    }

    private static PromotionDto MapPromotion(Promotion promotion)
    {
        return new PromotionDto(
            promotion.Id,
            promotion.Name,
            promotion.Code,
            promotion.Type,
            promotion.Status,
            promotion.Description,
            promotion.Priority,
            promotion.IsExclusive,
            promotion.AllowWithCoupons,
            promotion.StartAtUtc,
            promotion.EndAtUtc,
            promotion.UsageLimitTotal,
            promotion.UsageLimitPerCustomer,
            promotion.TimesUsedTotal,
            promotion.Scopes.Select(scope => new PromotionScopeData(scope.ScopeType, scope.TargetId)).ToList(),
            promotion.Conditions.Select(condition => new PromotionConditionData(condition.ConditionType, condition.Operator, condition.Value)).ToList(),
            promotion.Benefits.Select(benefit => new PromotionBenefitData(
                benefit.BenefitType,
                benefit.ValueAmount,
                benefit.ValuePercent,
                benefit.MaxDiscountAmount,
                benefit.ApplyPerUnit)).ToList(),
            promotion.CreatedAtUtc,
            promotion.UpdatedAtUtc);
    }

    private static CouponDto MapCoupon(Coupon coupon)
    {
        return new CouponDto(
            coupon.Id,
            coupon.Code,
            coupon.Description,
            coupon.PromotionId,
            coupon.Status,
            coupon.StartAtUtc,
            coupon.EndAtUtc,
            coupon.UsageLimitTotal,
            coupon.UsageLimitPerCustomer,
            coupon.TimesUsedTotal,
            coupon.CreatedAtUtc,
            coupon.UpdatedAtUtc);
    }
}
