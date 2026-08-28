using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class PromotionVideoConfiguration : IEntityTypeConfiguration<PromotionVideo>
{
    public void Configure(EntityTypeBuilder<PromotionVideo> builder)
    {
        builder.ToTable("PromotionVideos");
        builder.HasKey(video => video.Id);
        builder.Property(video => video.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(video => video.Title).HasMaxLength(150).IsRequired();
        builder.Property(video => video.Slug).HasMaxLength(180).IsRequired();
        builder.Property(video => video.VideoUrl).HasMaxLength(2048).IsRequired(false);
        builder.Property(video => video.VideoContentType).HasMaxLength(100).IsRequired();
        builder.Property(video => video.VideoFileName).HasMaxLength(255).IsRequired();
        builder.Property(video => video.IsActive).HasDefaultValue(false);
        builder.Ignore(video => video.HasExpired);
        builder.HasIndex(video => video.Slug).IsUnique().HasDatabaseName("PromotionVideosSlugUnique");
        builder.HasIndex(video => video.IsActive).IsUnique().HasFilter("\"IsActive\" = TRUE").HasDatabaseName("PromotionVideosOneActiveUnique");
    }
}
