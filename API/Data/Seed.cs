using System.Text.Json;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class Seed
{
    public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        // Skip if users already exist
        if (await userManager.Users.AnyAsync()) return;

        // Load user data from JSON
        var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

        if (users == null || users.Count == 0) return;

        // Define app roles
        var roles = new List<AppRole>
        {
            new AppRole { Name = "Member" },
            new AppRole { Name = "Admin" },
            new AppRole { Name = "Moderator" }
        };

        // Create roles if not already existing
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                await roleManager.CreateAsync(role);
            }
        }

        // Seed each user
        foreach (var user in users)
        {
            user.Photos.First().IsApproved = true;
            user.UserName = user.UserName!.ToLower();

            var result = await userManager.CreateAsync(user, "Pa$$w0rd");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Member");
            }
        }

        // Create Admin user
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            var admin = new AppUser
            {
                UserName = "admin",
                KnownAs = "Admin",
                Gender = "male",
                City = "Adminville",
                Country = "Nowhere"
            };

            var adminResult = await userManager.CreateAsync(admin, "Pa$$w0rd");

            if (adminResult.Succeeded)
            {
                await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });
                Console.WriteLine("Admin user seeded.");
            }
            else
            {
                Console.WriteLine("Failed to seed admin user:");
                foreach (var error in adminResult.Errors)
                {
                    Console.WriteLine($" - {error.Description}");
                }
            }
        }
    }
}
