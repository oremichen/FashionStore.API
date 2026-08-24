using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        builder.ToTable("ProductTags");
        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(tag => tag.Name).HasMaxLength(100);
        builder.Property(tag => tag.Slug).HasMaxLength(120);
        builder.HasIndex(tag => tag.Name).IsUnique();
        builder.HasIndex(tag => tag.Slug).IsUnique();
    }
}
