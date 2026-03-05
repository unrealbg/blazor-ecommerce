using BuildingBlocks.Domain.Shared;

namespace Catalog.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void Create_Should_NormalizeCurrency_And_RoundAmount()
    {
        var result = Money.Create("usd", 10.125m);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.Value.Currency);
        Assert.Equal(10.12m, result.Value.Amount);
    }

    [Fact]
    public void Round_Should_UseBankersRounding_When_MidpointToEven()
    {
        Assert.Equal(2.34m, Money.Round(2.345m));
        Assert.Equal(2.36m, Money.Round(2.355m));
    }

    [Fact]
    public void Equality_Should_ReturnTrue_ForSameCurrencyAndRoundedAmount()
    {
        var first = Money.Create("EUR", 5.005m).Value;
        var second = Money.Create("eur", 5.004m).Value;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equality_Should_ReturnFalse_When_CurrencyDiffers()
    {
        var usd = Money.Create("USD", 5m).Value;
        var eur = Money.Create("EUR", 5m).Value;

        Assert.NotEqual(usd, eur);
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_CurrencyIsInvalid()
    {
        var result = Money.Create("US", 5m);

        Assert.True(result.IsFailure);
    }
}
