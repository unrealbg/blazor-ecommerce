using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.Promotions;

namespace Pricing.Infrastructure.Persistence;

internal sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");
        builder.HasKey(promotion => promotion.Id);

        builder.Property(promotion => promotion.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(promotion => promotion.Code)
            .HasMaxLength(64);

        builder.Property(promotion => promotion.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(promotion => promotion.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(promotion => promotion.Description)
            .HasMaxLength(2000);

        builder.Property(promotion => promotion.CreatedAtUtc)
            .IsRequired();

        builder.Property(promotion => promotion.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(promotion => new { promotion.Status, promotion.StartAtUtc, promotion.EndAtUtc });

        builder.OwnsMany(promotion => promotion.Scopes, scopesBuilder =>
        {
            scopesBuilder.ToTable("promotion_scopes");
            scopesBuilder.WithOwner().HasForeignKey(nameof(PromotionScope.PromotionId));
            scopesBuilder.HasKey(scope => scope.Id);
            scopesBuilder.Property(scope => scope.Id).HasColumnName("id");
            scopesBuilder.Property(scope => scope.PromotionId).HasColumnName("promotion_id").IsRequired();
            scopesBuilder.Property(scope => scope.ScopeType)
                .HasColumnName("scope_type")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            scopesBuilder.Property(scope => scope.TargetId).HasColumnName("target_id");
            scopesBuilder.Property(scope => scope.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            scopesBuilder.HasIndex(scope => new { scope.PromotionId, scope.TargetId });
        });

        builder.OwnsMany(promotion => promotion.Conditions, conditionsBuilder =>
        {
            conditionsBuilder.ToTable("promotion_conditions");
            conditionsBuilder.WithOwner().HasForeignKey(nameof(PromotionCondition.PromotionId));
            conditionsBuilder.HasKey(condition => condition.Id);
            conditionsBuilder.Property(condition => condition.Id).HasColumnName("id");
            conditionsBuilder.Property(condition => condition.PromotionId).HasColumnName("promotion_id").IsRequired();
            conditionsBuilder.Property(condition => condition.ConditionType)
                .HasColumnName("condition_type")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            conditionsBuilder.Property(condition => condition.Operator)
                .HasColumnName("operator")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            conditionsBuilder.Property(condition => condition.Value)
                .HasColumnName("value")
                .HasMaxLength(512)
                .IsRequired();
            conditionsBuilder.Property(condition => condition.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();
        });

        builder.OwnsMany(promotion => promotion.Benefits, benefitsBuilder =>
        {
            benefitsBuilder.ToTable("promotion_benefits");
            benefitsBuilder.WithOwner().HasForeignKey(nameof(PromotionBenefit.PromotionId));
            benefitsBuilder.HasKey(benefit => benefit.Id);
            benefitsBuilder.Property(benefit => benefit.Id).HasColumnName("id");
            benefitsBuilder.Property(benefit => benefit.PromotionId).HasColumnName("promotion_id").IsRequired();
            benefitsBuilder.Property(benefit => benefit.BenefitType)
                .HasColumnName("benefit_type")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            benefitsBuilder.Property(benefit => benefit.ValueAmount)
                .HasColumnName("value_amount")
                .HasPrecision(18, 2);
            benefitsBuilder.Property(benefit => benefit.ValuePercent)
                .HasColumnName("value_percent")
                .HasPrecision(18, 2);
            benefitsBuilder.Property(benefit => benefit.MaxDiscountAmount)
                .HasColumnName("max_discount_amount")
                .HasPrecision(18, 2);
            benefitsBuilder.Property(benefit => benefit.ApplyPerUnit)
                .HasColumnName("apply_per_unit")
                .IsRequired();
            benefitsBuilder.Property(benefit => benefit.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();
        });

        builder.Navigation(promotion => promotion.Scopes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(promotion => promotion.Conditions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(promotion => promotion.Benefits).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
