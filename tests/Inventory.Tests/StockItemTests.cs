using Inventory.Domain.Stock;
using Inventory.Domain.Stock.Events;

namespace Inventory.Tests;

public sealed class StockItemTests
{
    [Fact]
    public void Create_Should_Fail_When_ProductIdMissing()
    {
        var result = StockItem.Create(Guid.Empty, Guid.NewGuid(), null, 10, true, false, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.stock_item.product_id.required", result.Error.Code);
    }

    [Fact]
    public void Create_Should_Fail_When_VariantIdMissing()
    {
        var result = StockItem.Create(Guid.NewGuid(), Guid.Empty, null, 10, true, false, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.stock_item.variant_id.required", result.Error.Code);
    }

    [Fact]
    public void Reserve_Should_Fail_When_InsufficientStock_AndBackorderDisabled()
    {
        var item = CreateTrackedStockItem(onHandQuantity: 2, allowBackorder: false);

        var result = item.Reserve(3, DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.stock.insufficient", result.Error.Code);
        Assert.Equal(0, item.ReservedQuantity);
    }

    [Fact]
    public void Reserve_Should_Succeed_When_EnoughStock()
    {
        var item = CreateTrackedStockItem(onHandQuantity: 5, allowBackorder: false);

        var result = item.Reserve(3, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, item.ReservedQuantity);
        Assert.Equal(2, item.AvailableQuantity);
        Assert.True(item.IsInStock);
    }

    [Fact]
    public void Reserve_Should_Succeed_When_BackorderEnabled()
    {
        var item = CreateTrackedStockItem(onHandQuantity: 1, allowBackorder: true);

        var result = item.Reserve(5, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, item.ReservedQuantity);
        Assert.Equal(-4, item.AvailableQuantity);
        Assert.True(item.IsInStock);
    }

    [Fact]
    public void Release_Should_DecreaseReservedQuantity()
    {
        var item = CreateTrackedStockItem(onHandQuantity: 5, allowBackorder: false);
        item.Reserve(4, DateTime.UtcNow);

        var result = item.Release(2, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, item.ReservedQuantity);
        Assert.Equal(3, item.AvailableQuantity);
    }

    [Fact]
    public void Consume_Should_DecreaseOnHand_AndReserved()
    {
        var item = CreateTrackedStockItem(onHandQuantity: 10, allowBackorder: false);
        item.Reserve(4, DateTime.UtcNow);

        var result = item.Consume(3, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, item.OnHandQuantity);
        Assert.Equal(1, item.ReservedQuantity);
        Assert.Equal(6, item.AvailableQuantity);
    }

    [Fact]
    public void AdjustOnHand_Should_RaiseStockAdjustedDomainEvent()
    {
        var item = CreateTrackedStockItem(onHandQuantity: 6, allowBackorder: false);

        var result = item.AdjustOnHand(2, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Contains(item.DomainEvents, domainEvent => domainEvent is StockAdjusted);
    }

    [Fact]
    public void UpdateTracking_Should_UpdateFlags()
    {
        var item = CreateTrackedStockItem(onHandQuantity: 2, allowBackorder: false);

        var result = item.UpdateTracking(false, true, DateTime.UtcNow);

        Assert.True(result.IsSuccess);
        Assert.False(item.IsTracked);
        Assert.True(item.AllowBackorder);
    }

    private static StockItem CreateTrackedStockItem(int onHandQuantity, bool allowBackorder)
    {
        var result = StockItem.Create(Guid.NewGuid(), Guid.NewGuid(), "SKU-1", onHandQuantity, true, allowBackorder, DateTime.UtcNow);
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
