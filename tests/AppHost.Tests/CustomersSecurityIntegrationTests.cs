using System.Net;

namespace AppHost.Tests;

public sealed class CustomersSecurityIntegrationTests
{
    [Fact]
    public async Task CustomersEndpoints_Should_RequireAuthentication()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/v1/customers/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OrdersMyEndpoint_Should_RequireAuthentication()
    {
        await using var testFactory = new AppHostWebApplicationFactory();
        using var client = testFactory.CreateClient();

        var response = await client.GetAsync("/api/v1/orders/my");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
