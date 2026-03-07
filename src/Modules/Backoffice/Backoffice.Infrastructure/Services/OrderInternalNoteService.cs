using Backoffice.Application.Backoffice;
using Backoffice.Domain.Notes;
using Backoffice.Infrastructure.Persistence;
using BuildingBlocks.Application.Auditing;
using BuildingBlocks.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Orders.Infrastructure.Persistence;

namespace Backoffice.Infrastructure.Services;

internal sealed class OrderInternalNoteService(
    BackofficeDbContext backofficeDbContext,
    OrdersDbContext ordersDbContext,
    IAuditTrail auditTrail)
    : IOrderInternalNoteService
{
    public async Task<Result<Guid>> AddOrderNoteAsync(
        Guid orderId,
        string note,
        string? authorUserId,
        string? authorEmail,
        string? authorDisplayName,
        CancellationToken cancellationToken)
    {
        if (orderId == Guid.Empty)
        {
            return Result<Guid>.Failure(new Error(
                "backoffice.order.not_found",
                "Order was not found."));
        }

        if (string.IsNullOrWhiteSpace(note))
        {
            return Result<Guid>.Failure(new Error(
                "backoffice.order_note.required",
                "Internal note text is required."));
        }

        var orderExists = await ordersDbContext.Orders
            .AsNoTracking()
            .AnyAsync(order => order.Id == orderId, cancellationToken);
        if (!orderExists)
        {
            return Result<Guid>.Failure(new Error(
                "backoffice.order.not_found",
                "Order was not found."));
        }

        var entity = OrderInternalNote.Create(
            orderId,
            note,
            DateTime.UtcNow,
            authorUserId,
            authorEmail,
            authorDisplayName);

        await backofficeDbContext.OrderInternalNotes.AddAsync(entity, cancellationToken);
        await backofficeDbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.WriteAsync(
            new AuditEntryInput(
                "OrderInternalNoteAdded",
                "Order",
                orderId.ToString("D"),
                "Added an internal order note.",
                $"{{\"orderId\":\"{orderId:D}\",\"noteId\":\"{entity.Id:D}\"}}",
                authorUserId,
                authorEmail,
                authorDisplayName,
                ipAddress: null,
                correlationId: null),
            cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
