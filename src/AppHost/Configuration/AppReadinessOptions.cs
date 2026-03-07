namespace AppHost.Configuration;

public sealed class AppReadinessOptions
{
    public const string SectionName = "Readiness";

    public int OutboxWarningThreshold { get; set; } = 250;

    public int FailedWebhookWarningThreshold { get; set; } = 25;

    public int OldestOutboxMinutesThreshold { get; set; } = 15;
}