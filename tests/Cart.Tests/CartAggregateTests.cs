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
    public void AddItem_Should_AddNewLine_When_ProductIsNew()
    {
        var cart = Cart.Domain.Carts.Cart.Create("customer-1").Value;
        var price = Money.Create("USD", 10m).Value;

        var result = cart.AddItem(Guid.NewGuid(), "Product A", price, 2);

        Assert.True(result.IsSuccess);
        Assert.Single(cart.Lines);
        Assert.Equal(2, cart.Lines.Single().Quantity);
    }

    [Fact]
    public void AddItem_Should_MergeQuantity_When_ProductAlreadyExists()
    {
        var productId = Guid.NewGuid();
        var cart = Cart.Domain.Carts.Cart.Create("customer-1").Value;
        var price = Money.Create("USD", 10m).Value;

        cart.AddItem(productId, "Product A", price, 1);
        var result = cart.AddItem(productId, "Product A", price, 3);

        Assert.True(result.IsSuccess);
        Assert.Single(cart.Lines);
        Assert.Equal(4, cart.Lines.Single().Quantity);
    }

    [Fact]
    public void AddItem_Should_ReturnFailure_When_QuantityInvalid()
    {
        var cart = Cart.Domain.Carts.Cart.Create("customer-1").Value;
        var price = Money.Create("USD", 10m).Value;

        var result = cart.AddItem(Guid.NewGuid(), "Product A", price, 0);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Clear_Should_RemoveAllLines()
    {
        var cart = Cart.Domain.Carts.Cart.Create("customer-1").Value;
        var price = Money.Create("USD", 10m).Value;
        cart.AddItem(Guid.NewGuid(), "Product A", price, 1);

        cart.Clear();

        Assert.Empty(cart.Lines);
    }
}
