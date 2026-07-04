using BoostingHub.backend.Models;
using BoostingHub.backend.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Data.Seeders;

public static class AdminSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var superAdminRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleTitle == "Super Admin");
        if (superAdminRole == null)
        {
            superAdminRole = new Role { RoleTitle = "Super Admin", Description = "Full system access", CreatedAt = DateTime.UtcNow };
            db.Roles.Add(superAdminRole);
            await db.SaveChangesAsync();
        }

        if (await db.Users.AnyAsync(u => u.Email == "admin@gmail.com")) return;
        var hasher = new PasswordHasher<User>();
        var admin = new User
        {
            Name = "Super Admin",
            Email = "admin@gmail.com",
            Password = hasher.HashPassword(null!, "admin123"),
            Status = 1,
            EmailVerifiedAt = DateTime.UtcNow
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        db.UserHasRoles.Add(new UserHasRole { UserId = admin.Id, RoleId = superAdminRole.Id });
        await db.SaveChangesAsync();
    }
}
