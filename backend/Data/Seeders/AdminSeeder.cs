using BoostingHub.backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Data.Seeders;

public static class AdminSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        try
        {
            await PermissionSeeder.SeedAsync(db);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminSeeder] Permission seeding failed: {ex.Message}. Continuing...");
        }

        try
        {
            if (await db.Users.AnyAsync(u => u.Email == "admin@gmail.com")) return;
            var hasher = new PasswordHasher<User>();
            var admin = new User
            {
                Name = "Admin",
                Email = "admin@gmail.com",
                Password = hasher.HashPassword(null!, "admin123"),
                Status = 1,
                EmailVerifiedAt = DateTime.UtcNow
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminSeeder] Admin user seeding failed: {ex.Message}. Continuing...");
        }
    }
}
