using BuildingBlocks.Domain.Shared;
using Catalog.Domain.Products;

namespace Catalog.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_Should_ReturnSuccess_When_InputIsValid()
    {
        var moneyResult = Money.Create("usd", 12.345m);
        var result = Product.Create("Product A", moneyResult.Value, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(12.34m, result.Value.Price.Amount);
        Assert.Equal("USD", result.Value.Price.Currency);
    }
}
