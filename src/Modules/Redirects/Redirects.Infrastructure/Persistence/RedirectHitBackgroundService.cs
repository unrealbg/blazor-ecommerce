using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BuildingBlocks.Infrastructure.Operations;
using Redirects.Application.RedirectRules;

namespace Redirects.Infrastructure.Persistence;

internal sealed class RedirectHitBackgroundService(
    RedirectHitQueue redirectHitQueue,
    IServiceScopeFactory serviceScopeFactory,
    IBackgroundJobMonitor backgroundJobMonitor,
    ILogger<RedirectHitBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var execution = backgroundJobMonitor.Start("redirect-hit-flush");
                if (!await redirectHitQueue.Reader.WaitToReadAsync(stoppingToken))
                {
                    execution.Complete(0, "no-buffered-hits");
                    continue;
                }

                var aggregatedHits = new Dictionary<string, (long Count, DateTime LastHitAtUtc)>(StringComparer.Ordinal);

                while (redirectHitQueue.Reader.TryRead(out var hitRecord))
                {
                    if (!aggregatedHits.TryGetValue(hitRecord.FromPath, out var currentValue))
                    {
                        aggregatedHits[hitRecord.FromPath] = (1, hitRecord.OccurredOnUtc);
                        continue;
                    }

                    var lastHitAtUtc = hitRecord.OccurredOnUtc > currentValue.LastHitAtUtc
                        ? hitRecord.OccurredOnUtc
                        : currentValue.LastHitAtUtc;

                    aggregatedHits[hitRecord.FromPath] = (currentValue.Count + 1, lastHitAtUtc);

                    if (aggregatedHits.Count >= 1024)
                    {
                        break;
                    }
                }

                if (aggregatedHits.Count == 0)
                {
                    execution.Complete(0, "no-buffered-hits");
                    continue;
                }

                using var scope = serviceScopeFactory.CreateScope();
                var redirectRuleRepository = scope.ServiceProvider.GetRequiredService<IRedirectRuleRepository>();

                foreach (var (fromPath, value) in aggregatedHits)
                {
                    await redirectRuleRepository.IncrementHitCountAsync(
                        fromPath,
                        value.Count,
                        value.LastHitAtUtc,
                        stoppingToken);
                }

                execution.Complete(aggregatedHits.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to flush redirect hit counters.");
            }
        }
    }
}
