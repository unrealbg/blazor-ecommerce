using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Storefront.Web.Services.Content;
using Storefront.Web.Services.Seo;

namespace Storefront.Web.Services.Media;

[SuppressMessage("Style", "SA1204:Static members should appear before instance members", Justification = "Keeps helper methods close to usage.")]
public sealed class MediaSourceResolver(
    IOptions<MediaOptions> mediaOptions,
    IOptions<CmsOptions> cmsOptions,
    IOptions<SiteOptions> siteOptions)
    : IMediaSourceResolver
{
    private static readonly StringComparer HostComparer = StringComparer.OrdinalIgnoreCase;

    private readonly Uri cmsBaseUri = NormalizeBaseUrl(cmsOptions.Value.BaseUrl, "http://localhost:8055");
    private readonly Uri siteBaseUri = NormalizeBaseUrl(siteOptions.Value.BaseUrl, "http://localhost:5100");
    private readonly HashSet<string> explicitAllowedHosts = BuildHostSet(mediaOptions.Value.AllowedHosts);
    private readonly HashSet<string> allowedHosts = BuildAllowedHostSet(
        mediaOptions.Value.AllowedHosts,
        cmsOptions.Value.BaseUrl,
        siteOptions.Value.BaseUrl);

    public MediaSourceResolution Resolve(string source, MediaSourceOrigin origin)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return MediaSourceResolution.Failure(StatusCodes.Status400BadRequest, "Image source is required.");
        }

        if (!TryBuildSourceUri(source, origin, out var sourceUri))
        {
            return MediaSourceResolution.Failure(StatusCodes.Status400BadRequest, "Invalid image source.");
        }

        if (!string.Equals(sourceUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(sourceUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return MediaSourceResolution.Failure(StatusCodes.Status400BadRequest, "Only HTTP/HTTPS image sources are supported.");
        }

        if (!string.IsNullOrWhiteSpace(sourceUri.UserInfo))
        {
            return MediaSourceResolution.Failure(StatusCodes.Status400BadRequest, "Image source credentials are not allowed.");
        }

        var host = sourceUri.Host.ToLowerInvariant();
        var authority = BuildAuthority(sourceUri);

        if (!this.allowedHosts.Contains(authority) && !this.allowedHosts.Contains(host))
        {
            return MediaSourceResolution.Failure(StatusCodes.Status403Forbidden, "Image source host is not allowed.");
        }

        if (IsPrivateOrLoopback(sourceUri) &&
            !this.explicitAllowedHosts.Contains(authority) &&
            !this.explicitAllowedHosts.Contains(host))
        {
            return MediaSourceResolution.Failure(StatusCodes.Status403Forbidden, "Private network image source is not allowed.");
        }

        return MediaSourceResolution.Success(sourceUri);
    }

    private bool TryBuildSourceUri(string source, MediaSourceOrigin origin, out Uri sourceUri)
    {
        sourceUri = this.cmsBaseUri;

        var normalizedSource = source.Trim();
        if (normalizedSource.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        if (Uri.TryCreate(normalizedSource, UriKind.Absolute, out var absoluteUri))
        {
            sourceUri = absoluteUri;
            return true;
        }

        var relativePath = NormalizeRelativePath(normalizedSource);
        var baseUri = origin switch
        {
            MediaSourceOrigin.Site => this.siteBaseUri,
            MediaSourceOrigin.Cms => this.cmsBaseUri,
            _ => ResolveAutoBaseUri(relativePath),
        };

        if (!Uri.TryCreate(baseUri, relativePath, out var combined))
        {
            return false;
        }

        sourceUri = combined;
        return true;
    }

    private Uri ResolveAutoBaseUri(string relativePath)
    {
        if (relativePath.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return this.cmsBaseUri;
        }

        if (relativePath.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
        {
            return this.siteBaseUri;
        }

        return this.cmsBaseUri;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "/";
        }

        return normalized.StartsWith("/", StringComparison.Ordinal) ? normalized : $"/{normalized}";
    }

    private static string BuildAuthority(Uri uri)
    {
        var port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        return $"{uri.Host.ToLowerInvariant()}{port}";
    }

    private static HashSet<string> BuildAllowedHostSet(
        IEnumerable<string> configuredHosts,
        string cmsBaseUrl,
        string siteBaseUrl)
    {
        var set = BuildHostSet(configuredHosts);

        AddHostFromBaseUrl(set, cmsBaseUrl);
        AddHostFromBaseUrl(set, siteBaseUrl);

        return set;
    }

    private static void AddHostFromBaseUrl(HashSet<string> set, string baseUrl)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            set.Add(BuildAuthority(uri));
            set.Add(uri.Host.ToLowerInvariant());
        }
    }

    private static HashSet<string> BuildHostSet(IEnumerable<string> hostValues)
    {
        var set = new HashSet<string>(HostComparer);

        foreach (var hostValue in hostValues)
        {
            if (string.IsNullOrWhiteSpace(hostValue))
            {
                continue;
            }

            var normalized = hostValue.Trim().ToLowerInvariant();
            if (normalized.Contains("://", StringComparison.Ordinal) &&
                Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
            {
                set.Add(BuildAuthority(absoluteUri));
                set.Add(absoluteUri.Host.ToLowerInvariant());
                continue;
            }

            set.Add(normalized.TrimEnd('/'));
            var separatorIndex = normalized.IndexOf(':');
            if (separatorIndex > 0)
            {
                set.Add(normalized[..separatorIndex]);
            }
        }

        return set;
    }

    private static Uri NormalizeBaseUrl(string value, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return new Uri(fallback, UriKind.Absolute);
        }

        return uri;
    }

    private static bool IsPrivateOrLoopback(Uri uri)
    {
        if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(uri.Host, out var address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254);
        }

        return address.IsIPv6LinkLocal ||
               address.IsIPv6SiteLocal ||
               (bytes[0] & 0xFE) == 0xFC;
    }
}
