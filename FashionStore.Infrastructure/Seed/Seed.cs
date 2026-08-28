using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FashionStore.Infrastructure.Seed
{
    public class Seed
    {
        public static async Task SeedData(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            var roles = Enum.GetValues(typeof(RoleEnums))
                .Cast<RoleEnums>()
                .ToList();

            foreach (var item in roles)
            {
                if (await roleManager.RoleExistsAsync(item.ToString()))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = item.ToString(),
                    NormalizedName = item.ToString().ToUpperInvariant()
                });
                EnsureSucceeded(result, $"create role {item}");
            }

            await SeedSuperAdminAsync(userManager, configuration, logger);
        }

        private static async Task SeedSuperAdminAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            var email = configuration["SeedData:SuperAdmin:Email"]
                ?? "superadmin@fashionstore.com";
            var password = configuration["SeedData:SuperAdmin:Password"]
                ?? "SuperAdmin@123";

            if (await userManager.FindByEmailAsync(email) != null)
            {
                logger.LogInformation("SuperAdmin seed user {Email} already exists. Skipping.", email);
                return;
            }

            var superAdmin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = configuration["SeedData:SuperAdmin:FirstName"] ?? "Super",
                LastName = configuration["SeedData:SuperAdmin:LastName"] ?? "Admin",
                EmailConfirmed = true,
                EmailVerified = true,
                UserStatus = "Active",
                IsPasswordChanged = false
            };

            EnsureSucceeded(
                await userManager.CreateAsync(superAdmin, password),
                $"create SuperAdmin user {email}");
            EnsureSucceeded(
                await userManager.AddToRoleAsync(superAdmin, RoleEnums.SuperAdmin.ToString()),
                $"assign SuperAdmin role to {email}");

            logger.LogInformation("Created SuperAdmin seed user {Email}.", email);
        }

        private static void EnsureSucceeded(IdentityResult result, string operation)
        {
            if (result.Succeeded)
            {
                return;
            }

            var errors = string.Join(", ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to {operation}: {errors}");
        }
    }
}
