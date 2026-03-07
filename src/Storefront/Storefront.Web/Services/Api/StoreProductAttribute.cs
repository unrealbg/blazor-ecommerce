namespace Storefront.Web.Services.Api;

public sealed class StoreProductAttribute
{
    public Guid Id { get; init; }

    public string? GroupName { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public int Position { get; init; }

    public bool IsFilterable { get; init; }
}
