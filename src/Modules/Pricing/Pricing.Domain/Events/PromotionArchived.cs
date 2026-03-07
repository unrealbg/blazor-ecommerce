using BuildingBlocks.Domain.Primitives;

namespace Pricing.Domain.Events;

public sealed record PromotionArchived(Guid PromotionId) : DomainEvent;
