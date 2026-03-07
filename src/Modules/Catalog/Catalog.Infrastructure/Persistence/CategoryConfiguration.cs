using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(category => category.Slug)
            .IsRequired()
            .HasMaxLength(220);

        builder.Property(category => category.Description)
            .HasMaxLength(2000);

        builder.Property(category => category.ParentCategoryId)
            .HasColumnName("parent_category_id");

        builder.Property(category => category.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(category => category.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(category => category.SeoTitle)
            .HasColumnName("seo_title")
            .HasMaxLength(200);

        builder.Property(category => category.SeoDescription)
            .HasColumnName("seo_description")
            .HasMaxLength(320);

        builder.Property(category => category.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(2000);

        builder.Property(category => category.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(category => category.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(category => category.Slug)
            .IsUnique();

        builder.HasIndex(category => category.ParentCategoryId);
    }
}
