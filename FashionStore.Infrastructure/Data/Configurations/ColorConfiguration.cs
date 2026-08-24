using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ColorConfiguration : IEntityTypeConfiguration<Color>
{
    public void Configure(EntityTypeBuilder<Color> builder)
    {
        builder.ToTable("Colors");
        builder.HasKey(color => color.Id);
        builder.Property(color => color.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(color => color.Name).HasMaxLength(100);
        builder.Property(color => color.HexCode).HasMaxLength(9);
        builder.HasIndex(color => color.Name).IsUnique();
    }
}
