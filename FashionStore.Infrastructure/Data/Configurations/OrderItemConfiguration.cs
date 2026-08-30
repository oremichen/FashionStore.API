using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(item => item.OrderId).HasMaxLength(50).IsRequired();
        builder.Property(item => item.ProductId).HasMaxLength(50).IsRequired();
        builder.Property(item => item.VariantId).HasMaxLength(50);
        builder.Property(item => item.ProductName).HasMaxLength(250).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.LineTotal).HasPrecision(18, 2);
    }
}
