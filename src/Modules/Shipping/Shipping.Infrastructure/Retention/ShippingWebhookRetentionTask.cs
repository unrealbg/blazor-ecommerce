using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shipping.Infrastructure.Persistence;

namespace Shipping.Infrastructure.Retention;

internal sealed class ShippingWebhookRetentionTask(
    ShippingDbContext dbContext,
    IOptions<RetentionOptions> options) : IRetentionTask
{
    private readonly RetentionOptions options = options.Value;

    public string Name => "shipping-webhook-payload-retention";

    public async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, options.WebhookPayloadDays));
        var messages = await dbContext.CarrierWebhookInboxMessages
            .Where(message => message.ProcessedAtUtc != null && message.ProcessedAtUtc < cutoff && message.Payload != "{}")
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.TrimPayloadForRetention(DateTime.UtcNow);
        }

        if (messages.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }
}