using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(variant => variant.Id);
        builder.Property(variant => variant.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(variant => variant.Sku).HasMaxLength(100);
        builder.Property(variant => variant.Barcode).HasMaxLength(100);
        builder.Property(variant => variant.NewPrice).HasPrecision(19, 4);
        builder.Property(variant => variant.OldPrice).HasPrecision(19, 4);
        builder.Property(variant => variant.CostPrice).HasPrecision(19, 4);
        builder.Property(variant => variant.Discount).HasPrecision(5, 2);
        builder.Property(variant => variant.Weight).HasPrecision(12, 3);
        builder.HasIndex(variant => variant.Sku).IsUnique();
        builder.HasIndex(variant => variant.Barcode).IsUnique();
        builder.HasIndex(variant => new { variant.ProductId, variant.SizeId, variant.ColorId }).IsUnique().AreNullsDistinct(false);
        builder.HasOne(variant => variant.Product).WithMany(product => product.Variants).HasForeignKey(variant => variant.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(variant => variant.Size).WithMany().HasForeignKey(variant => variant.SizeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(variant => variant.Color).WithMany().HasForeignKey(variant => variant.ColorId).OnDelete(DeleteBehavior.Restrict);
    }
}
