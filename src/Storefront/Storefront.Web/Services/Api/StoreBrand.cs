namespace Storefront.Web.Services.Api;

public sealed class StoreBrand
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? LogoImageUrl { get; init; }

    public bool IsActive { get; init; }
}
