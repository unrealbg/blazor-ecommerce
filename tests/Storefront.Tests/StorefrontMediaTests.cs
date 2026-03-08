using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Storefront.Web.Services.Media;

namespace Storefront.Tests;

public sealed class StorefrontMediaTests(StorefrontWebApplicationFactory factory) : IClassFixture<StorefrontWebApplicationFactory>
{
    [Fact]
    public async Task MediaEndpoint_Should_ReturnImage_ForAllowedHost()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/media/image?src=http://localhost:8055/assets/test.png&w=640&format=jpeg");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.StartsWith("image/", response.Content.Headers.ContentType!.MediaType, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MediaEndpoint_Should_Return403_ForDisallowedHost()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/media/image?src=https://evil.example.com/image.png&w=640");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MediaEndpoint_Should_UseDiskCache_OnSecondRequest()
    {
        using var client = factory.CreateClient();

        var path = "/media/image?src=http://localhost:8055/assets/test.png&w=641&format=webp";

        var firstResponse = await client.GetAsync(path);
        var secondResponse = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        Assert.True(firstResponse.Headers.TryGetValues("X-Media-Cache", out var firstCacheValues));
        Assert.True(secondResponse.Headers.TryGetValues("X-Media-Cache", out var secondCacheValues));
        Assert.Contains("MISS", firstCacheValues, StringComparer.Ordinal);
        Assert.Contains("HIT", secondCacheValues, StringComparer.Ordinal);

        Assert.True(Directory.Exists(factory.MediaCachePath));
        Assert.NotEmpty(Directory.GetFiles(factory.MediaCachePath, "*.*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ProductPage_Should_RenderAbsoluteProxyOgImage()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/product/mechanical-keyboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("property=\"og:image\"", html, StringComparison.Ordinal);
        Assert.Contains("https://shop.example.com/media/image?src=", html, StringComparison.Ordinal);
    }

    [Fact]
    public void MediaResolver_Should_MapSiteRelativeImages_ToAbsoluteHttpUrls()
    {
        using var scope = factory.Services.CreateScope();
        var mediaSourceResolver = scope.ServiceProvider.GetRequiredService<IMediaSourceResolver>();

        var resolution = mediaSourceResolver.Resolve("/images/mechanical-keyboard.png", MediaSourceOrigin.Site);

        Assert.True(resolution.IsSuccess);
        Assert.NotNull(resolution.SourceUri);
        Assert.Equal("https", resolution.SourceUri!.Scheme);
        Assert.Equal("shop.example.com", resolution.SourceUri.Host);
        Assert.Equal("/images/mechanical-keyboard.png", resolution.SourceUri.AbsolutePath);
    }

    [Fact]
    public async Task BlogPost_Should_RewriteInlineImages_ToMediaProxy()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/blog/shipping-checklist-2026");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/media/image?src=http%3A%2F%2Flocalhost%3A8055%2Fassets%2Fblog-inline.png", html, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"http://localhost:8055/assets/blog-inline.png\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MediaEndpoint_Should_ReturnWebp_WhenRequested()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/media/image?src=http://localhost:8055/assets/test.png&w=640&format=webp");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/webp", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task MediaEndpoint_Should_Return404_WhenSourceMissing()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/media/image?src=http://localhost:8055/assets/missing.png&w=640");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
