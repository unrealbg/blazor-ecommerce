namespace Storefront.Web.Services.Api;

public sealed record StorePromotionCondition(int ConditionType, int Operator, string Value);
