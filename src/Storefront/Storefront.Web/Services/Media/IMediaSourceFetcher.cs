namespace Storefront.Web.Services.Media;

public interface IMediaSourceFetcher
{
    Task<MediaSourceFetchResult> FetchAsync(
        Uri sourceUri,
        long maxSourceBytes,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
