namespace Shipping.Domain.Shipping;

public enum ShipmentStatus
{
    Pending = 1,
    LabelCreated = 2,
    ReadyForPickup = 3,
    Shipped = 4,
    InTransit = 5,
    OutForDelivery = 6,
    Delivered = 7,
    Failed = 8,
    Returned = 9,
    Cancelled = 10,
}
