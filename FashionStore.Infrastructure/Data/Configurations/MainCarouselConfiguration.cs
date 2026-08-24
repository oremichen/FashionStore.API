using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class MainCarouselConfiguration : IEntityTypeConfiguration<MainCarousel>
{
    public void Configure(EntityTypeBuilder<MainCarousel> builder)
    {
        builder.ToTable("MainCarousels");
        builder.HasKey(carousel => carousel.Id);
        builder.Property(carousel => carousel.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(carousel => carousel.Title).HasMaxLength(150).IsRequired(false);
        builder.Property(carousel => carousel.Subtitle).HasMaxLength(250);
        builder.Property(carousel => carousel.ButtonText).HasMaxLength(80).HasDefaultValue("Shop now").IsRequired();
        builder.Property(carousel => carousel.LinkUrl).HasMaxLength(2048);
        builder.Ignore(carousel => carousel.ImageData);
        builder.Property(carousel => carousel.ImageUrl).HasMaxLength(2048).IsRequired(false);
        builder.Property(carousel => carousel.ImageContentType).HasMaxLength(100).IsRequired();
        builder.Property(carousel => carousel.ImageFileName).HasMaxLength(255);
        builder.Property(carousel => carousel.SortOrder).HasDefaultValue(0);
        builder.Property(carousel => carousel.IsActive).HasDefaultValue(true);
        builder.ToTable(table => table.HasCheckConstraint("CK_MainCarousels_SortOrder", "\"SortOrder\" >= 0"));
    }
}
