namespace Storefront.Web.Services.Media;

public sealed record MediaSourceResolution(
    Uri? SourceUri,
    int? ErrorStatusCode,
    string? ErrorMessage)
{
    public bool IsSuccess => SourceUri is not null && ErrorStatusCode is null;

    public static MediaSourceResolution Success(Uri sourceUri)
    {
        return new MediaSourceResolution(sourceUri, null, null);
    }

    public static MediaSourceResolution Failure(int statusCode, string errorMessage)
    {
        return new MediaSourceResolution(null, statusCode, errorMessage);
    }
}
