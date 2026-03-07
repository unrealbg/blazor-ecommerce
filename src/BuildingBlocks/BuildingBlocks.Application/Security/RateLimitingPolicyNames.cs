namespace BuildingBlocks.Application.Security;

public static class RateLimitingPolicyNames
{
    public const string Auth = "auth";
    public const string ReviewsWrite = "reviews-write";
    public const string SearchSuggest = "search-suggest";
    public const string PaymentMutations = "payment-mutations";
    public const string PublicWebhook = "public-webhook";
}