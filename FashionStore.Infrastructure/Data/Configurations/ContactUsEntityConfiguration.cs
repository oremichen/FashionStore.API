using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FashionStore.Infrastructure.Data.Configurations;

public sealed class ContactUsEntityConfiguration : IEntityTypeConfiguration<ContactUs>
{
    public void Configure(EntityTypeBuilder<ContactUs> builder)
    {
        builder.ToTable("ContactUs");
        builder.HasKey(contact => contact.Id);
        builder.Property(contact => contact.Id).HasMaxLength(50).HasDefaultValueSql("gen_random_uuid()::text");
        builder.Property(contact => contact.Name).HasMaxLength(200).IsRequired();
        builder.Property(contact => contact.Email).HasMaxLength(254).IsRequired();
        builder.Property(contact => contact.Phone).HasMaxLength(50).IsRequired();
        builder.Property(contact => contact.Subject).HasMaxLength(250).IsRequired();
        builder.Property(contact => contact.Message).HasMaxLength(5000).IsRequired();
        builder.HasIndex(contact => contact.CreatedAt);
    }
}
