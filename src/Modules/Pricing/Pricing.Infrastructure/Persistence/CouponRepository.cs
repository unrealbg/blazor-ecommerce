using Microsoft.EntityFrameworkCore;
using Pricing.Application.Pricing;
using Pricing.Domain.Coupons;

namespace Pricing.Infrastructure.Persistence;

internal sealed class CouponRepository(PricingDbContext dbContext) : ICouponRepository
{
    public Task AddAsync(Coupon coupon, CancellationToken cancellationToken)
    {
        return dbContext.Coupons.AddAsync(coupon, cancellationToken).AsTask();
    }

    public Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Coupons.SingleOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);
    }

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return dbContext.Coupons.SingleOrDefaultAsync(coupon => coupon.Code == normalizedCode, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Coupon>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Coupons
            .OrderBy(coupon => coupon.Code)
            .ToListAsync(cancellationToken);
    }
}
