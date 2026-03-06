namespace Storefront.Web.Services.Media;

public sealed record MediaSourceFetchResult(
    MediaSourceFetchStatus Status,
    byte[]? Content,
    string? ContentType,
    DateTimeOffset? LastModified)
{
    public static MediaSourceFetchResult Success(byte[] content, string? contentType, DateTimeOffset? lastModified)
    {
        return new MediaSourceFetchResult(MediaSourceFetchStatus.Success, content, contentType, lastModified);
    }

    public static MediaSourceFetchResult NotFound()
    {
        return new MediaSourceFetchResult(MediaSourceFetchStatus.NotFound, null, null, null);
    }

    public static MediaSourceFetchResult TooLarge()
    {
        return new MediaSourceFetchResult(MediaSourceFetchStatus.TooLarge, null, null, null);
    }

    public static MediaSourceFetchResult Failed()
    {
        return new MediaSourceFetchResult(MediaSourceFetchStatus.Failed, null, null, null);
    }
}
