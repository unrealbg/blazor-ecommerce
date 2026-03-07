using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.Redemptions;

namespace Pricing.Infrastructure.Persistence;

internal sealed class PromotionRedemptionConfiguration : IEntityTypeConfiguration<PromotionRedemption>
{
    public void Configure(EntityTypeBuilder<PromotionRedemption> builder)
    {
        builder.ToTable("promotion_redemptions");
        builder.HasKey(redemption => redemption.Id);

        builder.Property(redemption => redemption.PromotionId)
            .HasColumnName("promotion_id")
            .IsRequired();

        builder.Property(redemption => redemption.CouponId)
            .HasColumnName("coupon_id");

        builder.Property(redemption => redemption.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(redemption => redemption.CustomerId)
            .HasColumnName("customer_id")
            .HasMaxLength(128);

        builder.Property(redemption => redemption.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(redemption => redemption.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(redemption => new { redemption.PromotionId, redemption.OrderId, redemption.CouponId })
            .IsUnique();

        builder.HasIndex(redemption => new { redemption.PromotionId, redemption.CustomerId });
        builder.HasIndex(redemption => new { redemption.CouponId, redemption.CustomerId });
    }
}
