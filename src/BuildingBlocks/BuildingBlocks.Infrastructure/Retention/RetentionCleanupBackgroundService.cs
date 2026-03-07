using BuildingBlocks.Infrastructure.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Retention;

internal sealed class RetentionCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    IBackgroundJobMonitor backgroundJobMonitor,
    IOptions<RetentionOptions> options,
    ILogger<RetentionCleanupBackgroundService> logger)
    : BackgroundService
{
    private readonly RetentionOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteSweepAsync(stoppingToken);

            try
            {
                await Task.Delay(options.SweepInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ExecuteSweepAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var tasks = scope.ServiceProvider.GetServices<IRetentionTask>().ToArray();

        foreach (var task in tasks)
        {
            using var execution = backgroundJobMonitor.Start($"retention:{task.Name}");
            try
            {
                var affected = await task.ExecuteAsync(cancellationToken);
                execution.Complete(affected, $"affected={affected}");
            }
            catch (Exception exception)
            {
                execution.Fail(exception);
                logger.LogError(exception, "Retention task {RetentionTask} failed.", task.Name);
            }
        }
    }
}