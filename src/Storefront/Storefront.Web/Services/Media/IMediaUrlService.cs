namespace Storefront.Web.Services.Media;

public interface IMediaUrlService
{
    string ProductImage(string source, int width, int? height = null);

    string BlogCover(string source, int width, int? height = null);

    string OgImage(string? source);

    string ContentInline(string source, int width);

    bool TryContentInline(string source, int width, out string proxiedUrl);
}
