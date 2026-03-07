namespace Storefront.Web.Configuration;

public sealed class StorefrontFeatureFlagsOptions
{
    public const string SectionName = "FeatureFlags";

    public bool EnableReviews { get; set; } = true;

    public bool EnableCmsContent { get; set; } = true;

    public bool EnableSearchSuggestions { get; set; } = true;

    public bool EnableWarmup { get; set; } = true;
}