using BuildingBlocks.Domain.Results;

namespace Backoffice.Application.Backoffice;

public interface ISystemOperationsService
{
    Task<Result> RetryOutboxMessageAsync(Guid outboxMessageId, CancellationToken cancellationToken);

    Task<Result<bool>> ReprocessPaymentWebhookAsync(Guid webhookMessageId, CancellationToken cancellationToken);

    Task<Result<bool>> ReprocessShippingWebhookAsync(Guid webhookMessageId, CancellationToken cancellationToken);
}