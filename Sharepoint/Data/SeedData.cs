using Microsoft.AspNetCore.Identity;

namespace Sharepoint.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            string[] roles = { "Admin", "User" };
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            var defaults = new[]
            {
                ("admin@company.com", "Admin123!", "Admin"),
            };

            foreach (var (email, password, role) in defaults)
            {
                var existing = await userManager.FindByEmailAsync(email);
                if (existing != null)
                {
                    // Force reset password in case it was created wrong
                    var token = await userManager.GeneratePasswordResetTokenAsync(existing);
                    await userManager.ResetPasswordAsync(existing, token, password);
                }
                else
                {
                    var user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        NormalizedEmail = email.ToUpper(),
                        NormalizedUserName = email.ToUpper()
                    };
                    var result = await userManager.CreateAsync(user, password);
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}