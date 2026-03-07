using Microsoft.Extensions.Options;
using Storefront.Web.Configuration;

namespace Storefront.Web.Services.Runtime;

public sealed class StorefrontBuildInfo(IOptions<BuildMetadataOptions> options)
{
    private readonly BuildMetadataOptions options = options.Value;

    public string ApplicationName => options.ApplicationName;

    public string Version => string.IsNullOrWhiteSpace(options.Version) ? "dev" : options.Version.Trim();

    public string? SourceRevisionId => string.IsNullOrWhiteSpace(options.SourceRevisionId) ? null : options.SourceRevisionId.Trim();

    public DateTimeOffset? BuildTimestampUtc
    {
        get
        {
            if (string.IsNullOrWhiteSpace(options.BuildTimestampUtc))
            {
                return null;
            }

            return DateTimeOffset.TryParse(options.BuildTimestampUtc, out var timestamp)
                ? timestamp.ToUniversalTime()
                : null;
        }
    }

    public string ShortVersionLabel
    {
        get
        {
            var sourceRevision = SourceRevisionId;
            if (string.IsNullOrWhiteSpace(sourceRevision))
            {
                return Version;
            }

            var shortSha = sourceRevision.Length <= 7 ? sourceRevision : sourceRevision[..7];
            return $"{Version} ({shortSha})";
        }
    }
}