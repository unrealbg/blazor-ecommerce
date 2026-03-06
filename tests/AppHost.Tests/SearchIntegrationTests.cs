using System.Net;
using System.Net.Http.Json;
using Catalog.Api;

namespace AppHost.Tests;

public sealed class SearchIntegrationTests
{
    [Fact]
    public async Task SearchByQuery_Should_ReturnExpectedProduct()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = testFactory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await CreateProductAsync(client, $"Gaming Keyboard {suffix}", "Peripherals", "Contoso", 129m);
        await RebuildSearchAsync(client);

        var response = await client.GetFromJsonAsync<SearchProductsResponse>($"/api/v1/search/products?q={suffix}");

        Assert.NotNull(response);
        Assert.Contains(response!.Items, item => item.Name.Contains(suffix, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CategoryFilter_Should_ReturnOnlyMatchingCategory()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = testFactory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await CreateProductAsync(client, $"Keyboard {suffix}", "Keyboards", "Contoso", 59m);
        await CreateProductAsync(client, $"Mouse {suffix}", "Mice", "Contoso", 49m);
        await RebuildSearchAsync(client);

        var response = await client.GetFromJsonAsync<SearchProductsResponse>("/api/v1/search/products?categorySlug=keyboards");

        Assert.NotNull(response);
        Assert.NotEmpty(response!.Items);
        Assert.All(response.Items, item => Assert.Equal("keyboards", item.CategorySlug));
    }

    [Fact]
    public async Task BrandFilter_Should_ReturnOnlyMatchingBrand()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = testFactory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await CreateProductAsync(client, $"Monitor {suffix}", "Displays", "Northwind", 199m);
        await CreateProductAsync(client, $"Monitor Plus {suffix}", "Displays", "Tailspin", 249m);
        await RebuildSearchAsync(client);

        var response = await client.GetFromJsonAsync<SearchProductsResponse>("/api/v1/search/products?brand=Northwind");

        Assert.NotNull(response);
        Assert.NotEmpty(response!.Items);
        Assert.All(response.Items, item => Assert.Equal("Northwind", item.Brand));
    }

    [Fact]
    public async Task PriceRangeFilter_Should_ReturnItemsWithinBounds()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = testFactory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await CreateProductAsync(client, $"Budget Mouse {suffix}", "Mice", "Fabrikam", 19m);
        await CreateProductAsync(client, $"Premium Mouse {suffix}", "Mice", "Fabrikam", 89m);
        await RebuildSearchAsync(client);

        var response = await client.GetFromJsonAsync<SearchProductsResponse>(
            "/api/v1/search/products?minPrice=50&maxPrice=100");

        Assert.NotNull(response);
        Assert.NotEmpty(response!.Items);
        Assert.All(response.Items, item => Assert.InRange(item.PriceAmount, 50m, 100m));
    }

    [Fact]
    public async Task SuggestEndpoint_Should_ReturnRelevantSuggestions()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = testFactory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        await CreateProductAsync(client, $"Iphone Case {suffix}", "Cases", "Contoso", 25m);
        await RebuildSearchAsync(client);

        var response = await client.GetAsync("/api/v1/search/suggest?q=iph");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Iphone Case", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RebuildEndpoint_Should_RepopulateSearchIndex()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = testFactory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await CreateProductAsync(client, $"Webcam {suffix}", "Cameras", "Contoso", 119m);

        var rebuildResponse = await client.PostAsync("/api/v1/search/rebuild", content: null);
        rebuildResponse.EnsureSuccessStatusCode();

        var payload = await rebuildResponse.Content.ReadFromJsonAsync<RebuildResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.IndexedDocuments > 0);

        var searchResponse = await client.GetFromJsonAsync<SearchProductsResponse>($"/api/v1/search/products?q={suffix}");
        Assert.NotNull(searchResponse);
        Assert.Contains(searchResponse!.Items, item => item.Name.Contains(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task CreateProductAsync(
        HttpClient client,
        string name,
        string categoryName,
        string brand,
        decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/catalog/products",
            new CatalogModuleExtensions.CreateProductRequest(
                Name: name,
                Description: $"{name} description",
                Currency: "EUR",
                Amount: amount,
                IsActive: true,
                Brand: brand,
                Sku: $"SKU-{Guid.NewGuid():N}",
                ImageUrl: "/images/test.png",
                IsInStock: true,
                CategorySlug: categoryName.Trim().ToLowerInvariant().Replace(' ', '-'),
                CategoryName: categoryName));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task RebuildSearchAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/v1/search/rebuild", content: null);
        response.EnsureSuccessStatusCode();
    }

    private sealed record SearchProductsResponse(IReadOnlyCollection<SearchProductItem> Items);

    private sealed record SearchProductItem(
        Guid ProductId,
        string Slug,
        string Name,
        string? CategorySlug,
        string? Brand,
        decimal PriceAmount);

    private sealed record RebuildResponse(int IndexedDocuments);
}
