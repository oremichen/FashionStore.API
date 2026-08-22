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
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<MainCarousel> MainCarousels { get; set; }
        public DbSet<PromotionBanner> PromotionBanners { get; set; }
        public DbSet<PromotionVideo> PromotionVideos { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<Color> Colors { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductTag> ProductTags { get; set; }
        public DbSet<ProductTagMapping> ProductTagMappings { get; set; }
        public DbSet<ProductVariantImage> ProductVariantImages { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<ProductColor> ProductColors { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(FashionStoreDbContext).Assembly);

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
