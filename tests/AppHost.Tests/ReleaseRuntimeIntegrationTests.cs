using System.Net;
using System.Text.Json;

namespace AppHost.Tests;

public sealed class ReleaseRuntimeIntegrationTests
{
    [Fact]
    public async Task VersionEndpoint_Should_ReturnBuildMetadata_AndReleaseProfile()
    {
        await using var factory = new AppHostWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Build:Version"] = "2026.03.07",
            ["Build:SourceRevisionId"] = "abcdef1234567890",
            ["Build:BuildTimestampUtc"] = "2026-03-07T11:22:33Z",
            ["Release:SeedMode"] = "demo",
            ["Release:MigrationMode"] = "manual",
            ["FeatureFlags:EnableOperationalRecoveryActions"] = "true",
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/version");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2026.03.07", payload.RootElement.GetProperty("version").GetString());
        Assert.Equal("abcdef1234567890", payload.RootElement.GetProperty("revision").GetString());
        Assert.Equal("demo", payload.RootElement.GetProperty("release").GetProperty("seedMode").GetString());
        Assert.Contains(
            payload.RootElement.GetProperty("activeFeatureFlags").EnumerateArray().Select(item => item.GetString()),
            value => string.Equals(value, "EnableOperationalRecoveryActions", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VersionEndpoint_Should_OmitDisabledFlags_FromActiveFlagList()
    {
        await using var factory = new AppHostWebApplicationFactory(new Dictionary<string, string?>
        {
            ["FeatureFlags:EnableOperationalRecoveryActions"] = "false",
            ["FeatureFlags:EnableDemoProviders"] = "false",
            ["FeatureFlags:EnableReviewModeration"] = "true",
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/version");
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var flags = payload.RootElement.GetProperty("activeFeatureFlags").EnumerateArray().Select(item => item.GetString()).ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("EnableOperationalRecoveryActions", flags, StringComparer.Ordinal);
        Assert.DoesNotContain("EnableDemoProviders", flags, StringComparer.Ordinal);
        Assert.Contains("EnableReviewModeration", flags, StringComparer.Ordinal);
    }
}