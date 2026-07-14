using BoostingHub.backend.Models;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Data.Seeders;

public static class PermissionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        var existingSlugs = new HashSet<string>();
        try
        {
            var slugs = await db.Permissions.Select(p => p.Slugs).Where(s => s != null).ToListAsync();
            existingSlugs = new HashSet<string>(slugs!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PermissionSeeder] Could not read permissions table: {ex.Message}. Skipping seed.");
            return;
        }

        var permissions = new List<Permission>
        {
            // Admin - Dashboard
            new() { Names = "View Dashboard", Slugs = "admin.dashboard.view" },
            new() { Names = "View In Progress Tasks", Slugs = "admin.tasks.in-progress.view" },
            new() { Names = "View Completed Tasks", Slugs = "admin.tasks.completed.view" },
            new() { Names = "View Client Requirements", Slugs = "admin.inquiries.view" },
            new() { Names = "View Settings", Slugs = "admin.settings.view" },

            // Admin - Roles & Permissions
            new() { Names = "View Roles", Slugs = "admin.roles.view" },
            new() { Names = "Create Roles", Slugs = "admin.roles.create" },
            new() { Names = "Edit Roles", Slugs = "admin.roles.edit" },
            new() { Names = "Delete Roles", Slugs = "admin.roles.delete" },

            // Orders
            new() { Names = "View Orders", Slugs = "admin.orders.view" },
            new() { Names = "Edit Orders", Slugs = "admin.orders.edit" },
            new() { Names = "Delete Orders", Slugs = "admin.orders.delete" },
            new() { Names = "Update Order Status", Slugs = "admin.orders.status.update" },

            // Tasks
            new() { Names = "View Tasks", Slugs = "admin.tasks.view" },
            new() { Names = "Create Tasks", Slugs = "admin.tasks.create" },
            new() { Names = "Edit Tasks", Slugs = "admin.tasks.edit" },
            new() { Names = "Delete Tasks", Slugs = "admin.tasks.delete" },

            // Users
            new() { Names = "View Users", Slugs = "admin.users.view" },
            new() { Names = "Edit Users", Slugs = "admin.users.edit" },
            new() { Names = "Delete Users", Slugs = "admin.users.delete" },

            // Notifications
            new() { Names = "View Notifications", Slugs = "admin.notifications.view" },
            new() { Names = "Send Notifications", Slugs = "admin.notifications.send" },

            // User-facing
            new() { Names = "View User Dashboard", Slugs = "user.dashboard.view" },
            new() { Names = "View Available Tasks", Slugs = "user.tasks.available.view" },
            new() { Names = "View My Tasks", Slugs = "user.tasks.my.view" },
            new() { Names = "Accept Tasks", Slugs = "user.tasks.accept" },
            new() { Names = "View Wallet", Slugs = "user.wallet.view" },
            new() { Names = "View Settings", Slugs = "user.settings.view" },
            new() { Names = "Edit Profile", Slugs = "user.profile.edit" },
        };

        foreach (var permission in permissions)
        {
            if (permission.Slugs != null && !existingSlugs.Contains(permission.Slugs))
            {
                permission.CreatedAt = DateTime.UtcNow;
                db.Permissions.Add(permission);
            }
        }

        await db.SaveChangesAsync();
    }
}
