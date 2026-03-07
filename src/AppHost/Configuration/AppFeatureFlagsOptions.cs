namespace AppHost.Configuration;

public sealed class AppFeatureFlagsOptions
{
    public const string SectionName = "FeatureFlags";

    public bool EnableOperationalRecoveryActions { get; set; } = true;

    public bool EnableDemoProviders { get; set; } = true;

    public bool EnableReviewModeration { get; set; } = true;
}