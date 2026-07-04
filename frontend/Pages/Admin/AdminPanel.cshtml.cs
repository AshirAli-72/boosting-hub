using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin;

public class AdminPanelModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public AdminPanelModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public AdminDashboardDto Dashboard { get; set; } = new();

    public async Task OnGetAsync()
    {
        Dashboard = await _dashboardService.GetAdminDashboardAsync();
    }
}
