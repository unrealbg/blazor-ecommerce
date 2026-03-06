using Shipping.Domain.Shipping;

namespace Shipping.Application.Shipping;

public interface ICarrierWebhookInboxRepository
{
    Task<CarrierWebhookInboxMessage?> GetByProviderAndEventIdAsync(
        string provider,
        string externalEventId,
        CancellationToken cancellationToken);

    Task AddAsync(CarrierWebhookInboxMessage message, CancellationToken cancellationToken);
}
