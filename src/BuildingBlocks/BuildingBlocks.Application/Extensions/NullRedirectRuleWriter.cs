using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullRedirectRuleWriter : IRedirectRuleWriter
{
    public Task UpsertAsync(string fromPath, string toPath, int statusCode, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
