namespace AppHost.Configuration;

public sealed class AppRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthPermits { get; set; } = 5;

    public int AuthWindowSeconds { get; set; } = 60;

    public int ReviewPermits { get; set; } = 10;

    public int ReviewWindowSeconds { get; set; } = 300;

    public int SearchSuggestPermits { get; set; } = 30;

    public int SearchSuggestWindowSeconds { get; set; } = 60;

    public int PaymentPermits { get; set; } = 10;

    public int PaymentWindowSeconds { get; set; } = 60;

    public int WebhookPermits { get; set; } = 30;

    public int WebhookWindowSeconds { get; set; } = 60;
}