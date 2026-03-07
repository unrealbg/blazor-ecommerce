using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");
        builder.HasKey(category => new { category.ProductId, category.CategoryId });

        builder.Property(category => category.ProductId)
            .HasColumnName("product_id");

        builder.Property(category => category.CategoryId)
            .HasColumnName("category_id");

        builder.Property(category => category.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();

        builder.Property(category => category.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.HasIndex(category => category.CategoryId);
    }
}

internal sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(variant => variant.Id);

        builder.Property(variant => variant.ProductId)
            .HasColumnName("product_id");

        builder.Property(variant => variant.Sku)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(variant => variant.Name)
            .HasMaxLength(200);

        builder.Property(variant => variant.Slug)
            .HasMaxLength(220);

        builder.Property(variant => variant.Barcode)
            .HasMaxLength(128);

        builder.Property(variant => variant.PriceAmount)
            .HasColumnName("price_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(variant => variant.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(variant => variant.CompareAtPriceAmount)
            .HasColumnName("compare_at_price_amount")
            .HasPrecision(18, 2);

        builder.Property(variant => variant.WeightKg)
            .HasColumnName("weight_kg")
            .HasPrecision(18, 3);

        builder.Property(variant => variant.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(variant => variant.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Property(variant => variant.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(variant => variant.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(variant => variant.Sku)
            .IsUnique();

        builder.HasIndex(variant => new { variant.ProductId, variant.IsActive });

        builder.Navigation(variant => variant.OptionAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(variant => variant.OptionAssignments)
            .WithOne()
            .HasForeignKey(assignment => assignment.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.ToTable("product_options");
        builder.HasKey(option => option.Id);

        builder.Property(option => option.ProductId)
            .HasColumnName("product_id");

        builder.Property(option => option.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(option => option.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Navigation(option => option.Values)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(option => option.Values)
            .WithOne()
            .HasForeignKey(value => value.ProductOptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.ToTable("product_option_values");
        builder.HasKey(value => value.Id);

        builder.Property(value => value.ProductOptionId)
            .HasColumnName("product_option_id");

        builder.Property(value => value.Value)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(value => value.Position)
            .HasColumnName("position")
            .IsRequired();
    }
}

internal sealed class VariantOptionAssignmentConfiguration : IEntityTypeConfiguration<VariantOptionAssignment>
{
    public void Configure(EntityTypeBuilder<VariantOptionAssignment> builder)
    {
        builder.ToTable("variant_option_assignments");
        builder.HasKey(assignment => new
        {
            assignment.VariantId,
            assignment.ProductOptionId,
        });

        builder.Property(assignment => assignment.VariantId)
            .HasColumnName("variant_id");

        builder.Property(assignment => assignment.ProductOptionId)
            .HasColumnName("product_option_id");

        builder.Property(assignment => assignment.ProductOptionValueId)
            .HasColumnName("product_option_value_id");
    }
}

internal sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("product_attributes");
        builder.HasKey(attribute => attribute.Id);

        builder.Property(attribute => attribute.ProductId)
            .HasColumnName("product_id");

        builder.Property(attribute => attribute.GroupName)
            .HasColumnName("group_name")
            .HasMaxLength(120);

        builder.Property(attribute => attribute.Name)
            .IsRequired()
            .HasMaxLength(160);

        builder.Property(attribute => attribute.Value)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(attribute => attribute.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Property(attribute => attribute.IsFilterable)
            .HasColumnName("is_filterable")
            .IsRequired();
    }
}

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(image => image.Id);

        builder.Property(image => image.ProductId)
            .HasColumnName("product_id");

        builder.Property(image => image.VariantId)
            .HasColumnName("variant_id");

        builder.Property(image => image.SourceUrl)
            .HasColumnName("source_url")
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(image => image.AltText)
            .HasColumnName("alt_text")
            .HasMaxLength(320);

        builder.Property(image => image.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Property(image => image.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();

        builder.Property(image => image.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(image => new { image.ProductId, image.VariantId, image.Position });
    }
}

internal sealed class ProductRelationConfiguration : IEntityTypeConfiguration<ProductRelation>
{
    public void Configure(EntityTypeBuilder<ProductRelation> builder)
    {
        builder.ToTable("product_relations");
        builder.HasKey(relation => relation.Id);

        builder.Property(relation => relation.ProductId)
            .HasColumnName("product_id");

        builder.Property(relation => relation.RelatedProductId)
            .HasColumnName("related_product_id");

        builder.Property(relation => relation.RelationType)
            .HasConversion<string>()
            .HasColumnName("relation_type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(relation => relation.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Property(relation => relation.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(relation => new { relation.ProductId, relation.RelationType });
    }
}
