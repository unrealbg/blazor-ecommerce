namespace AppHost.Configuration;

public sealed class AppReleaseOptions
{
    public const string SectionName = "Release";

    public string SeedMode { get; set; } = "none";

    public string MigrationMode { get; set; } = "manual";

    public bool RunSmokeTestsAfterDeploy { get; set; } = true;
}