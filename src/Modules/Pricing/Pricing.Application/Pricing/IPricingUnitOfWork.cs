namespace Pricing.Application.Pricing;

public interface IPricingUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
