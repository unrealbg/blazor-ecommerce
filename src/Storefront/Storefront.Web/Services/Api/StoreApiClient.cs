using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

namespace Storefront.Web.Services.Api;

public sealed class StoreApiClient(HttpClient httpClient) : IStoreApiClient
{
    public async Task<IReadOnlyCollection<StoreProduct>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var products = await httpClient.GetFromJsonAsync<IReadOnlyCollection<StoreProduct>>(
            "/api/v1/catalog/products",
            cancellationToken);

        return products ?? [];
    }

    public Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StoreProduct>($"/api/v1/catalog/products/by-slug/{slug}", cancellationToken);
    }

    public async Task<StoreSearchProductsResponse> SearchProductsAsync(
        StoreSearchProductsRequest request,
        CancellationToken cancellationToken)
    {
        var uri = BuildSearchUri(request);
        var response = await httpClient.GetFromJsonAsync<StoreSearchProductsResponse>(uri, cancellationToken);

        return response ?? new StoreSearchProductsResponse(
            [],
            0,
            1,
            24,
            1,
            new StoreSearchFacets([], [], 0, new StoreSearchPriceSummary(null, null)),
            new StoreSearchAppliedFilters(null, null, [], null, null, null, "popular", 1, 24));
    }

    public async Task<StoreSearchSuggestionsResponse> SuggestProductsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var encodedQuery = Uri.EscapeDataString(query);
        var normalizedLimit = Math.Clamp(limit, 1, 10);
        var uri = $"/api/v1/search/suggest?q={encodedQuery}&limit={normalizedLimit}";

        var response = await httpClient.GetFromJsonAsync<StoreSearchSuggestionsResponse>(uri, cancellationToken);
        return response ?? new StoreSearchSuggestionsResponse(query, []);
    }

    public Task<StoreCart?> GetCartAsync(string customerId, CancellationToken cancellationToken)
    {
        return GetOrDefaultAsync<StoreCart>($"/api/v1/cart/{customerId}", cancellationToken);
    }

    public async Task<bool> AddItemToCartAsync(
        string customerId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new AddCartItemRequest(productId, quantity),
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateCartItemQuantityAsync(
        string customerId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"/api/v1/cart/{customerId}/items/{productId}",
            new UpdateCartItemQuantityRequest(quantity),
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveCartItemAsync(
        string customerId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync($"/api/v1/cart/{customerId}/items/{productId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> CheckoutAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orders/checkout/{customerId}");
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CheckoutResponse>(cancellationToken);
        return payload?.Id;
    }

    public Task<StoreRedirectMatch?> ResolveRedirectAsync(string path, CancellationToken cancellationToken)
    {
        var encodedPath = Uri.EscapeDataString(path);
        return GetOrDefaultAsync<StoreRedirectMatch>($"/api/v1/redirects/resolve?path={encodedPath}", cancellationToken);
    }

    public async Task<StoreRedirectRulePage> GetRedirectRulesAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 20 : pageSize;

        var response = await httpClient.GetFromJsonAsync<StoreRedirectRulePage>(
            $"/api/v1/redirects?page={normalizedPage}&pageSize={normalizedPageSize}",
            cancellationToken);

        return response ?? new StoreRedirectRulePage(normalizedPage, normalizedPageSize, 0, []);
    }

    public async Task<Guid?> CreateRedirectRuleAsync(
        string fromPath,
        string toPath,
        int statusCode,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/v1/redirects",
            new CreateRedirectRuleRequest(fromPath, toPath, statusCode),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<CreateRedirectRuleResponse>(cancellationToken);
        return payload?.Id;
    }

    public async Task<bool> DeactivateRedirectRuleAsync(Guid redirectRuleId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/redirects/{redirectRuleId}/deactivate");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static string BuildSearchUri(StoreSearchProductsRequest request)
    {
        var queryParameters = new List<KeyValuePair<string, string?>>();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("q", request.Query.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("categorySlug", request.CategorySlug.Trim()));
        }

        foreach (var brand in request.Brands.Where(brand => !string.IsNullOrWhiteSpace(brand)))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("brand", brand.Trim()));
        }

        if (request.MinPrice is not null)
        {
            queryParameters.Add(new KeyValuePair<string, string?>(
                "minPrice",
                request.MinPrice.Value.ToString("0.##", CultureInfo.InvariantCulture)));
        }

        if (request.MaxPrice is not null)
        {
            queryParameters.Add(new KeyValuePair<string, string?>(
                "maxPrice",
                request.MaxPrice.Value.ToString("0.##", CultureInfo.InvariantCulture)));
        }

        if (request.InStock is not null)
        {
            queryParameters.Add(new KeyValuePair<string, string?>("inStock", request.InStock.Value ? "true" : "false"));
        }

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            queryParameters.Add(new KeyValuePair<string, string?>("sort", request.Sort));
        }

        queryParameters.Add(new KeyValuePair<string, string?>("page", Math.Max(1, request.Page).ToString()));
        queryParameters.Add(new KeyValuePair<string, string?>("pageSize", Math.Clamp(request.PageSize, 1, 100).ToString()));

        return queryParameters.Count == 0
            ? "/api/v1/search/products"
            : $"/api/v1/search/products{QueryString.Create(queryParameters)}";
    }

    private async Task<T?> GetOrDefaultAsync<T>(string uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private sealed record AddCartItemRequest(Guid ProductId, int Quantity);

    private sealed record UpdateCartItemQuantityRequest(int Quantity);

    private sealed record CheckoutResponse(Guid Id);

    private sealed record CreateRedirectRuleRequest(string FromPath, string ToPath, int StatusCode);

    private sealed record CreateRedirectRuleResponse(Guid Id);
}
