namespace AppHost.Configuration;

public sealed class AppBuildOptions
{
    public const string SectionName = "Build";

    public string ApplicationName { get; set; } = "blazor-ecommerce-app";

    public string Version { get; set; } = "dev";

    public string? SourceRevisionId { get; set; }

    public string? BuildTimestampUtc { get; set; }
}