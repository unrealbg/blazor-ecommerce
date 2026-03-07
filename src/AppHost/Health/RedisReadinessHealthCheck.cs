using StackExchange.Redis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppHost.Health;

public sealed class RedisReadinessHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Degraded("Redis connection string is not configured.");
        }

        try
        {
            await using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var database = connection.GetDatabase();
            await database.PingAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return HealthCheckResult.Healthy("Redis is reachable.");
        }
        catch (Exception exception) when (exception is RedisConnectionException or RedisTimeoutException)
        {
            return HealthCheckResult.Degraded("Redis is unavailable.", exception);
        }
    }
}