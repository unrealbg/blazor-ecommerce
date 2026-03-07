using BuildingBlocks.Domain.Shared;
using Cart.Domain.Carts;

namespace Cart.Tests;

public sealed class CartAggregateTests
{
    [Fact]
    public void Create_Should_ReturnFailure_When_CustomerIdMissing()
    {
        var result = Cart.Domain.Carts.Cart.Create(string.Empty);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddItem_Should_AddNewLine_When_VariantIsNew()
    {
        var cart = Cart.Domain.Carts.Cart.Create("customer-1").Value;
        var price = Money.Create("EUR", 10m).Value;
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var result = cart.AddItem(
            productId,
            variantId,
            "SKU-1",
            "Product A",
            "Red / M",
            "{\"Color\":\"Red\"}",
            "/images/product-a.jpg",
            price,
            2);

        Assert.True(result.IsSuccess);
        Assert.Single(cart.Lines);
        Assert.Equal(productId, cart.Lines.Single().ProductId);
        Assert.Equal(variantId, cart.Lines.Single().VariantId);
        Assert.Equal(2, cart.Lines.Single().Quantity);
    }

    [Fact]
    public void AddItem_Should_MergeQuantity_When_VariantAlreadyExists()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var cart = Cart.Domain.Carts.Cart.Create("customer-1").Value;
        var price = Money.Create("EUR", 10m).Value;

        cart.AddItem(productId, variantId, "SKU-1", "Product A", "Red / M", null, null, price, 1);
        var result = cart.AddItem(productId, variantId, "SKU-1", "Product A", "Red / M", null, null, price, 3);

        Assert.True(result.IsSuccess);
        Assert.Single(cart.Lines);
        Assert.Equal(4, cart.Lines.Single().Quantity);
    }

    [Fact]
    public void AddItem_Should_ReturnFailure_When_VariantIdMissing()
    {
        var cart = Cart.Domain.Carts.Cart.Create("customer-1").Value;
        var price = Money.Create("EUR", 10m).Value;

        var result = cart.AddItem(Guid.NewGuid(), Guid.Empty, "SKU-1", "Product A", null, null, null, price, 1);

        Assert.True(result.IsFailure);
        Assert.Equal("cart.item.variant.required", result.Error.Code);
    }

    [Fact]
    public void Clear_Should_RemoveAllLines()
    {
        var cart = Cart.Domain.Carts.Cart.Create("customer-1").Value;
        var price = Money.Create("EUR", 10m).Value;
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Product A", null, null, null, price, 1);

        cart.Clear();

        Assert.Empty(cart.Lines);
    }
}
