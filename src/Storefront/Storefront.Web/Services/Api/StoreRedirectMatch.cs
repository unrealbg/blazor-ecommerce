namespace Storefront.Web.Services.Api;

public sealed record StoreRedirectMatch(
    string FromPath,
    string ToPath,
    int StatusCode);
