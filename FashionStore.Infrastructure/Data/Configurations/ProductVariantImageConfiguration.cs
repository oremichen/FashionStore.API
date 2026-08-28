using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductVariantImageConfiguration : IEntityTypeConfiguration<ProductVariantImage>
{
    public void Configure(EntityTypeBuilder<ProductVariantImage> builder)
    {
        builder.ToTable("ProductVariantImages");
        builder.HasKey(mapping => mapping.Id);
        builder.Property(mapping => mapping.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.HasIndex(mapping => new { mapping.ProductVariantId, mapping.ProductImageId }).IsUnique();
        builder.HasOne(mapping => mapping.ProductVariant).WithMany().HasForeignKey(mapping => mapping.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mapping => mapping.ProductImage).WithMany().HasForeignKey(mapping => mapping.ProductImageId).OnDelete(DeleteBehavior.Cascade);
    }
}
