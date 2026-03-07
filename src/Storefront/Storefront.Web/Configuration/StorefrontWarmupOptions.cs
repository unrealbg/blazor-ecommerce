namespace Storefront.Web.Configuration;

public sealed class StorefrontWarmupOptions
{
    public const string SectionName = "Warmup";

    public bool Enabled { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 20;

    public int MaxFeaturedProducts { get; set; } = 8;

    public bool WarmSitemap { get; set; } = true;

    public bool WarmContent { get; set; } = true;

    public bool WarmSearch { get; set; } = true;
}