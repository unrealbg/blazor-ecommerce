using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Shipping.Domain.Shipping;

public sealed class ShippingMethod : AggregateRoot<Guid>
{
    private ShippingMethod()
    {
    }

    private ShippingMethod(
        Guid id,
        string code,
        string name,
        string? description,
        string provider,
        string type,
        decimal basePriceAmount,
        string currency,
        bool supportsTracking,
        bool supportsPickupPoint,
        int? estimatedMinDays,
        int? estimatedMaxDays,
        int priority,
        DateTime createdAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        Provider = provider;
        Type = type;
        BasePriceAmount = basePriceAmount;
        Currency = currency;
        IsActive = true;
        SupportsTracking = supportsTracking;
        SupportsPickupPoint = supportsPickupPoint;
        EstimatedMinDays = estimatedMinDays;
        EstimatedMaxDays = estimatedMaxDays;
        Priority = priority;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = 0;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public decimal BasePriceAmount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool SupportsTracking { get; private set; }

    public bool SupportsPickupPoint { get; private set; }

    public int? EstimatedMinDays { get; private set; }

    public int? EstimatedMaxDays { get; private set; }

    public int Priority { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long RowVersion { get; private set; }

    public static Result<ShippingMethod> Create(
        string code,
        string name,
        string? description,
        string provider,
        string type,
        decimal basePriceAmount,
        string currency,
        bool supportsTracking,
        bool supportsPickupPoint,
        int? estimatedMinDays,
        int? estimatedMaxDays,
        int priority,
        DateTime createdAtUtc)
    {
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode.IsFailure)
        {
            return Result<ShippingMethod>.Failure(normalizedCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ShippingMethod>.Failure(new Error(
                "shipping.method.name.required",
                "Shipping method name is required."));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result<ShippingMethod>.Failure(new Error(
                "shipping.method.provider.required",
                "Shipping method provider is required."));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            return Result<ShippingMethod>.Failure(new Error(
                "shipping.method.type.required",
                "Shipping method type is required."));
        }

        if (basePriceAmount < 0m)
        {
            return Result<ShippingMethod>.Failure(new Error(
                "shipping.method.base_price.invalid",
                "Base shipping price cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result<ShippingMethod>.Failure(new Error(
                "shipping.method.currency.invalid",
                "Shipping currency must be a 3-letter code."));
        }

        if (estimatedMinDays is < 0 || estimatedMaxDays is < 0)
        {
            return Result<ShippingMethod>.Failure(new Error(
                "shipping.method.eta.invalid",
                "Estimated shipping days cannot be negative."));
        }

        if (estimatedMinDays is not null && estimatedMaxDays is not null && estimatedMinDays > estimatedMaxDays)
        {
            return Result<ShippingMethod>.Failure(new Error(
                "shipping.method.eta.invalid",
                "Estimated min days cannot be greater than max days."));
        }

        return Result<ShippingMethod>.Success(new ShippingMethod(
            Guid.NewGuid(),
            normalizedCode.Value,
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            provider.Trim(),
            type.Trim(),
            basePriceAmount,
            currency.Trim().ToUpperInvariant(),
            supportsTracking,
            supportsPickupPoint,
            estimatedMinDays,
            estimatedMaxDays,
            priority,
            createdAtUtc));
    }

    public Result Update(
        string name,
        string? description,
        string provider,
        string type,
        decimal basePriceAmount,
        string currency,
        bool supportsTracking,
        bool supportsPickupPoint,
        int? estimatedMinDays,
        int? estimatedMaxDays,
        int priority,
        bool isActive,
        DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(new Error(
                "shipping.method.name.required",
                "Shipping method name is required."));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            return Result.Failure(new Error(
                "shipping.method.provider.required",
                "Shipping method provider is required."));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            return Result.Failure(new Error(
                "shipping.method.type.required",
                "Shipping method type is required."));
        }

        if (basePriceAmount < 0m)
        {
            return Result.Failure(new Error(
                "shipping.method.base_price.invalid",
                "Base shipping price cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result.Failure(new Error(
                "shipping.method.currency.invalid",
                "Shipping currency must be a 3-letter code."));
        }

        if (estimatedMinDays is < 0 || estimatedMaxDays is < 0)
        {
            return Result.Failure(new Error(
                "shipping.method.eta.invalid",
                "Estimated shipping days cannot be negative."));
        }

        if (estimatedMinDays is not null && estimatedMaxDays is not null && estimatedMinDays > estimatedMaxDays)
        {
            return Result.Failure(new Error(
                "shipping.method.eta.invalid",
                "Estimated min days cannot be greater than max days."));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Provider = provider.Trim();
        Type = type.Trim();
        BasePriceAmount = basePriceAmount;
        Currency = currency.Trim().ToUpperInvariant();
        SupportsTracking = supportsTracking;
        SupportsPickupPoint = supportsPickupPoint;
        EstimatedMinDays = estimatedMinDays;
        EstimatedMaxDays = estimatedMaxDays;
        Priority = priority;
        IsActive = isActive;
        Touch(updatedAtUtc);

        return Result.Success();
    }

    private static Result<string> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<string>.Failure(new Error(
                "shipping.method.code.required",
                "Shipping method code is required."));
        }

        return Result<string>.Success(code.Trim().ToLowerInvariant());
    }

    private void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        RowVersion++;
    }
}
