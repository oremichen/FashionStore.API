using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Data
{
    public class FashionStoreDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
    }
}
