using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public DashboardModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public AdminDashboardDto Dashboard { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin")
            return RedirectToPage("/Account/Login");

        try
        {
            Dashboard = await _dashboardService.GetAdminDashboardAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message + " | " + ex.StackTrace;
            Dashboard = new AdminDashboardDto();
        }

        return Page();
    }
}
