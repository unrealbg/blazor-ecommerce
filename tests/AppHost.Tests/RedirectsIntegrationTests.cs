using System.Net;
using System.Net.Http.Json;
using Catalog.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Redirects.Api;
using Redirects.Domain.RedirectRules;
using Redirects.Infrastructure.Persistence;

namespace AppHost.Tests;

public sealed class RedirectsIntegrationTests
{
    [Fact]
    public async Task BlogOldPath_Should_Return301_ToNewPath()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = CreateNoRedirectClient(testFactory);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var fromPath = $"/blog/old-{suffix}";
        var toPath = $"/blog/new-{suffix}";

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/redirects",
            new RedirectsModuleExtensions.CreateRedirectRuleRequest(fromPath, toPath));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var redirectResponse = await client.GetAsync(fromPath);

        Assert.Equal(HttpStatusCode.MovedPermanently, redirectResponse.StatusCode);
        Assert.EndsWith(toPath, redirectResponse.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Redirect_Should_PreserveQueryString()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = CreateNoRedirectClient(testFactory);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var fromPath = $"/blog/query-old-{suffix}";
        var toPath = $"/blog/query-new-{suffix}";

        await client.PostAsJsonAsync(
            "/api/v1/redirects",
            new RedirectsModuleExtensions.CreateRedirectRuleRequest(fromPath, toPath));

        var redirectResponse = await client.GetAsync($"{fromPath}?x=1&y=2");

        Assert.Equal(HttpStatusCode.MovedPermanently, redirectResponse.StatusCode);
        Assert.EndsWith($"{toPath}?x=1&y=2", redirectResponse.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoopRule_Should_NotRedirect_AndContinuePipeline()
    {
        await using var testFactory = new AppHostWebApplicationFactory();

        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RedirectsDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var samePath = $"/loop-{suffix}";
        var createResult = RedirectRule.Create(samePath, samePath, 301, DateTime.UtcNow);

        Assert.True(createResult.IsSuccess);

        dbContext.RedirectRules.Add(createResult.Value);
        await dbContext.SaveChangesAsync();

        using var client = CreateNoRedirectClient(testFactory);
        var response = await client.GetAsync(samePath);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProductSlugChange_Should_CreateAutomaticRedirect()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = CreateNoRedirectClient(testFactory);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var oldName = $"Old Product {suffix}";
        var oldSlug = $"old-product-{suffix}";
        var newSlug = $"new-product-{suffix}";

        var createProductResponse = await client.PostAsJsonAsync(
            "/api/v1/catalog/products",
            new CatalogModuleExtensions.CreateProductRequest(
                Name: oldName,
                Description: "Test product",
                Currency: "EUR",
                Amount: 10m,
                IsActive: true,
                Brand: "TestBrand",
                Sku: $"SKU-{suffix}",
                ImageUrl: null,
                IsInStock: true,
                CategorySlug: null,
                CategoryName: null));

        createProductResponse.EnsureSuccessStatusCode();

        var payload = await createProductResponse.Content.ReadFromJsonAsync<CreateProductResponse>();
        Assert.NotNull(payload);

        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/catalog/products/{payload.Id}/slug",
            new CatalogModuleExtensions.UpdateProductSlugRequest(newSlug));

        updateResponse.EnsureSuccessStatusCode();

        HttpResponseMessage? redirectResponse = null;
        for (var retry = 0; retry < 40; retry++)
        {
            var candidate = await client.GetAsync($"/product/{oldSlug}");
            if (candidate.StatusCode == HttpStatusCode.MovedPermanently)
            {
                redirectResponse = candidate;
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150));
        }

        Assert.NotNull(redirectResponse);
        Assert.EndsWith($"/product/{newSlug}", redirectResponse!.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DirectusWebhook_Should_CreateRedirectRule()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = CreateNoRedirectClient(testFactory);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var oldSlug = $"old-post-{suffix}";
        var newSlug = $"new-post-{suffix}";

        var webhookResponse = await client.PostAsJsonAsync(
            "/api/webhooks/directus",
            new RedirectsModuleExtensions.DirectusSlugWebhookRequest(
                Collection: "blog_posts",
                Event: "items.update",
                OldSlug: oldSlug,
                NewSlug: newSlug,
                Data: null,
                Previous: null));

        Assert.Equal(HttpStatusCode.Accepted, webhookResponse.StatusCode);

        var redirectResponse = await client.GetAsync($"/blog/{oldSlug}");

        Assert.Equal(HttpStatusCode.MovedPermanently, redirectResponse.StatusCode);
        Assert.EndsWith($"/blog/{newSlug}", redirectResponse.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    private static HttpClient CreateNoRedirectClient(AppHostWebApplicationFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    private sealed record CreateProductResponse(Guid Id);
}
