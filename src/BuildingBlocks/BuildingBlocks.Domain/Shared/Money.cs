using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Domain.Shared;

public sealed class Money : ValueObject
{
    private const int ExpectedCurrencyLength = 3;

    private Money()
    {
        Currency = "USD";
    }

    private Money(string currency, decimal amount)
    {
        Currency = currency;
        Amount = Round(amount);
    }

    public string Currency { get; private set; }

    public decimal Amount { get; private set; }

    public static Result<Money> Create(string currency, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != ExpectedCurrencyLength)
        {
            return Result<Money>.Failure(
                new Error("money.currency.invalid", "Currency must be a 3-letter ISO code."));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        return Result<Money>.Success(new Money(normalizedCurrency, amount));
    }

    public static decimal Round(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.ToEven);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Currency;
        yield return Amount;
    }
}
