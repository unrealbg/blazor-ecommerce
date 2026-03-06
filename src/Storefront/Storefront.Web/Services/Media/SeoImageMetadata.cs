namespace Storefront.Web.Services.Media;

public static class SeoImageMetadata
{
    public static string ResolveOpenGraphImage(IMediaUrlService mediaUrlService, string? imageSource)
    {
        return mediaUrlService.OgImage(imageSource);
    }
}
