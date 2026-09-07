using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations");
        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(reservation => reservation.OrderId).HasMaxLength(50).IsRequired();
        builder.Property(reservation => reservation.ProductId).HasMaxLength(50).IsRequired();
        builder.Property(reservation => reservation.Status).HasMaxLength(20).IsRequired();
        builder.Property(reservation => reservation.ExpiresAt).IsRequired();
        builder.Property(reservation => reservation.CreatedAt).IsRequired();
        builder.HasIndex(reservation => new { reservation.OrderId, reservation.ProductId }).IsUnique()
            .HasDatabaseName("ux_inventoryreservations_orderid_productid");
        builder.HasIndex(reservation => new { reservation.Status, reservation.ExpiresAt })
            .HasDatabaseName("ix_inventoryreservations_status_expiresat");
        builder.HasOne(reservation => reservation.Order).WithMany(order => order.InventoryReservations)
            .HasForeignKey(reservation => reservation.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(reservation => reservation.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
