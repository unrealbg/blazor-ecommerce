using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Redirects.Domain.RedirectRules;

namespace Redirects.Infrastructure.Persistence;

internal sealed class RedirectRuleConfiguration : IEntityTypeConfiguration<RedirectRule>
{
    public void Configure(EntityTypeBuilder<RedirectRule> builder)
    {
        builder.ToTable("redirect_rules");

        builder.HasKey(redirectRule => redirectRule.Id);

        builder.Property(redirectRule => redirectRule.FromPath)
            .HasColumnName("from_path")
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(redirectRule => redirectRule.ToPath)
            .HasColumnName("to_path")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(redirectRule => redirectRule.StatusCode)
            .HasColumnName("status_code")
            .IsRequired();

        builder.Property(redirectRule => redirectRule.CreatedAtUtc)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(redirectRule => redirectRule.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(redirectRule => redirectRule.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(redirectRule => redirectRule.HitCount)
            .HasColumnName("hit_count")
            .IsRequired();

        builder.Property(redirectRule => redirectRule.LastHitAtUtc)
            .HasColumnName("last_hit_at");

        builder.HasIndex(redirectRule => redirectRule.FromPath)
            .HasDatabaseName("ux_redirect_rules_from_path_active")
            .IsUnique()
            .HasFilter("is_active = TRUE");

        builder.HasIndex(redirectRule => redirectRule.ToPath)
            .HasDatabaseName("ix_redirect_rules_to_path");
    }
}
