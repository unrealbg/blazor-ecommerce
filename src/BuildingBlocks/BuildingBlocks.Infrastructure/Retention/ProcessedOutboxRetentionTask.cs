using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Retention;

internal sealed class ProcessedOutboxRetentionTask(
    OutboxDbContext dbContext,
    IOptions<RetentionOptions> options) : IRetentionTask
{
    private readonly RetentionOptions options = options.Value;

    public string Name => "processed-outbox-retention";

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, options.ProcessedOutboxDays));

        var candidates = await dbContext.OutboxMessages
            .Where(message => message.ProcessedOnUtc != null && message.ProcessedOnUtc < cutoff)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return 0;
        }

        dbContext.OutboxMessages.RemoveRange(candidates);
        await dbContext.SaveChangesAsync(cancellationToken);
        return candidates.Count;
    }
}