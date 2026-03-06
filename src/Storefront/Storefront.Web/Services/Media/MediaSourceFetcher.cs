using System.Net;

namespace Storefront.Web.Services.Media;

public sealed class MediaSourceFetcher(
    HttpClient httpClient,
    ILogger<MediaSourceFetcher> logger)
    : IMediaSourceFetcher
{
    public async Task<MediaSourceFetchResult> FetchAsync(
        Uri sourceUri,
        long maxSourceBytes,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        using var request = new HttpRequestMessage(HttpMethod.Get, sourceUri);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutCts.Token);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Failed to fetch source image {SourceUri}", sourceUri);
            return MediaSourceFetchResult.Failed();
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return MediaSourceFetchResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Source image request for {SourceUri} failed with status code {StatusCode}",
                    sourceUri,
                    response.StatusCode);
                return MediaSourceFetchResult.Failed();
            }

            if (response.Content.Headers.ContentLength is > 0 &&
                response.Content.Headers.ContentLength > maxSourceBytes)
            {
                return MediaSourceFetchResult.TooLarge();
            }

            var bytes = await this.ReadWithLimitAsync(
                response,
                maxSourceBytes,
                timeoutCts.Token);

            if (bytes is null)
            {
                return MediaSourceFetchResult.TooLarge();
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            var lastModified = response.Content.Headers.LastModified;

            return MediaSourceFetchResult.Success(bytes, contentType, lastModified);
        }
    }

    private async Task<byte[]?> ReadWithLimitAsync(
        HttpResponseMessage response,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new MemoryStream();

        var buffer = new byte[81920];
        long totalRead = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
            if (totalRead > maxBytes)
            {
                return null;
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return output.ToArray();
    }
}
