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
            entity.Property(user => user.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(160);
            entity.Property(user => user.Department)
                .HasColumnName("department")
                .HasMaxLength(120);
            entity.Property(user => user.IsStaff)
                .HasColumnName("is_staff")
                .IsRequired();
            entity.Property(user => user.IsActive)
                .HasColumnName("is_active")
                .IsRequired();
            entity.Property(user => user.LastLoginAtUtc)
                .HasColumnName("last_login_at_utc");
            entity.HasIndex(user => new { user.IsStaff, user.IsActive })
                .HasDatabaseName("IX_users_staff_active");
        });

        builder.Entity<IdentityRole<Guid>>(entity => entity.ToTable("roles"));
        builder.Entity<IdentityUserRole<Guid>>(entity => entity.ToTable("user_roles"));
        builder.Entity<IdentityUserClaim<Guid>>(entity => entity.ToTable("user_claims"));
        builder.Entity<IdentityUserLogin<Guid>>(entity => entity.ToTable("user_logins"));
        builder.Entity<IdentityRoleClaim<Guid>>(entity => entity.ToTable("role_claims"));
        builder.Entity<IdentityUserToken<Guid>>(entity => entity.ToTable("user_tokens"));
    }
}
