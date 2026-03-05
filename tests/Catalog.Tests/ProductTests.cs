using BuildingBlocks.Domain.Shared;
using Catalog.Domain.Products;

namespace Catalog.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_Should_ReturnSuccess_When_InputIsValid()
    {
        var money = Money.Create("usd", 12.345m).Value;

        var result = Product.Create("Product A", "product-a", "Description", money, true);

        Assert.True(result.IsSuccess);
        Assert.Equal("Product A", result.Value.Name);
        Assert.Equal("product-a", result.Value.Slug);
        Assert.Equal("Description", result.Value.Description);
        Assert.Equal(12.34m, result.Value.Price.Amount);
        Assert.Equal("USD", result.Value.Price.Currency);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public void Create_Should_ReturnFailure_When_NameMissing()
    {
        var money = Money.Create("USD", 10m).Value;

        var result = Product.Create(string.Empty, "product-a", null, money, true);

        Assert.True(result.IsFailure);
    }
}
