namespace Storefront.Web.Services.Content;

public sealed class CmsOptions
{
    public const string SectionName = "Cms";

    public string BaseUrl { get; set; } = "http://localhost:8055";

    public string ApiToken { get; set; } = string.Empty;

    public int CacheSeconds { get; set; } = 60;
}
