using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Data
{
    public class FashionStoreDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public FashionStoreDbContext(DbContextOptions<FashionStoreDbContext> options) : base(options)
        {
        }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<QueueEmailNotification> QueueEmailNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Deletes addresses when user is deleted

            builder.Entity<QueueEmailNotification>(entity =>
            {
                entity.ToTable("QueueEmailNotification");
                entity.Property(item => item.Id).HasDefaultValueSql("gen_random_uuid()");
            });
        }
    }
}
