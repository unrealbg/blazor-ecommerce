namespace Storefront.Web.Services.Media;

public interface IMediaImageProxyService
{
    Task<MediaProxyPayload> GetImageAsync(
        MediaImageRequest request,
        string? acceptHeader,
        CancellationToken cancellationToken);
}
