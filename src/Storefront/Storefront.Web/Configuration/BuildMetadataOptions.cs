namespace Storefront.Web.Configuration;

public sealed class BuildMetadataOptions
{
    public const string SectionName = "Build";

    public string ApplicationName { get; set; } = "blazor-ecommerce-storefront";

    public string Version { get; set; } = "dev";

    public string? SourceRevisionId { get; set; }

    public string? BuildTimestampUtc { get; set; }
}