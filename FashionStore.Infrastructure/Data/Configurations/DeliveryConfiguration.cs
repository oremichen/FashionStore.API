using FashionStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.ToTable("DeliveryZone"); builder.HasKey("Id");
        builder.Property("Id").HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property("Name").HasMaxLength(100).IsRequired();
        builder.Property("CreatedAt").HasDefaultValueSql("now()");
        builder.HasMany("Locations").WithOne("Zone").HasForeignKey("ZoneId").OnDelete(DeleteBehavior.Cascade);
        builder.HasMany("Rates").WithOne("Zone").HasForeignKey("ZoneId").OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DeliveryZoneLocationConfiguration : IEntityTypeConfiguration<DeliveryZoneLocation>
{
    public void Configure(EntityTypeBuilder<DeliveryZoneLocation> builder)
    {
        builder.ToTable("DeliveryZoneLocation"); builder.HasKey("Id");
        builder.Property("Id").HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property("ZoneId").HasMaxLength(50).IsRequired();
        builder.Property("State").HasMaxLength(100).IsRequired();
        builder.HasIndex("ZoneId", "State").IsUnique();
    }
}

public sealed class DeliveryMethodConfiguration : IEntityTypeConfiguration<DeliveryMethod>
{
    public void Configure(EntityTypeBuilder<DeliveryMethod> builder)
    {
        builder.ToTable("DeliveryMethod"); builder.HasKey("Id");
        builder.Property("Id").HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property("Name").HasMaxLength(100).IsRequired();
        builder.HasMany("Rates").WithOne("Method").HasForeignKey("MethodId").OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DeliveryRateConfiguration : IEntityTypeConfiguration<DeliveryRate>
{
    public void Configure(EntityTypeBuilder<DeliveryRate> builder)
    {
        builder.ToTable("DeliveryRate"); builder.HasKey("Id");
        builder.Property("Id").HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property("ZoneId").HasMaxLength(50).IsRequired();
        builder.Property("MethodId").HasMaxLength(50).IsRequired();
        builder.HasIndex("ZoneId", "MethodId").IsUnique();
        builder.HasCheckConstraint("CK_DeliveryRate_PriceKobo", "\"PriceKobo\" >= 0");
        builder.HasCheckConstraint("CK_DeliveryRate_EstimatedDays", "\"EstimatedDaysMin\" IS NULL OR \"EstimatedDaysMax\" IS NULL OR \"EstimatedDaysMin\" <= \"EstimatedDaysMax\"");
    }
}
