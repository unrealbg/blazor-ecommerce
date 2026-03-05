using System.Net;

namespace Storefront.Tests;

public sealed class StorefrontSeoTests(StorefrontWebApplicationFactory factory) : IClassFixture<StorefrontWebApplicationFactory>
{
    [Fact]
    public async Task ProductPage_Should_Return200_AndContainProductName()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/product/mechanical-keyboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mechanical Keyboard", html);
    }

    [Fact]
    public async Task Sitemap_Should_Return200_AndContainProductUrl()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("https://shop.example.com/product/mechanical-keyboard", xml);
    }
}
