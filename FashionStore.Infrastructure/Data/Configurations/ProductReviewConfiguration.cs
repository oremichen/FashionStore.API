using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReviews");
        builder.HasKey(review => review.Id);
        builder.Property(review => review.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(review => review.ReviewerName).HasMaxLength(150);
        builder.Property(review => review.ReviewerEmail).HasMaxLength(320);
        builder.Property(review => review.Title).HasMaxLength(200);
        builder.Property(review => review.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasOne(review => review.Product).WithMany(product => product.Reviews).HasForeignKey(review => review.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
