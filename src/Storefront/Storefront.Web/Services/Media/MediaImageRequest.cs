namespace Storefront.Web.Services.Media;

public sealed record MediaImageRequest(
    string Source,
    int? Width,
    int? Height,
    MediaFitMode Fit,
    MediaOutputFormat Format,
    MediaSourceOrigin Origin = MediaSourceOrigin.Auto);
