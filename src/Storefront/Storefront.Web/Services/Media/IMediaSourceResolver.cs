namespace Storefront.Web.Services.Media;

public interface IMediaSourceResolver
{
    MediaSourceResolution Resolve(string source, MediaSourceOrigin origin);
}
