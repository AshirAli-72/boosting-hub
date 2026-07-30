using BoostingHub.backend.Common;
using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Admin.Settings.Website;

public class IndexModel : PageModel
{
    private readonly IWebsiteSettingService _websiteSettingService;

    public IndexModel(IWebsiteSettingService websiteSettingService)
    {
        _websiteSettingService = websiteSettingService;
    }

    [BindProperty]
    public WebsiteSettingDto Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var result = await _websiteSettingService.GetAsync();
        if (result.IsSuccess && result.Data != null)
            Input = result.Data;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role != "Admin") return RedirectToPage("/Account/Login");

        var result = await _websiteSettingService.UpdateAsync(Input);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message;
            return RedirectToPage();
        }

        TempData["Error"] = result.Message;
        return Page();
    }
}
