using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Inventory.Domain.Stock.Events;

namespace Inventory.Domain.Stock;

public sealed class StockReservation : AggregateRoot<Guid>
{
    private StockReservation()
    {
    }

    private StockReservation(
        Guid id,
        Guid productId,
        string? sku,
        string? cartId,
        Guid? customerId,
        int quantity,
        string reservationToken,
        DateTime expiresAtUtc,
        DateTime createdAtUtc)
    {
        Id = id;
        ProductId = productId;
        Sku = NormalizeSku(sku);
        CartId = NormalizeCartId(cartId);
        CustomerId = customerId;
        Quantity = quantity;
        Status = StockReservationStatus.Active;
        ReservationToken = reservationToken;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;

        RaiseDomainEvent(new StockReserved(ProductId, Sku, Id, quantity));
    }

    public Guid ProductId { get; private set; }

    public string? Sku { get; private set; }

    public string? CartId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public Guid? OrderId { get; private set; }

    public int Quantity { get; private set; }

    public StockReservationStatus Status { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public string ReservationToken { get; private set; } = string.Empty;

    public static Result<StockReservation> Create(
        Guid productId,
        string? sku,
        string? cartId,
        Guid? customerId,
        int quantity,
        string reservationToken,
        DateTime expiresAtUtc,
        DateTime createdAtUtc)
    {
        if (productId == Guid.Empty)
        {
            return Result<StockReservation>.Failure(
                new Error("inventory.reservation.product_id.required", "Product id is required."));
        }

        if (quantity <= 0)
        {
            return Result<StockReservation>.Failure(
                new Error("inventory.reservation.quantity.invalid", "Quantity must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(cartId) && customerId is null)
        {
            return Result<StockReservation>.Failure(
                new Error(
                    "inventory.reservation.owner.required",
                    "Reservation should have cart id or customer id."));
        }

        if (string.IsNullOrWhiteSpace(reservationToken))
        {
            return Result<StockReservation>.Failure(
                new Error(
                    "inventory.reservation.token.required",
                    "Reservation token is required."));
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            return Result<StockReservation>.Failure(
                new Error(
                    "inventory.reservation.expiration.invalid",
                    "Reservation expiration should be in the future."));
        }

        return Result<StockReservation>.Success(new StockReservation(
            Guid.NewGuid(),
            productId,
            sku,
            cartId,
            customerId,
            quantity,
            reservationToken.Trim(),
            expiresAtUtc,
            createdAtUtc));
    }

    public Result SetQuantity(int quantity, DateTime utcNow)
    {
        if (Status != StockReservationStatus.Active)
        {
            return Result.Failure(new Error(
                "inventory.reservation.not_active",
                "Only active reservations can be updated."));
        }

        if (quantity <= 0)
        {
            return Result.Failure(new Error(
                "inventory.reservation.quantity.invalid",
                "Quantity must be greater than zero."));
        }

        Quantity = quantity;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result RefreshExpiration(DateTime expiresAtUtc, DateTime utcNow)
    {
        if (Status != StockReservationStatus.Active)
        {
            return Result.Failure(new Error(
                "inventory.reservation.not_active",
                "Only active reservations can be refreshed."));
        }

        if (expiresAtUtc <= utcNow)
        {
            return Result.Failure(new Error(
                "inventory.reservation.expiration.invalid",
                "Reservation expiration should be in the future."));
        }

        ExpiresAtUtc = expiresAtUtc;
        UpdatedAtUtc = utcNow;
        return Result.Success();
    }

    public Result Consume(Guid orderId, DateTime utcNow)
    {
        if (Status == StockReservationStatus.Consumed)
        {
            return Result.Failure(new Error(
                "inventory.reservation.already_consumed",
                "Reservation is already consumed."));
        }

        if (Status != StockReservationStatus.Active)
        {
            return Result.Failure(new Error(
                "inventory.reservation.not_active",
                "Only active reservations can be consumed."));
        }

        OrderId = orderId;
        Status = StockReservationStatus.Consumed;
        UpdatedAtUtc = utcNow;
        RaiseDomainEvent(new StockConsumed(ProductId, Sku, orderId, Quantity));
        return Result.Success();
    }

    public Result Release(DateTime utcNow)
    {
        if (Status != StockReservationStatus.Active)
        {
            return Result.Failure(new Error(
                "inventory.reservation.not_active",
                "Only active reservations can be released."));
        }

        Status = StockReservationStatus.Released;
        UpdatedAtUtc = utcNow;
        RaiseDomainEvent(new StockReservationReleased(ProductId, Sku, Id, Quantity));
        return Result.Success();
    }

    public Result Expire(DateTime utcNow)
    {
        if (Status != StockReservationStatus.Active)
        {
            return Result.Failure(new Error(
                "inventory.reservation.not_active",
                "Only active reservations can expire."));
        }

        Status = StockReservationStatus.Expired;
        UpdatedAtUtc = utcNow;
        RaiseDomainEvent(new StockReservationExpired(ProductId, Sku, Id, Quantity));
        return Result.Success();
    }

    private static string? NormalizeSku(string? sku)
    {
        return string.IsNullOrWhiteSpace(sku) ? null : sku.Trim();
    }

    private static string? NormalizeCartId(string? cartId)
    {
        return string.IsNullOrWhiteSpace(cartId) ? null : cartId.Trim();
    }
}
