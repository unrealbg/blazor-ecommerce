namespace Storefront.Web.Services.Api;

public sealed record StoreCart(
    Guid Id,
    string CustomerId,
    IReadOnlyCollection<StoreCartLine> Lines);
