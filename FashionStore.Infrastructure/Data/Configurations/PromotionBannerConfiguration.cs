using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class PromotionBannerConfiguration : IEntityTypeConfiguration<PromotionBanner>
{
    public void Configure(EntityTypeBuilder<PromotionBanner> builder)
    {
        builder.ToTable("Promotion_banner");
        builder.HasKey(banner => banner.Id);
        builder.Property(banner => banner.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(banner => banner.Title).HasMaxLength(150);
        builder.Property(banner => banner.Subtitle).HasMaxLength(250);
        builder.Property(banner => banner.DestinationUrl).HasMaxLength(2048);
        builder.Property(banner => banner.Placement).HasMaxLength(100).HasDefaultValue("homepage-banner-grid").IsRequired();
        builder.Property(banner => banner.IsActive).HasDefaultValue(false);
        builder.Ignore(banner => banner.ImageData);
        builder.Property(banner => banner.ImageUrl).HasMaxLength(2048).IsRequired(false);
        builder.Property(banner => banner.ImageContentType).HasMaxLength(100).IsRequired();
        builder.Property(banner => banner.ImageFileName).HasMaxLength(255).IsRequired();
        builder.HasIndex(banner => banner.Slot).IsUnique().HasDatabaseName("Promotion_banner_slot_unique");
        builder.ToTable(table => table.HasCheckConstraint("CK_Promotion_banner_Slot", "\"Slot\" > 0"));
    }
}
