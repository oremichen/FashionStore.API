using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", table =>
        {
            table.HasCheckConstraint("CK_Categories_Name_NotBlank", "btrim(\"Name\") <> ''");
            table.HasCheckConstraint("CK_Categories_Id_UuidFormat", "\"Id\" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$'");
            table.HasCheckConstraint("CK_Categories_Slug_Format", "\"Slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
            table.HasCheckConstraint("CK_Categories_SortOrder_Nonnegative", "\"SortOrder\" >= 0");
            table.HasCheckConstraint("CK_Categories_ParentId_NotSelf", "\"ParentId\" IS NULL OR \"ParentId\" <> \"Id\"");
        });

        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(category => category.ParentId).HasMaxLength(50);
        builder.Property(category => category.Name).HasMaxLength(150).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(180).IsRequired();
        builder.Property(category => category.SortOrder).HasDefaultValue(0);
        builder.Property(category => category.IsActive).HasDefaultValue(true);
        builder.Property(category => category.ShowInMenu).HasDefaultValue(true);
        builder.Property(category => category.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(category => category.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(category => category.Parent)
            .WithMany(category => category.Children)
            .HasForeignKey(category => category.ParentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Categories_ParentId");

        builder.HasIndex(category => category.Slug)
            .HasDatabaseName("UX_Categories_Active_Slug")
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasIndex(category => new { category.ParentId, category.Name })
            .HasDatabaseName("UX_Categories_Active_ParentId_Name")
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasIndex(category => new { category.ParentId, category.SortOrder, category.Name })
            .HasDatabaseName("IX_Categories_ParentId_SortOrder")
            .HasFilter("\"DeletedAt\" IS NULL");
    }
}
