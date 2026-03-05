namespace Storefront.Web.Services.Content;

public sealed class CmsOptions
{
    public const string SectionName = "Cms";

    public string CmsBaseUrl { get; set; } = "http://localhost:8055";

    public string CmsApiKey { get; set; } = string.Empty;

    public int CacheSeconds { get; set; } = 60;
}
