using System.Net.Sockets;
using StackExchange.Redis;

internal static class RedisConnectionHelper
{
    public static ConfigurationOptions BuildOptions(string connectionString)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 1;
        options.ConnectTimeout = 1000;
        options.SyncTimeout = 1000;
        return options;
    }

    public static bool CanConnect(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            using var connection = ConnectionMultiplexer.Connect(BuildOptions(connectionString));
            return connection.IsConnected;
        }
        catch (RedisConnectionException)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
