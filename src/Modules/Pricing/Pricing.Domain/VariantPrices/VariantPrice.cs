using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Pricing.Domain.Events;

namespace Pricing.Domain.VariantPrices;

public sealed class VariantPrice : AggregateRoot<Guid>
{
    private VariantPrice()
    {
    }

    private VariantPrice(
        Guid id,
        Guid priceListId,
        Guid variantId,
        decimal basePriceAmount,
        decimal? compareAtPriceAmount,
        string currency,
        bool isActive,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        DateTime createdAtUtc)
    {
        Id = id;
        PriceListId = priceListId;
        VariantId = variantId;
        BasePriceAmount = basePriceAmount;
        CompareAtPriceAmount = compareAtPriceAmount;
        Currency = currency;
        IsActive = isActive;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid PriceListId { get; private set; }

    public Guid VariantId { get; private set; }

    public decimal BasePriceAmount { get; private set; }

    public decimal? CompareAtPriceAmount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime? ValidFromUtc { get; private set; }

    public DateTime? ValidToUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<VariantPrice> Create(
        Guid priceListId,
        Guid variantId,
        decimal basePriceAmount,
        decimal? compareAtPriceAmount,
        string currency,
        bool isActive,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        DateTime createdAtUtc)
    {
        var validation = Validate(priceListId, variantId, basePriceAmount, compareAtPriceAmount, currency, validFromUtc, validToUtc);
        if (validation.IsFailure)
        {
            return Result<VariantPrice>.Failure(validation.Error);
        }

        var variantPrice = new VariantPrice(
            Guid.NewGuid(),
            priceListId,
            variantId,
            decimal.Round(basePriceAmount, 2, MidpointRounding.AwayFromZero),
            compareAtPriceAmount is null ? null : decimal.Round(compareAtPriceAmount.Value, 2, MidpointRounding.AwayFromZero),
            currency.Trim().ToUpperInvariant(),
            isActive,
            validFromUtc,
            validToUtc,
            createdAtUtc);

        variantPrice.RaiseDomainEvent(new VariantPriceChanged(variantPrice.VariantId, variantPrice.Id));
        return Result<VariantPrice>.Success(variantPrice);
    }

    public Result Update(
        decimal basePriceAmount,
        decimal? compareAtPriceAmount,
        string currency,
        bool isActive,
        DateTime? validFromUtc,
        DateTime? validToUtc,
        DateTime updatedAtUtc)
    {
        var validation = Validate(PriceListId, VariantId, basePriceAmount, compareAtPriceAmount, currency, validFromUtc, validToUtc);
        if (validation.IsFailure)
        {
            return validation;
        }

        BasePriceAmount = decimal.Round(basePriceAmount, 2, MidpointRounding.AwayFromZero);
        CompareAtPriceAmount = compareAtPriceAmount is null
            ? null
            : decimal.Round(compareAtPriceAmount.Value, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim().ToUpperInvariant();
        IsActive = isActive;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
        UpdatedAtUtc = updatedAtUtc;
        RaiseDomainEvent(new VariantPriceChanged(VariantId, Id));
        return Result.Success();
    }

    public bool IsActiveAt(DateTime utcNow)
    {
        if (!IsActive)
        {
            return false;
        }

        if (ValidFromUtc is not null && utcNow < ValidFromUtc.Value)
        {
            return false;
        }

        return ValidToUtc is null || utcNow <= ValidToUtc.Value;
    }

    private static Result Validate(
        Guid priceListId,
        Guid variantId,
        decimal basePriceAmount,
        decimal? compareAtPriceAmount,
        string currency,
        DateTime? validFromUtc,
        DateTime? validToUtc)
    {
        if (priceListId == Guid.Empty)
        {
            return Result.Failure(new Error("pricing.price_list.required", "Price list id is required."));
        }

        if (variantId == Guid.Empty)
        {
            return Result.Failure(new Error("pricing.variant.required", "Variant id is required."));
        }

        if (basePriceAmount < 0m)
        {
            return Result.Failure(new Error("pricing.base_price.invalid", "Base price cannot be negative."));
        }

        if (compareAtPriceAmount is < 0m)
        {
            return Result.Failure(new Error("pricing.compare_at_price.invalid", "Compare-at price cannot be negative."));
        }

        if (compareAtPriceAmount is not null && compareAtPriceAmount.Value < basePriceAmount)
        {
            return Result.Failure(new Error(
                "pricing.compare_at_price.lower_than_base",
                "Compare-at price cannot be lower than the base price."));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure(new Error("pricing.currency.required", "Currency is required."));
        }

        if (validFromUtc is not null && validToUtc is not null && validFromUtc > validToUtc)
        {
            return Result.Failure(new Error("pricing.price_window.invalid", "Price validity window is invalid."));
        }

        return Result.Success();
    }
}
