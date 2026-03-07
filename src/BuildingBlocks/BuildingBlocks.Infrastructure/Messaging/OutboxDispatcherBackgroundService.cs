using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Application.Operations;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Operations;
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
    IBackgroundJobMonitor backgroundJobMonitor,
    IOperationalAlertSink operationalAlertSink,
    ILogger<OutboxDispatcherBackgroundService> logger)
    : BackgroundService
{
    private readonly OutboxDispatcherOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox dispatcher background service started.");

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

        logger.LogInformation("Outbox dispatcher background service stopped.");
    }

    private async Task DispatchPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var execution = backgroundJobMonitor.Start("outbox-dispatcher");
        using var scope = serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
        var now = clock.UtcNow;

        var pendingMessages = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null || message.Error != null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
        {
            execution.Complete(0, "no-pending-messages");
            return;
        }

        var eligibleMessages = pendingMessages
            .Where(message => IsEligibleForDispatch(message, now))
            .ToArray();

        if (eligibleMessages.Length == 0)
        {
            execution.Complete(0, "messages-waiting-for-retry-window");
            return;
        }

        foreach (var message in eligibleMessages)
        {
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                message.MarkProcessed(clock.UtcNow);
                logger.LogInformation(
                    "Processed outbox message {OutboxMessageId} {EventType}",
                    message.Id,
                    message.Type);
            }
            catch (Exception exception)
            {
                var failureState = OutboxFailureState.Parse(message.Error);
                var nextAttempt = now.AddSeconds(Math.Pow(2, failureState.Attempt) * _options.RetryBaseDelay.TotalSeconds);
                var attempt = failureState.Attempt + 1;
                var deadLettered = attempt >= _options.MaxRetryAttempts;
                var storage = new OutboxFailureState(
                    attempt,
                    deadLettered,
                    deadLettered ? null : nextAttempt,
                    exception.Message);

                logger.LogError(
                    exception,
                    "Failed processing outbox message {OutboxMessageId} {EventType} attempt {Attempt} deadLettered {DeadLettered}",
                    message.Id,
                    message.Type,
                    attempt,
                    deadLettered);

                message.MarkFailed(storage.ToStorageString(), clock.UtcNow);

                if (deadLettered)
                {
                    await operationalAlertSink.PublishAsync(
                        new OperationalAlert(
                            "outbox.dead_lettered",
                            "error",
                            "An outbox message reached the maximum retry threshold.",
                            exception.Message,
                            new Dictionary<string, string?>
                            {
                                ["outboxMessageId"] = message.Id.ToString("D"),
                                ["eventType"] = message.Type,
                            },
                            now),
                        cancellationToken);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var oldestPending = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Select(message => (DateTime?)message.OccurredOnUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (oldestPending is not null && now - oldestPending.Value > _options.StuckWarningAge)
        {
            await operationalAlertSink.PublishAsync(
                new OperationalAlert(
                    "outbox.backlog.stuck",
                    "warning",
                    "Outbox backlog age exceeded the configured threshold.",
                    null,
                    new Dictionary<string, string?>
                    {
                        ["oldestPendingAgeMinutes"] = (now - oldestPending.Value).TotalMinutes.ToString("F0", System.Globalization.CultureInfo.InvariantCulture),
                    },
                    now),
                cancellationToken);
        }

        execution.Complete(eligibleMessages.Length);
    }

    private static bool IsEligibleForDispatch(OutboxMessage message, DateTime utcNow)
    {
        if (message.ProcessedOnUtc is null)
        {
            return true;
        }

        var failureState = OutboxFailureState.Parse(message.Error);
        if (failureState.DeadLettered)
        {
            return false;
        }

        return failureState.NextVisibleAtUtc is null || failureState.NextVisibleAtUtc <= utcNow;
    }
}
