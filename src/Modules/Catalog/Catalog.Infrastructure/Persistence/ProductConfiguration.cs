using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(product => product.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(product => product.ShortDescription)
            .HasColumnName("short_description")
            .HasMaxLength(800);

        builder.Property(product => product.Description)
            .HasMaxLength(8000);

        builder.Property(product => product.BrandId)
            .HasColumnName("brand_id");

        builder.Property(product => product.DefaultCategoryId)
            .HasColumnName("default_category_id");

        builder.Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(product => product.ProductType)
            .HasConversion<string>()
            .HasColumnName("product_type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(product => product.SeoTitle)
            .HasColumnName("seo_title")
            .HasMaxLength(200);

        builder.Property(product => product.SeoDescription)
            .HasColumnName("seo_description")
            .HasMaxLength(320);

        builder.Property(product => product.CanonicalUrl)
            .HasColumnName("canonical_url")
            .HasMaxLength(2000);

        builder.Property(product => product.IsFeatured)
            .HasColumnName("is_featured")
            .IsRequired();

        builder.Property(product => product.PublishedAtUtc)
            .HasColumnName("published_at_utc");

        builder.Property(product => product.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(product => product.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(product => product.DefaultVariantId)
            .HasColumnName("default_variant_id")
            .IsRequired();

        builder.HasIndex(product => product.Slug)
            .IsUnique();

        builder.HasIndex(product => product.BrandId);

        builder.HasIndex(product => product.Status);

        builder.Navigation(product => product.Categories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(product => product.Variants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(product => product.Options)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(product => product.Attributes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(product => product.Images)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(product => product.Relations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(product => product.Categories)
            .WithOne()
            .HasForeignKey(category => category.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Variants)
            .WithOne()
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Options)
            .WithOne()
            .HasForeignKey(option => option.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Attributes)
            .WithOne()
            .HasForeignKey(attribute => attribute.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Images)
            .WithOne()
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(product => product.Relations)
            .WithOne()
            .HasForeignKey(relation => relation.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
