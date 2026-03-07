using BuildingBlocks.Application.Operations;

namespace BuildingBlocks.Application.Extensions;

public sealed class NullOperationalAlertSink : IOperationalAlertSink
{
    public Task PublishAsync(OperationalAlert alert, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}