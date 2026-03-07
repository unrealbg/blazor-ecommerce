using Microsoft.AspNetCore.Identity;

namespace Customers.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? DisplayName { get; set; }

    public string? Department { get; set; }

    public bool IsStaff { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAtUtc { get; set; }
}
