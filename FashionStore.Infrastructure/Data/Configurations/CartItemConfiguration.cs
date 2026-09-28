using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(item => item.CartId).HasMaxLength(50).IsRequired();
        builder.Property(item => item.ProductId).HasMaxLength(50).IsRequired();
        builder.Property(item => item.VariantId).HasMaxLength(50);
        builder.Property(item => item.ColorId).HasMaxLength(50);
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();
        builder.HasIndex(item => new { item.CartId, item.ProductId, item.VariantId, item.ColorId })
            .IsUnique()
            .AreNullsDistinct(false);
        builder.HasOne<Product>().WithMany().HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(item => item.VariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Color>().WithMany().HasForeignKey(item => item.ColorId).OnDelete(DeleteBehavior.Restrict);
    }
}
