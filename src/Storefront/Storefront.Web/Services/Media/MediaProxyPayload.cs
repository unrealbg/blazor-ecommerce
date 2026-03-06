namespace Storefront.Web.Services.Media;

public sealed record MediaProxyPayload(
    string FilePath,
    string ContentType,
    string ETag,
    bool FromCache);
