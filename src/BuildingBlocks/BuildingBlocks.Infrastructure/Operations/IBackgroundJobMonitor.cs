namespace BuildingBlocks.Infrastructure.Operations;

public interface IBackgroundJobMonitor
{
    BackgroundJobExecution Start(string workerName, string? correlationId = null);
}