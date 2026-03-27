using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Data
{
    public class FashionStoreDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public FashionStoreDbContext(DbContextOptions<FashionStoreDbContext> options) : base(options)
        {
        }
    }
}
