using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(item => item.UserId).HasMaxLength(450).IsRequired();
        builder.Property(item => item.AddressId).HasMaxLength(50).IsRequired();
        builder.Property(item => item.Email).HasMaxLength(320).IsRequired();
        builder.Property(item => item.DeliveryMethod).HasMaxLength(30).IsRequired();
        builder.Property(item => item.Subtotal).HasPrecision(18, 2);
        builder.Property(item => item.DeliveryFee).HasPrecision(18, 2);
        builder.Property(item => item.Total).HasPrecision(18, 2);
        builder.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        builder.Property(item => item.Status).HasMaxLength(30).IsRequired();
        builder.Property(item => item.PaymentReference).HasMaxLength(100).IsRequired();
        builder.Property(item => item.PaymentStatus).HasMaxLength(30).IsRequired();
        builder.HasIndex(item => item.PaymentReference).IsUnique();
        builder.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Items).WithOne(item => item.Order).HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(item => item.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
