using System.Net;

namespace Storefront.Tests;

public sealed class StorefrontReviewsTests(StorefrontWebApplicationFactory factory) : IClassFixture<StorefrontWebApplicationFactory>
{
    [Fact]
    public async Task ProductPage_Should_RenderAggregateRatingJsonLd_WhenApprovedReviewsExist()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/product/mechanical-keyboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"aggregateRating\"", html, StringComparison.Ordinal);
        Assert.Contains("\"reviewCount\":2", html, StringComparison.Ordinal);
        Assert.Contains("\"ratingValue\":\"4.50\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProductPage_Should_RenderApprovedReviews_AndOfficialAnswer()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/product/mechanical-keyboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ratings and reviews", html, StringComparison.Ordinal);
        Assert.Contains("Verified purchase", html, StringComparison.Ordinal);
        Assert.Contains("Excellent keyboard", html, StringComparison.Ordinal);
        Assert.Contains("Product Q&amp;A", html, StringComparison.Ordinal);
        Assert.Contains("Official answer", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdminReviewPages_Should_RenderModerationAndReports()
    {
        using var client = factory.CreateClient();

        var moderationResponse = await client.GetAsync("/admin/reviews");
        var moderationHtml = await moderationResponse.Content.ReadAsStringAsync();
        var reportsResponse = await client.GetAsync("/admin/reviews/reports");
        var reportsHtml = await reportsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, moderationResponse.StatusCode);
        Assert.Contains("Review Moderation", moderationHtml, StringComparison.Ordinal);
        Assert.Contains("Mechanical Keyboard", moderationHtml, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, reportsResponse.StatusCode);
        Assert.Contains("Review Reports", reportsHtml, StringComparison.Ordinal);
    }
}
