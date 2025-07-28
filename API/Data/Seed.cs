using System.Text.Json;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class Seed
{
    public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        Console.WriteLine("Starting user seeding...");
        // Skip if users already exist
        if (await userManager.Users.AnyAsync()) return;

        // Load user data
        var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

        if (users == null || users.Count == 0) return;

        // Define roles
        var roles = new List<AppRole>
        {
            new() { Name = "Member" },
            new() { Name = "Admin" },
            new() { Name = "Moderator" }
        };

        // Create roles if they don't exist
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name))
            {
                await roleManager.CreateAsync(role);
            }
        }

        // Create users from JSON
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

        // Create admin user if not exists
        if (await userManager.FindByNameAsync("admin") == null)
        {
            var admin = new AppUser
            {
                UserName = "admin",
                KnownAs = "Admin",
                Gender = "male",
                City = "Adminville",
                Country = "Nowhere"
            };

            var result = await userManager.CreateAsync(admin, "Pa$$w0rd");
            Console.WriteLine("Admin created: " + result.Succeeded);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Error creating admin: {error.Code} - {error.Description}");
                }
            }
            else
            {
                var roleResult = await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });
                Console.WriteLine("Admin roles added success: " + roleResult.Succeeded);
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        Console.WriteLine($"Error adding roles to admin: {error.Code} - {error.Description}");
                    }
                }
            }
        }
    }

    //how to delete user - and uncomment the line in Program.cs
    // public static async Task DeleteUser(UserManager<AppUser> userManager, string username)
    // {
    //     var user = await userManager.FindByNameAsync(username.ToLower());
    //     if (user == null)
    //     {
    //         Console.WriteLine($"User '{username}' not found.");
    //         return;
    //     }

    //     var result = await userManager.DeleteAsync(user);
    //     if (result.Succeeded)
    //     {
    //         Console.WriteLine($"User '{username}' deleted successfully.");
    //     }
    //     else
    //     {
    //         Console.WriteLine($"Failed to delete '{username}':");
    //         foreach (var error in result.Errors)
    //             Console.WriteLine($"- {error.Description}");
    //     }
    // }

}
