using Pricing.Domain.Coupons;

namespace Pricing.Application.Pricing;

public interface ICouponRepository
{
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken);

    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Coupon>> ListAsync(CancellationToken cancellationToken);
}
