using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ContactUsConfigurationEntityConfiguration : IEntityTypeConfiguration<ContactUsConfiguration>
{
    public void Configure(EntityTypeBuilder<ContactUsConfiguration> builder)
    {
        builder.ToTable("ContactUsConfiguration");
        builder.HasKey(contact => contact.Id);
        builder.Property(contact => contact.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(contact => contact.Address).HasMaxLength(500).IsRequired();
        builder.Property(contact => contact.ContactPhone).HasMaxLength(50).IsRequired();
        builder.Property(contact => contact.BusinessPhone).HasMaxLength(50).IsRequired(false);
        builder.Property(contact => contact.ContactEmail).HasMaxLength(254).IsRequired();
        builder.Property(contact => contact.BusinessEmail).HasMaxLength(254).IsRequired(false);
        builder.Property(contact => contact.IsActive).HasDefaultValue(false);
        builder.HasIndex(contact => contact.IsActive)
            .IsUnique()
            .HasFilter("\"IsActive\" = TRUE")
            .HasDatabaseName("ContactUsOneActiveUnique");
    }
}
