namespace Pricing.Domain.Promotions;

public sealed record PromotionScopeData(PromotionScopeType ScopeType, Guid? TargetId);
