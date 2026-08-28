using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(product => product.CategoryId).HasMaxLength(50);
        builder.Property(product => product.BrandId).HasMaxLength(50);
        builder.Property(product => product.Name).HasMaxLength(250).IsRequired();
        builder.Property(product => product.Slug).HasMaxLength(280).IsRequired();
        builder.Property(product => product.ShortDescription).HasMaxLength(500);
        builder.Property(product => product.OldPrice).HasPrecision(19, 4);
        builder.Property(product => product.NewPrice).HasPrecision(19, 4);
        builder.Property(product => product.MinPrice).HasPrecision(18, 2);
        builder.Property(product => product.MaxPrice).HasPrecision(18, 2);
        builder.Property(product => product.Discount).HasPrecision(5, 2);
        builder.Property(product => product.CurrencyCode).HasMaxLength(3);
        builder.Property(product => product.Weight).HasPrecision(12, 3);
        builder.Property(product => product.RatingsValue).HasPrecision(14, 4);
        builder.Property(product => product.IsArchived).HasDefaultValue(false);
        builder.HasIndex(product => product.Slug).IsUnique();
        builder.HasOne(product => product.Category).WithMany().HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(product => product.Brand).WithMany(brand => brand.Products).HasForeignKey(product => product.BrandId).OnDelete(DeleteBehavior.SetNull);
    }
}
