using BuildingBlocks.Application.Auditing;
using Customers.Application.Compliance;
using Customers.Infrastructure.Identity;
using Customers.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Compliance;

internal sealed class CustomerDataErasureService(
    CustomersDbContext customersDbContext,
    UserManager<ApplicationUser> userManager,
    IAuditTrail auditTrail)
    : ICustomerDataErasureService
{
    public async Task<CustomerDataErasureResult?> EraseByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var customer = await customersDbContext.Customers
            .Include(entity => entity.Addresses)
            .SingleOrDefaultAsync(entity => entity.UserId == userId, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var replacementEmail = $"deleted+{customer.Id:D}@redacted.local";
        customer.Anonymize(replacementEmail, now);

        var identityUser = await userManager.FindByIdAsync(userId.ToString());
        var identityUserUpdated = false;
        if (identityUser is not null)
        {
            identityUser.Email = replacementEmail;
            identityUser.NormalizedEmail = replacementEmail.ToUpperInvariant();
            identityUser.UserName = replacementEmail;
            identityUser.NormalizedUserName = replacementEmail.ToUpperInvariant();
            identityUser.PhoneNumber = null;
            identityUser.DisplayName = null;
            identityUser.Department = null;
            identityUser.IsActive = false;
            identityUser.LockoutEnabled = true;
            identityUser.LockoutEnd = DateTimeOffset.MaxValue;
            var updateResult = await userManager.UpdateAsync(identityUser);
            identityUserUpdated = updateResult.Succeeded;
        }

        await customersDbContext.SaveChangesAsync(cancellationToken);

        await auditTrail.WriteAsync(
            new AuditEntryInput(
                "CustomerDataErased",
                "Customer",
                customer.Id.ToString("D"),
                "Customer personal data was anonymized.",
                MetadataJson: null,
                ActorUserId: userId.ToString("D"),
                ActorEmail: replacementEmail,
                ActorDisplayName: null,
                IpAddress: null,
                CorrelationId: null,
                OccurredAtUtc: now),
            cancellationToken);

        return new CustomerDataErasureResult(customer.Id, userId, identityUserUpdated, now, replacementEmail);
    }
}