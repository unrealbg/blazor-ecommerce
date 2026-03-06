namespace Customers.Infrastructure.Identity;

internal static class ApplicationUserExtensions
{
    public static bool IsActive(this ApplicationUser user)
    {
        return user.LockoutEnd is null || user.LockoutEnd <= DateTimeOffset.UtcNow;
    }
}
