using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Messaging;

public sealed class OutboxDispatcherBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<OutboxDispatcherOptions> options,
    IClock clock,
    ILogger<OutboxDispatcherBackgroundService> logger)
    : BackgroundService
{
    private readonly OutboxDispatcherOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DispatchPendingMessagesAsync(stoppingToken);

            try
            {
                await Task.Delay(_options.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore graceful shutdown cancellation.
            }
        }
    }

    private async Task DispatchPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        foreach (var message in pendingMessages)
        {
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                message.MarkProcessed(clock.UtcNow);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed processing outbox message {OutboxMessageId}", message.Id);
                message.MarkFailed(exception.ToString(), clock.UtcNow);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
