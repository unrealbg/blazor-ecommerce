using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Customers.Api;

namespace AppHost.Tests;

public sealed class HardeningIntegrationTests
{
    [Fact]
    public async Task HealthEndpoints_ShouldReturnStructuredPayload()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var liveResponse = await client.GetAsync("/health/live");
        var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

        var liveDocument = JsonDocument.Parse(await liveResponse.Content.ReadAsStringAsync());
        var readyDocument = JsonDocument.Parse(await readyResponse.Content.ReadAsStringAsync());

        Assert.True(liveDocument.RootElement.TryGetProperty("correlationId", out var liveCorrelationId));
        Assert.False(string.IsNullOrWhiteSpace(liveCorrelationId.GetString()));
        Assert.True(liveDocument.RootElement.TryGetProperty("entries", out _));

        Assert.True(readyDocument.RootElement.TryGetProperty("status", out var readyStatus));
        Assert.True(readyStatus.GetString() is "Healthy" or "Degraded");
        Assert.True(readyDocument.RootElement.TryGetProperty("entries", out var readyEntries));
        Assert.True(readyEntries.TryGetProperty("outbox", out _));
    }

    [Fact]
    public async Task CustomerExport_ShouldReturnCurrentCustomerData_WhenAuthenticated()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"export-{Guid.NewGuid():N}@example.com";
        await AuthenticateAsync(client, email, "Export!Pass123");

        var response = await client.GetAsync("/api/v1/customers/me/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(email, payload.RootElement.GetProperty("email").GetString());
        Assert.True(payload.RootElement.TryGetProperty("orders", out _));
        Assert.True(payload.RootElement.TryGetProperty("reviews", out _));
        Assert.True(payload.RootElement.TryGetProperty("questions", out _));
    }

    [Fact]
    public async Task LoginEndpoint_ShouldReturnTooManyRequests_WhenAuthRateLimitIsExceeded()
    {
        await using var factory = new AppHostWebApplicationFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:AuthPermits"] = "1",
            ["RateLimiting:AuthWindowSeconds"] = "60",
        });
        using var client = factory.CreateClient();

        var firstResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new CustomersModuleExtensions.LoginRequest("missing@example.com", "Wrong!Pass123", RememberMe: false));
        var secondResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new CustomersModuleExtensions.LoginRequest("missing@example.com", "Wrong!Pass123", RememberMe: false));

        Assert.Equal(HttpStatusCode.BadRequest, firstResponse.StatusCode);
        Assert.Equal((HttpStatusCode)429, secondResponse.StatusCode);

        var payload = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
        Assert.Equal("Too many requests", payload.RootElement.GetProperty("title").GetString());
        Assert.True(payload.RootElement.TryGetProperty("correlationId", out var correlationId));
        Assert.False(string.IsNullOrWhiteSpace(correlationId.GetString()));
    }

    private static async Task AuthenticateAsync(HttpClient client, string email, string password)
    {
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CustomersModuleExtensions.RegisterRequest(
                Email: email,
                Password: password,
                FirstName: "Export",
                LastName: "User",
                PhoneNumber: "+359888123456"));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new CustomersModuleExtensions.LoginRequest(email, password, RememberMe: false));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authCookie = loginResponse.Headers.GetValues("Set-Cookie")
            .First(value => value.Contains("blazor-ecommerce-auth", StringComparison.Ordinal))
            .Split(';', 2)[0];

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", authCookie);
    }
}