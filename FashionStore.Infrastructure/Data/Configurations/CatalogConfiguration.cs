using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

internal static class CatalogConfiguration
{
    internal static void Id<T>(EntityTypeBuilder<T> b) where T : class { b.Property("Id").HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text"); }
}

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> b) { b.ToTable("Brands"); b.HasKey(x => x.Id); CatalogConfiguration.Id(b); b.Property(x => x.Name).HasMaxLength(150).IsRequired(); b.Property(x => x.Slug).HasMaxLength(180).IsRequired(); b.Property(x => x.WebsiteUrl).HasColumnType("text"); b.Property(x => x.ImageData).HasColumnType("bytea"); b.Property(x => x.ImageContentType).HasMaxLength(100); b.Property(x => x.ImageFileName).HasMaxLength(255); b.HasIndex(x => x.Name).IsUnique().HasDatabaseName("BrandsNameUnique"); b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("BrandsSlugUnique"); }
}
public sealed class MainCarouselConfiguration : IEntityTypeConfiguration<MainCarousel>
{
    public void Configure(EntityTypeBuilder<MainCarousel> b)
    {
        b.ToTable("MainCarousels"); b.HasKey(x => x.Id); CatalogConfiguration.Id(b);
        b.Property(x => x.Title).HasMaxLength(150).IsRequired(false); b.Property(x => x.Subtitle).HasMaxLength(250);
        b.Property(x => x.ButtonText).HasMaxLength(80).HasDefaultValue("Shop now").IsRequired(); b.Property(x => x.LinkUrl).HasMaxLength(2048);
        b.Property(x => x.ImageData).HasColumnType("bytea").IsRequired(); b.Property(x => x.ImageContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.ImageFileName).HasMaxLength(255); b.Property(x => x.SortOrder).HasDefaultValue(0); b.Property(x => x.IsActive).HasDefaultValue(true);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_MainCarousels_ImageData_NotEmpty", "octet_length(\"ImageData\") > 0");
            t.HasCheckConstraint("CK_MainCarousels_ImageContentType", "\"ImageContentType\" IN ('image/jpeg', 'image/png', 'image/webp')");
            t.HasCheckConstraint("CK_MainCarousels_ImageFileSize", "\"ImageFileSize\" > 0 AND \"ImageFileSize\" <= 5242880");
            t.HasCheckConstraint("CK_MainCarousels_ImageDimensions", "\"ImageWidth\" = 1920 AND \"ImageHeight\" = 750");
            t.HasCheckConstraint("CK_MainCarousels_SortOrder", "\"SortOrder\" >= 0");
        });
    }
}
public sealed class PromotionBannerConfiguration : IEntityTypeConfiguration<PromotionBanner>
{
    public void Configure(EntityTypeBuilder<PromotionBanner> b)
    {
        b.ToTable("Promotion_banner"); b.HasKey(x => x.Id); CatalogConfiguration.Id(b);
        b.Property(x => x.Title).HasMaxLength(150); b.Property(x => x.Subtitle).HasMaxLength(250);
        b.Property(x => x.DestinationUrl).HasMaxLength(2048); b.Property(x => x.Placement).HasMaxLength(100).HasDefaultValue("homepage-banner-grid").IsRequired();
        b.Property(x => x.IsActive).HasDefaultValue(false); b.Property(x => x.ImageData).HasColumnType("bytea").IsRequired();
        b.Property(x => x.ImageContentType).HasMaxLength(100).IsRequired(); b.Property(x => x.ImageFileName).HasMaxLength(255).IsRequired();
        b.HasIndex(x => x.Slot).IsUnique().HasDatabaseName("Promotion_banner_slot_unique");
        b.ToTable(t => { t.HasCheckConstraint("CK_Promotion_banner_Slot", "\"Slot\" > 0"); t.HasCheckConstraint("CK_Promotion_banner_ImageData", "octet_length(\"ImageData\") > 0"); t.HasCheckConstraint("CK_Promotion_banner_ImageFileSize", "\"ImageFileSize\" > 0 AND \"ImageFileSize\" <= 5242880"); });
    }
}
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b) { b.ToTable("Products"); b.HasKey(x => x.Id); CatalogConfiguration.Id(b); b.Property(x => x.CategoryId).HasMaxLength(50); b.Property(x => x.BrandId).HasMaxLength(50); b.Property(x => x.Name).HasMaxLength(250).IsRequired(); b.Property(x => x.Slug).HasMaxLength(280).IsRequired(); b.Property(x => x.ShortDescription).HasMaxLength(500); b.Property(x => x.OldPrice).HasPrecision(19,4); b.Property(x => x.NewPrice).HasPrecision(19,4); b.Property(x => x.Discount).HasPrecision(5,2); b.Property(x => x.CurrencyCode).HasMaxLength(3); b.Property(x => x.Weight).HasPrecision(12,3); b.Property(x => x.RatingsValue).HasPrecision(14,4); b.Property(x => x.IsArchived).HasDefaultValue(false); b.HasIndex(x => x.Slug).IsUnique(); b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Brand).WithMany(x => x.Products).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.SetNull); }
}
public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage> { public void Configure(EntityTypeBuilder<ProductImage> b) { b.ToTable("ProductImages"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.Property(x=>x.ProductId).HasMaxLength(50); b.Property(x=>x.AlternativeText).HasMaxLength(250); b.Property(x=>x.SmallImageData).HasColumnType("bytea"); b.Property(x=>x.MediumImageData).HasColumnType("bytea"); b.Property(x=>x.ImageData).HasColumnType("bytea"); b.Property(x=>x.ImageContentType).HasMaxLength(100); b.Property(x=>x.ImageFileName).HasMaxLength(255); b.HasOne(x=>x.Product).WithMany(x=>x.Images).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x=>new{x.ProductId,x.SortOrder}).IsUnique(); } }
public sealed class SizeConfiguration : IEntityTypeConfiguration<Size> { public void Configure(EntityTypeBuilder<Size> b) { b.ToTable("Sizes"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.Property(x=>x.Name).HasMaxLength(50); b.Property(x=>x.DisplayName).HasMaxLength(100); b.HasIndex(x=>x.Name).IsUnique(); } }
public sealed class ColorConfiguration : IEntityTypeConfiguration<Color> { public void Configure(EntityTypeBuilder<Color> b) { b.ToTable("Colors"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.Property(x=>x.Name).HasMaxLength(100); b.Property(x=>x.HexCode).HasMaxLength(9); b.HasIndex(x=>x.Name).IsUnique(); } }
public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant> { public void Configure(EntityTypeBuilder<ProductVariant> b) { b.ToTable("ProductVariants"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.Property(x=>x.Sku).HasMaxLength(100); b.Property(x=>x.Barcode).HasMaxLength(100); b.Property(x=>x.NewPrice).HasPrecision(19,4); b.Property(x=>x.OldPrice).HasPrecision(19,4); b.Property(x=>x.CostPrice).HasPrecision(19,4); b.Property(x=>x.Discount).HasPrecision(5,2); b.Property(x=>x.Weight).HasPrecision(12,3); b.HasIndex(x=>x.Sku).IsUnique(); b.HasIndex(x=>x.Barcode).IsUnique(); b.HasIndex(x=>new{x.ProductId,x.SizeId,x.ColorId}).IsUnique().AreNullsDistinct(false); b.HasOne(x=>x.Product).WithMany(x=>x.Variants).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade); b.HasOne(x=>x.Size).WithMany().HasForeignKey(x=>x.SizeId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x=>x.Color).WithMany().HasForeignKey(x=>x.ColorId).OnDelete(DeleteBehavior.Restrict); } }
public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview> { public void Configure(EntityTypeBuilder<ProductReview> b) { b.ToTable("ProductReviews"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.Property(x=>x.ReviewerName).HasMaxLength(150); b.Property(x=>x.ReviewerEmail).HasMaxLength(320); b.Property(x=>x.Title).HasMaxLength(200); b.Property(x=>x.Status).HasConversion<string>().HasMaxLength(30); b.HasOne(x=>x.Product).WithMany(x=>x.Reviews).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute> { public void Configure(EntityTypeBuilder<ProductAttribute> b) { b.ToTable("ProductAttributes"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.Property(x=>x.Name).HasMaxLength(150); b.HasIndex(x=>new{x.ProductId,x.Name}).IsUnique(); b.HasOne(x=>x.Product).WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag> { public void Configure(EntityTypeBuilder<ProductTag> b) { b.ToTable("ProductTags"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.Property(x=>x.Name).HasMaxLength(100); b.Property(x=>x.Slug).HasMaxLength(120); b.HasIndex(x=>x.Name).IsUnique(); b.HasIndex(x=>x.Slug).IsUnique(); } }
public sealed class ProductTagMappingConfiguration : IEntityTypeConfiguration<ProductTagMapping> { public void Configure(EntityTypeBuilder<ProductTagMapping> b) { b.ToTable("ProductTagMappings"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.HasIndex(x=>new{x.ProductId,x.ProductTagId}).IsUnique(); b.HasOne(x=>x.Product).WithMany().HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade); b.HasOne(x=>x.ProductTag).WithMany().HasForeignKey(x=>x.ProductTagId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class ProductVariantImageConfiguration : IEntityTypeConfiguration<ProductVariantImage> { public void Configure(EntityTypeBuilder<ProductVariantImage> b) { b.ToTable("ProductVariantImages"); b.HasKey(x=>x.Id); CatalogConfiguration.Id(b); b.HasIndex(x=>new{x.ProductVariantId,x.ProductImageId}).IsUnique(); b.HasOne(x=>x.ProductVariant).WithMany().HasForeignKey(x=>x.ProductVariantId).OnDelete(DeleteBehavior.Cascade); b.HasOne(x=>x.ProductImage).WithMany().HasForeignKey(x=>x.ProductImageId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class ProductSizeConfiguration : IEntityTypeConfiguration<ProductSize>
{
    public void Configure(EntityTypeBuilder<ProductSize> b)
    {
        b.ToTable("ProductSize"); b.HasKey(x => new { x.ProductId, x.SizeId });
        b.Property(x => x.ProductId).HasMaxLength(50); b.Property(x => x.SizeId).HasMaxLength(50);
        b.HasOne(x => x.Product).WithMany(x => x.ProductSizes).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Size).WithMany().HasForeignKey(x => x.SizeId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class ProductColorConfiguration : IEntityTypeConfiguration<ProductColor>
{
    public void Configure(EntityTypeBuilder<ProductColor> b)
    {
        b.ToTable("ProductColor"); b.HasKey(x => new { x.ProductId, x.ColorId });
        b.Property(x => x.ProductId).HasMaxLength(50); b.Property(x => x.ColorId).HasMaxLength(50);
        b.HasOne(x => x.Product).WithMany(x => x.ProductColors).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Color).WithMany().HasForeignKey(x => x.ColorId).OnDelete(DeleteBehavior.Cascade);
    }
}
