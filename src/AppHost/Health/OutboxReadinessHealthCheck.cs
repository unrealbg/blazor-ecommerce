using AppHost.Configuration;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AppHost.Health;

public sealed class OutboxReadinessHealthCheck(
    OutboxDbContext outboxDbContext,
    IOptions<AppReadinessOptions> options)
    : IHealthCheck
{
    private readonly AppReadinessOptions options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var pendingCount = await outboxDbContext.OutboxMessages.CountAsync(
            message => message.ProcessedOnUtc == null,
            cancellationToken);

        var oldestPending = await outboxDbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Select(message => (DateTime?)message.OccurredOnUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var data = new Dictionary<string, object>
        {
            ["pendingCount"] = pendingCount,
        };

        if (oldestPending is not null)
        {
            data["oldestPendingAgeMinutes"] = (DateTime.UtcNow - oldestPending.Value).TotalMinutes;
        }

        if (pendingCount >= options.OutboxWarningThreshold)
        {
            return HealthCheckResult.Degraded("Outbox backlog exceeded warning threshold.", data: data);
        }

        if (oldestPending is not null && (DateTime.UtcNow - oldestPending.Value).TotalMinutes >= options.OldestOutboxMinutesThreshold)
        {
            return HealthCheckResult.Degraded("Outbox backlog age exceeded warning threshold.", data: data);
        }

        return HealthCheckResult.Healthy("Outbox backlog is within expected limits.", data);
    }
}