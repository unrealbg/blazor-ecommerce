using Microsoft.EntityFrameworkCore;
using Payments.Application.Payments;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

internal sealed class WebhookInboxRepository(PaymentsDbContext dbContext) : IWebhookInboxRepository
{
    public Task AddAsync(WebhookInboxMessage message, CancellationToken cancellationToken)
    {
        return dbContext.WebhookInboxMessages.AddAsync(message, cancellationToken).AsTask();
    }

    public Task<WebhookInboxMessage?> GetByProviderAndExternalEventIdAsync(
        string provider,
        string externalEventId,
        CancellationToken cancellationToken)
    {
        return dbContext.WebhookInboxMessages.SingleOrDefaultAsync(
            message => message.Provider == provider && message.ExternalEventId == externalEventId,
            cancellationToken);
    }
}
