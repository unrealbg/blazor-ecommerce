namespace BuildingBlocks.Application.Contracts;

public interface IRedirectRuleWriter
{
    Task UpsertAsync(string fromPath, string toPath, int statusCode, CancellationToken cancellationToken);
}
