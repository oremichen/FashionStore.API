using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(cart => cart.Id);
        builder.Property(cart => cart.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(cart => cart.UserId).HasMaxLength(450).IsRequired();
        builder.Property(cart => cart.CreatedAt).IsRequired();
        builder.Property(cart => cart.UpdatedAt).IsRequired();
        builder.HasIndex(cart => cart.UserId).IsUnique();
        builder.HasOne(cart => cart.User).WithMany().HasForeignKey(cart => cart.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(cart => cart.Items).WithOne(item => item.Cart).HasForeignKey(item => item.CartId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(cart => cart.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
