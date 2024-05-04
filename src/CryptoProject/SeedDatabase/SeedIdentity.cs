using CryptoProject.Entities.Enums;
using CryptoProject.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace CryptoProject.SeedDatabase
{
    public static class SeedIdentity
    {
        public static async Task SeedAsync(UserManager<User>? userManager, RoleManager<Role>? roleManager)
        {
            if (userManager is null || roleManager is null) return;
            await SeedRoles(roleManager);

            //Seed Super Admin
            var superAdminEmail = Environment.GetEnvironmentVariable("ROOT_ADMIN_EMAIL");
            if (await userManager.FindByEmailAsync(superAdminEmail) is null)
            {
                var superAdmin = new User()
                {
                    Id = Guid.NewGuid(),
                    Email = superAdminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    PhoneNumber = Environment.GetEnvironmentVariable("ROOT_ADMIN_PHONENUMBER"),
                    UserName = superAdminEmail,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    Role = RoleType.SuperAdmin,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(superAdmin, Environment.GetEnvironmentVariable("ROOT_DEFAULT_PASSWORD"));
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, nameof(RoleType.SuperAdmin));
                }
            }
            
            //ADMIN
            var Admin = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            if (await userManager.FindByEmailAsync(Admin) is null)
            {
                var admin = new User()
                {
                    Id = Guid.NewGuid(),
                    Email = Admin,
                    FirstName = "Boss",
                    LastName = "Admin",
                    PhoneNumber = Environment.GetEnvironmentVariable("ADMIN_PHONENUMBER"),
                    UserName = Admin,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    Role = RoleType.Admin,
                    IsActive = true
                };
                var result = await userManager.CreateAsync(admin, Environment.GetEnvironmentVariable("ADMIN_PASSWORD"));
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, nameof(RoleType.Admin));
                }
            }

        }
        private static async Task SeedRoles(RoleManager<Role> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(nameof(RoleType.SuperAdmin)))
            {
                await roleManager.CreateAsync(new Role(nameof(RoleType.SuperAdmin)));
            }
            if (!await roleManager.RoleExistsAsync(nameof(RoleType.Admin)))
            {
                await roleManager.CreateAsync(new Role(nameof(RoleType.Admin)));
            }
            if (!await roleManager.RoleExistsAsync(nameof(RoleType.User)))
            {
                await roleManager.CreateAsync(new Role(nameof(RoleType.User)));
            }
        }
    }

}
