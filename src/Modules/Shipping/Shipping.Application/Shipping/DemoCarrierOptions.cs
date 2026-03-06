namespace Shipping.Application.Shipping;

public sealed class DemoCarrierOptions
{
    public const string SectionName = "Shipping:DemoCarrier";

    public bool AutoAdvanceStatuses { get; set; }

    public string BaseTrackingUrl { get; set; } = "http://localhost:5000/demo-tracking/";
}
