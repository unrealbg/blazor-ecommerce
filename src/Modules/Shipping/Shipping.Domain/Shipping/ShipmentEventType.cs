namespace Shipping.Domain.Shipping;

public enum ShipmentEventType
{
    StatusChanged = 1,
    TrackingUpdated = 2,
    LabelCreated = 3,
    CarrierWebhook = 4,
    ManualNote = 5,
}
