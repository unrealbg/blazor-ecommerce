using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Search.Domain.Documents;

namespace Search.Infrastructure.Persistence;

internal sealed class ProductSearchDocumentConfiguration : IEntityTypeConfiguration<ProductSearchDocument>
{
    public void Configure(EntityTypeBuilder<ProductSearchDocument> builder)
    {
        builder.ToTable("product_search_documents");

        builder.HasKey(document => document.ProductId);

        builder.Property(document => document.ProductId)
            .HasColumnName("product_id");

        builder.Property(document => document.Slug)
            .HasColumnName("slug")
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(document => document.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(document => document.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(document => document.DescriptionText)
            .HasColumnName("description_text")
            .HasMaxLength(4000);

        builder.Property(document => document.CategorySlug)
            .HasColumnName("category_slug")
            .HasMaxLength(120);

        builder.Property(document => document.CategoryName)
            .HasColumnName("category_name")
            .HasMaxLength(200);

        builder.Property(document => document.Brand)
            .HasColumnName("brand")
            .HasMaxLength(120);

        builder.Property(document => document.PriceAmount)
            .HasColumnName("price_amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(document => document.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(document => document.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(document => document.IsInStock)
            .HasColumnName("is_in_stock")
            .IsRequired();

        builder.Property(document => document.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(2000);

        builder.Property(document => document.PopularityScore)
            .HasColumnName("popularity_score")
            .HasPrecision(18, 4);

        builder.Property(document => document.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(document => document.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(document => document.Slug)
            .HasDatabaseName("ix_product_search_documents_slug");

        builder.HasIndex(document => document.CategorySlug)
            .HasDatabaseName("ix_product_search_documents_category_slug");

        builder.HasIndex(document => document.Brand)
            .HasDatabaseName("ix_product_search_documents_brand");

        builder.HasIndex(document => document.PriceAmount)
            .HasDatabaseName("ix_product_search_documents_price_amount");

        builder.HasIndex(document => document.IsActive)
            .HasDatabaseName("ix_product_search_documents_is_active");

        builder.HasIndex(document => document.IsInStock)
            .HasDatabaseName("ix_product_search_documents_is_in_stock");

        builder.HasIndex(document => document.NormalizedName)
            .HasDatabaseName("ix_product_search_documents_normalized_name");
    }
}
