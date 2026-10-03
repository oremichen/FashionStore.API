using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(item => item.WishlistId).HasMaxLength(50).IsRequired();
        builder.Property(item => item.ProductId).HasMaxLength(50).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.HasIndex(item => new { item.WishlistId, item.ProductId }).IsUnique();
        builder.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
