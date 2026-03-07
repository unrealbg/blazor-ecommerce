namespace Storefront.Web.Services.Api;

public sealed class StoreProductOptionSelection
{
    public Guid ProductOptionId { get; init; }

    public string OptionName { get; init; } = string.Empty;

    public Guid ProductOptionValueId { get; init; }

    public string Value { get; init; } = string.Empty;
}
