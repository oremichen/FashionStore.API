using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TypeEntity = FashionStore.Domain.Entities.Type;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class TypeConfiguration : IEntityTypeConfiguration<TypeEntity>
{
    public void Configure(EntityTypeBuilder<TypeEntity> builder)
    {
        builder.ToTable("Types", table =>
        {
            table.HasCheckConstraint("CK_Types_Name_NotBlank", "btrim(\"Name\") <> ''");
            table.HasCheckConstraint("CK_Types_Slug_Format", "\"Slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
        });

        builder.HasKey(type => type.Id);
        builder.Property(type => type.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(type => type.Name).HasMaxLength(150).IsRequired();
        builder.Property(type => type.Slug).HasMaxLength(150).IsRequired();
        builder.Property(type => type.Description).HasMaxLength(500);
        builder.Property(type => type.IsActive).HasDefaultValue(true);
        builder.Property(type => type.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(type => type.Name).IsUnique();
        builder.HasIndex(type => type.Slug).IsUnique();
    }
}
