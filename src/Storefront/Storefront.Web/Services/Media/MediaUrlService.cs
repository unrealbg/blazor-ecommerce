using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Storefront.Web.Services.Seo;

namespace Storefront.Web.Services.Media;

[SuppressMessage("Style", "SA1204:Static members should appear before instance members", Justification = "Keeps helper methods close to usage.")]
public sealed class MediaUrlService(
    IPageMetadataService pageMetadataService,
    IMediaSourceResolver sourceResolver)
    : IMediaUrlService
{
    private const string DefaultOgImageRelativePath = "/images/og-default.jpg";

    public string ProductImage(string source, int width, int? height = null)
    {
        return this.BuildOrFallback(
            source,
            MediaSourceOrigin.Site,
            width,
            height,
            MediaFitMode.Max,
            MediaOutputFormat.Auto);
    }

    public string BlogCover(string source, int width, int? height = null)
    {
        return this.BuildOrFallback(
            source,
            MediaSourceOrigin.Cms,
            width,
            height,
            MediaFitMode.Cover,
            MediaOutputFormat.Auto);
    }

    public string OgImage(string? source)
    {
        var candidate = string.IsNullOrWhiteSpace(source)
            ? DefaultOgImageRelativePath
            : source;

        var origin = ResolveOrigin(candidate);
        return this.BuildOrFallback(
            candidate,
            origin,
            1200,
            630,
            MediaFitMode.Cover,
            MediaOutputFormat.Auto);
    }

    public string ContentInline(string source, int width)
    {
        if (this.TryContentInline(source, width, out var proxiedUrl))
        {
            return proxiedUrl;
        }

        return this.OgImage(null);
    }

    public bool TryContentInline(string source, int width, out string proxiedUrl)
    {
        return this.TryBuild(
            source,
            MediaSourceOrigin.Cms,
            width,
            null,
            MediaFitMode.Max,
            MediaOutputFormat.Auto,
            out proxiedUrl);
    }

    private string BuildOrFallback(
        string source,
        MediaSourceOrigin origin,
        int width,
        int? height,
        MediaFitMode fit,
        MediaOutputFormat format)
    {
        if (this.TryBuild(source, origin, width, height, fit, format, out var proxied))
        {
            return proxied;
        }

        this.TryBuild(
            DefaultOgImageRelativePath,
            MediaSourceOrigin.Site,
            width,
            height,
            fit,
            format,
            out var fallback);

        return fallback;
    }

    private bool TryBuild(
        string source,
        MediaSourceOrigin origin,
        int width,
        int? height,
        MediaFitMode fit,
        MediaOutputFormat format,
        out string proxiedUrl)
    {
        proxiedUrl = string.Empty;

        if (IsAlreadyProxied(source, out var existingProxiedUrl))
        {
            proxiedUrl = existingProxiedUrl;
            return true;
        }

        var resolution = sourceResolver.Resolve(source, origin);
        if (!resolution.IsSuccess || resolution.SourceUri is null)
        {
            return false;
        }

        var normalizedWidth = Math.Max(1, width);
        var queryParameters = new List<KeyValuePair<string, string?>>
        {
            new("src", resolution.SourceUri.AbsoluteUri),
            new("w", normalizedWidth.ToString()),
            new("fit", fit.ToString().ToLowerInvariant()),
            new("format", format.ToString().ToLowerInvariant()),
        };

        if (height is > 0)
        {
            queryParameters.Add(new KeyValuePair<string, string?>("h", height.Value.ToString()));
        }

        var mediaPath = $"/media/image{QueryString.Create(queryParameters)}";
        proxiedUrl = pageMetadataService.BuildAbsoluteUrl(mediaPath);
        return true;
    }

    private static bool IsAlreadyProxied(string source, out string absoluteUrl)
    {
        absoluteUrl = string.Empty;

        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        if (!Uri.TryCreate(source.Trim(), UriKind.RelativeOrAbsolute, out var uri))
        {
            return false;
        }

        if (!uri.IsAbsoluteUri)
        {
            if (!source.StartsWith("/media/image", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            absoluteUrl = source;
            return true;
        }

        if (!string.Equals(uri.AbsolutePath, "/media/image", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        absoluteUrl = source;
        return true;
    }

    private static MediaSourceOrigin ResolveOrigin(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return MediaSourceOrigin.Site;
        }

        return source.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)
               || source.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)
               || source.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
            ? MediaSourceOrigin.Cms
            : MediaSourceOrigin.Auto;
    }
}
