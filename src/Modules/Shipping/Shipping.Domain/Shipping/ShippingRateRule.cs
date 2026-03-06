using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Shipping.Domain.Shipping;

public sealed class ShippingRateRule : AggregateRoot<Guid>
{
    private ShippingRateRule()
    {
    }

    private ShippingRateRule(
        Guid id,
        Guid shippingMethodId,
        Guid shippingZoneId,
        decimal? minOrderAmount,
        decimal? maxOrderAmount,
        decimal? minWeightKg,
        decimal? maxWeightKg,
        decimal priceAmount,
        decimal? freeShippingThresholdAmount,
        string currency,
        DateTime createdAtUtc)
    {
        Id = id;
        ShippingMethodId = shippingMethodId;
        ShippingZoneId = shippingZoneId;
        MinOrderAmount = minOrderAmount;
        MaxOrderAmount = maxOrderAmount;
        MinWeightKg = minWeightKg;
        MaxWeightKg = maxWeightKg;
        PriceAmount = priceAmount;
        FreeShippingThresholdAmount = freeShippingThresholdAmount;
        Currency = currency;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = 0;
    }

    public Guid ShippingMethodId { get; private set; }

    public Guid ShippingZoneId { get; private set; }

    public decimal? MinOrderAmount { get; private set; }

    public decimal? MaxOrderAmount { get; private set; }

    public decimal? MinWeightKg { get; private set; }

    public decimal? MaxWeightKg { get; private set; }

    public decimal PriceAmount { get; private set; }

    public decimal? FreeShippingThresholdAmount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long RowVersion { get; private set; }

    public static Result<ShippingRateRule> Create(
        Guid shippingMethodId,
        Guid shippingZoneId,
        decimal? minOrderAmount,
        decimal? maxOrderAmount,
        decimal? minWeightKg,
        decimal? maxWeightKg,
        decimal priceAmount,
        decimal? freeShippingThresholdAmount,
        string currency,
        DateTime createdAtUtc)
    {
        var validateResult = Validate(
            shippingMethodId,
            shippingZoneId,
            minOrderAmount,
            maxOrderAmount,
            minWeightKg,
            maxWeightKg,
            priceAmount,
            freeShippingThresholdAmount,
            currency);
        if (validateResult.IsFailure)
        {
            return Result<ShippingRateRule>.Failure(validateResult.Error);
        }

        return Result<ShippingRateRule>.Success(new ShippingRateRule(
            Guid.NewGuid(),
            shippingMethodId,
            shippingZoneId,
            minOrderAmount,
            maxOrderAmount,
            minWeightKg,
            maxWeightKg,
            priceAmount,
            freeShippingThresholdAmount,
            currency.Trim().ToUpperInvariant(),
            createdAtUtc));
    }

    public Result Update(
        decimal? minOrderAmount,
        decimal? maxOrderAmount,
        decimal? minWeightKg,
        decimal? maxWeightKg,
        decimal priceAmount,
        decimal? freeShippingThresholdAmount,
        string currency,
        bool isActive,
        DateTime updatedAtUtc)
    {
        var validateResult = Validate(
            ShippingMethodId,
            ShippingZoneId,
            minOrderAmount,
            maxOrderAmount,
            minWeightKg,
            maxWeightKg,
            priceAmount,
            freeShippingThresholdAmount,
            currency);
        if (validateResult.IsFailure)
        {
            return validateResult;
        }

        MinOrderAmount = minOrderAmount;
        MaxOrderAmount = maxOrderAmount;
        MinWeightKg = minWeightKg;
        MaxWeightKg = maxWeightKg;
        PriceAmount = priceAmount;
        FreeShippingThresholdAmount = freeShippingThresholdAmount;
        Currency = currency.Trim().ToUpperInvariant();
        IsActive = isActive;
        Touch(updatedAtUtc);

        return Result.Success();
    }

    public bool Matches(decimal subtotalAmount, decimal? totalWeightKg)
    {
        if (!IsActive)
        {
            return false;
        }

        if (MinOrderAmount is not null && subtotalAmount < MinOrderAmount.Value)
        {
            return false;
        }

        if (MaxOrderAmount is not null && subtotalAmount > MaxOrderAmount.Value)
        {
            return false;
        }

        if (totalWeightKg is not null)
        {
            if (MinWeightKg is not null && totalWeightKg < MinWeightKg.Value)
            {
                return false;
            }

            if (MaxWeightKg is not null && totalWeightKg > MaxWeightKg.Value)
            {
                return false;
            }
        }

        return true;
    }

    public decimal ResolvePrice(decimal subtotalAmount)
    {
        if (FreeShippingThresholdAmount is not null && subtotalAmount >= FreeShippingThresholdAmount.Value)
        {
            return 0m;
        }

        return PriceAmount;
    }

    private static Result Validate(
        Guid shippingMethodId,
        Guid shippingZoneId,
        decimal? minOrderAmount,
        decimal? maxOrderAmount,
        decimal? minWeightKg,
        decimal? maxWeightKg,
        decimal priceAmount,
        decimal? freeShippingThresholdAmount,
        string currency)
    {
        if (shippingMethodId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "shipping.rule.method.required",
                "Shipping method id is required."));
        }

        if (shippingZoneId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "shipping.rule.zone.required",
                "Shipping zone id is required."));
        }

        if (priceAmount < 0m)
        {
            return Result.Failure(new Error(
                "shipping.rule.price.invalid",
                "Shipping rule price cannot be negative."));
        }

        if (minOrderAmount is < 0m || maxOrderAmount is < 0m)
        {
            return Result.Failure(new Error(
                "shipping.rule.order_amount.invalid",
                "Order amount constraints cannot be negative."));
        }

        if (minOrderAmount is not null && maxOrderAmount is not null && minOrderAmount > maxOrderAmount)
        {
            return Result.Failure(new Error(
                "shipping.rule.order_amount.invalid",
                "Min order amount cannot be greater than max order amount."));
        }

        if (minWeightKg is < 0m || maxWeightKg is < 0m)
        {
            return Result.Failure(new Error(
                "shipping.rule.weight.invalid",
                "Weight constraints cannot be negative."));
        }

        if (minWeightKg is not null && maxWeightKg is not null && minWeightKg > maxWeightKg)
        {
            return Result.Failure(new Error(
                "shipping.rule.weight.invalid",
                "Min weight cannot be greater than max weight."));
        }

        if (freeShippingThresholdAmount is < 0m)
        {
            return Result.Failure(new Error(
                "shipping.rule.free_threshold.invalid",
                "Free shipping threshold cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result.Failure(new Error(
                "shipping.rule.currency.invalid",
                "Shipping rule currency must be a 3-letter code."));
        }

        return Result.Success();
    }

    private void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        RowVersion++;
    }
}
