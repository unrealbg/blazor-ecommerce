using Microsoft.Extensions.Primitives;

namespace Storefront.Web.Services.Seo;

public interface ICanonicalUrlService
{
    CanonicalUrls Build(
        string path,
        IReadOnlyDictionary<string, StringValues> query,
        bool hasNextPage);

    CanonicalUrls BuildForSearch(
        IReadOnlyDictionary<string, StringValues> query,
        bool hasNextPage);

    CanonicalUrls BuildForCategory(
        string categorySlug,
        IReadOnlyDictionary<string, StringValues> query,
        bool hasNextPage);
}
