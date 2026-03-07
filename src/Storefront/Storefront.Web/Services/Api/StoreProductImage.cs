namespace Storefront.Web.Services.Api;

public sealed class StoreProductImage
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public Guid? VariantId { get; init; }

    public string SourceUrl { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public int Position { get; init; }

    public bool IsPrimary { get; init; }
}
