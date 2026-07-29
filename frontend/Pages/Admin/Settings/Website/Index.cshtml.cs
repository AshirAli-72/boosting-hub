using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin.Settings.Website;

public class IndexModel : PageModel
{
    private readonly ISiteSettingService _siteSettingService;

    public IndexModel(ISiteSettingService siteSettingService)
    {
        _siteSettingService = siteSettingService;
    }

    [BindProperty]
    public SiteSettingDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var result = await _siteSettingService.GetAsync();
        if (result.IsSuccess && result.Data != null)
            Input = result.Data;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var result = await _siteSettingService.UpdateAsync(Input);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message;
            return RedirectToPage();
        }

        TempData["Error"] = result.Message;
        return Page();
    }
}
