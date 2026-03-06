using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Catalog.Domain.Products.Events;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Products.OnProductSlugChanged;

public sealed class ProductSlugChangedDomainEventHandler(
    IRedirectRuleWriter redirectRuleWriter,
    ILogger<ProductSlugChangedDomainEventHandler> logger)
    : IDomainEventHandler<ProductSlugChanged>
{
    public async Task Handle(ProductSlugChanged domainEvent, CancellationToken cancellationToken)
    {
        await redirectRuleWriter.UpsertAsync(
            $"/product/{domainEvent.PreviousSlug}",
            $"/product/{domainEvent.CurrentSlug}",
            301,
            cancellationToken);

        logger.LogInformation(
            "Product slug changed redirect was registered. ProductId: {ProductId}, From: {FromPath}, To: {ToPath}",
            domainEvent.ProductId,
            domainEvent.PreviousSlug,
            domainEvent.CurrentSlug);
    }
}
