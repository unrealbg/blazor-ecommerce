using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Storefront.Tests;

public sealed class StorefrontRuntimeHardeningTests(StorefrontWebApplicationFactory factory) : IClassFixture<StorefrontWebApplicationFactory>
{
    [Fact]
    public async Task HomePage_Should_ReturnPublicCacheHeaders_ForAnonymousRequests()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.Public);
        Assert.Equal(TimeSpan.FromSeconds(60), response.Headers.CacheControl.MaxAge);
    }

    [Fact]
    public async Task ProductPage_Should_ReturnNoStore_WhenAuthCookieIsPresent()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/product/mechanical-keyboard");
        request.Headers.Add("Cookie", "blazor-ecommerce-auth=test-cookie");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task CategoryPage_Should_VaryByRelevantQueryKeys()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/category/keyboards?brand=Contoso&sort=price_desc&page=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Accept-Encoding", response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchPage_Should_ReturnShortPublicCacheHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/search?q=keyboard&sort=popular&page=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.Public);
        Assert.Equal(TimeSpan.FromSeconds(15), response.Headers.CacheControl.MaxAge);
    }

    [Fact]
    public async Task BlogIndex_Should_ReturnContentCacheHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/blog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(300), response.Headers.CacheControl?.MaxAge);
    }

    [Fact]
    public async Task BlogPost_Should_ReturnContentCacheHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/blog/shipping-checklist-2026");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(300), response.Headers.CacheControl?.MaxAge);
    }

    [Fact]
    public async Task LandingPage_Should_ReturnContentCacheHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/p/wholesale-program");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(300), response.Headers.CacheControl?.MaxAge);
    }

    [Fact]
    public async Task Sitemap_Should_ReturnLongerCacheHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(600), response.Headers.CacheControl?.MaxAge);
    }

    [Fact]
    public async Task Robots_Should_ReturnStaticCacheHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/robots.txt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(3600), response.Headers.CacheControl?.MaxAge);
    }

    [Fact]
    public async Task Rss_Should_ReturnFeedCacheHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/rss.xml");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(300), response.Headers.CacheControl?.MaxAge);
    }

    [Fact]
    public async Task AdminRoutes_Should_ReturnNoStoreHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/admin/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task CartRoute_Should_ReturnNoStoreHeaders()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/cart");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task VersionEndpoint_Should_ExposeBuildMetadata_AndFlags()
    {
        await using var localFactory = CreateFactory(
            ("Build:Version", "2026.03.07"),
            ("Build:SourceRevisionId", "abcdef1234567890"),
            ("Build:BuildTimestampUtc", "2026-03-07T12:34:56Z"));
        using var client = localFactory.CreateClient();

        var response = await client.GetAsync("/version");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2026.03.07", payload.RootElement.GetProperty("version").GetString());
        Assert.Equal("abcdef1234567890", payload.RootElement.GetProperty("revision").GetString());
        Assert.Equal("Testing", payload.RootElement.GetProperty("environment").GetString());
        Assert.Contains(
            payload.RootElement.GetProperty("activeFeatureFlags").EnumerateArray().Select(item => item.GetString()),
            value => string.Equals(value, "EnableReviews", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VersionEndpoint_Should_IncludeWarmupSnapshot()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/version");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(payload.RootElement.TryGetProperty("warmup", out var warmup));
        Assert.True(warmup.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task Blog_Should_DegradeGracefully_WhenCmsContentFlagIsDisabled()
    {
        await using var localFactory = CreateFactory(("FeatureFlags:EnableCmsContent", "false"));
        using var client = localFactory.CreateClient();

        var response = await client.GetAsync("/blog");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("temporarily unavailable", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductPage_Should_HideReviewSections_WhenReviewsAreDisabled()
    {
        await using var localFactory = CreateFactory(("FeatureFlags:EnableReviews", "false"));
        using var client = localFactory.CreateClient();

        var response = await client.GetAsync("/product/mechanical-keyboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Ratings and reviews", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Product Q&amp;A", html, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory(params (string Key, string Value)[] overrides)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(overrides.Select(item => new KeyValuePair<string, string?>(item.Key, item.Value)));
            });
        });
    }
}