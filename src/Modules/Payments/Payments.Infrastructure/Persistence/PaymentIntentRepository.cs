using Microsoft.EntityFrameworkCore;
using Payments.Application.Payments;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

internal sealed class PaymentIntentRepository(PaymentsDbContext dbContext) : IPaymentIntentRepository
{
    public Task AddAsync(PaymentIntent paymentIntent, CancellationToken cancellationToken)
    {
        return dbContext.PaymentIntents.AddAsync(paymentIntent, cancellationToken).AsTask();
    }

    public Task<PaymentIntent?> GetByIdAsync(Guid paymentIntentId, CancellationToken cancellationToken)
    {
        return dbContext.PaymentIntents.SingleOrDefaultAsync(
            paymentIntent => paymentIntent.Id == paymentIntentId,
            cancellationToken);
    }

    public Task<PaymentIntent?> GetByProviderIntentIdAsync(
        string provider,
        string providerPaymentIntentId,
        CancellationToken cancellationToken)
    {
        return dbContext.PaymentIntents.SingleOrDefaultAsync(
            paymentIntent => paymentIntent.Provider == provider &&
                             paymentIntent.ProviderPaymentIntentId == providerPaymentIntentId,
            cancellationToken);
    }

    public Task<PaymentIntent?> GetLatestByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return dbContext.PaymentIntents
            .Where(paymentIntent => paymentIntent.OrderId == orderId)
            .OrderByDescending(paymentIntent => paymentIntent.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentIntent>> ListAsync(
        string? provider,
        PaymentIntentStatus? status,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = BuildFilteredQuery(provider, status, createdFromUtc, createdToUtc);

        return await query
            .AsNoTracking()
            .OrderByDescending(paymentIntent => paymentIntent.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        string? provider,
        PaymentIntentStatus? status,
        DateTime? createdFromUtc,
        DateTime? createdToUtc,
        CancellationToken cancellationToken)
    {
        return BuildFilteredQuery(provider, status, createdFromUtc, createdToUtc)
            .CountAsync(cancellationToken);
    }

    private IQueryable<PaymentIntent> BuildFilteredQuery(
        string? provider,
        PaymentIntentStatus? status,
        DateTime? createdFromUtc,
        DateTime? createdToUtc)
    {
        var query = dbContext.PaymentIntents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(provider))
        {
            var normalizedProvider = provider.Trim();
            query = query.Where(paymentIntent => paymentIntent.Provider == normalizedProvider);
        }

        if (status is not null)
        {
            query = query.Where(paymentIntent => paymentIntent.Status == status.Value);
        }

        if (createdFromUtc is not null)
        {
            query = query.Where(paymentIntent => paymentIntent.CreatedAtUtc >= createdFromUtc.Value);
        }

        if (createdToUtc is not null)
        {
            query = query.Where(paymentIntent => paymentIntent.CreatedAtUtc <= createdToUtc.Value);
        }

        return query;
    }
}
