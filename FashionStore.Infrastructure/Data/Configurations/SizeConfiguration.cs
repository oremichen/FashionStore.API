using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class SizeConfiguration : IEntityTypeConfiguration<Size>
{
    public void Configure(EntityTypeBuilder<Size> builder)
    {
        builder.ToTable("Sizes");
        builder.HasKey(size => size.Id);
        builder.Property(size => size.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(size => size.Name).HasMaxLength(50);
        builder.Property(size => size.DisplayName).HasMaxLength(100);
        builder.HasIndex(size => size.Name).IsUnique();
    }
}
