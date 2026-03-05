using System.Net;

namespace Storefront.Tests;

public sealed class StorefrontSeoTests(StorefrontWebApplicationFactory factory) : IClassFixture<StorefrontWebApplicationFactory>
{
    [Fact]
    public async Task ProductPage_Should_Return200_AndContainStructuredDataAndCanonical()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/product/mechanical-keyboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mechanical Keyboard", html);
        Assert.Contains("application/ld&#x2B;json", html, StringComparison.Ordinal);
        Assert.Contains("\"@type\":\"Product\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://shop.example.com/product/mechanical-keyboard\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sitemap_Should_Return200_AndContainProductUrl()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("https://shop.example.com/blog", xml);
        Assert.Contains("https://shop.example.com/blog/shipping-checklist-2026", xml);
        Assert.Contains("https://shop.example.com/product/mechanical-keyboard", xml);
    }

    [Fact]
    public async Task CategoryPage_Page1_Should_HaveCleanCanonicalWithoutPageParam()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/category/keyboards?page=1&pageSize=48&sort=price-desc&utm_source=newsletter");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("href=\"https://shop.example.com/category/keyboards\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("rel=\"prev\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://shop.example.com/category/keyboards?page=2\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CategoryPage_Page2_Should_HaveCanonicalWithPageParam_AndPrevNextLinks()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/category/keyboards?page=2&pageSize=12&sort=price-desc&gclid=abc123");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("href=\"https://shop.example.com/category/keyboards?page=2\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://shop.example.com/category/keyboards\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://shop.example.com/category/keyboards?page=3\"", html, StringComparison.Ordinal);
        Assert.Contains("rel=\"prev\"", html, StringComparison.Ordinal);
        Assert.Contains("rel=\"next\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlogIndex_Should_Return200()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/blog");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Blog</h1>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlogPost_Should_Return200_AndContainJsonLd()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/blog/shipping-checklist-2026");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Shipping Checklist for 2026", html, StringComparison.Ordinal);
        Assert.Contains("application/ld&#x2B;json", html, StringComparison.Ordinal);
        Assert.Contains("\"@type\":\"BlogPosting\"", html, StringComparison.Ordinal);
        Assert.Contains("\"@type\":\"BreadcrumbList\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://shop.example.com/blog/shipping-checklist-2026\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rss_Should_Return200()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/rss.xml");
        var xml = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<rss version=\"2.0\">", xml, StringComparison.Ordinal);
        Assert.Contains("shipping-checklist-2026", xml, StringComparison.Ordinal);
    }
}
