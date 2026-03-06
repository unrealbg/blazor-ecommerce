namespace Shipping.Application.Shipping;

public sealed class ShippingModuleOptions
{
    public const string SectionName = "Shipping";

    public string DefaultCarrier { get; set; } = "DemoCarrier";

    public string DefaultCurrency { get; set; } = "EUR";

    public bool AllowStorePickup { get; set; }

    public int QuoteCacheSeconds { get; set; } = 60;

    public TimeSpan QuoteCacheTtl => TimeSpan.FromSeconds(Math.Max(5, QuoteCacheSeconds));
}
