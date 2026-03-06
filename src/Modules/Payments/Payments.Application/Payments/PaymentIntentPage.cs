namespace Payments.Application.Payments;

public sealed record PaymentIntentPage(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<PaymentIntentSummaryDto> Items);
