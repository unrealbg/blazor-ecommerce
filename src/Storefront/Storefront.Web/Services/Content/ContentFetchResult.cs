namespace Storefront.Web.Services.Content;

public sealed record ContentFetchResult<T>(ContentFetchStatus Status, T? Value)
{
    public bool IsSuccess => this.Status == ContentFetchStatus.Success;

    public bool IsNotFound => this.Status == ContentFetchStatus.NotFound;

    public bool IsUnavailable => this.Status == ContentFetchStatus.Unavailable;

    public static ContentFetchResult<T> Success(T value)
    {
        return new ContentFetchResult<T>(ContentFetchStatus.Success, value);
    }

    public static ContentFetchResult<T> NotFound()
    {
        return new ContentFetchResult<T>(ContentFetchStatus.NotFound, default);
    }

    public static ContentFetchResult<T> Unavailable()
    {
        return new ContentFetchResult<T>(ContentFetchStatus.Unavailable, default);
    }
}
