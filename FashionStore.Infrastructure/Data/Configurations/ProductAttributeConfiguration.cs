using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("ProductAttributes");
        builder.HasKey(attribute => attribute.Id);
        builder.Property(attribute => attribute.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(attribute => attribute.Name).HasMaxLength(150);
        builder.HasIndex(attribute => new { attribute.ProductId, attribute.Name }).IsUnique();
        builder.HasOne(attribute => attribute.Product).WithMany().HasForeignKey(attribute => attribute.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
