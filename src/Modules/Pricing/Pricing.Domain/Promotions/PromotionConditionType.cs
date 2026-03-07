namespace Pricing.Domain.Promotions;

public enum PromotionConditionType
{
    MinSubtotal = 0,
    MinQuantity = 1,
    CustomerLoggedIn = 2,
    CategoryInCart = 3,
    VariantInCart = 4,
    CouponRequired = 5,
}
