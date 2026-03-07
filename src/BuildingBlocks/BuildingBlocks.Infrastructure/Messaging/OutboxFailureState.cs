using System.Text.Json;

namespace BuildingBlocks.Infrastructure.Messaging;

public sealed record OutboxFailureState(int Attempt, bool DeadLettered, DateTime? NextVisibleAtUtc, string Message)
{
    public static OutboxFailureState Initial { get; } = new(0, false, null, string.Empty);

    public static OutboxFailureState Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Initial;
        }

        try
        {
            return JsonSerializer.Deserialize<OutboxFailureState>(value) ?? Initial;
        }
        catch (JsonException)
        {
            return new OutboxFailureState(1, false, null, value.Trim());
        }
    }

    public string ToStorageString()
    {
        return JsonSerializer.Serialize(this);
    }
}