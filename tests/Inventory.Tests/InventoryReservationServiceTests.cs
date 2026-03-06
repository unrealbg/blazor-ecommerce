using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Inventory.Tests;

public sealed class InventoryReservationServiceTests
{
    [Fact]
    public async Task SyncCartReservationAsync_Should_CreateReservation_AndReserveStock()
    {
        await using var context = CreateDbContext();
        var stockItem = await SeedTrackedStockItemAsync(context, 5);
        var service = CreateService(context);

        var result = await service.SyncCartReservationAsync("cart-1", stockItem.ProductId, stockItem.Sku, 3, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var reservation = await context.StockReservations.SingleAsync();
        Assert.Equal(StockReservationStatus.Active, reservation.Status);
        Assert.Equal(3, reservation.Quantity);

        var refreshedStockItem = await context.StockItems.SingleAsync();
        Assert.Equal(3, refreshedStockItem.ReservedQuantity);
        Assert.Equal(2, refreshedStockItem.AvailableQuantity);
    }

    [Fact]
    public async Task SyncCartReservationAsync_Should_UpdateQuantityOnExistingReservation()
    {
        await using var context = CreateDbContext();
        var stockItem = await SeedTrackedStockItemAsync(context, 10);
        var service = CreateService(context);

        await service.SyncCartReservationAsync("cart-1", stockItem.ProductId, stockItem.Sku, 4, CancellationToken.None);
        var result = await service.SyncCartReservationAsync("cart-1", stockItem.ProductId, stockItem.Sku, 2, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var reservation = await context.StockReservations.SingleAsync();
        Assert.Equal(2, reservation.Quantity);

        var refreshedStockItem = await context.StockItems.SingleAsync();
        Assert.Equal(2, refreshedStockItem.ReservedQuantity);
    }

    [Fact]
    public async Task SyncCartReservationAsync_Should_Fail_WhenStockIsInsufficient()
    {
        await using var context = CreateDbContext();
        var stockItem = await SeedTrackedStockItemAsync(context, 1);
        var service = CreateService(context);

        var result = await service.SyncCartReservationAsync("cart-1", stockItem.ProductId, stockItem.Sku, 2, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.stock.insufficient", result.Error.Code);
    }

    [Fact]
    public async Task SyncCartReservationAsync_Should_ReleaseReservation_WhenQuantityIsZero()
    {
        await using var context = CreateDbContext();
        var stockItem = await SeedTrackedStockItemAsync(context, 5);
        var service = CreateService(context);

        await service.SyncCartReservationAsync("cart-1", stockItem.ProductId, stockItem.Sku, 2, CancellationToken.None);
        var result = await service.SyncCartReservationAsync("cart-1", stockItem.ProductId, stockItem.Sku, 0, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var reservation = await context.StockReservations.SingleAsync();
        Assert.Equal(StockReservationStatus.Released, reservation.Status);

        var refreshedStockItem = await context.StockItems.SingleAsync();
        Assert.Equal(0, refreshedStockItem.ReservedQuantity);
    }

    [Fact]
    public async Task ConsumeCartReservationsAsync_Should_ConsumeReservation_AndDecreaseOnHand()
    {
        await using var context = CreateDbContext();
        var stockItem = await SeedTrackedStockItemAsync(context, 10);
        var service = CreateService(context);

        await service.SyncCartReservationAsync("cart-1", stockItem.ProductId, stockItem.Sku, 3, CancellationToken.None);
        var consumeResult = await service.ConsumeCartReservationsAsync(
            "cart-1",
            Guid.NewGuid(),
            [new InventoryCartLineRequest(stockItem.ProductId, stockItem.Sku, 3)],
            CancellationToken.None);

        Assert.True(consumeResult.IsSuccess);

        var reservation = await context.StockReservations.SingleAsync();
        Assert.Equal(StockReservationStatus.Consumed, reservation.Status);

        var refreshedStockItem = await context.StockItems.SingleAsync();
        Assert.Equal(7, refreshedStockItem.OnHandQuantity);
        Assert.Equal(0, refreshedStockItem.ReservedQuantity);
    }

    [Fact]
    public async Task ConsumeCartReservationsAsync_Should_Fail_WhenReservationExpired()
    {
        await using var context = CreateDbContext();
        var stockItem = await SeedTrackedStockItemAsync(context, 10);
        var service = CreateService(context);

        await service.SyncCartReservationAsync("cart-1", stockItem.ProductId, stockItem.Sku, 1, CancellationToken.None);

        var reservation = await context.StockReservations.SingleAsync();
        var expiresAtProperty = typeof(StockReservation).GetProperty(nameof(StockReservation.ExpiresAtUtc));
        Assert.NotNull(expiresAtProperty);
        expiresAtProperty!.SetValue(reservation, DateTime.UtcNow.AddMinutes(-5));
        await context.SaveChangesAsync();

        var consumeResult = await service.ConsumeCartReservationsAsync(
            "cart-1",
            Guid.NewGuid(),
            [new InventoryCartLineRequest(stockItem.ProductId, stockItem.Sku, 1)],
            CancellationToken.None);

        Assert.True(consumeResult.IsFailure);
        Assert.Equal("inventory.reservation.expired", consumeResult.Error.Code);
    }

    [Fact]
    public async Task ValidateCartReservationsAsync_Should_ReturnInvalid_WhenReservationMissing()
    {
        await using var context = CreateDbContext();
        var stockItem = await SeedTrackedStockItemAsync(context, 3);
        var service = CreateService(context);

        var result = await service.ValidateCartReservationsAsync(
            "cart-1",
            [new InventoryCartLineRequest(stockItem.ProductId, stockItem.Sku, 1)],
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsValid);
        Assert.Contains(result.Value.Issues, issue => issue.Code == "inventory.reservation.not_found");
    }

    [Fact]
    public async Task ReleaseAllCartReservationsAsync_Should_ReleaseAllReservationsForCart()
    {
        await using var context = CreateDbContext();
        var firstItem = await SeedTrackedStockItemAsync(context, 4, "SKU-1");
        var secondItem = await SeedTrackedStockItemAsync(context, 4, "SKU-2");
        var service = CreateService(context);

        await service.SyncCartReservationAsync("cart-1", firstItem.ProductId, firstItem.Sku, 2, CancellationToken.None);
        await service.SyncCartReservationAsync("cart-1", secondItem.ProductId, secondItem.Sku, 1, CancellationToken.None);

        var releaseResult = await service.ReleaseAllCartReservationsAsync("cart-1", CancellationToken.None);

        Assert.True(releaseResult.IsSuccess);

        Assert.DoesNotContain(context.StockReservations, reservation =>
            reservation.CartId == "cart-1" && reservation.Status == StockReservationStatus.Active);

        Assert.All(context.StockItems, item => Assert.Equal(0, item.ReservedQuantity));
    }

    private static InventoryReservationService CreateService(InventoryDbContext context)
    {
        var options = Options.Create(new InventoryModuleOptions
        {
            ReservationTtlMinutes = 30,
            ExpirationSweepSeconds = 60,
            RefreshReservationOnCartMutation = true,
            RetryOnConcurrencyCount = 3,
        });

        return new InventoryReservationService(
            new StockItemRepository(context),
            new StockReservationRepository(context),
            new StockMovementRepository(context),
            context,
            new StubClock(),
            options);
    }

    private static async Task<StockItem> SeedTrackedStockItemAsync(
        InventoryDbContext context,
        int onHandQuantity,
        string sku = "SKU-1")
    {
        var createResult = StockItem.Create(
            Guid.NewGuid(),
            sku,
            onHandQuantity,
            isTracked: true,
            allowBackorder: false,
            DateTime.UtcNow);

        Assert.True(createResult.IsSuccess);
        context.StockItems.Add(createResult.Value);
        await context.SaveChangesAsync();

        return createResult.Value;
    }

    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"inventory-reservation-service-{Guid.NewGuid():N}")
            .Options;

        return new InventoryDbContext(options, new BuildingBlocks.Infrastructure.Messaging.SystemTextJsonEventSerializer());
    }

    private sealed class StubClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
