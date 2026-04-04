using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace FashionStore.Infrastructure.Seed
{
    public class Seed
    {
        public static async Task SeedData(FashionStoreDbContext context, RoleManager<ApplicationRole> roleManager, IConfiguration configuration)
        {
            var roles = Enum.GetValues(typeof(RoleEnums))
                            .Cast<RoleEnums>()
                            .ToList();

            foreach (var item in roles)
            {
                var roleExist = await roleManager.RoleExistsAsync(item.ToString());
                if (!roleExist)
                {
                    var role = new ApplicationRole
                    {
                        Name = item.ToString(),
                        NormalizedName = item.ToString().ToUpper()
                    };
                    await roleManager.CreateAsync(role);
                }
            }
        }
    }
}
