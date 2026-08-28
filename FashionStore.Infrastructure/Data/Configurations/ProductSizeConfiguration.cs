using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> builder)
    {
        builder.ToTable("ProductSize");
        builder.HasKey(mapping => new { mapping.ProductId, mapping.SizeId });
        builder.Property(mapping => mapping.ProductId).HasMaxLength(50);
        builder.Property(mapping => mapping.SizeId).HasMaxLength(50);
        builder.HasOne(mapping => mapping.Product).WithMany(product => product.ProductSizes).HasForeignKey(mapping => mapping.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mapping => mapping.Size).WithMany().HasForeignKey(mapping => mapping.SizeId).OnDelete(DeleteBehavior.Cascade);
    }
}
