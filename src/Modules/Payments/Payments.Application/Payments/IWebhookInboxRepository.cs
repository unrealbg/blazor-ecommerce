using Payments.Domain.Payments;

namespace Payments.Application.Payments;

public interface IWebhookInboxRepository
{
    Task AddAsync(WebhookInboxMessage message, CancellationToken cancellationToken);

    Task<WebhookInboxMessage?> GetByProviderAndExternalEventIdAsync(
        string provider,
        string externalEventId,
        CancellationToken cancellationToken);
}
