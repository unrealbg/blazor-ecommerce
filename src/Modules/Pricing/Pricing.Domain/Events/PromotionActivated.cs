using BuildingBlocks.Domain.Primitives;

namespace Pricing.Domain.Events;

public sealed record PromotionActivated(Guid PromotionId) : DomainEvent;
