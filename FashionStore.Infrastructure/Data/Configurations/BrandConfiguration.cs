using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(brand => brand.Id);
        builder.Property(brand => brand.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(brand => brand.Name).HasMaxLength(150).IsRequired();
        builder.Property(brand => brand.Slug).HasMaxLength(180).IsRequired();
        builder.Property(brand => brand.WebsiteUrl).HasColumnType("text");
        builder.Ignore(brand => brand.ImageData);
        builder.Property(brand => brand.ImageContentType).HasMaxLength(100);
        builder.Property(brand => brand.ImageFileName).HasMaxLength(255);
        builder.Property(brand => brand.ImageUrl).HasMaxLength(2048);
        builder.HasIndex(brand => brand.Name).IsUnique().HasDatabaseName("BrandsNameUnique");
        builder.HasIndex(brand => brand.Slug).IsUnique().HasDatabaseName("BrandsSlugUnique");
    }
}
