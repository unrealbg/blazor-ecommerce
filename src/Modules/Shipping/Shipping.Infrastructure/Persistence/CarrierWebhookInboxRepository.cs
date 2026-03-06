using Microsoft.EntityFrameworkCore;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Persistence;

internal sealed class CarrierWebhookInboxRepository(ShippingDbContext dbContext) : ICarrierWebhookInboxRepository
{
    public Task<CarrierWebhookInboxMessage?> GetByProviderAndEventIdAsync(
        string provider,
        string externalEventId,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim();
        var normalizedEventId = externalEventId.Trim();

        return dbContext.CarrierWebhookInboxMessages
            .SingleOrDefaultAsync(
                entity => entity.Provider == normalizedProvider &&
                          entity.ExternalEventId == normalizedEventId,
                cancellationToken);
    }

    public Task AddAsync(CarrierWebhookInboxMessage message, CancellationToken cancellationToken)
    {
        return dbContext.CarrierWebhookInboxMessages.AddAsync(message, cancellationToken).AsTask();
    }
}
