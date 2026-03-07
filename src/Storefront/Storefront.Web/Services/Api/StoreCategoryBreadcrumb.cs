namespace Storefront.Web.Services.Api;

public sealed class StoreCategoryBreadcrumb
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;
}
