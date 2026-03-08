using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pricing.Application.Pricing;
using Pricing.Domain.Coupons;
using Pricing.Domain.Promotions;

namespace Pricing.Infrastructure.Pricing;

internal sealed class PricingService(
    IPriceListRepository priceListRepository,
    IVariantPriceRepository variantPriceRepository,
    IPromotionRepository promotionRepository,
    ICouponRepository couponRepository,
    IPromotionRedemptionRepository promotionRedemptionRepository,
    IProductCatalogReader productCatalogReader,
    IClock clock,
    IDistributedCache distributedCache,
    IOptions<PricingModuleOptions> options,
    ILogger<PricingService> logger)
    : IVariantPricingService, ICartPricingService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PricingModuleOptions moduleOptions = options.Value;

    public async Task<VariantPricingSnapshot?> GetVariantPricingAsync(Guid variantId, CancellationToken cancellationToken)
    {
        if (variantId == Guid.Empty)
        {
            return null;
        }

        var cacheKey = $"pricing:variant:{variantId:D}";
        var cached = await GetCachedAsync<VariantPricingSnapshot>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var pricingResult = await PriceAsync(
            new CartPricingRequest(
                CustomerId: null,
                IsAuthenticated: false,
                Lines: [new CartPricingLineRequest(Guid.Empty, variantId, 1)],
                CouponCode: null,
                Shipping: null,
                BypassCache: true),
            cancellationToken);

        if (pricingResult.IsFailure || pricingResult.Value.Lines.Count == 0)
        {
            return null;
        }

        var line = pricingResult.Value.Lines.Single();
        var isDiscounted = (line.CompareAtUnitPriceAmount ?? 0m) > line.FinalUnitPriceAmount ||
                           line.DiscountTotalAmount > 0m;
        var snapshot = new VariantPricingSnapshot(
            line.VariantId,
            line.Currency,
            line.BaseUnitPriceAmount,
            line.CompareAtUnitPriceAmount,
            line.FinalUnitPriceAmount,
            isDiscounted,
            line.AppliedDiscounts);

        await SetCachedAsync(cacheKey, snapshot, moduleOptions.VariantCacheSeconds, cancellationToken);
        return snapshot;
    }

    public async Task<IReadOnlyDictionary<Guid, VariantPricingSnapshot>> GetVariantPricingAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        var normalizedVariantIds = variantIds
            .Where(variantId => variantId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedVariantIds.Length == 0)
        {
            return new Dictionary<Guid, VariantPricingSnapshot>();
        }

        var results = new Dictionary<Guid, VariantPricingSnapshot>(normalizedVariantIds.Length);
        foreach (var variantId in normalizedVariantIds)
        {
            var snapshot = await GetVariantPricingAsync(variantId, cancellationToken);
            if (snapshot is not null)
            {
                results[variantId] = snapshot;
            }
        }

        return results;
    }

    public async Task<Result<CartPricingResult>> PriceAsync(CartPricingRequest request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
        {
            var shippingAmount = request.Shipping?.PriceAmount ?? 0m;
            return Result<CartPricingResult>.Success(new CartPricingResult(
                moduleOptions.DefaultCurrency.ToUpperInvariant(),
                0m,
                0m,
                0m,
                0m,
                shippingAmount,
                shippingAmount,
                0m,
                shippingAmount,
                null,
                [],
                [],
                []));
        }

        var cacheKey = request.BypassCache ? null : BuildCartCacheKey(request);
        if (cacheKey is not null)
        {
            var cached = await GetCachedAsync<CartPricingResult>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return Result<CartPricingResult>.Success(cached);
            }
        }

        var result = await EvaluateAsync(request, cancellationToken);
        if (result.IsSuccess && cacheKey is not null)
        {
            await SetCachedAsync(cacheKey, result.Value, moduleOptions.CartCacheSeconds, cancellationToken);
        }

        return result;
    }

    public async Task<Result<CouponValidationResult>> ValidateCouponAsync(
        CouponValidationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return Result<CouponValidationResult>.Failure(new Error(
                "pricing.coupon.code.required",
                "Coupon code is required."));
        }

        var pricingResult = await PriceAsync(
            new CartPricingRequest(
                request.CustomerId,
                request.IsAuthenticated,
                request.Lines,
                request.Code,
                Shipping: null,
                BypassCache: true,
                StrictCouponValidation: true),
            cancellationToken);

        if (pricingResult.IsFailure)
        {
            return Result<CouponValidationResult>.Failure(pricingResult.Error);
        }

        return Result<CouponValidationResult>.Success(new CouponValidationResult(
            request.Code.Trim().ToUpperInvariant(),
            IsValid: true,
            ErrorCode: null,
            ErrorMessage: null,
            PromotionName: pricingResult.Value.AppliedDiscounts.FirstOrDefault()?.Description));
    }

    private async Task<Result<CartPricingResult>> EvaluateAsync(
        CartPricingRequest request,
        CancellationToken cancellationToken)
    {
        var invalidLine = request.Lines.FirstOrDefault(line => line.Quantity <= 0 || line.VariantId == Guid.Empty);
        if (invalidLine is not null)
        {
            return Result<CartPricingResult>.Failure(new Error(
                "pricing.cart_line.invalid",
                "Cart contains an invalid line."));
        }

        var now = clock.UtcNow;
        var requestedVariantIds = request.Lines.Select(line => line.VariantId).Distinct().ToArray();
        var productsByVariant = await productCatalogReader.GetByVariantIdsAsync(requestedVariantIds, cancellationToken);
        if (productsByVariant.Count != requestedVariantIds.Length)
        {
            return Result<CartPricingResult>.Failure(new Error(
                "pricing.variant.not_found",
                "One or more cart variants could not be resolved."));
        }

        var lineContextsResult = await BuildLineContextsAsync(request.Lines, productsByVariant, now, cancellationToken);
        if (lineContextsResult.IsFailure)
        {
            return Result<CartPricingResult>.Failure(lineContextsResult.Error);
        }

        var contexts = lineContextsResult.Value;
        var currency = contexts[0].Currency;
        if (contexts.Any(context => !string.Equals(context.Currency, currency, StringComparison.Ordinal)))
        {
            return Result<CartPricingResult>.Failure(new Error(
                "pricing.currency.mismatch",
                "All cart lines must use the same currency."));
        }

        var subtotalBeforeDiscount = Round(contexts.Sum(context => context.BaseUnitPriceAmount * context.Quantity));
        var promotions = await promotionRepository.ListActiveAsync(now, cancellationToken);
        var messages = new List<string>();

        Coupon? coupon = null;
        Promotion? couponPromotion = null;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var couponResult = await ResolveCouponAsync(
                request.CouponCode,
                request.CustomerId,
                request.IsAuthenticated,
                contexts,
                subtotalBeforeDiscount,
                promotions,
                now,
                cancellationToken);

            if (couponResult.IsFailure)
            {
                if (request.StrictCouponValidation)
                {
                    return Result<CartPricingResult>.Failure(couponResult.Error);
                }

                messages.Add(couponResult.Error.Message);
            }
            else
            {
                coupon = couponResult.Value.Coupon;
                couponPromotion = couponResult.Value.Promotion;
            }
        }

        var automaticPromotions = promotions
            .Where(promotion => couponPromotion is null || promotion.Id != couponPromotion.Id)
            .Where(promotion => couponPromotion is null || promotion.AllowWithCoupons)
            .ToList();

        if (couponPromotion is not null && !couponPromotion.AllowWithCoupons)
        {
            automaticPromotions.Clear();
        }

        var exclusiveCandidates = await EvaluateExclusiveCandidatesAsync(
            contexts,
            automaticPromotions,
            coupon,
            couponPromotion,
            request,
            subtotalBeforeDiscount,
            cancellationToken);

        var chosenExclusive = exclusiveCandidates
            .Where(candidate => candidate.TotalDiscountAmount > 0m)
            .OrderByDescending(candidate => candidate.TotalDiscountAmount)
            .ThenByDescending(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.PromotionId)
            .FirstOrDefault();

        if (chosenExclusive is not null)
        {
            return Result<CartPricingResult>.Success(BuildResult(
                contexts,
                subtotalBeforeDiscount,
                request,
                chosenExclusive,
                coupon?.Code,
                messages));
        }

        var lineApplications = await EvaluateBestLinePromotionsAsync(
            contexts,
            automaticPromotions,
            coupon,
            couponPromotion,
            request,
            subtotalBeforeDiscount,
            cancellationToken);

        var lineDiscountTotal = Round(lineApplications.Sum(application => application.Amount));
        var subtotalAfterLineDiscounts = Math.Max(0m, Round(subtotalBeforeDiscount - lineDiscountTotal));

        var cartPromotion = await EvaluateBestCartPromotionAsync(
            contexts,
            automaticPromotions,
            coupon,
            couponPromotion,
            request,
            subtotalBeforeDiscount,
            subtotalAfterLineDiscounts,
            cancellationToken);

        var shippingPromotion = await EvaluateBestShippingPromotionAsync(
            contexts,
            automaticPromotions,
            coupon,
            couponPromotion,
            request,
            subtotalBeforeDiscount,
            cancellationToken);

        var applications = lineApplications
            .Concat(cartPromotion?.Applications ?? [])
            .Concat(shippingPromotion?.Applications ?? [])
            .ToList();

        var nonExclusiveEvaluation = new PromotionEvaluation(
            Guid.Empty,
            0,
            applications,
            Round(applications.Sum(application => application.Amount)));

        return Result<CartPricingResult>.Success(BuildResult(
            contexts,
            subtotalBeforeDiscount,
            request,
            nonExclusiveEvaluation,
            coupon?.Code,
            messages));
    }

    private CartPricingResult BuildResult(
        IReadOnlyCollection<LinePricingContext> contexts,
        decimal subtotalBeforeDiscount,
        CartPricingRequest request,
        PromotionEvaluation evaluation,
        string? appliedCouponCode,
        IReadOnlyCollection<string> messages)
    {
        var allApplications = evaluation.Applications.ToList();
        var lineDiscountLookup = allApplications
            .Where(application => application.TargetLineVariantId is not null)
            .GroupBy(application => application.TargetLineVariantId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var lineResults = contexts
            .Select(context =>
            {
                lineDiscountLookup.TryGetValue(context.VariantId, out var discounts);
                discounts ??= [];
                var lineDiscountTotal = Round(discounts.Sum(discount => discount.Amount));
                var baseLineTotal = Round(context.BaseUnitPriceAmount * context.Quantity);
                var finalLineTotal = Math.Max(0m, Round(baseLineTotal - lineDiscountTotal));
                var finalUnitPrice = context.Quantity == 0
                    ? 0m
                    : Round(finalLineTotal / context.Quantity);

                return new CartPricingLineResult(
                    context.ProductId,
                    context.VariantId,
                    context.Currency,
                    context.BaseUnitPriceAmount,
                    context.CompareAtPriceAmount,
                    finalUnitPrice,
                    finalLineTotal,
                    lineDiscountTotal,
                    discounts);
            })
            .ToList();

        var lineDiscountTotalAmount = Round(lineResults.Sum(line => line.DiscountTotalAmount));
        var cartDiscountTotalAmount = Round(allApplications
            .Where(application =>
                application.TargetLineVariantId is null &&
                !string.Equals(application.ScopeType, PromotionScopeType.Shipping.ToString(), StringComparison.Ordinal))
            .Sum(application => application.Amount));
        var shippingBefore = request.Shipping?.PriceAmount ?? 0m;
        var shippingDiscountAmount = Round(allApplications
            .Where(application => string.Equals(application.ScopeType, PromotionScopeType.Shipping.ToString(), StringComparison.Ordinal))
            .Sum(application => application.Amount));
        var shippingAmount = Math.Max(0m, Round(shippingBefore - shippingDiscountAmount));
        var subtotalAmount = Math.Max(0m, Round(subtotalBeforeDiscount - lineDiscountTotalAmount - cartDiscountTotalAmount));
        var grandTotal = Round(subtotalAmount + shippingAmount);

        return new CartPricingResult(
            contexts.First().Currency,
            subtotalBeforeDiscount,
            subtotalAmount,
            lineDiscountTotalAmount,
            cartDiscountTotalAmount,
            shippingBefore,
            shippingAmount,
            shippingDiscountAmount,
            grandTotal,
            appliedCouponCode,
            lineResults,
            allApplications,
            messages);
    }

    private async Task<Result<IReadOnlyList<LinePricingContext>>> BuildLineContextsAsync(
        IReadOnlyCollection<CartPricingLineRequest> lines,
        IReadOnlyDictionary<Guid, ProductSnapshot> productsByVariant,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var currency = productsByVariant.Values.First().Currency;
        var priceList = await priceListRepository.GetDefaultAsync(currency, cancellationToken);
        var resolvedPrices = priceList is null
            ? new Dictionary<Guid, Domain.VariantPrices.VariantPrice>()
            : await variantPriceRepository.GetActiveForVariantsAsync(
                priceList.Id,
                productsByVariant.Keys.ToArray(),
                now,
                cancellationToken);

        var contexts = new List<LinePricingContext>(lines.Count);
        foreach (var line in lines)
        {
            if (!productsByVariant.TryGetValue(line.VariantId, out var product))
            {
                return Result<IReadOnlyList<LinePricingContext>>.Failure(new Error(
                    "pricing.variant.not_found",
                    "A cart variant could not be resolved."));
            }

            var variant = product.Variants.FirstOrDefault(item => item.Id == line.VariantId);
            if (variant is null)
            {
                return Result<IReadOnlyList<LinePricingContext>>.Failure(new Error(
                    "pricing.variant.not_found",
                    "A cart variant could not be resolved."));
            }

            resolvedPrices.TryGetValue(line.VariantId, out var variantPrice);

            contexts.Add(new LinePricingContext(
                line.ProductId == Guid.Empty ? product.Id : line.ProductId,
                line.VariantId,
                line.Quantity,
                product,
                variant,
                variantPrice?.BasePriceAmount ?? variant.Amount,
                variantPrice?.CompareAtPriceAmount ?? variant.CompareAtAmount,
                variantPrice?.Currency ?? variant.Currency));
        }

        return Result<IReadOnlyList<LinePricingContext>>.Success(contexts);
    }

    private async Task<Result<CouponResolution>> ResolveCouponAsync(
        string couponCode,
        string? customerId,
        bool isAuthenticated,
        IReadOnlyCollection<LinePricingContext> contexts,
        decimal subtotalBeforeDiscount,
        IReadOnlyCollection<Promotion> promotions,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var coupon = await couponRepository.GetByCodeAsync(couponCode, cancellationToken);
        if (coupon is null)
        {
            return Result<CouponResolution>.Failure(new Error("pricing.coupon.not_found", "Coupon code was not found."));
        }

        if (!coupon.IsActiveAt(now))
        {
            return Result<CouponResolution>.Failure(new Error("pricing.coupon.inactive", "Coupon is not active."));
        }

        if (coupon.UsageLimitTotal is not null && coupon.TimesUsedTotal >= coupon.UsageLimitTotal.Value)
        {
            return Result<CouponResolution>.Failure(new Error(
                "pricing.coupon.usage_limit_reached",
                "Coupon usage limit has been reached."));
        }

        if (coupon.UsageLimitPerCustomer is not null)
        {
            var customerRedemptions = await promotionRedemptionRepository.CountCouponRedemptionsAsync(
                coupon.Id,
                customerId,
                cancellationToken);
            if (!coupon.CanRedeemForCustomer(customerRedemptions))
            {
                return Result<CouponResolution>.Failure(new Error(
                    "pricing.coupon.usage_limit_reached",
                    "Coupon usage limit has been reached for this customer."));
            }
        }

        var promotion = promotions.FirstOrDefault(item => item.Id == coupon.PromotionId)
                        ?? await promotionRepository.GetByIdAsync(coupon.PromotionId, cancellationToken);

        if (promotion is null || !promotion.IsActiveAt(now))
        {
            return Result<CouponResolution>.Failure(new Error(
                "pricing.promotion.inactive",
                "Coupon promotion is not active."));
        }

        var eligibility = await IsPromotionEligibleAsync(
            promotion,
            coupon,
            customerId,
            isAuthenticated,
            contexts,
            subtotalBeforeDiscount,
            cancellationToken);

        return eligibility.IsFailure
            ? Result<CouponResolution>.Failure(eligibility.Error)
            : Result<CouponResolution>.Success(new CouponResolution(coupon, promotion));
    }

    private async Task<IReadOnlyCollection<PromotionEvaluation>> EvaluateExclusiveCandidatesAsync(
        IReadOnlyCollection<LinePricingContext> contexts,
        IReadOnlyCollection<Promotion> automaticPromotions,
        Coupon? coupon,
        Promotion? couponPromotion,
        CartPricingRequest request,
        decimal subtotalBeforeDiscount,
        CancellationToken cancellationToken)
    {
        var candidates = automaticPromotions.Where(promotion => promotion.IsExclusive).ToList();
        if (couponPromotion is not null && couponPromotion.IsExclusive)
        {
            candidates.Add(couponPromotion);
        }

        var evaluations = new List<PromotionEvaluation>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var sourceCoupon = couponPromotion?.Id == candidate.Id ? coupon : null;
            var eligibility = await IsPromotionEligibleAsync(
                candidate,
                sourceCoupon,
                request.CustomerId,
                request.IsAuthenticated,
                contexts,
                subtotalBeforeDiscount,
                cancellationToken);
            if (eligibility.IsFailure || !eligibility.Value)
            {
                continue;
            }

            evaluations.Add(EvaluatePromotion(candidate, sourceCoupon, contexts, subtotalBeforeDiscount, request.Shipping));
        }

        return evaluations;
    }

    private async Task<IReadOnlyCollection<PricingDiscountApplication>> EvaluateBestLinePromotionsAsync(
        IReadOnlyCollection<LinePricingContext> contexts,
        IReadOnlyCollection<Promotion> automaticPromotions,
        Coupon? coupon,
        Promotion? couponPromotion,
        CartPricingRequest request,
        decimal subtotalBeforeDiscount,
        CancellationToken cancellationToken)
    {
        var results = new List<PricingDiscountApplication>();

        foreach (var context in contexts)
        {
            PromotionEvaluation? bestEvaluation = null;

            foreach (var promotion in automaticPromotions.Where(IsLinePromotion))
            {
                var eligibility = await IsPromotionEligibleAsync(
                    promotion,
                    null,
                    request.CustomerId,
                    request.IsAuthenticated,
                    contexts,
                    subtotalBeforeDiscount,
                    cancellationToken);
                if (eligibility.IsFailure || !eligibility.Value)
                {
                    continue;
                }

                var evaluation = EvaluatePromotion(promotion, null, [context], subtotalBeforeDiscount, null);
                if (evaluation.TotalDiscountAmount <= 0m)
                {
                    continue;
                }

                if (bestEvaluation is null ||
                    evaluation.TotalDiscountAmount > bestEvaluation.TotalDiscountAmount ||
                    (evaluation.TotalDiscountAmount == bestEvaluation.TotalDiscountAmount &&
                     evaluation.Priority > bestEvaluation.Priority))
                {
                    bestEvaluation = evaluation;
                }
            }

            if (couponPromotion is not null && IsLinePromotion(couponPromotion))
            {
                var eligibility = await IsPromotionEligibleAsync(
                    couponPromotion,
                    coupon,
                    request.CustomerId,
                    request.IsAuthenticated,
                    contexts,
                    subtotalBeforeDiscount,
                    cancellationToken);
                if (eligibility.IsSuccess && eligibility.Value)
                {
                    var evaluation = EvaluatePromotion(couponPromotion, coupon, [context], subtotalBeforeDiscount, null);
                    if (evaluation.TotalDiscountAmount > 0m &&
                        (bestEvaluation is null ||
                         evaluation.TotalDiscountAmount > bestEvaluation.TotalDiscountAmount ||
                         (evaluation.TotalDiscountAmount == bestEvaluation.TotalDiscountAmount &&
                          evaluation.Priority > bestEvaluation.Priority)))
                    {
                        bestEvaluation = evaluation;
                    }
                }
            }

            if (bestEvaluation is not null)
            {
                results.AddRange(bestEvaluation.Applications);
            }
        }

        return results;
    }

    private async Task<PromotionEvaluation?> EvaluateBestCartPromotionAsync(
        IReadOnlyCollection<LinePricingContext> contexts,
        IReadOnlyCollection<Promotion> automaticPromotions,
        Coupon? coupon,
        Promotion? couponPromotion,
        CartPricingRequest request,
        decimal subtotalBeforeDiscount,
        decimal subtotalAfterLineDiscounts,
        CancellationToken cancellationToken)
    {
        var candidates = automaticPromotions.Where(IsCartPromotion).ToList();
        if (couponPromotion is not null && IsCartPromotion(couponPromotion))
        {
            candidates.Add(couponPromotion);
        }

        PromotionEvaluation? bestEvaluation = null;
        foreach (var candidate in candidates)
        {
            var sourceCoupon = couponPromotion?.Id == candidate.Id ? coupon : null;
            var eligibility = await IsPromotionEligibleAsync(
                candidate,
                sourceCoupon,
                request.CustomerId,
                request.IsAuthenticated,
                contexts,
                subtotalBeforeDiscount,
                cancellationToken);
            if (eligibility.IsFailure || !eligibility.Value)
            {
                continue;
            }

            var evaluation = EvaluatePromotion(candidate, sourceCoupon, contexts, subtotalAfterLineDiscounts, null);
            if (evaluation.TotalDiscountAmount <= 0m)
            {
                continue;
            }

            if (bestEvaluation is null ||
                evaluation.TotalDiscountAmount > bestEvaluation.TotalDiscountAmount ||
                (evaluation.TotalDiscountAmount == bestEvaluation.TotalDiscountAmount &&
                 evaluation.Priority > bestEvaluation.Priority))
            {
                bestEvaluation = evaluation;
            }
        }

        return bestEvaluation;
    }

    private async Task<PromotionEvaluation?> EvaluateBestShippingPromotionAsync(
        IReadOnlyCollection<LinePricingContext> contexts,
        IReadOnlyCollection<Promotion> automaticPromotions,
        Coupon? coupon,
        Promotion? couponPromotion,
        CartPricingRequest request,
        decimal subtotalBeforeDiscount,
        CancellationToken cancellationToken)
    {
        if (request.Shipping is null || request.Shipping.PriceAmount <= 0m)
        {
            return null;
        }

        var candidates = automaticPromotions.Where(IsShippingPromotion).ToList();
        if (couponPromotion is not null && IsShippingPromotion(couponPromotion))
        {
            candidates.Add(couponPromotion);
        }

        PromotionEvaluation? bestEvaluation = null;
        foreach (var candidate in candidates)
        {
            var sourceCoupon = couponPromotion?.Id == candidate.Id ? coupon : null;
            var eligibility = await IsPromotionEligibleAsync(
                candidate,
                sourceCoupon,
                request.CustomerId,
                request.IsAuthenticated,
                contexts,
                subtotalBeforeDiscount,
                cancellationToken);
            if (eligibility.IsFailure || !eligibility.Value)
            {
                continue;
            }

            var evaluation = EvaluatePromotion(candidate, sourceCoupon, contexts, subtotalBeforeDiscount, request.Shipping);
            if (evaluation.TotalDiscountAmount <= 0m)
            {
                continue;
            }

            if (bestEvaluation is null ||
                evaluation.TotalDiscountAmount > bestEvaluation.TotalDiscountAmount ||
                (evaluation.TotalDiscountAmount == bestEvaluation.TotalDiscountAmount &&
                 evaluation.Priority > bestEvaluation.Priority))
            {
                bestEvaluation = evaluation;
            }
        }

        return bestEvaluation;
    }

    private async Task<Result<bool>> IsPromotionEligibleAsync(
        Promotion promotion,
        Coupon? coupon,
        string? customerId,
        bool isAuthenticated,
        IReadOnlyCollection<LinePricingContext> contexts,
        decimal subtotalBeforeDiscount,
        CancellationToken cancellationToken)
    {
        if (promotion.UsageLimitTotal is not null && promotion.TimesUsedTotal >= promotion.UsageLimitTotal.Value)
        {
            return Result<bool>.Failure(new Error(
                "pricing.promotion.usage_limit_reached",
                "Promotion usage limit has been reached."));
        }

        if (promotion.UsageLimitPerCustomer is not null)
        {
            var customerRedemptions = await promotionRedemptionRepository.CountPromotionRedemptionsAsync(
                promotion.Id,
                customerId,
                cancellationToken);
            if (!promotion.CanRedeemForCustomer(customerRedemptions))
            {
                return Result<bool>.Failure(new Error(
                    "pricing.promotion.usage_limit_reached",
                    "Promotion usage limit has been reached for this customer."));
            }
        }

        foreach (var condition in promotion.Conditions)
        {
            if (!EvaluateCondition(condition, coupon, isAuthenticated, contexts, subtotalBeforeDiscount))
            {
                return Result<bool>.Failure(new Error(
                    "pricing.promotion.not_applicable",
                    "Promotion conditions were not satisfied."));
            }
        }

        return Result<bool>.Success(true);
    }

    private static bool EvaluateCondition(
        PromotionCondition condition,
        Coupon? coupon,
        bool isAuthenticated,
        IReadOnlyCollection<LinePricingContext> contexts,
        decimal subtotalBeforeDiscount)
    {
        return condition.ConditionType switch
        {
            PromotionConditionType.MinSubtotal => EvaluateDecimalCondition(subtotalBeforeDiscount, condition.Operator, condition.Value),
            PromotionConditionType.MinQuantity => EvaluateIntCondition(contexts.Sum(context => context.Quantity), condition.Operator, condition.Value),
            PromotionConditionType.CustomerLoggedIn => EvaluateBooleanCondition(isAuthenticated, condition.Operator, condition.Value),
            PromotionConditionType.CategoryInCart => contexts.Any(context => ContextContainsCategory(context, condition.Value)),
            PromotionConditionType.VariantInCart => contexts.Any(context => ContextContainsVariant(context, condition.Value)),
            PromotionConditionType.CouponRequired => coupon is not null && CouponMatches(coupon, condition.Value),
            _ => false,
        };
    }

    private PromotionEvaluation EvaluatePromotion(
        Promotion promotion,
        Coupon? coupon,
        IReadOnlyCollection<LinePricingContext> contexts,
        decimal subtotalReferenceAmount,
        ShippingPriceSelection? shipping)
    {
        var applications = new List<PricingDiscountApplication>();
        var benefit = promotion.Benefits.First();

        if (IsLinePromotion(promotion))
        {
            foreach (var context in contexts)
            {
                if (!PromotionAppliesToLine(promotion, context))
                {
                    continue;
                }

                var amount = CalculateLineDiscountAmount(benefit, context);
                if (amount > 0m)
                {
                    applications.Add(CreateDiscountApplication(promotion, coupon, PromotionScopeType.Product, context.VariantId, amount));
                }
            }
        }
        else if (IsShippingPromotion(promotion) && shipping is not null)
        {
            var amount = CalculateShippingDiscountAmount(benefit, shipping.PriceAmount);
            if (amount > 0m)
            {
                applications.Add(CreateDiscountApplication(promotion, coupon, PromotionScopeType.Shipping, null, amount));
            }
        }
        else if (IsCartPromotion(promotion))
        {
            var amount = CalculateCartDiscountAmount(benefit, subtotalReferenceAmount);
            if (amount > 0m)
            {
                applications.Add(CreateDiscountApplication(promotion, coupon, PromotionScopeType.Cart, null, amount));
            }
        }

        return new PromotionEvaluation(
            promotion.Id,
            promotion.Priority,
            applications,
            Round(applications.Sum(application => application.Amount)));
    }

    private static decimal CalculateLineDiscountAmount(PromotionBenefit benefit, LinePricingContext context)
    {
        var baseLineTotal = Round(context.BaseUnitPriceAmount * context.Quantity);
        return ApplyDiscountCap(benefit, benefit.BenefitType switch
        {
            PromotionBenefitType.PercentageOff => Round(baseLineTotal * (benefit.ValuePercent!.Value / 100m)),
            PromotionBenefitType.FixedAmountOff => Round((benefit.ValueAmount ?? 0m) * (benefit.ApplyPerUnit ? context.Quantity : 1)),
            PromotionBenefitType.FixedPrice => Math.Max(0m, Round((context.BaseUnitPriceAmount - (benefit.ValueAmount ?? 0m)) * context.Quantity)),
            _ => 0m,
        });
    }

    private static decimal CalculateCartDiscountAmount(PromotionBenefit benefit, decimal subtotalAmount)
    {
        return ApplyDiscountCap(benefit, benefit.BenefitType switch
        {
            PromotionBenefitType.PercentageOff => Round(subtotalAmount * (benefit.ValuePercent!.Value / 100m)),
            PromotionBenefitType.FixedAmountOff => Round(benefit.ValueAmount ?? 0m),
            PromotionBenefitType.FixedPrice => Math.Max(0m, Round(subtotalAmount - (benefit.ValueAmount ?? 0m))),
            _ => 0m,
        });
    }

    private static decimal CalculateShippingDiscountAmount(PromotionBenefit benefit, decimal shippingAmount)
    {
        return ApplyDiscountCap(benefit, benefit.BenefitType switch
        {
            PromotionBenefitType.FreeShipping => Round(shippingAmount),
            PromotionBenefitType.PercentageOff => Round(shippingAmount * (benefit.ValuePercent!.Value / 100m)),
            PromotionBenefitType.FixedAmountOff => Round(benefit.ValueAmount ?? 0m),
            PromotionBenefitType.FixedPrice => Math.Max(0m, Round(shippingAmount - (benefit.ValueAmount ?? 0m))),
            _ => 0m,
        });
    }

    private static decimal ApplyDiscountCap(PromotionBenefit benefit, decimal amount)
    {
        var normalized = Math.Max(0m, Round(amount));
        return benefit.MaxDiscountAmount is null
            ? normalized
            : Math.Min(normalized, Round(benefit.MaxDiscountAmount.Value));
    }

    private static PricingDiscountApplication CreateDiscountApplication(
        Promotion promotion,
        Coupon? coupon,
        PromotionScopeType scopeType,
        Guid? targetLineVariantId,
        decimal amount)
    {
        var sourceType = coupon is null ? "Promotion" : "Coupon";
        var sourceId = coupon?.Id ?? promotion.Id;

        return new PricingDiscountApplication(
            sourceType,
            sourceId,
            promotion.Id,
            coupon?.Id,
            scopeType.ToString(),
            targetLineVariantId,
            promotion.Name,
            Round(amount),
            "EUR",
            coupon?.Code);
    }

    private static bool IsLinePromotion(Promotion promotion)
    {
        return promotion.Scopes.Any(scope => scope.ScopeType is PromotionScopeType.Product or PromotionScopeType.Variant or PromotionScopeType.Brand or PromotionScopeType.Category);
    }

    private static bool IsCartPromotion(Promotion promotion)
    {
        return promotion.Scopes.Any(scope => scope.ScopeType == PromotionScopeType.Cart);
    }

    private static bool IsShippingPromotion(Promotion promotion)
    {
        return promotion.Scopes.Any(scope => scope.ScopeType == PromotionScopeType.Shipping) ||
               promotion.Type == PromotionType.FreeShipping ||
               promotion.Benefits.Any(benefit => benefit.BenefitType == PromotionBenefitType.FreeShipping);
    }

    private static bool PromotionAppliesToLine(Promotion promotion, LinePricingContext context)
    {
        return promotion.Scopes.Any(scope => ScopeMatches(scope, context));
    }

    private static bool ScopeMatches(PromotionScope scope, LinePricingContext context)
    {
        return scope.ScopeType switch
        {
            PromotionScopeType.Product => scope.TargetId == context.ProductId,
            PromotionScopeType.Variant => scope.TargetId == context.VariantId,
            PromotionScopeType.Brand => scope.TargetId == context.BrandId,
            PromotionScopeType.Category => ContextMatchesCategory(context, scope.TargetId),
            _ => false,
        };
    }

    private static bool ContextMatchesCategory(LinePricingContext context, Guid? targetId)
    {
        if (targetId is null)
        {
            return false;
        }

        if (context.DefaultCategoryId == targetId.Value)
        {
            return true;
        }

        return context.CategoryBreadcrumbIds.Contains(targetId.Value);
    }

    private static bool ContextContainsCategory(LinePricingContext context, string rawValue)
    {
        var normalized = rawValue.Trim();
        if (Guid.TryParse(normalized, out var categoryId))
        {
            return ContextMatchesCategory(context, categoryId);
        }

        if (!string.IsNullOrWhiteSpace(context.CategorySlug) &&
            string.Equals(context.CategorySlug, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return context.CategoryBreadcrumbSlugs.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContextContainsVariant(LinePricingContext context, string rawValue)
    {
        var normalized = rawValue.Trim();
        return Guid.TryParse(normalized, out var variantId)
            ? context.VariantId == variantId
            : string.Equals(context.Sku, normalized, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CouponMatches(Coupon coupon, string rawValue)
    {
        return string.IsNullOrWhiteSpace(rawValue) ||
               string.Equals(coupon.Code, rawValue.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateDecimalCondition(decimal actual, PromotionConditionOperator @operator, string rawValue)
    {
        if (!decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var expected))
        {
            return false;
        }

        return @operator switch
        {
            PromotionConditionOperator.Gte => actual >= expected,
            PromotionConditionOperator.Eq => actual == expected,
            _ => false,
        };
    }

    private static bool EvaluateIntCondition(int actual, PromotionConditionOperator @operator, string rawValue)
    {
        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var expected))
        {
            return false;
        }

        return @operator switch
        {
            PromotionConditionOperator.Gte => actual >= expected,
            PromotionConditionOperator.Eq => actual == expected,
            _ => false,
        };
    }

    private static bool EvaluateBooleanCondition(bool actual, PromotionConditionOperator @operator, string rawValue)
    {
        var expected = string.IsNullOrWhiteSpace(rawValue) || bool.Parse(rawValue);
        return @operator switch
        {
            PromotionConditionOperator.Eq => actual == expected,
            _ => actual == expected,
        };
    }

    private async Task<T?> GetCachedAsync<T>(string key, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await distributedCache.GetStringAsync(key, cancellationToken);
            return string.IsNullOrWhiteSpace(payload)
                ? default
                : JsonSerializer.Deserialize<T>(payload, JsonOptions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed reading pricing cache entry {CacheKey}.", key);
            return default;
        }
    }

    private async Task SetCachedAsync<T>(string key, T value, int ttlSeconds, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(value, JsonOptions);
            await distributedCache.SetStringAsync(
                key,
                payload,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(5, ttlSeconds)),
                },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed writing pricing cache entry {CacheKey}.", key);
        }
    }

    private static string BuildCartCacheKey(CartPricingRequest request)
    {
        var builder = new StringBuilder();
        builder.Append(request.CustomerId?.Trim() ?? "anonymous");
        builder.Append('|').Append(request.IsAuthenticated ? '1' : '0');
        builder.Append('|').Append(request.CouponCode?.Trim().ToUpperInvariant() ?? string.Empty);
        builder.Append('|').Append(request.Shipping?.ShippingMethodCode?.Trim().ToLowerInvariant() ?? string.Empty);
        builder.Append('|').Append(request.Shipping?.PriceAmount.ToString("0.00", CultureInfo.InvariantCulture) ?? "0.00");

        foreach (var line in request.Lines.OrderBy(line => line.VariantId).ThenBy(line => line.ProductId))
        {
            builder.Append('|').Append(line.ProductId.ToString("D"));
            builder.Append(':').Append(line.VariantId.ToString("D"));
            builder.Append(':').Append(line.Quantity.ToString(CultureInfo.InvariantCulture));
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return $"pricing:cart:{Convert.ToHexString(bytes)}";
    }

    private static decimal Round(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private sealed record CouponResolution(Coupon Coupon, Promotion Promotion);

    private sealed record LinePricingContext(
        Guid ProductId,
        Guid VariantId,
        int Quantity,
        ProductSnapshot Product,
        ProductVariantSnapshot Variant,
        decimal BaseUnitPriceAmount,
        decimal? CompareAtPriceAmount,
        string Currency)
    {
        public string? Sku => Variant.Sku;

        public Guid? BrandId => Product.Brand?.Id;

        public Guid? DefaultCategoryId => Product.DefaultCategoryId;

        public string? CategorySlug => Product.CategorySlug;

        public IReadOnlyCollection<Guid> CategoryBreadcrumbIds => Product.CategoryBreadcrumbs.Select(item => item.Id).ToArray();

        public IReadOnlyCollection<string> CategoryBreadcrumbSlugs => Product.CategoryBreadcrumbs
            .Select(item => item.Slug)
            .Where(slug => !string.IsNullOrWhiteSpace(slug))
            .Cast<string>()
            .ToArray();
    }

    private sealed record PromotionEvaluation(
        Guid PromotionId,
        int Priority,
        IReadOnlyCollection<PricingDiscountApplication> Applications,
        decimal TotalDiscountAmount);
}
