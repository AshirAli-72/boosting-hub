using BoostingHub.backend.DTOs;
using BoostingHub.backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BoostingHub.frontend.Pages.Users.Settings;

public class SecurityModel : PageModel
{
    private readonly IAuthenticationService _authService;

    public SecurityModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [BindProperty] public ChangePasswordDto ChangePassword { get; set; } = new();
    [BindProperty] public UpdateProfileDto UpdateProfile { get; set; } = new();
    public string CurrentUserName { get; set; } = "User";
    public string CurrentUserEmail { get; set; } = "";
    
    public string? SuccessMessage => TempData["Success"] as string;
    public string ActiveTab { get; set; } = "configuration";

    public async Task OnGetAsync(string tab = "configuration")
    {
        ActiveTab = tab;
        await LoadUserDataAsync();
    }

    private async Task LoadUserDataAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
        {
            var result = await _authService.GetCurrentUserAsync(userId);
            if (result.IsSuccess && result.Data != null)
            {
                CurrentUserName = result.Data.Name ?? "User";
                CurrentUserEmail = result.Data.Email ?? "";
                
                if (string.IsNullOrEmpty(UpdateProfile.Name))
                {
                    UpdateProfile.Name = result.Data.Name ?? "";
                    UpdateProfile.Email = result.Data.Email ?? "";
                    UpdateProfile.Phone = result.Data.Phone ?? "";
                }
            }
        }
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        ActiveTab = "configuration";
        var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        if (userId == 0) return RedirectToPage("/Account/Login");

        if (string.IsNullOrEmpty(UpdateProfile.Name))
        {
            ModelState.AddModelError("UpdateProfile.Name", "Name is required");
            await LoadUserDataAsync();
            return Page();
        }

        var result = await _authService.UpdateProfileAsync(userId, UpdateProfile, HttpContext);
        if (result.IsSuccess)
        {
            TempData["Success"] = result.Message;
            return RedirectToPage(new { tab = "configuration" });
        }
        else
        {
            ModelState.AddModelError("", result.Message ?? "Failed to update profile");
        }

        await LoadUserDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        ActiveTab = "security";
        if (!ModelState.IsValid)
        {
            await LoadUserDataAsync();
            return Page();
        }
        
        var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        if (userId == 0) return RedirectToPage("/Account/Login");

        var result = await _authService.ChangePasswordAsync(userId, ChangePassword);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Password changed successfully";
            return RedirectToPage(new { tab = "security" });
        }
        else
        {
            ModelState.AddModelError("", result.Message ?? "Failed to change password");
        }

        await LoadUserDataAsync();
        return Page();
    }
}
