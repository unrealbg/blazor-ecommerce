using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using Shipping.Domain.Events;

namespace Shipping.Domain.Shipping;

public sealed class Shipment : AggregateRoot<Guid>
{
    private Shipment()
    {
    }

    private Shipment(
        Guid id,
        Guid orderId,
        Guid shippingMethodId,
        string carrierName,
        string? carrierServiceCode,
        string recipientName,
        string? recipientPhone,
        string addressSnapshotJson,
        decimal shippingPriceAmount,
        string currency,
        DateTime createdAtUtc)
    {
        Id = id;
        OrderId = orderId;
        ShippingMethodId = shippingMethodId;
        CarrierName = carrierName;
        CarrierServiceCode = carrierServiceCode;
        RecipientName = recipientName;
        RecipientPhone = recipientPhone;
        AddressSnapshotJson = addressSnapshotJson;
        ShippingPriceAmount = shippingPriceAmount;
        Currency = currency;
        Status = ShipmentStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = 0;

        RaiseDomainEvent(new ShipmentCreated(Id, OrderId, CarrierName));
    }

    public Guid OrderId { get; private set; }

    public Guid ShippingMethodId { get; private set; }

    public string CarrierName { get; private set; } = string.Empty;

    public string? CarrierServiceCode { get; private set; }

    public string? TrackingNumber { get; private set; }

    public string? TrackingUrl { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public string RecipientName { get; private set; } = string.Empty;

    public string? RecipientPhone { get; private set; }

    public string AddressSnapshotJson { get; private set; } = string.Empty;

    public decimal ShippingPriceAmount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public DateTime? ShippedAtUtc { get; private set; }

    public DateTime? DeliveredAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long RowVersion { get; private set; }

    public static Result<Shipment> Create(
        Guid orderId,
        Guid shippingMethodId,
        string carrierName,
        string? carrierServiceCode,
        string recipientName,
        string? recipientPhone,
        string addressSnapshotJson,
        decimal shippingPriceAmount,
        string currency,
        DateTime createdAtUtc)
    {
        if (orderId == Guid.Empty)
        {
            return Result<Shipment>.Failure(new Error(
                "shipping.shipment.order.required",
                "Order id is required."));
        }

        if (shippingMethodId == Guid.Empty)
        {
            return Result<Shipment>.Failure(new Error(
                "shipping.shipment.method.required",
                "Shipping method id is required."));
        }

        if (string.IsNullOrWhiteSpace(carrierName))
        {
            return Result<Shipment>.Failure(new Error(
                "shipping.shipment.carrier.required",
                "Carrier name is required."));
        }

        if (string.IsNullOrWhiteSpace(recipientName))
        {
            return Result<Shipment>.Failure(new Error(
                "shipping.shipment.recipient.required",
                "Recipient name is required."));
        }

        if (string.IsNullOrWhiteSpace(addressSnapshotJson))
        {
            return Result<Shipment>.Failure(new Error(
                "shipping.shipment.address.required",
                "Address snapshot is required."));
        }

        if (shippingPriceAmount < 0m)
        {
            return Result<Shipment>.Failure(new Error(
                "shipping.shipment.price.invalid",
                "Shipping price cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            return Result<Shipment>.Failure(new Error(
                "shipping.shipment.currency.invalid",
                "Shipment currency must be a 3-letter code."));
        }

        return Result<Shipment>.Success(new Shipment(
            Guid.NewGuid(),
            orderId,
            shippingMethodId,
            carrierName.Trim(),
            string.IsNullOrWhiteSpace(carrierServiceCode) ? null : carrierServiceCode.Trim(),
            recipientName.Trim(),
            string.IsNullOrWhiteSpace(recipientPhone) ? null : recipientPhone.Trim(),
            addressSnapshotJson,
            shippingPriceAmount,
            currency.Trim().ToUpperInvariant(),
            createdAtUtc));
    }

    public Result SetTracking(string trackingNumber, string? trackingUrl, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            return Result.Failure(new Error(
                "shipping.shipment.tracking.required",
                "Tracking number is required."));
        }

        TrackingNumber = trackingNumber.Trim();
        TrackingUrl = string.IsNullOrWhiteSpace(trackingUrl) ? null : trackingUrl.Trim();
        Touch(updatedAtUtc);
        return Result.Success();
    }

    public Result MarkLabelCreated(DateTime updatedAtUtc)
    {
        var result = TransitionTo(ShipmentStatus.LabelCreated, updatedAtUtc);
        if (result.IsSuccess)
        {
            RaiseDomainEvent(new ShipmentLabelCreated(Id, OrderId, CarrierName));
        }

        return result;
    }

    public Result MarkShipped(DateTime shippedAtUtc)
    {
        var result = TransitionTo(ShipmentStatus.Shipped, shippedAtUtc);
        if (result.IsSuccess)
        {
            ShippedAtUtc ??= shippedAtUtc;
            RaiseDomainEvent(new ShipmentShipped(Id, OrderId, CarrierName));
        }

        return result;
    }

    public Result MarkInTransit(DateTime updatedAtUtc)
    {
        var result = TransitionTo(ShipmentStatus.InTransit, updatedAtUtc);
        if (result.IsSuccess)
        {
            RaiseDomainEvent(new ShipmentInTransit(Id, OrderId, CarrierName));
        }

        return result;
    }

    public Result MarkReadyForPickup(DateTime updatedAtUtc)
    {
        return TransitionTo(ShipmentStatus.ReadyForPickup, updatedAtUtc);
    }

    public Result MarkOutForDelivery(DateTime updatedAtUtc)
    {
        return TransitionTo(ShipmentStatus.OutForDelivery, updatedAtUtc);
    }

    public Result MarkDelivered(DateTime deliveredAtUtc)
    {
        var result = TransitionTo(ShipmentStatus.Delivered, deliveredAtUtc);
        if (result.IsSuccess)
        {
            DeliveredAtUtc ??= deliveredAtUtc;
            RaiseDomainEvent(new ShipmentDelivered(Id, OrderId, CarrierName));
        }

        return result;
    }

    public Result MarkFailed(string? reason, DateTime updatedAtUtc)
    {
        var result = TransitionTo(ShipmentStatus.Failed, updatedAtUtc);
        if (result.IsSuccess)
        {
            RaiseDomainEvent(new ShipmentFailed(Id, OrderId, CarrierName, reason));
        }

        return result;
    }

    public Result MarkReturned(string? reason, DateTime updatedAtUtc)
    {
        var result = TransitionTo(ShipmentStatus.Returned, updatedAtUtc);
        if (result.IsSuccess)
        {
            RaiseDomainEvent(new ShipmentReturned(Id, OrderId, CarrierName, reason));
        }

        return result;
    }

    public Result MarkCancelled(DateTime updatedAtUtc)
    {
        return TransitionTo(ShipmentStatus.Cancelled, updatedAtUtc);
    }

    private Result TransitionTo(ShipmentStatus next, DateTime utcNow)
    {
        if (Status == next)
        {
            Touch(utcNow);
            return Result.Success();
        }

        if (!IsTransitionAllowed(Status, next))
        {
            return Result.Failure(new Error(
                "shipping.shipment.status.transition.invalid",
                $"Cannot move shipment from '{Status}' to '{next}'."));
        }

        Status = next;
        Touch(utcNow);
        return Result.Success();
    }

    private bool IsTransitionAllowed(ShipmentStatus current, ShipmentStatus next)
    {
        return current switch
        {
            ShipmentStatus.Pending => next is ShipmentStatus.LabelCreated or ShipmentStatus.Shipped or ShipmentStatus.Cancelled,
            ShipmentStatus.LabelCreated => next is ShipmentStatus.ReadyForPickup or ShipmentStatus.Shipped or ShipmentStatus.Cancelled,
            ShipmentStatus.ReadyForPickup => next is ShipmentStatus.Shipped or ShipmentStatus.Cancelled,
            ShipmentStatus.Shipped => next is ShipmentStatus.InTransit or ShipmentStatus.Delivered or ShipmentStatus.Failed,
            ShipmentStatus.InTransit => next is ShipmentStatus.OutForDelivery or ShipmentStatus.Delivered or ShipmentStatus.Failed,
            ShipmentStatus.OutForDelivery => next is ShipmentStatus.Delivered or ShipmentStatus.Failed,
            ShipmentStatus.Failed => next is ShipmentStatus.Returned or ShipmentStatus.Cancelled,
            _ => false,
        };
    }

    private void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        RowVersion++;
    }
}
