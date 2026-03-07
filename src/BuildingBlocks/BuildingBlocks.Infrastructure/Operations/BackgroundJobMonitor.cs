namespace BuildingBlocks.Infrastructure.Operations;

internal sealed class BackgroundJobMonitor(IOperationalStateRegistry registry) : IBackgroundJobMonitor
{
    public BackgroundJobExecution Start(string workerName, string? correlationId = null)
    {
        return new BackgroundJobExecution(registry, workerName, correlationId);
    }
}