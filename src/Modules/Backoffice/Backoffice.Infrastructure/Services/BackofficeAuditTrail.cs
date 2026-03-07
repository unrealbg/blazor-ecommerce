using System.Text.Json;
using System.Text.Json.Nodes;
using Backoffice.Domain.Audit;
using Backoffice.Infrastructure.Persistence;
using BuildingBlocks.Application.Auditing;

namespace Backoffice.Infrastructure.Services;

internal sealed class BackofficeAuditTrail(BackofficeDbContext dbContext) : IAuditTrail
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "password",
        "secret",
        "token",
        "clientsecret",
        "apikey",
        "authorization",
    ];

    public async Task WriteAsync(AuditEntryInput input, CancellationToken cancellationToken)
    {
        var entity = AuditEntry.Create(
            input.OccurredAtUtc ?? DateTime.UtcNow,
            input.ActionType,
            input.TargetType,
            input.TargetId,
            input.Summary,
            SanitizeMetadata(input.MetadataJson),
            input.ActorUserId,
            input.ActorEmail,
            input.ActorDisplayName,
            input.IpAddress,
            input.CorrelationId);

        await dbContext.AuditEntries.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? SanitizeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(metadataJson);
            if (node is null)
            {
                return null;
            }

            SanitizeNode(node);
            return node.ToJsonString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void SanitizeNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    if (property.Value is null)
                    {
                        continue;
                    }

                    if (IsSensitive(property.Key))
                    {
                        jsonObject[property.Key] = "[redacted]";
                        continue;
                    }

                    SanitizeNode(property.Value);
                }

                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    if (item is not null)
                    {
                        SanitizeNode(item);
                    }
                }

                break;
        }
    }

    private static bool IsSensitive(string key)
    {
        var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToLowerInvariant();

        return SensitiveKeyFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }
}
