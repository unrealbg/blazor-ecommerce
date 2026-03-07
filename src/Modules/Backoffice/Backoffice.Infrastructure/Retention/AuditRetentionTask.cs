using Backoffice.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backoffice.Infrastructure.Retention;

internal sealed class AuditRetentionTask(
    BackofficeDbContext dbContext,
    IOptions<RetentionOptions> options) : IRetentionTask
{
    private readonly RetentionOptions options = options.Value;

    public string Name => "audit-log-retention";

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, options.AuditLogDays));
        var entries = await dbContext.AuditEntries
            .Where(entry => entry.OccurredAtUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return 0;
        }

        dbContext.AuditEntries.RemoveRange(entries);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entries.Count;
    }
}