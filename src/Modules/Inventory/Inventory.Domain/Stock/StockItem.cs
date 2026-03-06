using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Inventory.Domain.Stock.Events;

namespace Inventory.Domain.Stock;

public sealed class StockItem : AggregateRoot<Guid>
{
    private StockItem()
    {
    }

    private StockItem(
        Guid id,
        Guid productId,
        string? sku,
        int onHandQuantity,
        bool isTracked,
        bool allowBackorder,
        DateTime createdAtUtc)
    {
        Id = id;
        ProductId = productId;
        Sku = NormalizeSku(sku);
        OnHandQuantity = onHandQuantity;
        ReservedQuantity = 0;
        IsTracked = isTracked;
        AllowBackorder = allowBackorder;
        RowVersion = 0;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid ProductId { get; private set; }

    public string? Sku { get; private set; }

    public int OnHandQuantity { get; private set; }

    public int ReservedQuantity { get; private set; }

    public int AvailableQuantity => OnHandQuantity - ReservedQuantity;

    public bool IsTracked { get; private set; }

    public bool AllowBackorder { get; private set; }

    public long RowVersion { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsInStock => !IsTracked || AllowBackorder || AvailableQuantity > 0;

    public static Result<StockItem> Create(
        Guid productId,
        string? sku,
        int onHandQuantity,
        bool isTracked,
        bool allowBackorder,
        DateTime createdAtUtc)
    {
        if (productId == Guid.Empty)
        {
            return Result<StockItem>.Failure(
                new Error("inventory.stock_item.product_id.required", "Product id is required."));
        }

        if (onHandQuantity < 0)
        {
            return Result<StockItem>.Failure(
                new Error("inventory.stock_item.on_hand.invalid", "On-hand quantity must be zero or positive."));
        }

        var item = new StockItem(
            Guid.NewGuid(),
            productId,
            sku,
            onHandQuantity,
            isTracked,
            allowBackorder,
            createdAtUtc);

        item.RaiseAvailabilityChangedEvent();
        return Result<StockItem>.Success(item);
    }

    public Result Reserve(int quantity, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            return Result.Failure(new Error(
                "inventory.reservation.quantity.invalid",
                "Reservation quantity must be greater than zero."));
        }

        if (!IsTracked)
        {
            return Result.Success();
        }

        var nextReserved = ReservedQuantity + quantity;
        if (!AllowBackorder && nextReserved > OnHandQuantity)
        {
            return Result.Failure(new Error(
                "inventory.stock.insufficient",
                "Insufficient stock."));
        }

        var previousInStock = IsInStock;
        ReservedQuantity = nextReserved;
        Touch(utcNow);
        RaiseAvailabilityChangedIfNeeded(previousInStock);
        return Result.Success();
    }

    public Result Release(int quantity, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            return Result.Failure(new Error(
                "inventory.reservation.quantity.invalid",
                "Release quantity must be greater than zero."));
        }

        if (!IsTracked)
        {
            return Result.Success();
        }

        var previousInStock = IsInStock;
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
        Touch(utcNow);
        RaiseAvailabilityChangedIfNeeded(previousInStock);
        return Result.Success();
    }

    public Result Consume(int quantity, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            return Result.Failure(new Error(
                "inventory.reservation.quantity.invalid",
                "Consume quantity must be greater than zero."));
        }

        if (!IsTracked)
        {
            return Result.Success();
        }

        if (quantity > ReservedQuantity)
        {
            return Result.Failure(new Error(
                "inventory.reservation.not_found",
                "Reservation quantity is not available for consumption."));
        }

        if (!AllowBackorder && quantity > OnHandQuantity)
        {
            return Result.Failure(new Error(
                "inventory.stock.insufficient",
                "Insufficient stock."));
        }

        var nextOnHand = OnHandQuantity - quantity;
        if (!AllowBackorder && nextOnHand < 0)
        {
            return Result.Failure(new Error(
                "inventory.stock.insufficient",
                "Insufficient stock."));
        }

        var previousInStock = IsInStock;
        ReservedQuantity -= quantity;
        OnHandQuantity = nextOnHand;
        Touch(utcNow);
        RaiseAvailabilityChangedIfNeeded(previousInStock);
        return Result.Success();
    }

    public Result AdjustOnHand(int quantityDelta, DateTime utcNow)
    {
        var nextOnHand = OnHandQuantity + quantityDelta;
        if (nextOnHand < 0 && !AllowBackorder)
        {
            return Result.Failure(new Error(
                "inventory.stock.adjustment.invalid",
                "Manual adjustment would make on-hand quantity negative."));
        }

        if (!AllowBackorder && ReservedQuantity > nextOnHand)
        {
            return Result.Failure(new Error(
                "inventory.stock.adjustment.invalid",
                "Manual adjustment would make reserved quantity exceed on-hand quantity."));
        }

        var previousInStock = IsInStock;
        OnHandQuantity = nextOnHand;
        Touch(utcNow);
        RaiseDomainEvent(new StockAdjusted(ProductId, Sku, quantityDelta, null));
        RaiseAvailabilityChangedIfNeeded(previousInStock);
        return Result.Success();
    }

    public Result UpdateTracking(bool isTracked, bool allowBackorder, DateTime utcNow)
    {
        if (IsTracked == isTracked && AllowBackorder == allowBackorder)
        {
            return Result.Success();
        }

        var previousInStock = IsInStock;
        IsTracked = isTracked;
        AllowBackorder = allowBackorder;
        Touch(utcNow);
        RaiseAvailabilityChangedIfNeeded(previousInStock);
        return Result.Success();
    }

    private static string? NormalizeSku(string? sku)
    {
        return string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
    }

    private void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        RowVersion++;
    }

    private void RaiseAvailabilityChangedIfNeeded(bool previousIsInStock)
    {
        if (previousIsInStock != IsInStock)
        {
            RaiseAvailabilityChangedEvent();
        }
    }

    private void RaiseAvailabilityChangedEvent()
    {
        RaiseDomainEvent(new StockAvailabilityChanged(
            ProductId,
            Sku,
            IsTracked,
            AllowBackorder,
            AvailableQuantity,
            IsInStock));
    }
}
