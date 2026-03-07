namespace Customers.Infrastructure.Identity;

internal static class ApplicationUserExtensions
{
    public static bool IsActive(this ApplicationUser user)
    {
        return user.IsActive &&
               (user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow);
    }

    public static bool HasStaffAccess(this ApplicationUser user)
    {
        return user.IsStaff && user.IsActive();
    }
}
