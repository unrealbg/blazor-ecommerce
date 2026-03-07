namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxDispatcherOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; set; } = 20;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    public int MaxRetryAttempts { get; set; } = 5;

    public int RetryBaseDelaySeconds { get; set; } = 15;

    public int StuckWarningAgeMinutes { get; set; } = 15;

    public TimeSpan RetryBaseDelay => TimeSpan.FromSeconds(Math.Max(1, RetryBaseDelaySeconds));

    public TimeSpan StuckWarningAge => TimeSpan.FromMinutes(Math.Max(1, StuckWarningAgeMinutes));
}
