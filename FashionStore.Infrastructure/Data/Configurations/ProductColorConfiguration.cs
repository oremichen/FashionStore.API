using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductColorConfiguration : IEntityTypeConfiguration<ProductColor>
{
    public void Configure(EntityTypeBuilder<ProductColor> builder)
    {
        builder.ToTable("ProductColor");
        builder.HasKey(mapping => new { mapping.ProductId, mapping.ColorId });
        builder.Property(mapping => mapping.ProductId).HasMaxLength(50);
        builder.Property(mapping => mapping.ColorId).HasMaxLength(50);
        builder.HasOne(mapping => mapping.Product).WithMany(product => product.ProductColors).HasForeignKey(mapping => mapping.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mapping => mapping.Color).WithMany().HasForeignKey(mapping => mapping.ColorId).OnDelete(DeleteBehavior.Cascade);
    }
}
