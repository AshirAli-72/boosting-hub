using BoostingHub.backend.Data;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoostingHub.backend.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<UserDashboardDto> GetUserDashboardAsync(int userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        return new UserDashboardDto
        {
            UserName = user?.Name ?? "User",
            UserEmail = user?.Email ?? "",
            UserStatus = user?.Status == 1 ? "Active" : "Locked"
        };
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var users = await _db.Users.ToListAsync();
        var orders = await _db.Orders.ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new AdminDashboardDto
        {
            TotalUsers = users.Count,
            LockedAccounts = users.Count(u => u.Status == 0),
            UnverifiedEmails = users.Count(u => u.EmailVerifiedAt == null),
            RegisteredToday = users.Count(u => u.CreatedAt >= today),
            TotalOrders = orders.Count,
            TotalRevenue = orders.Where(o => o.Status == "Approved").Sum(o => o.Budget)
        };
    }
}
