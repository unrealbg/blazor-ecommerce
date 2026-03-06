namespace Storefront.Web.Services.Api;

public sealed record StoreRedirectRulePage(
    int Page,
    int PageSize,
    long TotalCount,
    IReadOnlyCollection<StoreRedirectRule> Items);
