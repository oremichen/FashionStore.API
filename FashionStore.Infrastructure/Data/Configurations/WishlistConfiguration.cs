using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists");
        builder.HasKey(wishlist => wishlist.Id);
        builder.Property(wishlist => wishlist.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(wishlist => wishlist.UserId).HasMaxLength(450).IsRequired();
        builder.Property(wishlist => wishlist.CreatedAt).IsRequired();
        builder.Property(wishlist => wishlist.UpdatedAt).IsRequired();
        builder.HasIndex(wishlist => wishlist.UserId).IsUnique();
        builder.HasOne(wishlist => wishlist.User).WithMany().HasForeignKey(wishlist => wishlist.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(wishlist => wishlist.Items).WithOne(item => item.Wishlist).HasForeignKey(item => item.WishlistId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(wishlist => wishlist.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
