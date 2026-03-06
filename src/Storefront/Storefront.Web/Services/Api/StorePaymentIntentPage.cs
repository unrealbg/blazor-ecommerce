namespace Storefront.Web.Services.Api;

public sealed record StorePaymentIntentPage(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<StorePaymentIntentSummary> Items);
