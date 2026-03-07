namespace Storefront.Web.Services.Api;

public sealed class StoreProductRelation
{
    public Guid ProductId { get; init; }

    public string Slug { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? ImageUrl { get; init; }

    public string Currency { get; init; } = "EUR";

    public decimal Amount { get; init; }

    public string RelationType { get; init; } = string.Empty;
}
