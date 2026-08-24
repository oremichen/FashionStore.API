using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(image => image.Id);
        builder.Property(image => image.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(image => image.ProductId).HasMaxLength(50);
        builder.Property(image => image.AlternativeText).HasMaxLength(250);
        builder.Ignore(image => image.SmallImageData);
        builder.Ignore(image => image.MediumImageData);
        builder.Ignore(image => image.ImageData);
        builder.Property(image => image.ImageContentType).HasMaxLength(100);
        builder.Property(image => image.ImageFileName).HasMaxLength(255);
        builder.Property(image => image.SmallUrl).HasMaxLength(2048).IsRequired(false);
        builder.Property(image => image.MediumUrl).HasMaxLength(2048).IsRequired(false);
        builder.Property(image => image.BigUrl).HasMaxLength(2048).IsRequired(false);
        builder.HasOne(image => image.Product).WithMany(product => product.Images).HasForeignKey(image => image.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(image => new { image.ProductId, image.SortOrder }).IsUnique();
    }
}
