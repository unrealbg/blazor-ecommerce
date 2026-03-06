using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Storefront.Web.Services.Content;

namespace Storefront.Web.Services.Media;

public static class MediaPipelineExtensions
{
    public static IServiceCollection AddMediaPipeline(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
            [
                "application/xml",
                "application/rss+xml",
                "text/plain",
            ]);
        });

        services.AddHttpClient<IMediaSourceFetcher, MediaSourceFetcher>();
        services.AddScoped<IMediaSourceResolver, MediaSourceResolver>();
        services.AddScoped<IMediaImageProxyService, MediaImageProxyService>();
        services.AddScoped<IMediaUrlService, MediaUrlService>();
        services.AddScoped<IContentHtmlRenderer, ContentHtmlRenderer>();

        return services;
    }

    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/media/image",
            async Task<IResult> (
                HttpContext context,
                IMediaImageProxyService mediaImageProxyService,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    var request = BuildRequest(context.Request.Query);
                    var payload = await mediaImageProxyService.GetImageAsync(
                        request,
                        context.Request.Headers.Accept.ToString(),
                        cancellationToken);

                    context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                    context.Response.Headers.ETag = payload.ETag;
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["X-Media-Cache"] = payload.FromCache ? "HIT" : "MISS";

                    if (MatchesIfNoneMatch(context.Request.Headers.IfNoneMatch.ToString(), payload.ETag))
                    {
                        return Results.StatusCode(StatusCodes.Status304NotModified);
                    }

                    return Results.File(payload.FilePath, payload.ContentType, enableRangeProcessing: true);
                }
                catch (MediaRequestException exception)
                {
                    return Results.Problem(
                        title: exception.Message,
                        statusCode: exception.StatusCode);
                }
                catch (Exception exception)
                {
                    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("MediaEndpoint");
                    logger.LogError(exception, "Unhandled media endpoint error.");

                    return Results.Problem(
                        title: "Image processing failed.",
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .WithName("MediaImage")
            .WithTags("Media");

        return endpoints;
    }

    private static MediaImageRequest BuildRequest(IQueryCollection query)
    {
        var source = query["src"].ToString();
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new MediaRequestException(StatusCodes.Status400BadRequest, "src query parameter is required.");
        }

        var width = ParseInt(query["w"].ToString(), "w");
        var height = ParseInt(query["h"].ToString(), "h");

        var fit = ParseFit(query["fit"].ToString());
        var format = ParseFormat(query["format"].ToString());

        return new MediaImageRequest(source, width, height, fit, format, MediaSourceOrigin.Auto);
    }

    private static int? ParseInt(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, out var parsedValue))
        {
            throw new MediaRequestException(
                StatusCodes.Status400BadRequest,
                $"{parameter} query parameter must be an integer.");
        }

        return parsedValue;
    }

    private static MediaFitMode ParseFit(string fit)
    {
        if (string.IsNullOrWhiteSpace(fit))
        {
            return MediaFitMode.Max;
        }

        return fit.Trim().ToLowerInvariant() switch
        {
            "max" => MediaFitMode.Max,
            "cover" => MediaFitMode.Cover,
            "contain" => MediaFitMode.Contain,
            _ => throw new MediaRequestException(StatusCodes.Status400BadRequest, "Unsupported fit mode."),
        };
    }

    private static MediaOutputFormat ParseFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return MediaOutputFormat.Auto;
        }

        return format.Trim().ToLowerInvariant() switch
        {
            "auto" => MediaOutputFormat.Auto,
            "webp" => MediaOutputFormat.Webp,
            "avif" => MediaOutputFormat.Avif,
            "jpeg" => MediaOutputFormat.Jpeg,
            "jpg" => MediaOutputFormat.Jpeg,
            "png" => MediaOutputFormat.Png,
            _ => throw new MediaRequestException(StatusCodes.Status400BadRequest, "Unsupported output format."),
        };
    }

    private static bool MatchesIfNoneMatch(string ifNoneMatchHeader, string etag)
    {
        if (string.IsNullOrWhiteSpace(ifNoneMatchHeader) || string.IsNullOrWhiteSpace(etag))
        {
            return false;
        }

        return ifNoneMatchHeader
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => string.Equals(value, etag, StringComparison.Ordinal));
    }
}
