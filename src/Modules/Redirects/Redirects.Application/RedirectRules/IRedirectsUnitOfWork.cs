namespace Redirects.Application.RedirectRules;

public interface IRedirectsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
