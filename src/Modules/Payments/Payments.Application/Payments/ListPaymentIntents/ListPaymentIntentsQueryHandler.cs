using BuildingBlocks.Application.Abstractions;

namespace Payments.Application.Payments.ListPaymentIntents;

public sealed class ListPaymentIntentsQueryHandler(IPaymentIntentRepository paymentIntentRepository)
    : IQueryHandler<ListPaymentIntentsQuery, PaymentIntentPage>
{
    public async Task<PaymentIntentPage> Handle(
        ListPaymentIntentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(100, request.PageSize);

        var items = await paymentIntentRepository.ListAsync(
            request.Provider,
            request.Status,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            page,
            pageSize,
            cancellationToken);

        var totalCount = await paymentIntentRepository.CountAsync(
            request.Provider,
            request.Status,
            request.CreatedFromUtc,
            request.CreatedToUtc,
            cancellationToken);

        return new PaymentIntentPage(
            page,
            pageSize,
            totalCount,
            items.Select(PaymentIntentMappings.ToSummaryDto).ToList());
    }
}
