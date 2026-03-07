using Pricing.Domain.Coupons;
using Pricing.Domain.Events;
using Pricing.Domain.PriceLists;
using Pricing.Domain.Promotions;
using Pricing.Domain.Redemptions;
using Pricing.Domain.VariantPrices;

namespace Pricing.Tests;

public sealed class PricingDomainTests
{
    [Fact]
    public void PriceListCreate_ShouldFail_WhenNameMissing()
    {
        var result = PriceList.Create(string.Empty, "default", "eur", true, true, 100, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list.name.required", result.Error.Code);
    }

    [Fact]
    public void PriceListCreate_ShouldNormalizeCodeAndCurrency()
    {
        var result = PriceList.Create("Default", " DEFAULT ", " eur ", true, true, 100, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal("default", result.Value.Code);
        Assert.Equal("EUR", result.Value.Currency);
    }

    [Fact]
    public void PriceListUpdate_ShouldFail_WhenCurrencyMissing()
    {
        var priceList = PriceList.Create("Default", "default", "EUR", true, true, 100, DateTime.UtcNow).Value;

        var result = priceList.Update("Updated", string.Empty, false, true, 10, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_list.currency.required", result.Error.Code);
    }

    [Fact]
    public void VariantPriceCreate_ShouldFail_WhenCompareAtLowerThanBase()
    {
        var result = VariantPrice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            99m,
            "EUR",
            true,
            null,
            null,
            DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.compare_at_price.lower_than_base", result.Error.Code);
    }

    [Fact]
    public void VariantPriceCreate_ShouldFail_WhenWindowIsInvalid()
    {
        var now = DateTime.UtcNow;
        var result = VariantPrice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            160m,
            "EUR",
            true,
            now.AddDays(2),
            now.AddDays(1),
            now);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.price_window.invalid", result.Error.Code);
    }

    [Fact]
    public void VariantPriceCreate_ShouldRoundValuesAndRaiseEvent()
    {
        var result = VariantPrice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            19.995m,
            29.999m,
            "eur",
            true,
            null,
            null,
            DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(20.00m, result.Value.BasePriceAmount);
        Assert.Equal(30.00m, result.Value.CompareAtPriceAmount);
        Assert.Equal("EUR", result.Value.Currency);
        Assert.Contains(result.Value.DomainEvents, domainEvent => domainEvent is VariantPriceChanged);
    }

    [Fact]
    public void VariantPriceIsActiveAt_ShouldRespectWindow()
    {
        var now = new DateTime(2026, 3, 7, 12, 0, 0, DateTimeKind.Utc);
        var variantPrice = VariantPrice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            20m,
            30m,
            "EUR",
            true,
            now.AddHours(-1),
            now.AddHours(1),
            now).Value;

        Assert.True(variantPrice.IsActiveAt(now));
        Assert.False(variantPrice.IsActiveAt(now.AddHours(2)));
    }

    [Fact]
    public void VariantPriceUpdate_ShouldRaiseChangedEvent()
    {
        var variantPrice = VariantPrice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            20m,
            30m,
            "EUR",
            true,
            null,
            null,
            DateTime.UtcNow).Value;

        variantPrice.ClearDomainEvents();
        var result = variantPrice.Update(21m, 31m, "EUR", true, null, null, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Contains(variantPrice.DomainEvents, domainEvent => domainEvent is VariantPriceChanged);
    }

    [Fact]
    public void PromotionCreate_ShouldFail_WhenBenefitsMissing()
    {
        var result = Promotion.Create(
            "Spring",
            null,
            PromotionType.PercentageOff,
            null,
            10,
            false,
            true,
            null,
            null,
            null,
            null,
            [new PromotionScopeData(PromotionScopeType.Cart, null)],
            [],
            [],
            DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.promotion.benefit.required", result.Error.Code);
    }

    [Fact]
    public void PromotionActivate_ShouldFail_WhenArchived()
    {
        var promotion = CreatePromotion();
        promotion.Archive(DateTime.UtcNow);

        var result = promotion.Activate(DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.promotion.transition.invalid", result.Error.Code);
    }

    [Fact]
    public void PromotionIsActiveAt_ShouldRespectStatusAndWindow()
    {
        var now = new DateTime(2026, 3, 7, 12, 0, 0, DateTimeKind.Utc);
        var promotion = Promotion.Create(
            "Windowed",
            null,
            PromotionType.PercentageOff,
            null,
            5,
            false,
            true,
            now.AddHours(-1),
            now.AddHours(1),
            null,
            null,
            [new PromotionScopeData(PromotionScopeType.Cart, null)],
            [],
            [new PromotionBenefitData(PromotionBenefitType.PercentageOff, null, 10m, null, false)],
            now).Value;

        promotion.Activate(now);

        Assert.True(promotion.IsActiveAt(now));
        Assert.False(promotion.IsActiveAt(now.AddHours(2)));
    }

    [Fact]
    public void PromotionCanRedeemForCustomer_ShouldRespectTotalLimit()
    {
        var promotion = Promotion.Create(
            "Limited",
            null,
            PromotionType.PercentageOff,
            null,
            5,
            false,
            true,
            null,
            null,
            1,
            5,
            [new PromotionScopeData(PromotionScopeType.Cart, null)],
            [],
            [new PromotionBenefitData(PromotionBenefitType.PercentageOff, null, 10m, null, false)],
            DateTime.UtcNow).Value;

        promotion.Activate(DateTime.UtcNow);
        promotion.RegisterRedemption(null, Guid.NewGuid(), "customer-1", 10m, DateTime.UtcNow);

        Assert.False(promotion.CanRedeemForCustomer(0));
    }

    [Fact]
    public void PromotionRegisterRedemption_ShouldIncrementUsageAndRaiseEvent()
    {
        var promotion = CreatePromotion();
        promotion.Activate(DateTime.UtcNow);
        promotion.ClearDomainEvents();

        promotion.RegisterRedemption(null, Guid.NewGuid(), "customer-1", 15m, DateTime.UtcNow);

        Assert.Equal(1, promotion.TimesUsedTotal);
        Assert.Contains(promotion.DomainEvents, domainEvent => domainEvent is PromotionRedeemed);
    }

    [Fact]
    public void CouponCreate_ShouldNormalizeCode()
    {
        var result = Coupon.Create(
            " save10 ",
            null,
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal("SAVE10", result.Value.Code);
    }

    [Fact]
    public void CouponUpdate_ShouldFail_WhenWindowInvalid()
    {
        var now = DateTime.UtcNow;
        var coupon = Coupon.Create("SAVE10", null, Guid.NewGuid(), null, null, null, null, now).Value;

        var result = coupon.Update(null, now.AddDays(2), now.AddDays(1), null, null, now);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.coupon.window.invalid", result.Error.Code);
    }

    [Fact]
    public void CouponDisable_ShouldMakeCouponInactive()
    {
        var coupon = Coupon.Create("SAVE10", null, Guid.NewGuid(), null, null, null, null, DateTime.UtcNow).Value;

        coupon.Disable(DateTime.UtcNow);

        Assert.False(coupon.IsActiveAt(DateTime.UtcNow));
    }

    [Fact]
    public void CouponCanRedeemForCustomer_ShouldRespectPerCustomerLimit()
    {
        var coupon = Coupon.Create("SAVE10", null, Guid.NewGuid(), null, null, 10, 1, DateTime.UtcNow).Value;

        Assert.False(coupon.CanRedeemForCustomer(1));
    }

    [Fact]
    public void CouponRegisterRedemption_ShouldIncrementUsageAndRaiseEvent()
    {
        var coupon = Coupon.Create("SAVE10", null, Guid.NewGuid(), null, null, null, null, DateTime.UtcNow).Value;
        coupon.ClearDomainEvents();

        coupon.RegisterRedemption(Guid.NewGuid(), "customer-1", 12.5m, DateTime.UtcNow);

        Assert.Equal(1, coupon.TimesUsedTotal);
        Assert.Contains(coupon.DomainEvents, domainEvent => domainEvent is CouponRedeemed);
    }

    [Fact]
    public void PromotionRedemptionCreate_ShouldFail_WhenPromotionMissing()
    {
        var result = PromotionRedemption.Create(Guid.Empty, null, Guid.NewGuid(), null, 5m, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.redemption.promotion.required", result.Error.Code);
    }

    [Fact]
    public void PromotionRedemptionCreate_ShouldFail_WhenOrderMissing()
    {
        var result = PromotionRedemption.Create(Guid.NewGuid(), null, Guid.Empty, null, 5m, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.redemption.order.required", result.Error.Code);
    }

    [Fact]
    public void PromotionRedemptionCreate_ShouldFail_WhenDiscountNegative()
    {
        var result = PromotionRedemption.Create(Guid.NewGuid(), null, Guid.NewGuid(), null, -1m, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("pricing.redemption.discount.invalid", result.Error.Code);
    }

    [Fact]
    public void PromotionRedemptionCreate_ShouldTrimCustomerIdAndRoundAmount()
    {
        var result = PromotionRedemption.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            " customer-1 ",
            10.555m,
            DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal("customer-1", result.Value.CustomerId);
        Assert.Equal(10.56m, result.Value.DiscountAmount);
    }

    private static Promotion CreatePromotion()
    {
        return Promotion.Create(
            "Spring",
            "SPRING",
            PromotionType.PercentageOff,
            "Seasonal promotion",
            10,
            false,
            true,
            null,
            null,
            null,
            null,
            [new PromotionScopeData(PromotionScopeType.Cart, null)],
            [],
            [new PromotionBenefitData(PromotionBenefitType.PercentageOff, null, 10m, null, false)],
            DateTime.UtcNow).Value;
    }
}
