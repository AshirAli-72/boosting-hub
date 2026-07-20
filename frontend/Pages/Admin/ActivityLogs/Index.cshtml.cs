using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin.ActivityLogs;

public class IndexModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public IndexModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public PagedResult<ActivityLogDto> ActivityLogs { get; set; } = new();
    public ActivityLogFilterDto Filter { get; set; } = new();
    public ActivityLogStatsDto Stats { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(
        string? search, string? @event, string? role,
        string? dateFrom, string? dateTo, int page = 1)
    {
        var sessionRole = HttpContext.Session.GetString("UserRole");
        if (sessionRole != "Admin")
            return RedirectToPage("/Account/Login");

        Filter = new ActivityLogFilterDto
        {
            Page     = page,
            PageSize = 25,
            Search   = search,
            Event    = @event,
            Role     = role,
            DateFrom = DateTime.TryParse(dateFrom, out var df) ? df : null,
            DateTo   = DateTime.TryParse(dateTo, out var dt) ? dt : null,
        };

        try
        {
            Stats = await _dashboardService.GetActivityLogStatsAsync();
            ActivityLogs = await _dashboardService.GetActivityLogsAsync(Filter);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return Page();
    }
}
