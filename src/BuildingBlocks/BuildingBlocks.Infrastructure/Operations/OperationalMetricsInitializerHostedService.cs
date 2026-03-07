using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Infrastructure.Operations;

internal sealed class OperationalMetricsInitializerHostedService(OperationalMetricsObserver observer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = observer;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}