using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductTagMappingConfiguration : IEntityTypeConfiguration<ProductTagMapping>
{
    public void Configure(EntityTypeBuilder<ProductTagMapping> builder)
    {
        builder.ToTable("ProductTagMappings");
        builder.HasKey(mapping => mapping.Id);
        builder.Property(mapping => mapping.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.HasIndex(mapping => new { mapping.ProductId, mapping.ProductTagId }).IsUnique();
        builder.HasOne(mapping => mapping.Product).WithMany().HasForeignKey(mapping => mapping.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(mapping => mapping.ProductTag).WithMany().HasForeignKey(mapping => mapping.ProductTagId).OnDelete(DeleteBehavior.Cascade);
    }
}
