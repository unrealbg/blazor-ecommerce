using Inventory.Domain.Stock;

namespace Inventory.Tests;

public sealed class StockReservationTests
{
    [Fact]
    public void Create_Should_Fail_When_CartAndCustomerAreMissing()
    {
        var now = DateTime.UtcNow;

        var result = StockReservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-1",
            cartId: null,
            customerId: null,
            quantity: 1,
            reservationToken: "token-1",
            expiresAtUtc: now.AddMinutes(10),
            createdAtUtc: now);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.reservation.owner.required", result.Error.Code);
    }

    [Fact]
    public void Consume_Should_SetStatusConsumed_AndOrderId()
    {
        var reservation = CreateReservation();
        var orderId = Guid.NewGuid();

        var result = reservation.Consume(orderId, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(StockReservationStatus.Consumed, reservation.Status);
        Assert.Equal(orderId, reservation.OrderId);
    }

    [Fact]
    public void Release_Should_SetStatusReleased()
    {
        var reservation = CreateReservation();

        var result = reservation.Release(DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(StockReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public void Expire_Should_SetStatusExpired()
    {
        var reservation = CreateReservation();

        var result = reservation.Expire(DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(StockReservationStatus.Expired, reservation.Status);
    }

    [Fact]
    public void RefreshExpiration_Should_Fail_When_ReservationIsNotActive()
    {
        var reservation = CreateReservation();
        reservation.Release(DateTime.UtcNow);

        var result = reservation.RefreshExpiration(DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.reservation.not_active", result.Error.Code);
    }

    private static StockReservation CreateReservation()
    {
        var now = DateTime.UtcNow;
        var result = StockReservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SKU-1",
            cartId: "cart-1",
            customerId: null,
            quantity: 2,
            reservationToken: Guid.NewGuid().ToString("N"),
            expiresAtUtc: now.AddMinutes(10),
            createdAtUtc: now);

        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
