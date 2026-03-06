using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Identity;

public sealed class IdentityAppDbContext(DbContextOptions<IdentityAppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(user => user.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();
        });

        builder.Entity<IdentityRole<Guid>>(entity => entity.ToTable("roles"));
        builder.Entity<IdentityUserRole<Guid>>(entity => entity.ToTable("user_roles"));
        builder.Entity<IdentityUserClaim<Guid>>(entity => entity.ToTable("user_claims"));
        builder.Entity<IdentityUserLogin<Guid>>(entity => entity.ToTable("user_logins"));
        builder.Entity<IdentityRoleClaim<Guid>>(entity => entity.ToTable("role_claims"));
        builder.Entity<IdentityUserToken<Guid>>(entity => entity.ToTable("user_tokens"));
    }
}
