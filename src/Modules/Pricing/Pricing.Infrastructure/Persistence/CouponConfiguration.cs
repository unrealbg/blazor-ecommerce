using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricing.Domain.Coupons;

namespace Pricing.Infrastructure.Persistence;

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");
        builder.HasKey(coupon => coupon.Id);

        builder.Property(coupon => coupon.Code)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(coupon => coupon.Description)
            .HasMaxLength(512);

        builder.Property(coupon => coupon.PromotionId)
            .HasColumnName("promotion_id")
            .IsRequired();

        builder.Property(coupon => coupon.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(coupon => coupon.CreatedAtUtc)
            .IsRequired();

        builder.Property(coupon => coupon.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(coupon => coupon.Code)
            .IsUnique();
    }
}
