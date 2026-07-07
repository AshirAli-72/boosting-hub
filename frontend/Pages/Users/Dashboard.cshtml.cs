using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Users;

public class DashboardModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public DashboardModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public UserDashboardDto Dashboard { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
        {
            Dashboard = await _dashboardService.GetUserDashboardAsync(userId);
        }
    }
}
