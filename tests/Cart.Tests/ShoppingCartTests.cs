using BuildingBlocks.Domain.Shared;
using Cart.Domain.Carts;

namespace Cart.Tests;

public sealed class ShoppingCartTests
{
    [Fact]
    public void Checkout_Should_RaiseDomainEvent_When_CartIsOpen()
    {
        var cart = ShoppingCart.Create(Guid.NewGuid(), DateTime.UtcNow).Value;
        var money = Money.Create("EUR", 20m).Value;

        var result = cart.Checkout(money, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Single(cart.DomainEvents);
    }
}
