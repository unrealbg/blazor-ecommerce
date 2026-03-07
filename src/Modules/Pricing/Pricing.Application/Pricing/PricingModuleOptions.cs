namespace Pricing.Application.Pricing;

public sealed class PricingModuleOptions
{
    public const string SectionName = "Pricing";

    public int VariantCacheSeconds { get; set; } = 30;

    public int CartCacheSeconds { get; set; } = 30;

    public string DefaultCurrency { get; set; } = "EUR";
}
