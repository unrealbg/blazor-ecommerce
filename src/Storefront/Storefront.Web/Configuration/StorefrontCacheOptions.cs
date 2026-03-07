namespace Storefront.Web.Configuration;

public sealed class StorefrontCacheOptions
{
    public const string SectionName = "Caching";

    public int HomePageSeconds { get; set; } = 60;

    public int CategoryPageSeconds { get; set; } = 30;

    public int ProductPageSeconds { get; set; } = 45;

    public int SearchPageSeconds { get; set; } = 15;

    public int ContentPageSeconds { get; set; } = 300;

    public int SitemapSeconds { get; set; } = 600;

    public int RobotsSeconds { get; set; } = 3600;

    public int RssSeconds { get; set; } = 300;

    public int ProductProjectionSeconds { get; set; } = 60;

    public int SearchResultSeconds { get; set; } = 30;

    public int SuggestionSeconds { get; set; } = 15;

    public int ReviewSummarySeconds { get; set; } = 60;
}