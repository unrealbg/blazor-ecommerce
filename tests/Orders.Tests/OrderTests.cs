using BuildingBlocks.Domain.Shared;
using Orders.Domain.Orders;

namespace Orders.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Create_Should_ReturnFailure_When_CartIdMissing()
    {
        var total = Money.Create("USD", 30m).Value;
        var result = Order.Create(Guid.Empty, Guid.NewGuid(), total, DateTime.UtcNow);

        Assert.True(result.IsFailure);
    }
}
