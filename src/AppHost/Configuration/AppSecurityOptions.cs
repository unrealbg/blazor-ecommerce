namespace AppHost.Configuration;

public sealed class AppSecurityOptions
{
    public const string SectionName = "Security";

    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    public string FrameOptions { get; set; } = "DENY";

    public string? ContentSecurityPolicy { get; set; } = "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'";
}