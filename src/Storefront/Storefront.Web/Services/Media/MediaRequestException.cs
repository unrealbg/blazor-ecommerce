namespace Storefront.Web.Services.Media;

public sealed class MediaRequestException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
