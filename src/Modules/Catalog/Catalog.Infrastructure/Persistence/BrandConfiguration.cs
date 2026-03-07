using Catalog.Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(brand => brand.Id);

        builder.Property(brand => brand.Name)
            .IsRequired()
            .HasMaxLength(160);

        builder.Property(brand => brand.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(brand => brand.Description)
            .HasMaxLength(2000);

        builder.Property(brand => brand.WebsiteUrl)
            .HasColumnName("website_url")
            .HasMaxLength(2000);

        builder.Property(brand => brand.LogoImageUrl)
            .HasColumnName("logo_image_url")
            .HasMaxLength(2000);

        builder.Property(brand => brand.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(brand => brand.SeoTitle)
            .HasColumnName("seo_title")
            .HasMaxLength(200);

        builder.Property(brand => brand.SeoDescription)
            .HasColumnName("seo_description")
            .HasMaxLength(320);

        builder.Property(brand => brand.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(brand => brand.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(brand => brand.Slug)
            .IsUnique();
    }
}
