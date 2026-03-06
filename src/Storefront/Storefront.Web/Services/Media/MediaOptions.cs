namespace Storefront.Web.Services.Media;

public sealed class MediaOptions
{
    public const string SectionName = "Media";

    public string[] AllowedHosts { get; set; } = [];

    public string CachePath { get; set; } = "cache/media";

    public int DefaultQualityJpeg { get; set; } = 82;

    public int DefaultQualityWebp { get; set; } = 80;

    public int DefaultQualityAvif { get; set; } = 55;

    public bool EnableAvif { get; set; } = true;

    public long MaxSourceBytes { get; set; } = 20 * 1024 * 1024;

    public int FetchTimeoutSeconds { get; set; } = 10;

    public bool AllowUpscale { get; set; }
}
