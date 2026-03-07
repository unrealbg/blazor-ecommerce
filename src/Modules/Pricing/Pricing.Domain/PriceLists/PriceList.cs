using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Pricing.Domain.PriceLists;

public sealed class PriceList : AggregateRoot<Guid>
{
    private PriceList()
    {
    }

    private PriceList(
        Guid id,
        string name,
        string code,
        string currency,
        bool isDefault,
        bool isActive,
        int priority,
        DateTime createdAtUtc)
    {
        Id = id;
        Name = name;
        Code = code;
        Currency = currency;
        IsDefault = isDefault;
        IsActive = isActive;
        Priority = priority;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public int Priority { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static Result<PriceList> Create(
        string name,
        string code,
        string currency,
        bool isDefault,
        bool isActive,
        int priority,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<PriceList>.Failure(new Error("pricing.price_list.name.required", "Price list name is required."));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<PriceList>.Failure(new Error("pricing.price_list.code.required", "Price list code is required."));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result<PriceList>.Failure(new Error("pricing.price_list.currency.required", "Price list currency is required."));
        }

        return Result<PriceList>.Success(new PriceList(
            Guid.NewGuid(),
            name.Trim(),
            code.Trim().ToLowerInvariant(),
            currency.Trim().ToUpperInvariant(),
            isDefault,
            isActive,
            priority,
            createdAtUtc));
    }

    public Result Update(string name, string currency, bool isDefault, bool isActive, int priority, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(new Error("pricing.price_list.name.required", "Price list name is required."));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return Result.Failure(new Error("pricing.price_list.currency.required", "Price list currency is required."));
        }

        Name = name.Trim();
        Currency = currency.Trim().ToUpperInvariant();
        IsDefault = isDefault;
        IsActive = isActive;
        Priority = priority;
        UpdatedAtUtc = updatedAtUtc;
        return Result.Success();
    }
}
