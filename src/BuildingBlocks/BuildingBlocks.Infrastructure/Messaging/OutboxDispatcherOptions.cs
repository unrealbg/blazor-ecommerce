namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxDispatcherOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; set; } = 20;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
}
